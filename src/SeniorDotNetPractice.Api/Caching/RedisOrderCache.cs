using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Polly;
using Polly.CircuitBreaker;
using SeniorDotNetPractice.Api.Responses;

namespace SeniorDotNetPractice.Api.Caching;

public class RedisOrderCache : IOrderCache
{
    private readonly IDistributedCache _cache;
    private readonly ResiliencePipeline _redisPipeline;
    private readonly ILogger<RedisOrderCache> _logger;

    public RedisOrderCache(
        IDistributedCache cache,
        ResiliencePipeline redisPipeline,
        ILogger<RedisOrderCache> logger)
    {
        _cache = cache;
        _redisPipeline = redisPipeline;
        _logger = logger;
    }

    public async Task<OrderDetailsResponse?> GetAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(orderId);

        try
        {
            var cachedOrderJson = await _redisPipeline.ExecuteAsync(
                async token =>
                    await _cache.GetStringAsync(cacheKey, token),
                cancellationToken);

            if (cachedOrderJson is null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<OrderDetailsResponse>(
                cachedOrderJson);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning(
                "Redis circuit is OPEN for order {OrderId}.",
                orderId);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Redis cache is unavailable for order {OrderId}.",
                orderId);

            return null;
        }
    }

    public async Task SetAsync(
    OrderDetailsResponse order,
    CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(order.Id);
        var orderJson = JsonSerializer.Serialize(order);

        try
        {
            await _redisPipeline.ExecuteAsync(
                async token =>
                    await _cache.SetStringAsync(
                        cacheKey,
                        orderJson,
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow =
                                TimeSpan.FromMinutes(5)
                        },
                        token),
                cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning(
                "Redis circuit is OPEN. Order {OrderId} was not cached.",
                order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to cache order {OrderId}.",
                order.Id);
        }
    }

    public async Task RemoveAsync(
    int orderId,
    CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(orderId);

        try
        {
            await _redisPipeline.ExecuteAsync(
                async token =>
                    await _cache.RemoveAsync(cacheKey, token),
                cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning(
                "Redis circuit is OPEN. Cache for order {OrderId} was not invalidated.",
                orderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to invalidate cache for order {OrderId}.",
                orderId);
        }
    }

    private static string GetCacheKey(int orderId)
    {
        return $"orders:{orderId}";
    }
}