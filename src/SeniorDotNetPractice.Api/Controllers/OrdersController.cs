using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SeniorDotNetPractice.Api.Data;
using SeniorDotNetPractice.Api.Entities;
using SeniorDotNetPractice.Api.Requests;
using SeniorDotNetPractice.Api.Responses;
using System.ComponentModel.DataAnnotations;
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
            Status = request.Status!.Value,
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
            ItemCount = order.Items.Count,
            Version = order.Version
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
                Version = o.Version,
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
    public async Task<IActionResult> UpdateOrder(
    int id,
    UpdateOrderRequest request)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        order.OrderNumber = request.OrderNumber;
        order.Status = request.Status!.Value;

        _dbContext.Entry(order)
            .Property(o => o.Version)
            .OriginalValue = request.Version;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message = "The order was modified by another request. Reload the latest data and try again."
            });
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

        return NoContent();
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

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message = "The order was modified by another request. Reload the latest data and try again."
            });
        }

        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryResponse>>> GetOrders(
    OrderStatus? status,
    [FromQuery] OrderStatus[]? statuses,
    string? orderNumber,
    DateTime? createdFrom,
    DateTime? createdTo,
    [Range(1, int.MaxValue)] int page = 1,
    [Range(1, 100)] int pageSize = 20)
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

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message = "The order was modified by another request. Reload the latest data and try again."
            });
        }

        return NoContent();
    }
}
