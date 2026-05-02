using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.DTOs.Reports;
using Parcial2.DTOs.Sales;

namespace Parcial2.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db) => _db = db;

    [HttpGet("sales/daily")]
    public async Task<IActionResult> DailySales([FromQuery] DateTime? date)
    {
        var day = (date ?? DateTime.UtcNow).Date;

        var sales = await _db.Sales
            .Include(s => s.User)
            .Include(s => s.SaleItems).ThenInclude(si => si.Product)
            .Where(s => s.SaleDate.Date == day && s.Status == "Completed")
            .ToListAsync();

        return Ok(new DailySalesReportDto
        {
            Date         = day,
            TotalSales   = sales.Count,
            TotalRevenue = sales.Sum(s => s.TotalAmount),
            Sales        = sales.Select(s => new SaleResponseDto
            {
                Id              = s.Id,
                CashierUsername = s.User.Username,
                TotalAmount     = s.TotalAmount,
                Status          = s.Status,
                SaleDate        = s.SaleDate,
                Items           = s.SaleItems.Select(si => new SaleItemResponseDto
                {
                    ProductId   = si.ProductId,
                    ProductName = si.Product.Name,
                    Quantity    = si.Quantity,
                    UnitPrice   = si.UnitPrice
                }).ToList()
            }).ToList()
        });
    }

    [HttpGet("sales/weekly")]
    public async Task<IActionResult> WeeklySales([FromQuery] DateTime? weekStart)
    {
        var start = weekStart.HasValue
            ? weekStart.Value.Date
            : DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        var end = start.AddDays(6);

        var sales = await _db.Sales
            .Where(s => s.SaleDate.Date >= start && s.SaleDate.Date <= end && s.Status == "Completed")
            .ToListAsync();

        var breakdown = sales
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new DailySummaryDto
            {
                Date       = g.Key,
                SalesCount = g.Count(),
                Revenue    = g.Sum(s => s.TotalAmount)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return Ok(new WeeklySalesReportDto
        {
            WeekStart      = start,
            WeekEnd        = end,
            TotalSales     = sales.Count,
            TotalRevenue   = sales.Sum(s => s.TotalAmount),
            DailyBreakdown = breakdown
        });
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory()
    {
        var products = await _db.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .ToListAsync();

        var stocks = products.Select(p => new ProductStockDto
        {
            Id                = p.Id,
            Name              = p.Name,
            Category          = p.Category,
            Stock             = p.Stock,
            LowStockThreshold = p.LowStockThreshold,
            Price             = p.Price
        }).ToList();

        return Ok(new InventoryReportDto
        {
            ReportDate    = DateTime.UtcNow,
            TotalProducts = products.Count,
            LowStockCount = stocks.Count(s => s.IsLowStock),
            Products      = stocks
        });
    }

    [HttpGet("inventory/low-stock")]
    public async Task<IActionResult> LowStock()
    {
        var products = await _db.Products
            .Where(p => p.IsActive && p.Stock <= p.LowStockThreshold)
            .OrderBy(p => p.Stock)
            .ToListAsync();

        var stocks = products.Select(p => new ProductStockDto
        {
            Id                = p.Id,
            Name              = p.Name,
            Category          = p.Category,
            Stock             = p.Stock,
            LowStockThreshold = p.LowStockThreshold,
            Price             = p.Price
        }).ToList();

        return Ok(new { count = stocks.Count, products = stocks });
    }
}
