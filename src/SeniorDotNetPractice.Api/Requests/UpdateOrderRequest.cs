using SeniorDotNetPractice.Api.Entities;

namespace SeniorDotNetPractice.Api.Requests;

public class UpdateOrderRequest
{
    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public uint Version { get; set; }
}
