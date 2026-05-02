using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.Models;

namespace Parcial2.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<Sale> DailySales        { get; set; } = new();
    public List<Sale> WeeklySales       { get; set; } = new();
    public List<Product> AllProducts    { get; set; } = new();
    public List<Product> LowStockProducts { get; set; } = new();
    public List<DayRow> WeeklyBreakdown { get; set; } = new();

    public DateTime Date      { get; set; }
    public DateTime WeekStart { get; set; }

    public record DayRow(DateTime Date, int Count, decimal Revenue);

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public DateTime? date { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public DateTime? weekStart { get; set; }

    public async Task OnGetAsync()
    {
        Date      = (date      ?? DateTime.UtcNow).Date;
        WeekStart = (weekStart ?? GetMonday(DateTime.UtcNow.Date)).Date;

        // Daily
        DailySales = await _db.Sales
            .Include(s => s.User)
            .Include(s => s.SaleItems).ThenInclude(si => si.Product)
            .Where(s => s.SaleDate >= Date && s.SaleDate < Date.AddDays(1) && s.Status == "Completed")
            .OrderBy(s => s.SaleDate)
            .ToListAsync();

        // Weekly
        var weekEnd = WeekStart.AddDays(7);
        WeeklySales = await _db.Sales
            .Where(s => s.SaleDate >= WeekStart && s.SaleDate < weekEnd && s.Status == "Completed")
            .ToListAsync();

        WeeklyBreakdown = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var day   = WeekStart.AddDays(i);
                var sales = WeeklySales.Where(s => s.SaleDate.Date == day).ToList();
                return new DayRow(day, sales.Count, sales.Sum(s => s.TotalAmount));
            })
            .ToList();

        // Inventory
        AllProducts = await _db.Products
            .Where(p => p.IsActive)
            .ToListAsync();

        LowStockProducts = AllProducts
            .Where(p => p.Stock <= p.LowStockThreshold)
            .ToList();
    }

    private static DateTime GetMonday(DateTime d)
    {
        var dow = d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek;
        return d.AddDays(-(dow - 1));
    }
}
