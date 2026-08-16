namespace SeniorDotNetPractice.Api.Entities;

public class Order
{
    public const int OrderNumberMaxLength = 50;

    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public uint Version { get; set; }
}
