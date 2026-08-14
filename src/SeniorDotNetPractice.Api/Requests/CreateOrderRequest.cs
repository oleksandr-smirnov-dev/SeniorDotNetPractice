using SeniorDotNetPractice.Api.Entities;

namespace SeniorDotNetPractice.Api.Requests;

public class CreateOrderRequest
{
    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
