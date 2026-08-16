using SeniorDotNetPractice.Api.Entities;
using System.ComponentModel.DataAnnotations;

namespace SeniorDotNetPractice.Api.Requests;

public class UpdateOrderRequest
{
    [Required]
    [MaxLength(Order.OrderNumberMaxLength)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus? Status { get; set; }

    [Range(typeof(uint), "1", "4294967295")]
    public uint Version { get; set; }
}
