namespace SeniorDotNetPractice.Api.Requests;

public class CreateOrderRequest
{
    public string OrderNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
