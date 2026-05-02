namespace Parcial2.DTOs.Cart;

public class CartResponseDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<CartItemResponseDto> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.Subtotal);
}

public class CartItemResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal => Quantity * UnitPrice;
}
