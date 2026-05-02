namespace Parcial2.DTOs.Reports;

public class InventoryReportDto
{
    public DateTime ReportDate { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public List<ProductStockDto> Products { get; set; } = new();
}

public class ProductStockDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsLowStock => Stock <= LowStockThreshold;
    public decimal Price { get; set; }
}
