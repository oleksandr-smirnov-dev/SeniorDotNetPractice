using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.CircuitBreaker;
using SeniorDotNetPractice.Api.Caching;
using SeniorDotNetPractice.Api.Data;
using StackExchange.Redis;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });
builder.Services.AddHealthChecks();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "Connection string 'Postgres' was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Connection string 'Redis' was not found.");

var redisOptions = ConfigurationOptions.Parse(redisConnectionString);

redisOptions.ConnectTimeout = 500;
redisOptions.AsyncTimeout = 500;
redisOptions.SyncTimeout = 500;
redisOptions.AbortOnConnectFail = false;

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConfigurationOptions = redisOptions;
    options.InstanceName = "SeniorDotNetPractice:";
});

builder.Services.AddSingleton(sp =>
{
    return new ResiliencePipelineBuilder()
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(10),
            MinimumThroughput = 2,
            BreakDuration = TimeSpan.FromSeconds(10),

            ShouldHandle = new PredicateBuilder()
                .Handle<RedisConnectionException>()
                .Handle<RedisTimeoutException>()
        })
        .Build();
});
builder.Services.AddSingleton<IOrderCache, RedisOrderCache>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
