namespace Parcial2.DTOs.Reports;

public class WeeklySalesReportDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<DailySummaryDto> DailyBreakdown { get; set; } = new();
}

public class DailySummaryDto
{
    public DateTime Date { get; set; }
    public int SalesCount { get; set; }
    public decimal Revenue { get; set; }
}
