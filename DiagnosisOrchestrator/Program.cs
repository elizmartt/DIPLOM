using DiagnosisOrchestrator.Models;
using DiagnosisOrchestrator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Diagnosis Orchestrator API",
        Version = "v1",
        Description = "Multi-modal medical diagnosis orchestration service"
    });
});

// Configure options from appsettings.json
builder.Services.Configure<OrchestratorOptions>(
    builder.Configuration.GetSection("OrchestratorOptions"));

builder.Services.Configure<ModuleEndpoints>(
    builder.Configuration.GetSection("ModuleEndpoints"));

builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));

// Register HttpClient with proper configuration
builder.Services.AddHttpClient<IModuleClientService, ModuleClientService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true // For development
});

// Register repository with connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("TimescaleDB")
    ?? throw new InvalidOperationException("TimescaleDB connection string not configured");

builder.Services.AddSingleton<IDiagnosisRepository>(sp =>
    new DiagnosisRepository(
        connectionString,
        sp.GetRequiredService<ILogger<DiagnosisRepository>>()));

// Register orchestrator service
builder.Services.AddScoped<IDiagnosisOrchestratorService, DiagnosisOrchestratorService>();

// Register Kafka services
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddHostedService<KafkaConsumerService>();

// CRITICAL FIX: Make consumer available for dependency injection
builder.Services.AddSingleton<KafkaConsumerService>(sp =>
    (KafkaConsumerService)sp.GetServices<IHostedService>()
        .First(s => s is KafkaConsumerService));

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// Logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Diagnosis Orchestrator API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Diagnosis Orchestrator starting...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

app.Run();