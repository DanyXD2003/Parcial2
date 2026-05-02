using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.DTOs.Sales;
using Parcial2.Models;

namespace Parcial2.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SalesController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleDto dto)
    {
        if (!dto.Items.Any())
            return BadRequest(new { message = "La venta debe tener al menos un ítem" });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var sale = new Sale { UserId = userId, SaleDate = DateTime.UtcNow };
            _db.Sales.Add(sale);

            foreach (var item in dto.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product == null || !product.IsActive)
                    return BadRequest(new { message = $"Producto {item.ProductId} no encontrado" });
                if (product.Stock < item.Quantity)
                    return BadRequest(new { message = $"Stock insuficiente para '{product.Name}'. Disponible: {product.Stock}" });

                product.Stock     -= item.Quantity;
                product.UpdatedAt  = DateTime.UtcNow;

                sale.SaleItems.Add(new SaleItem
                {
                    ProductId = product.Id,
                    Quantity  = item.Quantity,
                    UnitPrice = product.Price
                });
                sale.TotalAmount += product.Price * item.Quantity;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return CreatedAtAction(nameof(GetById), new { id = sale.Id }, await BuildResponseAsync(sale.Id));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await BuildResponseAsync(id);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? date)
    {
        var query = _db.Sales
            .Include(s => s.User)
            .Include(s => s.SaleItems).ThenInclude(si => si.Product)
            .AsQueryable();

        if (date.HasValue)
        {
            var day = date.Value.Date;
            query = query.Where(s => s.SaleDate.Date == day);
        }

        var sales = await query.OrderByDescending(s => s.SaleDate).ToListAsync();
        return Ok(sales.Select(MapToResponse));
    }

    private async Task<SaleResponseDto?> BuildResponseAsync(int saleId)
    {
        var sale = await _db.Sales
            .Include(s => s.User)
            .Include(s => s.SaleItems).ThenInclude(si => si.Product)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        return sale == null ? null : MapToResponse(sale);
    }

    private static SaleResponseDto MapToResponse(Sale sale) => new()
    {
        Id              = sale.Id,
        CashierUsername = sale.User.Username,
        TotalAmount     = sale.TotalAmount,
        Status          = sale.Status,
        SaleDate        = sale.SaleDate,
        Items           = sale.SaleItems.Select(si => new SaleItemResponseDto
        {
            ProductId   = si.ProductId,
            ProductName = si.Product.Name,
            Quantity    = si.Quantity,
            UnitPrice   = si.UnitPrice
        }).ToList()
    };
}
