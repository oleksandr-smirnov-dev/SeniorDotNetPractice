using SeniorDotNetPractice.Api.Responses;

namespace SeniorDotNetPractice.Api.Caching;

public interface IOrderCache
{
    Task<OrderDetailsResponse?> GetAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        OrderDetailsResponse order,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        int orderId,
        CancellationToken cancellationToken = default);
}