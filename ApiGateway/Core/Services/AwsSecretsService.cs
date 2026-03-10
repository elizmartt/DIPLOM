using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace ApiGateway.Services;

public class AwsSecretsService
{
    private readonly IAmazonSecretsManager _client;
    private readonly Dictionary<string, string> _cache = new();

    public AwsSecretsService(IAmazonSecretsManager client)
    {
        _client = client;
    }

    public async Task LoadSecretsAsync()
    {
        var secretNames = new[]
        {
            "medical-diagnosis/db-password",
            "medical-diagnosis/jwt-secret",
            "medical-diagnosis/aes-key"
        };

        foreach (var name in secretNames)
        {
            try
            {
                var response = await _client.GetSecretValueAsync(
                    new GetSecretValueRequest { SecretId = name });

                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    response.SecretString)!;

                foreach (var kv in dict)
                    _cache[kv.Key] = kv.Value;

                Console.WriteLine($" Loaded secret: {name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Failed to load secret '{name}': {ex.Message}");
                throw;
            }
        }
    }

    public string Get(string key) =>
        _cache.TryGetValue(key, out var val) ? val
            : throw new KeyNotFoundException($"Secret key '{key}' not found in cache");
}