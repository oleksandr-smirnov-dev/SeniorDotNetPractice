using Microsoft.AspNetCore.Mvc;
using SeniorDotNetPractice.Api.Data;
using SeniorDotNetPractice.Api.Entities;
using SeniorDotNetPractice.Api.Requests;

namespace SeniorDotNetPractice.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public OrdersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(CreateOrderRequest request)
    {
        var order = new Order
        {
            OrderNumber = request.OrderNumber,
            Status = request.Status,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, order);
    }
}
