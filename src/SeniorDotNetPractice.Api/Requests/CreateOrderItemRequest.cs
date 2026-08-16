using SeniorDotNetPractice.Api.Entities;
using System.ComponentModel.DataAnnotations;

namespace SeniorDotNetPractice.Api.Requests;

public class CreateOrderItemRequest
{
    [Required]
    [MaxLength(OrderItem.ProductNameMaxLength)]
    public string ProductName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal UnitPrice { get; set; }
}
