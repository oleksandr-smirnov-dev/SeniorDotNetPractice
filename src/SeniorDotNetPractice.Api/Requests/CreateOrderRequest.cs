using SeniorDotNetPractice.Api.Entities;
using System.ComponentModel.DataAnnotations;

namespace SeniorDotNetPractice.Api.Requests;

public class CreateOrderRequest
{
    [Required]
    [MaxLength(Order.OrderNumberMaxLength)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus? Status { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
