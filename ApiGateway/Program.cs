using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ApiGateway.Adapters.Outbound.Http;
using ApiGateway.Adapters.Outbound.Messaging;
using ApiGateway.Adapters.Outbound.RateLimiting;
using ApiGateway.Core.Services;
using ApiGateway.Ports;
using ApiGateway.Configuration;
using ApiGateway.Services;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Interfaces;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Implementations;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Services;
using StackExchange.Redis;
using Amazon.SecretsManager;
using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddAWSService<IAmazonSecretsManager>();
builder.Services.AddSingleton<AwsSecretsService>();
builder.Configuration["AWS:Region"] = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? "eu-north-1";

var tempProvider = builder.Services.BuildServiceProvider();
var secretsSvc = tempProvider.GetRequiredService<AwsSecretsService>();
await secretsSvc.LoadSecretsAsync();

builder.Configuration["ConnectionStrings:MedicalDiagnosticDb"] =
    $"Host=host.docker.internal;Port=5432;Database=medical_diagnosis_db;Username=postgres;Password={secretsSvc.Get("password")};Include Error Detail=true";
builder.Configuration["JwtSettings:SecretKey"] = secretsSvc.Get("secret");
builder.Configuration["Encryption:Key"] = secretsSvc.Get("key");



// CONTROLLERS & SWAGGER

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Medical Diagnostic System API Gateway",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IReportGeneratorService, ReportGeneratorService>();

// DATABASE

var connectionString = builder.Configuration.GetConnectionString("MedicalDiagnosticDb")
    ?? throw new InvalidOperationException("Connection string 'MedicalDiagnosticDb' not found");

builder.Services.AddDbContext<MedicalDiagnosticDbContext>(options =>
{
    options.UseNpgsql(connectionString)
           .EnableSensitiveDataLogging();
});


// REDIS

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
redisOptions.AbortOnConnectFail = false;
redisOptions.ConnectRetry = 3;
redisOptions.ConnectTimeout = 5000;
redisOptions.SyncTimeout = 5000;

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connection = ConnectionMultiplexer.Connect(redisOptions);
    connection.ConnectionFailed += (sender, args) =>
        Console.WriteLine($" Redis connection failed: {args.Exception?.Message}");
    connection.ConnectionRestored += (sender, args) =>
        Console.WriteLine(" Redis connection restored");
    return connection;
});

builder.Services.AddHealthChecks()
    .AddRedis(redisConnectionString, name: "redis", tags: new[] { "cache", "ratelimit" });

// REPOSITORIES

builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IDiagnosisCaseRepository, DiagnosisCaseRepository>();
builder.Services.AddScoped<IMedicalImageRepository, MedicalImageRepository>();
builder.Services.AddScoped<IClinicalSymptomRepository, ClinicalSymptomRepository>();
builder.Services.AddScoped<ILabTestRepository, LabTestRepository>();
builder.Services.AddScoped<IImagingResultRepository, ImagingResultRepository>();
builder.Services.AddScoped<IClinicalResultRepository, ClinicalResultRepository>();
builder.Services.AddScoped<ILaboratoryResultRepository, LaboratoryResultRepository>();
builder.Services.AddScoped<IUnifiedDiagnosisResultRepository, UnifiedDiagnosisResultRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<DicomService>();

// JWT AUTHENTICATION

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var secretKey = jwtSettings.Get<JwtSettings>()?.SecretKey
    ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Get<JwtSettings>()?.Issuer,
        ValidAudience = jwtSettings.Get<JwtSettings>()?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();


// CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:80",
                "http://frontend"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


// APPLICATION SERVICES

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<DiagnosisKafkaService>();


// AWS

builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddSingleton<S3Service>();


// HTTP CLIENT & INFRASTRUCTURE

builder.Services.AddHttpClient();

builder.Services.Configure<ServiceClusterConfiguration>(
    builder.Configuration.GetSection("ServiceClusters")
);


// RATE LIMITER & MESSAGING

var requestsPerMinute = builder.Configuration.GetValue<int>("RateLimit:RequestsPerMinute", 100);

builder.Services.AddSingleton<IRateLimiter>(sp =>
{
    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
    return new RedisRateLimiter(redis, requestsPerMinute);
});

builder.Services.AddSingleton<IMessagePublisher>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var bootstrap = config["Kafka:BootstrapServers"] ?? "localhost:9092";
    return new KafkaMessagePublisher(bootstrap);
});

builder.Services.AddSingleton<ILoadBalancer, RoundRobinLoadBalancer>();
builder.Services.AddScoped<IDownstreamService, HttpDownstreamService>();
builder.Services.AddScoped<GatewayService>();
builder.Services.AddScoped<IGatewayService, GatewayService>();


// BUILD & MIDDLEWARE

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MedicalDiagnosticDbContext>();
    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                ""MigrationId"" character varying(150) NOT NULL,
                ""ProductVersion"" character varying(32) NOT NULL,
                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
            );
            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            SELECT '20260225134545_InitialCreate', '8.0.0'
            WHERE NOT EXISTS (
                SELECT 1 FROM ""__EFMigrationsHistory""
                WHERE ""MigrationId"" = '20260225134545_InitialCreate'
            );
        ");
        Console.WriteLine(" Database ready");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Database error: {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();