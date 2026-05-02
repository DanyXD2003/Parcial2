using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.DTOs.Cart;
using Parcial2.DTOs.Sales;
using Parcial2.Models;

namespace Parcial2.Controllers;

[ApiController]
[Route("api/carts")]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CartsController(AppDbContext db) => _db = db;

    [HttpGet("my")]
    public async Task<IActionResult> GetMyCart()
    {
        var cart = await GetOrCreateCartAsync();
        return Ok(MapToResponse(cart));
    }

    [HttpPost("my/items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto)
    {
        var product = await _db.Products.FindAsync(dto.ProductId);
        if (product == null || !product.IsActive)
            return NotFound(new { message = "Producto no encontrado" });
        if (dto.Quantity <= 0)
            return BadRequest(new { message = "La cantidad debe ser mayor a 0" });

        var cart     = await GetOrCreateCartAsync();
        var existing = cart.CartItems.FirstOrDefault(ci => ci.ProductId == dto.ProductId);

        if (existing != null)
            existing.Quantity += dto.Quantity;
        else
            cart.CartItems.Add(new CartItem { ProductId = dto.ProductId, Quantity = dto.Quantity });

        await _db.SaveChangesAsync();
        return Ok(MapToResponse(cart));
    }

    [HttpPut("my/items/{productId}")]
    public async Task<IActionResult> UpdateItem(int productId, [FromBody] UpdateCartItemDto dto)
    {
        if (dto.Quantity <= 0)
            return BadRequest(new { message = "La cantidad debe ser mayor a 0" });

        var cart = await GetOrCreateCartAsync();
        var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (item == null)
            return NotFound(new { message = "Ítem no encontrado en el carrito" });

        item.Quantity = dto.Quantity;
        await _db.SaveChangesAsync();
        return Ok(MapToResponse(cart));
    }

    [HttpDelete("my/items/{productId}")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var cart = await GetOrCreateCartAsync();
        var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (item == null)
            return NotFound(new { message = "Ítem no encontrado en el carrito" });

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(MapToResponse(cart));
    }

    [HttpDelete("my")]
    public async Task<IActionResult> ClearCart()
    {
        var cart = await GetOrCreateCartAsync();
        _db.CartItems.RemoveRange(cart.CartItems);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Carrito vaciado" });
    }

    [HttpPost("my/checkout")]
    public async Task<IActionResult> Checkout()
    {
        var cart = await GetOrCreateCartAsync();
        if (!cart.CartItems.Any())
            return BadRequest(new { message = "El carrito está vacío" });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var sale = new Sale { UserId = userId, SaleDate = DateTime.UtcNow };
            _db.Sales.Add(sale);

            foreach (var cartItem in cart.CartItems)
            {
                var product = await _db.Products.FindAsync(cartItem.ProductId);
                if (product == null || !product.IsActive)
                    return BadRequest(new { message = $"Producto {cartItem.ProductId} no disponible" });
                if (product.Stock < cartItem.Quantity)
                    return BadRequest(new { message = $"Stock insuficiente para '{product.Name}'. Disponible: {product.Stock}" });

                product.Stock     -= cartItem.Quantity;
                product.UpdatedAt  = DateTime.UtcNow;

                sale.SaleItems.Add(new SaleItem
                {
                    ProductId = product.Id,
                    Quantity  = cartItem.Quantity,
                    UnitPrice = product.Price
                });
                sale.TotalAmount += product.Price * cartItem.Quantity;
            }

            cart.Status = "CheckedOut";
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var response = await _db.Sales
                .Include(s => s.User)
                .Include(s => s.SaleItems).ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.Id == sale.Id);

            return Ok(new SaleResponseDto
            {
                Id              = response!.Id,
                CashierUsername = response.User.Username,
                TotalAmount     = response.TotalAmount,
                Status          = response.Status,
                SaleDate        = response.SaleDate,
                Items           = response.SaleItems.Select(si => new SaleItemResponseDto
                {
                    ProductId   = si.ProductId,
                    ProductName = si.Product.Name,
                    Quantity    = si.Quantity,
                    UnitPrice   = si.UnitPrice
                }).ToList()
            });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<Cart> GetOrCreateCartAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var cart = await _db.Carts
            .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        return cart;
    }

    private static CartResponseDto MapToResponse(Cart cart) => new()
    {
        Id        = cart.Id,
        Status    = cart.Status,
        CreatedAt = cart.CreatedAt,
        Items     = cart.CartItems.Select(ci => new CartItemResponseDto
        {
            ProductId   = ci.ProductId,
            ProductName = ci.Product?.Name ?? string.Empty,
            UnitPrice   = ci.Product?.Price ?? 0,
            Quantity    = ci.Quantity
        }).ToList()
    };
}
