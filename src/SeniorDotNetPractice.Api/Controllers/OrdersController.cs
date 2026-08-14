using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
            CreatedAtUtc = DateTime.UtcNow,
            Items = request.Items
                .Select(i => new OrderItem
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                })
                .ToList()
        };

        _dbContext.Orders.Add(order);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException postgresException &&
                  postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                  postgresException.ConstraintName == "IX_Orders_OrderNumber")
        {
            return Conflict(new
            {
                message = $"Order with number '{request.OrderNumber}' already exists."
            });
        }

        var response = new OrderDetailsResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            CreatedAtUtc = order.CreatedAtUtc,
            Items = order.Items
        .Select(i => new OrderItemResponse
        {
            Id = i.Id,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        })
        .ToList(),
            TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity),
            ItemCount = order.Items.Count
        };

        return CreatedAtAction(
            nameof(ReadOrder),
            new { id = order.Id },
            response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Order>> ReadOrder(int id)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderDetailsResponse
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                CreatedAtUtc = o.CreatedAtUtc,
                TotalAmount = o.Items.Sum(i => i.UnitPrice * i.Quantity),
                ItemCount = o.Items.Count,
                Items = o.Items
                    .Select(i => new OrderItemResponse
                    {
                        Id = i.Id,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

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
    [FromQuery] OrderStatus[]? statuses,
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

        if (statuses is { Length: > 0 })
        {
            query = query.Where(o => statuses.Contains(o.Status));
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

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> CompleteOrder(int id)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        var hasItems = await _dbContext.OrderItems
            .AnyAsync(i => i.OrderId == id);

        if (!hasItems)
        {
            return BadRequest("Order cannot be completed without items.");
        }

        order.Status = OrderStatus.Completed;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("n-plus-one-demo")]
    public async Task<IActionResult> GetOrdersNPlusOneDemo()
    {
        var result = await _dbContext.Orders
            .AsNoTracking()
            .OrderBy(o => o.Id)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                ItemCount = o.Items.Count
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("status-summary")]
    public async Task<IActionResult> GetStatusSummary()
    {
        var result = await _dbContext.Orders
            .AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("all-items-valid")]
    public async Task<IActionResult> GetOrdersWithAllValidItems()
    {
        var result = await _dbContext.Orders
            .AsNoTracking()
            .Where(o =>
                o.Items.Any() &&
                o.Items.All(i => i.Quantity > 0))
            .Select(o => new
            {
                o.Id,
                o.OrderNumber
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpPost("cancel-pending")]
    public async Task<IActionResult> CancelPendingOrders()
    {
        var affectedRows = await _dbContext.Orders
            .Where(o => o.Status == OrderStatus.Pending)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    o => o.Status,
                    OrderStatus.Cancelled));

        return Ok(new { affectedRows });
    }

    [HttpPost("{id:int}/bulk-update-tracking-demo")]
    public async Task<IActionResult> BulkUpdateTrackingDemo(int id)
    {
        var order = await _dbContext.Orders
            .FirstAsync(o => o.Id == id);

        var statusBefore = order.Status;

        await _dbContext.Orders
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    o => o.Status,
                    OrderStatus.Completed));
        await _dbContext.Entry(order).ReloadAsync();
        var statusAfter = order.Status;

        return Ok(new
        {
            statusBefore,
            statusAfter
        });
    }
}