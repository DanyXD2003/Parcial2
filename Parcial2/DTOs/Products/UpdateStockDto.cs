namespace Parcial2.DTOs.Products;

public class UpdateStockDto
{
    public int Quantity { get; set; }
    public string Reason { get; set; } = "adjustment";
}
