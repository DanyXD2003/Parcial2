namespace Parcial2.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
