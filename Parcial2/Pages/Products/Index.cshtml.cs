using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.Models;

namespace Parcial2.Pages.Products;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<Product> Products { get; set; } = new();

    public async Task OnGetAsync()
    {
        Products = await _db.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null || !product.IsActive)
        {
            TempData["Error"] = "Producto no encontrado";
            return RedirectToPage();
        }

        var userId = await GetDefaultUserIdAsync();
        var cart = await _db.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

        if (cart == null)
        {
            cart = new Parcial2.Models.Cart { UserId = userId };
            _db.Carts.Add(cart);
        }

        var existing = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (existing != null)
            existing.Quantity += quantity;
        else
            cart.CartItems.Add(new CartItem { ProductId = productId, Quantity = quantity });

        await _db.SaveChangesAsync();
        TempData["Success"] = $"'{product.Name}' agregado al carrito";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.IsActive  = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Producto '{product.Name}' eliminado";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAdjustStockAsync(int productId, int quantity, string reason)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return NotFound();

        if (product.Stock + quantity < 0)
        {
            TempData["Error"] = "El stock no puede quedar negativo";
            return RedirectToPage();
        }

        product.Stock    += quantity;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Stock de '{product.Name}' actualizado a {product.Stock} unidades ({reason})";
        return RedirectToPage();
    }

    private async Task<int> GetDefaultUserIdAsync()
    {
        var user = await _db.Users.FirstAsync();
        return user.Id;
    }
}
