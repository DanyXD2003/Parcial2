namespace Parcial2.DTOs.Sales;

public class SaleResponseDto
{
    public int Id { get; set; }
    public string CashierUsername { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public List<SaleItemResponseDto> Items { get; set; } = new();
}

public class SaleItemResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal => Quantity * UnitPrice;
}
