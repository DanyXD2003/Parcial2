using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.Models;

namespace Parcial2.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public int TodaySalesCount     { get; set; }
    public decimal TodayRevenue    { get; set; }
    public int TotalActiveProducts { get; set; }
    public int LowStockCount       { get; set; }
    public List<Sale> RecentSales  { get; set; } = new();

    public async Task OnGetAsync()
    {
        var today = DateTime.UtcNow.Date;

        var todaySales = await _db.Sales
            .Where(s => s.SaleDate >= today && s.SaleDate < today.AddDays(1) && s.Status == "Completed")
            .ToListAsync();

        TodaySalesCount     = todaySales.Count;
        TodayRevenue        = todaySales.Sum(s => s.TotalAmount);
        TotalActiveProducts = await _db.Products.CountAsync(p => p.IsActive);
        LowStockCount       = await _db.Products.CountAsync(p => p.IsActive && p.Stock <= p.LowStockThreshold);

        RecentSales = await _db.Sales
            .Include(s => s.User)
            .Include(s => s.SaleItems)
            .OrderByDescending(s => s.SaleDate)
            .Take(6)
            .ToListAsync();
    }
}
