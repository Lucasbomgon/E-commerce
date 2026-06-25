using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Api.Dtos.Products;

public class ProductRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 99999999.99)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}
