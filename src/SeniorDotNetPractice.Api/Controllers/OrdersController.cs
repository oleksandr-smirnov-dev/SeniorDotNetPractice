using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeniorDotNetPractice.Api.Data;
using SeniorDotNetPractice.Api.Entities;
using SeniorDotNetPractice.Api.Requests;
using SeniorDotNetPractice.Api.Responses;
using System.Linq;

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

        if (order is null)
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

    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryResponse>>> GetOrders(
    OrderStatus? status,
    string? orderNumber,
    DateTime? createdFrom,
    DateTime? createdTo,
    int page = 1,
    int pageSize = 20)
    {
        var query = _dbContext.Orders.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(orderNumber))
        {
            query = query.Where(
                o => EF.Functions.ILike(o.OrderNumber, $"%{orderNumber}%"));
        }

        if (createdFrom.HasValue)
        {
            query = query.Where(o => o.CreatedAtUtc >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            query = query.Where(o => o.CreatedAtUtc <= createdTo.Value);
        }

        var orders = await query
            .OrderBy(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryResponse
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                CreatedAtUtc = o.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(orders);
    }
}