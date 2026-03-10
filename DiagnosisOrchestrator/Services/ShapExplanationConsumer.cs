
using Confluent.Kafka;
using System.Text.Json;
using DiagnosisOrchestrator.Services;
using Npgsql;

namespace DiagnosisOrchestrator.Services
{

    public class ShapExplanationMessage
    {
        public string RequestId { get; set; }
        public Guid DiagnosisCaseId { get; set; }
        public string ModelName { get; set; }
        public JsonElement Explanation { get; set; }  
        public double Timestamp { get; set; }
    }


    public class ShapExplanationConsumer : BackgroundService
    {
        private readonly ILogger<ShapExplanationConsumer> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private IConsumer<string, string> _consumer;

        public ShapExplanationConsumer(
            ILogger<ShapExplanationConsumer> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("MedicalDiagnosticDb");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("SHAP Explanation Consumer starting (background)...");
    
    _ = Task.Run(async () =>
    {
        await Task.Delay(3000); 
        
        try
        {
            _logger.LogInformation("Connecting SHAP consumer to Kafka...");
            
            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                GroupId = "diagnosis-orchestrator-shap-group",
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = true
            };

            _consumer = new ConsumerBuilder<string, string>(config)
                .SetKeyDeserializer(Deserializers.Utf8)
                .SetValueDeserializer(Deserializers.Utf8)
                .Build();

            _consumer.Subscribe("shap-explanations");
            _logger.LogInformation("✓ Subscribed to 'shap-explanations' topic");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult != null)
                    {
                        await ProcessShapMessage(consumeResult.Message.Value);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming SHAP message");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "FATAL: SHAP consumer failed!");
        }
        finally
        {
            _consumer?.Close();
            _logger.LogInformation("SHAP Explanation Consumer stopped");
        }
    }, stoppingToken);
    
    return Task.CompletedTask;  
}
        private async Task ProcessShapMessage(string messageJson)
        {
            try
            {
                _logger.LogInformation("Raw SHAP message: {Message}", messageJson.Substring(0, Math.Min(200, messageJson.Length)));
        
                var shapMessage = JsonSerializer.Deserialize<ShapExplanationMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (shapMessage == null)
                {
                    _logger.LogWarning("Failed to deserialize SHAP message");
                    return;
                }

                _logger.LogInformation(
                    "Received SHAP explanation: Case {CaseId}, Model {Model}",
                    shapMessage.DiagnosisCaseId, shapMessage.ModelName);

                await SaveShapExplanationToDatabase(shapMessage);

                _logger.LogInformation(
                    "✓ Saved SHAP explanation for case {CaseId}", shapMessage.DiagnosisCaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SHAP message: {Message}", messageJson.Substring(0, Math.Min(100, messageJson.Length)));
            }
        }

        private async Task SaveShapExplanationToDatabase(ShapExplanationMessage shapMessage)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            int? predictedClass = null;
            decimal? baseValue = null;
            decimal? totalPositiveImpact = null;
            decimal? totalNegativeImpact = null;

            try
            {
                if (shapMessage.Explanation.TryGetProperty("predicted_class", out var pc))
                    predictedClass = pc.GetInt32();

                if (shapMessage.Explanation.TryGetProperty("base_value", out var bv))
                    baseValue = (decimal)bv.GetDouble();

                if (shapMessage.Explanation.TryGetProperty("total_positive_impact", out var tpi))
                    totalPositiveImpact = (decimal)tpi.GetDouble();

                if (shapMessage.Explanation.TryGetProperty("total_negative_impact", out var tni))
                    totalNegativeImpact = (decimal)tni.GetDouble();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract SHAP values from explanation");
            }

            const string sql = @"
                INSERT INTO shap_explanations (
                    diagnosis_case_id,
                    model_name,
                    explanation_data,
                    predicted_class,
                    base_value,
                    total_positive_impact,
                    total_negative_impact,
                    created_at
                ) VALUES (
                    @DiagnosisCaseId,
                    @ModelName,
                    @ExplanationData::jsonb,
                    @PredictedClass,
                    @BaseValue,
                    @TotalPositiveImpact,
                    @TotalNegativeImpact,
                    NOW()
                )
                ON CONFLICT (diagnosis_case_id, model_name) 
                DO UPDATE SET
                    explanation_data = EXCLUDED.explanation_data,
                    predicted_class = EXCLUDED.predicted_class,
                    base_value = EXCLUDED.base_value,
                    total_positive_impact = EXCLUDED.total_positive_impact,
                    total_negative_impact = EXCLUDED.total_negative_impact,
                    created_at = NOW()
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@DiagnosisCaseId", shapMessage.DiagnosisCaseId);
            command.Parameters.AddWithValue("@ModelName", shapMessage.ModelName);
            command.Parameters.AddWithValue("@ExplanationData", 
                shapMessage.Explanation.GetRawText());
            command.Parameters.AddWithValue("@PredictedClass", 
                predictedClass.HasValue ? predictedClass.Value : DBNull.Value);
            command.Parameters.AddWithValue("@BaseValue", 
                baseValue.HasValue ? baseValue.Value : DBNull.Value);
            command.Parameters.AddWithValue("@TotalPositiveImpact", 
                totalPositiveImpact.HasValue ? totalPositiveImpact.Value : DBNull.Value);
            command.Parameters.AddWithValue("@TotalNegativeImpact", 
                totalNegativeImpact.HasValue ? totalNegativeImpact.Value : DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public override void Dispose()
        {
            _consumer?.Dispose();
            base.Dispose();
        }
    }
}

public interface IShapRepository
{
    Task SaveShapExplanation(ShapExplanationMessage shapMessage);
    Task<string> GetShapExplanation(int diagnosisCaseId, string modelName);
}

public class ShapRepository : IShapRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ShapRepository> _logger;

