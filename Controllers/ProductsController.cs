using Ecommerce.Api.Data;
using Ecommerce.Api.Dtos.Products;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll(
        [FromQuery] int? categoryId,
        [FromQuery] string? search)
    {
        var query = context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(product =>
                product.Name.Contains(normalizedSearch) ||
                product.Description.Contains(normalizedSearch));
        }

        var products = await query
            .OrderBy(product => product.Name)
            .Select(product => ToResponse(product))
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .FirstOrDefaultAsync(product => product.Id == id);

        if (product is null)
        {
            return NotFound(new { message = "Produto nao encontrado." });
        }

        return Ok(ToResponse(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request)
    {
        var categoryExists = await context.Categories.AnyAsync(category => category.Id == request.CategoryId);

        if (!categoryExists)
        {
            return BadRequest(new { message = "Categoria informada nao existe." });
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            IsActive = request.IsActive,
            CategoryId = request.CategoryId
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        await context.Entry(product).Reference(item => item.Category).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductRequest request)
    {
        var product = await context.Products.FindAsync(id);

        if (product is null)
        {
            return NotFound(new { message = "Produto nao encontrado." });
        }

        var categoryExists = await context.Categories.AnyAsync(category => category.Id == request.CategoryId);

        if (!categoryExists)
        {
            return BadRequest(new { message = "Categoria informada nao existe." });
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.IsActive = request.IsActive;
        product.CategoryId = request.CategoryId;

        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await context.Products.FindAsync(id);

        if (product is null)
        {
            return NotFound(new { message = "Produto nao encontrado." });
        }

        product.IsActive = false;
        await context.SaveChangesAsync();

        return NoContent();
    }

    private static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty
        };
    }
}
