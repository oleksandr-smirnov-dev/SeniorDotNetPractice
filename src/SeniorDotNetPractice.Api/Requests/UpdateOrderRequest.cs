namespace SeniorDotNetPractice.Api.Requests;

public class UpdateOrderRequest
{
    public string OrderNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
