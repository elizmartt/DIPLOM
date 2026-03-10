using System.Text.Json;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Entities;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Implementations;

public class DiagnosisCaseRepository : IDiagnosisCaseRepository
{
    private readonly MedicalDiagnosticDbContext _context;

    public DiagnosisCaseRepository(MedicalDiagnosticDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DiagnosisCase>> GetByDoctorIdAndStatusAsync(Guid doctorId, string status,
        int limit = 50)
    {
        return await _context.DiagnosisCases
            .Where(c => c.DoctorId == doctorId && c.Status == status)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task DeleteAsync(Guid caseId)
    {
        var diagnosisCase = await GetByIdAsync(caseId);
        if (diagnosisCase == null)
            throw new KeyNotFoundException($"Case {caseId} not found");

        _context.DiagnosisCases.Remove(diagnosisCase);
        await _context.SaveChangesAsync();
    }

    public async Task<DiagnosisCase> CreateAsync(DiagnosisCase diagnosisCase)
    {
        _context.DiagnosisCases.Add(diagnosisCase);
        await _context.SaveChangesAsync();
        return diagnosisCase;
    }

    public async Task<DiagnosisCase?> GetByIdAsync(Guid caseId)
    {
        return await _context.DiagnosisCases
            .FirstOrDefaultAsync(c => c.CaseId == caseId);
    }

    public async Task<DiagnosisCase> UpdateAsync(DiagnosisCase diagnosisCase)
    {
        _context.DiagnosisCases.Update(diagnosisCase);
        await _context.SaveChangesAsync();
        return diagnosisCase;
    }

    public async Task<DiagnosisCase> UpdateStatusAsync(Guid caseId, string status)
    {
        var diagnosisCase = await GetByIdAsync(caseId);
        if (diagnosisCase == null)
            throw new InvalidOperationException($"DiagnosisCase {caseId} not found");

        diagnosisCase.Status = status;
        await _context.SaveChangesAsync();
        return diagnosisCase;
    }

    public async Task<IEnumerable<DiagnosisCase>> GetByDoctorIdAsync(Guid doctorId, int limit = 50)
    {
        return await _context.DiagnosisCases
            .Where(c => c.DoctorId == doctorId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<DiagnosisCase>> GetByPatientIdAsync(Guid patientId, int limit = 50)
    {
        return await _context.DiagnosisCases
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<DiagnosisCase>> GetRecentCasesAsync(int limit = 20)
    {
        return await _context.DiagnosisCases
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<DiagnosisCase>> GetByStatusAsync(string status, int limit = 50)
    {
        return await _context.DiagnosisCases
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}

public class PatientRepository : IPatientRepository
{
    private readonly MedicalDiagnosticDbContext _context;

    public PatientRepository(MedicalDiagnosticDbContext context)
    {
        _context = context;
    }

    public async Task<Patient> CreateAsync(Patient patient)
    {
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
        return patient;
    }

    public async Task<Patient?> GetByIdAsync(Guid patientId)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.PatientId == patientId);
    }

    public async Task<Patient?> GetByPatientCodeAsync(string patientCode)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.PatientCode == patientCode);
    }

    public async Task<Patient> UpdateAsync(Patient patient)
    {
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync();
        return patient;
    }

    public async Task<IEnumerable<Patient>> GetAllAsync(int limit = 100)
    {
        return await _context.Patients
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}

public class DoctorRepository : IDoctorRepository
{
    private readonly MedicalDiagnosticDbContext _context;

    public DoctorRepository(MedicalDiagnosticDbContext context)
    {
        _context = context;
    }

    public async Task<Doctor> CreateAsync(Doctor doctor)
    {
        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();
        return doctor;
    }

    public async Task<Doctor?> GetByIdAsync(Guid doctorId)
    {
        return await _context.Doctors
            .FirstOrDefaultAsync(d => d.DoctorId == doctorId);
    }

    public async Task<Doctor?> GetByEmailAsync(string email)
    {
        return await _context.Doctors
            .FirstOrDefaultAsync(d => d.Email == email);
    }

    public async Task<Doctor> UpdateAsync(Doctor doctor)
    {
        _context.Doctors.Update(doctor);
        await _context.SaveChangesAsync();
        return doctor;
    }

    public async Task<IEnumerable<Doctor>> GetActiveAsync(int limit = 100)
    {
        return await _context.Doctors
            .Where(d => d.IsActive)
            .OrderBy(d => d.FullName)
            .Take(limit)
            .ToListAsync();
    }
    public async Task<IEnumerable<Doctor>> GetAllAsync(int limit = 100)
    {
        return await _context.Doctors
            .OrderBy(d => d.FullName)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> DeactivateAsync(Guid doctorId)
    {
        var doctor = await GetByIdAsync(doctorId);
        if (doctor == null)
            return false;

        doctor.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }
}

public class MedicalImageRepository : IMedicalImageRepository
{
    private readonly MedicalDiagnosticDbContext _context;

    public MedicalImageRepository(MedicalDiagnosticDbContext context)
    {
        _context = context;
    }

    public async Task<MedicalImage> CreateAsync(MedicalImage image)
    {
        _context.MedicalImages.Add(image);
        await _context.SaveChangesAsync();
        return image;
    }

    public async Task<MedicalImage?> GetByIdAsync(Guid imageId)
    {
        return await _context.MedicalImages
            .FirstOrDefaultAsync(i => i.ImageId == imageId);
    }

    public async Task<IEnumerable<MedicalImage>> GetByCaseIdAsync(Guid caseId)
    {
        return await _context.MedicalImages
            .Where(i => i.CaseId == caseId)
            .OrderBy(i => i.UploadedAt)
            .ToListAsync();
    }

    public async Task<MedicalImage> UpdateAsync(MedicalImage image)
    {
        _context.MedicalImages.Update(image);
        await _context.SaveChangesAsync();
        return image;
    }

    public async Task<bool> MarkAsPreprocessedAsync(Guid imageId, List<string> preprocessingSteps)
    {
        var image = await GetByIdAsync(imageId);
        if (image == null)
            return false;

        image.IsPreprocessed = true;
        //image.PreprocessingSteps = preprocessingSteps != null 
        //    ? JsonSerializer.Serialize(preprocessingSteps) 
        //  : "[]";            await _context.SaveChangesAsync();
        return true;
    }
}

public class ClinicalSymptomRepository : IClinicalSymptomRepository
{
    private readonly MedicalDiagnosticDbContext _context;

    public ClinicalSymptomRepository(MedicalDiagnosticDbContext context)
    {
        _context = context;
    }

    public async Task<ClinicalSymptom> CreateAsync(ClinicalSymptom symptom)
    {
        _context.ClinicalSymptoms.Add(symptom);
        await _context.SaveChangesAsync();
        return symptom;
    }

    public async Task<ClinicalSymptom?> GetByIdAsync(Guid symptomId)
    {
        return await _context.ClinicalSymptoms
            .FirstOrDefaultAsync(s => s.SymptomId == symptomId);
    }

    public async Task<ClinicalSymptom?> GetByCaseIdAsync(Guid caseId)
    {
        return await _context.ClinicalSymptoms
            .FirstOrDefaultAsync(s => s.CaseId == caseId);
    }

    public async Task<ClinicalSymptom> UpdateAsync(ClinicalSymptom symptom)
    {
        _context.ClinicalSymptoms.Update(symptom);
        await _context.SaveChangesAsync();
        return symptom;
    }
}

public class LabTestRepository : ILabTestRepository
{
    private readonly MedicalDiagnosticDbContext _context;

    public LabTestRepository(MedicalDiagnosticDbContext context)
    {
        _context = context;
    }

    public async Task<LabTest> CreateAsync(LabTest labTest)
    {
        _context.LabTests.Add(labTest);
        await _context.SaveChangesAsync();
        return labTest;
    }

    public async Task<LabTest?> GetByIdAsync(Guid labId)
    {
        return await _context.LabTests
            .FirstOrDefaultAsync(l => l.LabId == labId);
    }

    public async Task<IEnumerable<LabTest>> GetByCaseIdAsync(Guid caseId)
    {
        return await _context.LabTests
            .Where(l => l.CaseId == caseId)
            .OrderByDescending(l => l.TestDate)
            .ToListAsync();
    }

    public async Task<LabTest> UpdateAsync(LabTest labTest)
    {
        _context.LabTests.Update(labTest);
        await _context.SaveChangesAsync();
        return labTest;
    }
}

public class ImagingResultRepository : IImagingResultRepository
{
    private readonly MedicalDiagnosticDbContext _context;

    public ImagingResultRepository(MedicalDiagnosticDbContext context)
    {
        _context = context;
    }

    public async Task<ImagingResult> CreateAsync(ImagingResult result)
    {
        _context.ImagingResults.Add(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<ImagingResult?> GetByIdAsync(int id) // int — was Guid
    {
        return await _context.ImagingResults
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ImagingResult?> GetByCaseIdAsync(Guid caseId)
    {
        return await _context.ImagingResults
            .FirstOrDefaultAsync(r => r.DiagnosisCaseId == caseId);
    }

    public async Task<IEnumerable<ImagingResult>> GetRecentAsync(int limit = 50)
    {
        return await _context.ImagingResults
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}

public class ClinicalResultRepository : IClinicalResultRepository
{
    private readonly MedicalDiagnosticDbContext _context;

    public ClinicalResultRepository(MedicalDiagnosticDbContext context)
    {
        _context = context;
    }

    public async Task<ClinicalResult> CreateAsync(ClinicalResult result)
    {
        _context.ClinicalResults.Add(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<ClinicalResult?> GetByIdAsync(int id)
    {
        return await _context.ClinicalResults
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ClinicalResult?> GetByCaseIdAsync(Guid caseId)
    {
        return await _context.ClinicalResults
            .FirstOrDefaultAsync(r => r.DiagnosisCaseId == caseId);
    }

    public async Task<IEnumerable<ClinicalResult>> GetRecentAsync(int limit = 50)
    {
        return await _context.ClinicalResults
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}

public class LaboratoryResultRepository : ILaboratoryResultRepository
{
    private readonly MedicalDiagnosticDbContext _context;

    public LaboratoryResultRepository(MedicalDiagnosticDbContext context)
    {
        _context = context;
    }

    public async Task<LaboratoryResult> CreateAsync(LaboratoryResult result)
    {
        _context.LaboratoryResults.Add(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<LaboratoryResult?> GetByIdAsync(int id)
    {
        return await _context.LaboratoryResults
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<LaboratoryResult?> GetByCaseIdAsync(Guid caseId)
    {
        return await _context.LaboratoryResults
            .FirstOrDefaultAsync(r => r.DiagnosisCaseId == caseId);
    }

    public async Task<IEnumerable<LaboratoryResult>> GetRecentAsync(int limit = 50)
    {
        return await _context.LaboratoryResults
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}

public class UnifiedDiagnosisResultRepository : IUnifiedDiagnosisResultRepository
{
    private readonly MedicalDiagnosticDbContext _context;

    public UnifiedDiagnosisResultRepository(MedicalDiagnosticDbContext context)
    {
        _context = context;
    }

    public async Task<UnifiedDiagnosisResult> CreateAsync(UnifiedDiagnosisResult result)
    {
        _context.UnifiedDiagnosisResults.Add(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<UnifiedDiagnosisResult?> GetByIdAsync(int id) // int — was Guid
    {
        return await _context.UnifiedDiagnosisResults
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<UnifiedDiagnosisResult?> GetByCaseIdAsync(Guid caseId)
    {
        return await _context.UnifiedDiagnosisResults
            .FirstOrDefaultAsync(r => r.DiagnosisCaseId == caseId);
    }

    public async Task<UnifiedDiagnosisResult> UpdateAsync(UnifiedDiagnosisResult result)
    {
        _context.UnifiedDiagnosisResults.Update(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<IEnumerable<UnifiedDiagnosisResult>> GetRecentAsync(int limit = 50)
    {
        return await _context.UnifiedDiagnosisResults
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<UnifiedDiagnosisResult>> GetByDiagnosisAsync(string diagnosis, int limit = 50)
    {
        return await _context.UnifiedDiagnosisResults
            .Where(r => r.FinalDiagnosis == diagnosis)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<UnifiedDiagnosisResult>> GetByRiskLevelAsync(string riskLevel, int limit = 50)
    {
        return await _context.UnifiedDiagnosisResults
            .Where(r => r.RiskLevel == riskLevel)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly MedicalDiagnosticDbContext _context;

    public AuditLogRepository(MedicalDiagnosticDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog> CreateAsync(AuditLog log)
    {
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task LogActionAsync(
        string action,
        Guid? doctorId = null,
        Guid? caseId = null,
        string? entityType = null,
        Guid? entityId = null,
        Dictionary<string, object>? actionDetails = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var log = new AuditLog
            {
                Action = action,
                DoctorId = doctorId,
                CaseId = caseId,
                EntityType = entityType,
                EntityId = entityId,
                ActionDetails = actionDetails == null
                    ? null
                    : JsonSerializer.Serialize(actionDetails),
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await CreateAsync(log);
        }
        catch (Exception)
        {
            // Audit logging is non-fatal: FK violations (e.g. stale JWT after DB reset)
            // must never block the primary operation. The failure is already visible
            // in application logs via the EF/Npgsql exception.
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByCaseIdAsync(Guid caseId)
    {
        return await _context.AuditLogs
            .Where(l => l.CaseId == caseId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByDoctorIdAsync(Guid doctorId, int limit = 100)
    {
        return await _context.AuditLogs
            .Where(l => l.DoctorId == doctorId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int limit = 100)
    {
        return await _context.AuditLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action, int limit = 100)
    {
        return await _context.AuditLogs
            .Where(l => l.Action == action)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}