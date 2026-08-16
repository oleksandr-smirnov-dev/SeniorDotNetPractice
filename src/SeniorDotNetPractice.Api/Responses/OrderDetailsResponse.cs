using SeniorDotNetPractice.Api.Entities;

namespace SeniorDotNetPractice.Api.Responses;

public class OrderDetailsResponse
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<OrderItemResponse> Items { get; set; } = new();

    public decimal TotalAmount { get; set; }

    public int ItemCount { get; set; }

    public uint Version { get; set; }
}