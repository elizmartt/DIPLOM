using ApiGateway.Adapters.Outbound.Http;
using ApiGateway.Adapters.Outbound.Messaging;
using ApiGateway.Adapters.Outbound.RateLimiting;
using ApiGateway.Core.Services;
using ApiGateway.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.Configure<ServiceClusterConfiguration>(
    builder.Configuration.GetSection("ServiceClusters")
);

builder.Services.AddSingleton<IRateLimiter>(_ =>
    new InMemoryRateLimiter(100)
);

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers(); 

app.MapFallbackToController("ForwardRequest", "Gateway"); 

Console.WriteLine("API Gateway running...");
Console.WriteLine($"Kafka Bootstrap: {builder.Configuration["Kafka:BootstrapServers"]}");

app.Run();