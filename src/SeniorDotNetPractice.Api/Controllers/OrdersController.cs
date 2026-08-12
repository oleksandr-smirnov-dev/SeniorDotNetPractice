using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Order>> ReadOrder(int id)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Order>> UpdateOrder(int id, UpdateOrderRequest request)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id);

        if(order is null)
        {
            return NotFound();
        }

        order.OrderNumber = request.OrderNumber;
        order.Status = request.Status;

        await _dbContext.SaveChangesAsync();

        return Ok(order);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteOrder(int id)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        _dbContext.Orders.Remove(order);

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
