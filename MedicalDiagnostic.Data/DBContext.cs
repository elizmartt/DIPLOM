using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MedicalDiagnosticSystem.MedicalDiagnostic.Data;

public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

public class MedicalDiagnosticDbContext : DbContext
{
    public MedicalDiagnosticDbContext(DbContextOptions<MedicalDiagnosticDbContext> options)
        : base(options)
    {
    }

    public DbSet<DiagnosisCase> DiagnosisCases { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<MedicalImage> MedicalImages { get; set; }
    public DbSet<ClinicalSymptom> ClinicalSymptoms { get; set; }
    public DbSet<LabTest> LabTests { get; set; }
    public DbSet<ImagingResult> ImagingResults { get; set; }
    public DbSet<ClinicalResult> ClinicalResults { get; set; }
    public DbSet<LaboratoryResult> LaboratoryResults { get; set; }
    public DbSet<UnifiedDiagnosisResult> UnifiedDiagnosisResults { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DiagnosisCase>(entity =>
        {
            entity.ToTable("diagnosis_cases");
            entity.HasKey(e => new { e.CaseId, e.CreatedAt });

            entity.Property(e => e.Status).HasDefaultValue("pending");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_log");
            entity.HasKey(e => new { e.LogId, e.CreatedAt });

            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.CaseId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("patients");
            entity.HasKey(e => e.PatientId);
            entity.Property(e => e.PatientCode).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.PatientCode).IsUnique();
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("doctors");
            entity.HasKey(e => e.DoctorId);
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.FullName).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<MedicalImage>(entity =>
        {
            entity.ToTable("medical_images");
            entity.HasKey(e => e.ImageId);

            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.CaseId).HasColumnName("case_id");
            entity.Property(e => e.ImageType).HasColumnName("image_type");
            entity.Property(e => e.ScanArea).HasColumnName("scan_area");
            entity.Property(e => e.FilePath).HasColumnName("file_path");
            entity.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");

            entity.Property(e => e.Modality).HasColumnName("modality");
            entity.Property(e => e.StudyUid).HasColumnName("study_uid");
            entity.Property(e => e.SeriesUid).HasColumnName("series_uid");
            entity.Property(e => e.InstanceUid).HasColumnName("instance_uid");
            entity.Property(e => e.StudyDate).HasColumnName("study_date");
            entity.Property(e => e.SeriesDescription).HasColumnName("series_description");
            entity.Property(e => e.SeriesNumber).HasColumnName("series_number");
            entity.Property(e => e.InstanceNumber).HasColumnName("instance_number");
            entity.Property(e => e.PixelSpacing).HasColumnName("pixel_spacing");
            entity.Property(e => e.SliceThickness).HasColumnName("slice_thickness");
            entity.Property(e => e.WindowCenter).HasColumnName("window_center");
            entity.Property(e => e.WindowWidth).HasColumnName("window_width");
            entity.Property(e => e.Rows).HasColumnName("rows");
            entity.Property(e => e.Columns).HasColumnName("columns");

            entity.Property(e => e.DicomMetadata)
                .HasColumnName("dicom_metadata")
                .HasColumnType("jsonb");

            entity.Property(e => e.IsPreprocessed).HasColumnName("is_preprocessed");
            entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at");

            entity.Ignore(e => e.DiagnosisCase);

            entity.HasOne<DiagnosisCase>()
                .WithMany()
                .HasForeignKey(e => e.CaseId)
                .HasPrincipalKey(d => d.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClinicalSymptom>(entity =>
        {
            entity.ToTable("clinical_symptoms");
            entity.HasKey(e => e.SymptomId);
            entity.Property(e => e.RecordedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.CaseId);
        });

        modelBuilder.Entity<LabTest>(entity =>
        {
            entity.ToTable("lab_tests");
            entity.HasKey(e => e.LabId);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.CaseId);
            entity.HasIndex(e => e.TestDate);
        });

        modelBuilder.Entity<ImagingResult>(entity =>
        {
            entity.ToTable("imaging_results");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Success).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.DiagnosisCaseId);
        });

        modelBuilder.Entity<ClinicalResult>(entity =>
        {
            entity.ToTable("clinical_results");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Success).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.DiagnosisCaseId);
        });

        modelBuilder.Entity<LaboratoryResult>(entity =>
        {
            entity.ToTable("laboratory_results");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Success).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.DiagnosisCaseId);
        });

        modelBuilder.Entity<UnifiedDiagnosisResult>(entity =>
        {
            entity.ToTable("unified_diagnosis_results");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Status).HasDefaultValue("completed");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.DiagnosisCaseId);
            entity.HasIndex(e => e.FinalDiagnosis);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
                switch (entry.Entity)
                {
                    case DiagnosisCase dc:
                        if (dc.CaseId == Guid.Empty) dc.CaseId = Guid.NewGuid();
                        if (dc.CreatedAt == default) dc.CreatedAt = DateTime.UtcNow;
                        dc.UpdatedAt = DateTime.UtcNow;
                        break;

                    case AuditLog al:
                        if (al.LogId == Guid.Empty) al.LogId = Guid.NewGuid();
                        if (al.CreatedAt == default) al.CreatedAt = DateTime.UtcNow;
                        break;
                }

            if (entry.State == EntityState.Modified)
                switch (entry.Entity)
                {
                    case DiagnosisCase dc:
                        dc.UpdatedAt = DateTime.UtcNow;
                        if (dc.Status == "completed" && dc.CompletedAt == null)
                            dc.CompletedAt = DateTime.UtcNow;
                        break;
                    case Patient p:
                        p.UpdatedAt = DateTime.UtcNow;
                        break;
                    case Doctor d:
                        d.UpdatedAt = DateTime.UtcNow;
                        break;
                    case UnifiedDiagnosisResult ur:
                        ur.UpdatedAt = DateTime.UtcNow;
                        break;
                }
        }
    }
}