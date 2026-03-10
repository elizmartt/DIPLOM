using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Interfaces;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Implementations;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace DiagnosisOrchestrator.Configuration
{
    public static class DatabaseServicesConfiguration
    {
        public static IServiceCollection AddDatabaseServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MedicalDiagnosticDb")
                ?? throw new InvalidOperationException(
                    "Connection string 'MedicalDiagnosticDb' not found in appsettings.json");

            services.AddDbContext<MedicalDiagnosticDbContext>(options =>
            {
                options.UseNpgsql(connectionString)
                       .EnableSensitiveDataLogging();
            });

            services.AddScoped<IDiagnosisCaseRepository,          DiagnosisCaseRepository>();
            services.AddScoped<IPatientRepository,                 PatientRepository>();
            services.AddScoped<IDoctorRepository,                  DoctorRepository>();
            services.AddScoped<IMedicalImageRepository,            MedicalImageRepository>();
            services.AddScoped<IClinicalSymptomRepository,         ClinicalSymptomRepository>();
            services.AddScoped<ILabTestRepository,                 LabTestRepository>();
            services.AddScoped<IImagingResultRepository,           ImagingResultRepository>();
            services.AddScoped<IClinicalResultRepository,          ClinicalResultRepository>();
            services.AddScoped<ILaboratoryResultRepository,        LaboratoryResultRepository>();
            services.AddScoped<IUnifiedDiagnosisResultRepository,  UnifiedDiagnosisResultRepository>();
            services.AddScoped<IAuditLogRepository,                AuditLogRepository>();

            services.AddScoped<DatabaseIntegrationService>();

            return services;
        }
    }
}