using System.Security.Claims;
using Ecommerce.Api.Data;
using Ecommerce.Api.Dtos.Cart;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartResponse>> GetCart()
    {
        var userId = GetUserId();
        var cart = await GetOrCreateCartAsync(userId);

        return Ok(ToResponse(cart));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartResponse>> AddItem(AddCartItemRequest request)
    {
        var userId = GetUserId();
        var product = await context.Products
            .FirstOrDefaultAsync(product => product.Id == request.ProductId && product.IsActive);

        if (product is null)
        {
            return NotFound(new { message = "Produto nao encontrado ou inativo." });
        }

        var cart = await GetOrCreateCartAsync(userId);
        var existingItem = cart.Items.FirstOrDefault(item => item.ProductId == request.ProductId);
        var requestedQuantity = request.Quantity + (existingItem?.Quantity ?? 0);

        if (requestedQuantity > product.StockQuantity)
        {
            return BadRequest(new { message = "Quantidade solicitada maior que o estoque disponivel." });
        }

        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                Quantity = request.Quantity
            });
        }
        else
        {
            existingItem.Quantity += request.Quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        cart = await LoadCartAsync(userId);

        return Ok(ToResponse(cart));
    }

    [HttpPut("items/{itemId:int}")]
    public async Task<ActionResult<CartResponse>> UpdateItem(int itemId, UpdateCartItemRequest request)
    {
        var userId = GetUserId();
        var cart = await LoadCartAsync(userId);
        var item = cart.Items.FirstOrDefault(item => item.Id == itemId);

        if (item is null)
        {
            return NotFound(new { message = "Item nao encontrado no carrinho." });
        }

        if (item.Product is null || !item.Product.IsActive)
        {
            return BadRequest(new { message = "Produto nao esta disponivel." });
        }

        if (request.Quantity > item.Product.StockQuantity)
        {
            return BadRequest(new { message = "Quantidade solicitada maior que o estoque disponivel." });
        }

        item.Quantity = request.Quantity;
        cart.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(ToResponse(cart));
    }

    [HttpDelete("items/{itemId:int}")]
    public async Task<ActionResult<CartResponse>> RemoveItem(int itemId)
    {
        var userId = GetUserId();
        var cart = await LoadCartAsync(userId);
        var item = cart.Items.FirstOrDefault(item => item.Id == itemId);

        if (item is null)
        {
            return NotFound(new { message = "Item nao encontrado no carrinho." });
        }

        context.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        cart = await LoadCartAsync(userId);

        return Ok(ToResponse(cart));
    }

    [HttpDelete("items")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetUserId();
        var cart = await LoadCartAsync(userId);

        context.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private int GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out var userId)
            ? userId
            : throw new InvalidOperationException("Usuario autenticado sem identificador valido.");
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await context.Carts
            .Include(cart => cart.Items)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(cart => cart.UserId == userId);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart { UserId = userId };
        context.Carts.Add(cart);
        await context.SaveChangesAsync();

        return cart;
    }

    private async Task<Cart> LoadCartAsync(int userId)
    {
        return await context.Carts
            .Include(cart => cart.Items)
            .ThenInclude(item => item.Product)
            .FirstAsync(cart => cart.UserId == userId);
    }

    private static CartResponse ToResponse(Cart cart)
    {
        var items = cart.Items
            .OrderBy(item => item.Product?.Name)
            .Select(item =>
            {
                var unitPrice = item.Product?.Price ?? 0;

                return new CartItemResponse
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? string.Empty,
                    UnitPrice = unitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = unitPrice * item.Quantity
                };
            })
            .ToList();

        return new CartResponse
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = items,
            TotalAmount = items.Sum(item => item.TotalPrice)
        };
    }
}