    public ShapRepository(IConfiguration configuration, ILogger<ShapRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("MedicalDiagnosticDb");
        _logger = logger;
    }

    public async Task SaveShapExplanation(ShapExplanationMessage shapMessage)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        int? predictedClass = null;
        decimal? baseValue = null;
        decimal? totalPositiveImpact = null;
        decimal? totalNegativeImpact = null;

        try
        {
            if (shapMessage.Explanation.TryGetProperty("predicted_class", out var pc))
                predictedClass = pc.GetInt32();
            if (shapMessage.Explanation.TryGetProperty("base_value", out var bv))
                baseValue = (decimal)bv.GetDouble();
            if (shapMessage.Explanation.TryGetProperty("total_positive_impact", out var tpi))
                totalPositiveImpact = (decimal)tpi.GetDouble();
            if (shapMessage.Explanation.TryGetProperty("total_negative_impact", out var tni))
                totalNegativeImpact = (decimal)tni.GetDouble();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract SHAP values");
        }

        const string sql = @"
            INSERT INTO shap_explanations (
                diagnosis_case_id, model_name, explanation_data,
                predicted_class, base_value, total_positive_impact, total_negative_impact
            ) VALUES (
                @DiagnosisCaseId, @ModelName, @ExplanationData::jsonb,
                @PredictedClass, @BaseValue, @TotalPositiveImpact, @TotalNegativeImpact
            )
            ON CONFLICT (diagnosis_case_id, model_name) 
            DO UPDATE SET
                explanation_data = EXCLUDED.explanation_data,
                predicted_class = EXCLUDED.predicted_class,
                base_value = EXCLUDED.base_value,
                total_positive_impact = EXCLUDED.total_positive_impact,
                total_negative_impact = EXCLUDED.total_negative_impact,
                created_at = NOW()
        ";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@DiagnosisCaseId", shapMessage.DiagnosisCaseId);
        command.Parameters.AddWithValue("@ModelName", shapMessage.ModelName);
        command.Parameters.AddWithValue("@ExplanationData", shapMessage.Explanation.GetRawText());
        command.Parameters.AddWithValue("@PredictedClass", 
            predictedClass.HasValue ? predictedClass.Value : DBNull.Value);
        command.Parameters.AddWithValue("@BaseValue", 
            baseValue.HasValue ? baseValue.Value : DBNull.Value);
        command.Parameters.AddWithValue("@TotalPositiveImpact", 
            totalPositiveImpact.HasValue ? totalPositiveImpact.Value : DBNull.Value);
        command.Parameters.AddWithValue("@TotalNegativeImpact", 
            totalNegativeImpact.HasValue ? totalNegativeImpact.Value : DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<string> GetShapExplanation(int diagnosisCaseId, string modelName)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT explanation_data
            FROM shap_explanations
            WHERE diagnosis_case_id = @DiagnosisCaseId
              AND model_name = @ModelName
        ";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@DiagnosisCaseId", diagnosisCaseId);
        command.Parameters.AddWithValue("@ModelName", modelName);

        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? "{}";
    }
}