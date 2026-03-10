using DiagnosisOrchestrator.Services;
using DiagnosisOrchestrator.Configuration;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Services;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Interfaces;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Implementations;
using MedicalDiagnostic.Data.Services;
using Amazon.SecretsManager;
using ApiGateway.Services;
using DatabaseIntegrationService = MedicalDiagnostic.Data.Services.DatabaseIntegrationService;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Logging
    .AddConsole()
    .SetMinimumLevel(LogLevel.Information)
    .AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information)
    .AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Information)
    .AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);

// AWS SECRETS MANAGER — load before everything else
builder.Services.AddAWSService<IAmazonSecretsManager>();
builder.Configuration["AWS:Region"] = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? "eu-north-1";
builder.Services.AddSingleton<AwsSecretsService>();

var tempProvider = builder.Services.BuildServiceProvider();
var secretsSvc = tempProvider.GetRequiredService<AwsSecretsService>();
await secretsSvc.LoadSecretsAsync();

builder.Configuration["ConnectionStrings:MedicalDiagnosticDb"] =
    $"Host=host.docker.internal;Port=5432;Database=medical_diagnosis_db;Username=postgres;Password={secretsSvc.Get("password")};Include Error Detail=true";

// DATABASE & REPOSITORIES

builder.Services.AddDatabaseServices(builder.Configuration);

builder.Services.AddScoped<IDiagnosisCaseRepository, DiagnosisCaseRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IMedicalImageRepository, MedicalImageRepository>();
builder.Services.AddScoped<IClinicalSymptomRepository, ClinicalSymptomRepository>();
builder.Services.AddScoped<ILabTestRepository, LabTestRepository>();
builder.Services.AddScoped<IImagingResultRepository, ImagingResultRepository>();
builder.Services.AddScoped<IClinicalResultRepository, ClinicalResultRepository>();
builder.Services.AddScoped<ILaboratoryResultRepository, LaboratoryResultRepository>();
builder.Services.AddScoped<IUnifiedDiagnosisResultRepository, UnifiedDiagnosisResultRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<DatabaseIntegrationService>();


// KAFKA & APPLICATION SERVICES

builder.Services.AddHostedService<ShapExplanationConsumer>();
builder.Services.AddControllers();
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<WeightedEnsembleFusion>();
builder.Services.AddSingleton<KafkaConsumerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<KafkaConsumerService>());

builder.Services.Configure<HostOptions>(options =>
{
    options.StartupTimeout = TimeSpan.FromSeconds(120);
});


// BUILD & MIDDLEWARE

var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
builder.WebHost.UseUrls(isDocker ? "http://0.0.0.0:5217" : "http://localhost:5217");

var app = builder.Build();

app.MapControllers();

app.MapGet("/health", async (MedicalDiagnosticDbContext dbContext) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "healthy", database = "connected", kafka = "running", timestamp = DateTime.UtcNow })
            : Results.Problem(title: "Database Unhealthy", detail: "Cannot connect to database", statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "Database Error", detail: ex.Message, statusCode: 503);
    }
});

Console.WriteLine($"Starting Orchestrator... Kafka: {builder.Configuration["Kafka:BootstrapServers"] ?? "NOT CONFIGURED"}");

app.Run();