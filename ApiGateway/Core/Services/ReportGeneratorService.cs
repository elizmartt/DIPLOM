using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Entities;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Services;
using ApiGateway.Resources;
using ApiGateway.Services;
using QuestPDF.Drawing;
using System.Security.Cryptography;
using System.Text.Json;

namespace ApiGateway.Core.Services;

public class ReportGeneratorService : IReportGeneratorService
{
    private readonly MedicalDiagnosticDbContext _db;
    private readonly ILogger<ReportGeneratorService> _logger;
    private readonly IEncryptionService _encryption;
    private readonly S3Service _s3;
    private readonly string _fontPath;

    private static readonly string DarkBlue  = Colors.Blue.Darken3;
    private static readonly string MidBlue   = Colors.Blue.Darken1;
    private static readonly string LightBlue = Colors.Blue.Lighten4;

    public ReportGeneratorService(
        MedicalDiagnosticDbContext db,
        IWebHostEnvironment env,
        IEncryptionService encryption,
        S3Service s3,
        ILogger<ReportGeneratorService> logger)
    {
        _db         = db;
        _logger     = logger;
        _encryption = encryption;
        _s3         = s3;
        _fontPath   = Path.Combine(env.ContentRootPath, "Resources", "Fonts", "arial.ttf");
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── Decryption helper (mirrors PatientController.SafeDecrypt) ─────────────
    private string SafeDecrypt(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
        try { return _encryption.Decrypt(value); }
        catch (FormatException)       { return value; }
        catch (CryptographicException){ return value; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Decrypt failed for report field, returning as-is");
            return value;
        }
    }

    public async Task<byte[]> GenerateCaseReportAsync(Guid caseId)
    {
        var diagCase = await _db.DiagnosisCases
            .FirstOrDefaultAsync(c => c.CaseId == caseId)
            ?? throw new KeyNotFoundException($"Case {caseId} not found");

        var patient  = await _db.Patients.FindAsync(diagCase.PatientId);
        var doctor   = await _db.Doctors.FindAsync(diagCase.DoctorId);
        var unified  = await _db.UnifiedDiagnosisResults
            .FirstOrDefaultAsync(r => r.DiagnosisCaseId == caseId);

        var imaging  = await _db.ImagingResults
            .Where(r => r.DiagnosisCaseId == caseId)
            .Select(r => new ImagingResult
            {
                Id                 = r.Id,
                DiagnosisCaseId    = r.DiagnosisCaseId,
                Prediction         = r.Prediction,
                Confidence         = r.Confidence,
                Probabilities      = r.Probabilities,
                ProcessingTimeMs   = r.ProcessingTimeMs,
                ExplainabilityData = r.ExplainabilityData,
                Success            = r.Success,
                ErrorMessage       = r.ErrorMessage,
                CreatedAt          = r.CreatedAt,
            })
            .FirstOrDefaultAsync();

        var lab      = await _db.LaboratoryResults
            .FirstOrDefaultAsync(r => r.DiagnosisCaseId == caseId);
        var clinical = await _db.ClinicalResults
            .FirstOrDefaultAsync(r => r.DiagnosisCaseId == caseId);

        // ── Decrypt patient names ────────────────────────────────────────────
        if (patient != null)
        {
            patient.FirstName = SafeDecrypt(patient.FirstName);
            patient.LastName  = SafeDecrypt(patient.LastName);
        }

        var caseImages = await LoadCaseImagesFromS3Async(imaging, caseId);

        if (!File.Exists(_fontPath))
            _logger.LogWarning("Arial font not found at {Path} — PDF may fall back to default font", _fontPath);
        else
            FontManager.RegisterFont(File.OpenRead(_fontPath));

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c =>
                    ComposeCoverPage(c, diagCase, patient, doctor, unified, caseImages));
                page.Footer().Element(ComposeFooter);
            });

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c =>
                    ComposeDetailPage(c, diagCase, patient, doctor, unified, imaging, lab, clinical, caseId));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }


    private async Task<CaseImages> LoadCaseImagesFromS3Async(ImagingResult? imaging, Guid caseId)
    {
        var result = new CaseImages();

        // ── Step 1: Get imageId from medical_images table ────────────────────
        // Python saves Grad-CAM files as:
        //   gradcam/{caseId}/heatmap_{imageId}.png
        //   gradcam/{caseId}/overlay_{imageId}.png

        var medImg = await _db.MedicalImages
            .Where(m => m.CaseId == caseId && m.FilePath != null && !m.FilePath.Contains("gradcam"))
            .OrderByDescending(m => m.UploadedAt)
            .FirstOrDefaultAsync();
        string? originalKey = null;
        string? overlayKey  = null;

        // ── Step 2: Build Grad-CAM keys from caseId + imageId (primary method) ─
        // S3 structure: gradcam/{caseId}/overlay_{imageId}.png
        if (medImg != null)
        {
            var filename = Path.GetFileNameWithoutExtension(medImg.FilePath ?? "");
            var imageId = filename.Split('/').Last(); // handles any path format
            overlayKey = $"gradcam/{caseId}/overlay_{imageId}.png";
            _logger.LogInformation("Grad-CAM key from file_path: {Key}", overlayKey);
        }
        // ── Step 3: Fallback — parse keys from explainability_data JSON ──────
        // (covers older cases where S3 keys were stored explicitly in the JSON)
        if (overlayKey == null && imaging != null &&
            !string.IsNullOrWhiteSpace(imaging.ExplainabilityData))
        {
            try
            {
                using var doc = JsonDocument.Parse(imaging.ExplainabilityData);
                var root = doc.RootElement;

                _logger.LogInformation("explainability_data keys: {Keys}",
                    string.Join(", ", root.EnumerateObject().Select(p => p.Name)));

                foreach (var k in new[] { "gradcam_overlay_s3_key", "overlay_s3_key",
                                          "gradcam_s3_key", "grad_cam_s3_key" })
                    if (root.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
                    { overlayKey = v.GetString(); break; }

                // Nested under "saved_files" (legacy format)
                if (overlayKey == null &&
                    root.TryGetProperty("saved_files", out var sf) &&
                    sf.ValueKind == JsonValueKind.Object)
                {
                    foreach (var k in new[] { "gradcam_overlay_s3_key", "overlay_s3_key" })
                        if (sf.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
                        { overlayKey = v.GetString(); break; }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse explainability_data JSON");
            }
        }

        // ── Step 4: Original image key from medical_images.file_path ─────────
        if (medImg != null && !string.IsNullOrEmpty(medImg.FilePath))
            originalKey = medImg.FilePath;

        _logger.LogInformation("S3 keys resolved — original: [{O}]  overlay: [{V}]",
            originalKey ?? "null", overlayKey ?? "null");

        // ── Step 5: Download ──────────────────────────────────────────────────
        if (!string.IsNullOrEmpty(originalKey))
        {
            try
            {
                result.OriginalBytes = await _s3.DownloadImageAsync(originalKey);
                _logger.LogInformation("✓ Original scan downloaded ({Bytes} bytes)", result.OriginalBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "✗ Failed to download original scan: {K}", originalKey);
            }
        }
        else
        {
            _logger.LogWarning("✗ No original image S3 key found");
        }

        if (!string.IsNullOrEmpty(overlayKey))
        {
            try
            {
                _logger.LogInformation("Attempting to download overlay from S3 key: [{Key}]", overlayKey);
                result.OverlayBytes = await _s3.DownloadImageAsync(overlayKey);
                _logger.LogInformation("✓ Grad-CAM overlay downloaded ({Bytes} bytes)", result.OverlayBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "✗ Failed to download Grad-CAM: {K}", overlayKey);
            }
        }
        else
        {
            _logger.LogWarning("✗ No Grad-CAM overlay S3 key found");
        }

        return result;
    }


    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(ReportLabels.SystemTitle)
                        .FontSize(15).Bold().FontColor(DarkBlue);
                    c.Item().Text(ReportLabels.ReportTitle)
                        .FontSize(10).FontColor(Colors.Grey.Darken2);
                });
                row.ConstantItem(175).AlignRight().Column(c =>
                {
                    c.Item().AlignRight()
                        .Text($"{ReportLabels.Generated}: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                    c.Item().PaddingTop(3).AlignRight().Text(ReportLabels.Confidential)
                        .FontSize(8).Bold().FontColor(Colors.Red.Darken2);
                });
            });
            col.Item().PaddingTop(5).BorderBottom(2).BorderColor(DarkBlue);
        });
    }


    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(4);
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(ReportLabels.SystemTitle)
                    .FontSize(7).FontColor(Colors.Grey.Medium);
                row.ConstantItem(110).AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Colors.Grey.Medium))
                    .Text(text =>
                    {
                        text.Span($"{ReportLabels.Page} ");
                        text.CurrentPageNumber();
                        text.Span($" {ReportLabels.Of} ");
                        text.TotalPages();
                    });
                row.ConstantItem(80).AlignRight().Text(ReportLabels.Confidential)
                    .FontSize(7).FontColor(Colors.Grey.Medium);
            });
        });
    }


    private void ComposeCoverPage(IContainer container, DiagnosisCase diagCase,
        Patient? patient, Doctor? doctor, UnifiedDiagnosisResult? unified,
        CaseImages caseImages)
    {
        container.Column(col =>
        {
            col.Spacing(10);

            // ── Patient banner ───────────────────────────────────────────────
            col.Item().Background(DarkBlue).Padding(12).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    var fullName = $"{patient?.FirstName} {patient?.LastName}".Trim();
                    c.Item().Text(string.IsNullOrEmpty(fullName) ? "—" : fullName)
                        .FontSize(20).Bold().FontColor(Colors.White);
                    c.Item().PaddingTop(3)
                        .Text($"{ReportLabels.PatientCode}: {patient?.PatientCode ?? "—"}" +
                              $"   {patient?.Age} {ReportLabels.Years}" +
                              $"   {ReportLabels.TranslateGender(patient?.Gender)}")
                        .FontSize(9).FontColor(Colors.Blue.Lighten3);
                });
                row.ConstantItem(185).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(diagCase.CreatedAt.ToString("dd.MM.yyyy"))
                        .FontSize(16).Bold().FontColor(Colors.White);
                    c.Item().PaddingTop(3).AlignRight().Text(doctor?.FullName ?? "—")
                        .FontSize(9).FontColor(Colors.Blue.Lighten3);
                    c.Item().PaddingTop(2).AlignRight()
                        .Text(ReportLabels.TranslateDiagnosisType(diagCase.DiagnosisType))
                        .FontSize(9).FontColor(Colors.Blue.Lighten3);
                });
            });

            // ── Medical images (original scan + Grad-CAM) ────────────────────
            bool hasOriginal = caseImages.OriginalBytes is { Length: > 0 };
            bool hasOverlay  = caseImages.OverlayBytes  is { Length: > 0 };

            if (hasOriginal || hasOverlay)
            {
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2)
                    .Background(Colors.Grey.Lighten5).Padding(8).Column(imgCol =>
                {
                    imgCol.Item().AlignCenter().Text(ReportLabels.GradCamTitle)
                        .FontSize(10).Bold().FontColor(DarkBlue);

                    imgCol.Item().PaddingTop(6).Row(row =>
                    {
                        if (hasOriginal)
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text(ReportLabels.OriginalScanLabel)
                                    .FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                c.Item().PaddingTop(4).AlignCenter()
                                    .Width(210).Image(caseImages.OriginalBytes!);
                            });

                        if (hasOriginal && hasOverlay)
                            row.ConstantItem(10);

                        if (hasOverlay)
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text(ReportLabels.GradCamOverlayLabel)
                                    .FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                c.Item().PaddingTop(4).AlignCenter()
                                    .Width(210).Image(caseImages.OverlayBytes!);
                            });
                    });

                    imgCol.Item().PaddingTop(6).AlignCenter()
                        .Text(ReportLabels.GradCamDescription)
                        .FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken1);
                });
            }
            else
            {
                col.Item().Background(Colors.Grey.Lighten4).Border(0.5f)
                    .BorderColor(Colors.Grey.Lighten3).Padding(18).AlignCenter()
                    .Text(ReportLabels.GradCamNotAvailable)
                    .FontSize(9).Italic().FontColor(Colors.Grey.Darken2);
            }

            // ── Unified diagnosis summary card ───────────────────────────────
            if (unified != null)
            {
                col.Item().Border(2).BorderColor(DarkBlue).Column(card =>
                {
                    card.Item().Background(DarkBlue).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text(ReportLabels.DiagnosisSummary)
                            .Bold().FontSize(11).FontColor(Colors.White);
                        row.ConstantItem(90).AlignRight()
                            .Background(RiskBadgeColor(unified.RiskLevel))
                            .Padding(4).AlignCenter()
                            .Text(TranslateRisk(unified.RiskLevel))
                            .Bold().FontSize(9).FontColor(Colors.White);
                    });

                    card.Item().Padding(10).Column(body =>
                    {
                        body.Item().Text(ReportLabels.TranslatePrediction(unified.FinalDiagnosis))
                            .FontSize(16).Bold().FontColor(DarkBlue);

                        body.Item().PaddingTop(6).Row(confRow =>
                        {
                            confRow.ConstantItem(140)
                                .Text($"{ReportLabels.OverallConfidence}:")
                                .FontSize(9).Bold();
                            var filled = Math.Max(0.001f, (float)unified.OverallConfidence);
                            var empty  = Math.Max(0.001f, 1f - filled);
                            confRow.RelativeItem().PaddingVertical(3).Row(barRow =>
                            {
                                barRow.RelativeItem(filled).Background(MidBlue).Height(12);
                                barRow.RelativeItem(empty).Background(Colors.Grey.Lighten3).Height(12);
                            });
                            confRow.ConstantItem(45).AlignRight().Padding(2)
                                .Text($"{unified.OverallConfidence:P1}")
                                .Bold().FontSize(9).FontColor(DarkBlue);
                        });

                        if (unified.Recommendations?.Length > 0)
                        {
                            body.Item().PaddingTop(8).Text(ReportLabels.Recommendations)
                                .Bold().FontSize(9);
                            foreach (var rec in unified.Recommendations)
                                body.Item().PaddingTop(2)
                                    .Text($"• {ReportLabels.TranslateRecommendation(rec)}")
                                    .FontSize(9);
                        }
                    });
                });
            }

            // ── AI explainability / agreement block ─────────────────────────
            if (unified != null)
            {
                var (agreementScore, isReliable, explanation) =
                    ParseExplainabilitySummary(unified.ExplainabilitySummary);

                col.Item().Background(LightBlue).Border(0.5f).BorderColor(Colors.Blue.Lighten2)
                    .Padding(10).Column(aiCol =>
                {
                    aiCol.Item().Text(ReportLabels.AIInterpretation)
                        .Bold().FontSize(10).FontColor(DarkBlue);

                    aiCol.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"{ReportLabels.AgreementScore}: {agreementScore:P1}")
                                .FontSize(9).Bold();
                            var agFilled = Math.Max(0.001f, (float)agreementScore);
                            var agEmpty  = Math.Max(0.001f, 1f - (float)agreementScore);
                            c.Item().PaddingTop(3).Row(barRow =>
                            {
                                barRow.RelativeItem(agFilled).Background(Colors.Blue.Medium).Height(9);
                                barRow.RelativeItem(agEmpty).Background(Colors.Grey.Lighten3).Height(9);
                            });
                        });

                        row.ConstantItem(16);

                        row.ConstantItem(100).AlignRight()
                            .Background(isReliable ? Colors.Green.Lighten3 : Colors.Orange.Lighten3)
                            .Padding(5).AlignCenter()
                            .Text(isReliable ? ReportLabels.Reliable : ReportLabels.NotReliable)
                            .Bold().FontSize(9)
                            .FontColor(isReliable ? Colors.Green.Darken3 : Colors.Orange.Darken3);
                    });

                    if (!string.IsNullOrWhiteSpace(explanation))
                        aiCol.Item().PaddingTop(6).Text(explanation)
                            .FontSize(9).FontColor(Colors.Grey.Darken2);
                });
            }
        });
    }


    private void ComposeDetailPage(IContainer container, DiagnosisCase diagCase,
        Patient? patient, Doctor? doctor, UnifiedDiagnosisResult? unified,
        ImagingResult? imaging, LaboratoryResult? lab, ClinicalResult? clinical,
        Guid caseId)
    {
        container.Column(col =>
        {
            col.Spacing(12);

            col.Item().Element(e => SectionHeader(e, ReportLabels.PatientInfo));
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                var fullName = $"{patient?.FirstName} {patient?.LastName}".Trim();
                AddRow(table, ReportLabels.PatientName,
                    string.IsNullOrEmpty(fullName) ? "—" : fullName);
                AddRow(table, ReportLabels.PatientCode,    patient?.PatientCode ?? "—");
                AddRow(table, ReportLabels.Age,            $"{patient?.Age} {ReportLabels.Years}");
                AddRow(table, ReportLabels.Gender,         ReportLabels.TranslateGender(patient?.Gender));
                AddRow(table, ReportLabels.CaseId,         caseId.ToString()[..8] + "...");
                AddRow(table, ReportLabels.DiagnosisType,
                    ReportLabels.TranslateDiagnosisType(diagCase.DiagnosisType));
                AddRow(table, ReportLabels.CaseDate,       diagCase.CreatedAt.ToString("dd.MM.yyyy"));
                AddRow(table, ReportLabels.AttendingDoctor, doctor?.FullName ?? "—");
            });

            col.Item().Element(e => SectionHeader(e, ReportLabels.ModuleScores));
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(200);
                    c.RelativeColumn();
                    c.ConstantColumn(55);
                });
                table.Header(h =>
                {
                    h.Cell().Padding(4).Text(ReportLabels.Module).Bold().FontSize(9);
                    h.Cell().Padding(4).Text(ReportLabels.Confidence).Bold().FontSize(9);
                    h.Cell().AlignRight().Padding(4).Text(ReportLabels.Score).Bold().FontSize(9);
                });
                AddModuleRow(table, ReportLabels.ImagingModule,  imaging?.Confidence  ?? 0, Colors.Blue.Medium);
                AddModuleRow(table, ReportLabels.LabModule,      lab?.Confidence      ?? 0, Colors.Green.Medium);
                AddModuleRow(table, ReportLabels.ClinicalModule, clinical?.Confidence ?? 0, Colors.Orange.Medium);
                AddModuleRow(table, ReportLabels.EnsembleModule, unified?.OverallConfidence ?? 0, Colors.Purple.Medium);
            });

            if (imaging != null)
            {
                col.Item().Element(e => SectionHeader(e, ReportLabels.ImagingResults));
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                    AddRow(table, ReportLabels.Prediction, ReportLabels.TranslatePrediction(imaging.Prediction));
                    AddRow(table, ReportLabels.Confidence, $"{imaging.Confidence:P1}");
                    AddRow(table, ReportLabels.Status,
                        imaging.Success ? ReportLabels.Success : ReportLabels.Failed);
                });
            }

            if (lab != null)
            {
                col.Item().Element(e => SectionHeader(e, ReportLabels.LabResults));
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                    AddRow(table, ReportLabels.Prediction, ReportLabels.TranslatePrediction(lab.Prediction));
                    AddRow(table, ReportLabels.Confidence, $"{lab.Confidence:P1}");
                    AddRow(table, ReportLabels.Status,
                        lab.Success ? ReportLabels.Success : ReportLabels.Failed);
                });
            }

            if (clinical != null)
            {
                col.Item().Element(e => SectionHeader(e, ReportLabels.ClinicalResults));
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                    AddRow(table, ReportLabels.Prediction, ReportLabels.TranslatePrediction(clinical.Prediction));
                    AddRow(table, ReportLabels.Confidence, $"{clinical.Confidence:P1}");
                    AddRow(table, ReportLabels.Status,
                        clinical.Success ? ReportLabels.Success : ReportLabels.Failed);
                });
            }

            col.Item().Element(e => SectionHeader(e, ReportLabels.DoctorNotes));
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
            {
                var notes = diagCase.DoctorNotes ?? diagCase.DoctorDiagnosis;
                c.Item().Text(string.IsNullOrWhiteSpace(notes) ? ReportLabels.NoNotes : notes)
                    .FontSize(10)
                    .FontColor(string.IsNullOrWhiteSpace(notes) ? Colors.Grey.Medium : Colors.Black);
                c.Item().PaddingTop(24).Text(ReportLabels.DoctorSignature)
                    .FontColor(Colors.Grey.Darken1);
                c.Item().PaddingTop(4)
                    .Text($"{ReportLabels.Date}: {ReportLabels.FormatDateArmenian(DateTime.Now)}")
                    .FontColor(Colors.Grey.Darken1);
            });

            col.Item().Background(Colors.Grey.Lighten4).Border(0.5f)
                .BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
            {
                c.Item().Row(row =>
                {
                    row.RelativeItem().Text(ReportLabels.SystemSignature)
                        .Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                    row.ConstantItem(150).AlignRight()
                        .Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"))
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });
                c.Item().PaddingTop(4)
                    .Text($"{ReportLabels.ReportId}: {caseId:N}")
                    .FontSize(7.5f).FontColor(Colors.Grey.Medium);
            });

            col.Item().Background(Colors.Grey.Lighten3).Padding(8)
                .Text(ReportLabels.Disclaimer)
                .FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken2);
        });
    }


    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SectionHeader(IContainer container, string title)
        => container.Background(LightBlue).Padding(6).Text(title).Bold().FontSize(11);

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Padding(4).Text(label).Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
        table.Cell().Padding(4).Text(value).FontSize(10);
    }

    private static void AddModuleRow(TableDescriptor table, string name, double confidence, string color)
    {
        var filled = Math.Max(0.001f, (float)confidence);
        var empty  = Math.Max(0.001f, (float)(1 - confidence));
        table.Cell().Padding(4).Text(name).FontSize(9);
        table.Cell().PaddingVertical(6).Element(bar =>
            bar.Row(r =>
            {
                r.RelativeItem(filled).Background(color).Height(10);
                r.RelativeItem(empty).Background(Colors.Grey.Lighten3).Height(10);
            }));
        table.Cell().AlignRight().Padding(4).Text($"{confidence:P1}").Bold().FontSize(9);
    }

    private static string TranslateRisk(string? risk) => risk?.ToLower() switch
    {
        "high"   => ReportLabels.RiskHigh,
        "medium" => ReportLabels.RiskMedium,
        "low"    => ReportLabels.RiskLow,
        _        => risk ?? "—"
    };

    private static string RiskBadgeColor(string? risk) => risk?.ToLower() switch
    {
        "high"   => Colors.Red.Darken2,
        "medium" => Colors.Orange.Darken1,
        "low"    => Colors.Green.Darken2,
        _        => Colors.Grey.Darken2
    };

    private static (double agreementScore, bool isReliable, string explanation)
        ParseExplainabilitySummary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return (0.0, false, string.Empty);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root     = doc.RootElement;
            var score    = root.TryGetProperty("agreement_score", out var s) ? s.GetDouble() : 0.0;
            var reliable = root.TryGetProperty("is_reliable",     out var r) && r.GetBoolean();

            var sb = new System.Text.StringBuilder();

            if (root.TryGetProperty("final_diagnosis", out var diag) &&
                root.TryGetProperty("overall_confidence", out var conf))
            {
                var diagArm = ReportLabels.TranslatePrediction(diag.GetString());
                sb.AppendLine($"Ախտ.: {diagArm}   Վ.: {conf.GetDouble():P1}");
            }

            if (root.TryGetProperty("models_available", out var modAvail))
                sb.AppendLine($"Հասանելի մոդ.: {modAvail.GetInt32()}/3");

            if (root.TryGetProperty("models_agreeing", out var modAgree))
                sb.AppendLine($"Համաձայն մոդ.: {modAgree.GetInt32()}/3");

            if (root.TryGetProperty("contributing_models", out var models) &&
                models.ValueKind == JsonValueKind.Object)
            {
                sb.AppendLine("Մոդուլներ:");
                foreach (var m in models.EnumerateObject())
                {
                    var label = m.Name switch
                    {
                        "imaging"  => "Պատկ.",
                        "labs"     => "Լաբ.",
                        "symptoms" => "Ախտ.",
                        _          => m.Name
                    };
                    var pred   = m.Value.TryGetProperty("prediction", out var p) ? ReportLabels.TranslatePrediction(p.GetString()) : "—";
                    var mConf  = m.Value.TryGetProperty("confidence",  out var c) ? $"{c.GetDouble():P1}" : "—";
                    var weight = m.Value.TryGetProperty("weight",      out var w) ? $"{w.GetDouble():P1}" : "—";
                    sb.AppendLine($"  • {label}: {pred} (Վ: {mConf}, Կ: {weight})");
                }
            }

            return (score, reliable, sb.ToString().TrimEnd());
        }
        catch
        {
            return (0.0, false, string.Empty);
        }
    }

    private sealed class CaseImages
    {
        public byte[]? OriginalBytes { get; set; }
        public byte[]? OverlayBytes  { get; set; }
    }
}