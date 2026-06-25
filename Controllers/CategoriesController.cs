using Ecommerce.Api.Data;
using Ecommerce.Api.Dtos.Categories;
using Ecommerce.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetAll()
    {
        var categories = await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => ToResponse(category))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetById(int id)
    {
        var category = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == id);

        if (category is null)
        {
            return NotFound(new { message = "Categoria nao encontrada." });
        }

        return Ok(ToResponse(category));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim()
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, ToResponse(category));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await context.Categories.FindAsync(id);

        if (category is null)
        {
            return NotFound(new { message = "Categoria nao encontrada." });
        }

        category.Name = request.Name.Trim();
        category.Description = request.Description.Trim();

        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await context.Categories
            .Include(category => category.Products)
            .FirstOrDefaultAsync(category => category.Id == id);

        if (category is null)
        {
            return NotFound(new { message = "Categoria nao encontrada." });
        }

        if (category.Products.Count > 0)
        {
            return Conflict(new { message = "Nao e possivel remover uma categoria com produtos vinculados." });
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private static CategoryResponse ToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }
}
