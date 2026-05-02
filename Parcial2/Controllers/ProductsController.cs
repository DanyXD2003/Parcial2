using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.DTOs.Products;
using Parcial2.Models;

namespace Parcial2.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _db.Products
            .Where(p => p.IsActive)
            .Select(p => ToDto(p))
            .ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null || !product.IsActive)
            return NotFound();
        return Ok(ToDto(product));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var product = new Product
        {
            Name              = dto.Name,
            Description       = dto.Description,
            Category          = dto.Category,
            Price             = dto.Price,
            Stock             = dto.Stock,
            LowStockThreshold = dto.LowStockThreshold
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDto(product));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null || !product.IsActive)
            return NotFound();

        if (dto.Name != null)              product.Name              = dto.Name;
        if (dto.Description != null)       product.Description       = dto.Description;
        if (dto.Category != null)          product.Category          = dto.Category;
        if (dto.Price.HasValue)            product.Price             = dto.Price.Value;
        if (dto.LowStockThreshold.HasValue) product.LowStockThreshold = dto.LowStockThreshold.Value;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToDto(product));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null || !product.IsActive)
            return NotFound();

        product.IsActive  = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null || !product.IsActive)
            return NotFound();

        product.Stock     += dto.Quantity;
        product.UpdatedAt  = DateTime.UtcNow;

        if (product.Stock < 0)
            return BadRequest(new { message = "El stock no puede ser negativo" });

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Stock actualizado ({dto.Reason})", product.Stock });
    }

    private static ProductResponseDto ToDto(Product p) => new()
    {
        Id                = p.Id,
        Name              = p.Name,
        Description       = p.Description,
        Category          = p.Category,
        Price             = p.Price,
        Stock             = p.Stock,
        LowStockThreshold = p.LowStockThreshold,
        IsActive          = p.IsActive,
        CreatedAt         = p.CreatedAt,
        UpdatedAt         = p.UpdatedAt
    };
}
