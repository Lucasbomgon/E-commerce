using System.Security.Claims;
using Ecommerce.Api.Data;
using Ecommerce.Api.Dtos.Orders;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetMyOrders()
    {
        var userId = GetUserId();
        var orders = await context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => ToResponse(order))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var userId = GetUserId();
        var order = await context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(order => order.Id == id && order.UserId == userId);

        if (order is null)
        {
            return NotFound(new { message = "Pedido nao encontrado." });
        }

        return Ok(ToResponse(order));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderResponse>> Checkout()
    {
        var userId = GetUserId();
        var cart = await context.Carts
            .Include(cart => cart.Items)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(cart => cart.UserId == userId);

        if (cart is null || cart.Items.Count == 0)
        {
            return BadRequest(new { message = "Carrinho vazio." });
        }

        var unavailableItem = cart.Items.FirstOrDefault(item =>
            item.Product is null ||
            !item.Product.IsActive ||
            item.Quantity > item.Product.StockQuantity);

        if (unavailableItem is not null)
        {
            return BadRequest(new
            {
                message = "O carrinho possui item indisponivel ou quantidade acima do estoque.",
                productId = unavailableItem.ProductId
            });
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Paid
        };

        foreach (var cartItem in cart.Items)
        {
            var product = cartItem.Product!;
            var totalPrice = product.Price * cartItem.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = cartItem.Quantity,
                UnitPrice = product.Price,
                TotalPrice = totalPrice
            });

            product.StockQuantity -= cartItem.Quantity;
            order.TotalAmount += totalPrice;
        }

        context.Orders.Add(order);
        context.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        var createdOrder = await context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .FirstAsync(item => item.Id == order.Id);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToResponse(createdOrder));
    }

    private int GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out var userId)
            ? userId
            : throw new InvalidOperationException("Usuario autenticado sem identificador valido.");
    }

    private static OrderResponse ToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            Items = order.Items
                .OrderBy(item => item.Product?.Name)
                .Select(item => new OrderItemResponse
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? string.Empty,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                })
                .ToList()
        };
    }
}
