using Parcial2.DTOs.Sales;

namespace Parcial2.DTOs.Reports;

public class DailySalesReportDto
{
    public DateTime Date { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<SaleResponseDto> Sales { get; set; } = new();
}
