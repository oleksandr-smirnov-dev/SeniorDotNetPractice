using SeniorDotNetPractice.Api.Entities;

namespace SeniorDotNetPractice.Api.Responses;

public class OrderSummaryResponse
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
