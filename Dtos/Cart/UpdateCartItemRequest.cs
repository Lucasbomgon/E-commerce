using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Api.Dtos.Cart;

public class UpdateCartItemRequest
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
