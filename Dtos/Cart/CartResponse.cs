namespace Ecommerce.Api.Dtos.Cart;

public class CartResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<CartItemResponse> Items { get; set; } = [];
}
