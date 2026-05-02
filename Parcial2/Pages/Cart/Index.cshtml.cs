using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.Models;

namespace Parcial2.Pages.Cart;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<CartItemView> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.Quantity * i.UnitPrice);

    public record CartItemView(int ProductId, string ProductName, string Category,
        decimal UnitPrice, int Quantity, int MaxStock);

    public async Task OnGetAsync()
    {
        await LoadItemsAsync();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int productId, int quantity)
    {
        if (quantity <= 0)
            return await OnPostRemoveItemAsync(productId);

        var cart = await GetOrCreateCartAsync();
        var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (item != null)
        {
            item.Quantity = quantity;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveItemAsync(int productId)
    {
        var cart = await GetOrCreateCartAsync();
        var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (item != null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "Item eliminado del carrito";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearCartAsync()
    {
        var cart = await GetOrCreateCartAsync();
        _db.CartItems.RemoveRange(cart.CartItems);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Carrito vaciado";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        var cart = await GetOrCreateCartAsync();
        if (!cart.CartItems.Any())
        {
            TempData["Error"] = "El carrito esta vacio";
            return RedirectToPage();
        }

        var userId = await GetDefaultUserIdAsync();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var sale = new Sale { UserId = userId, SaleDate = DateTime.UtcNow };
            _db.Sales.Add(sale);

            foreach (var cartItem in cart.CartItems)
            {
                var product = await _db.Products.FindAsync(cartItem.ProductId);
                if (product == null || !product.IsActive)
                {
                    await tx.RollbackAsync();
                    TempData["Error"] = "Uno o mas productos ya no estan disponibles";
                    return RedirectToPage();
                }
                if (product.Stock < cartItem.Quantity)
                {
                    await tx.RollbackAsync();
                    TempData["Error"] = $"Stock insuficiente para '{product.Name}'. Disponible: {product.Stock}";
                    return RedirectToPage();
                }

                product.Stock    -= cartItem.Quantity;
                product.UpdatedAt = DateTime.UtcNow;

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

            TempData["Success"] = $"Venta #{sale.Id} completada por Q{sale.TotalAmount:N2}";
            return RedirectToPage("/Sales/Index");
        }
        catch
        {
            await tx.RollbackAsync();
            TempData["Error"] = "Error al procesar la venta. Intenta nuevamente.";
            return RedirectToPage();
        }
    }

    private async Task LoadItemsAsync()
    {
        var userId = await GetDefaultUserIdAsync();
        var cart = await _db.Carts
            .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

        if (cart != null)
        {
            Items = cart.CartItems
                .Select(ci => new CartItemView(
                    ci.ProductId,
                    ci.Product.Name,
                    ci.Product.Category,
                    ci.Product.Price,
                    ci.Quantity,
                    ci.Product.Stock + ci.Quantity))
                .ToList();
        }
    }

    private async Task<Models.Cart> GetOrCreateCartAsync()
    {
        var userId = await GetDefaultUserIdAsync();
        var cart = await _db.Carts
            .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

        if (cart == null)
        {
            cart = new Models.Cart { UserId = userId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }
        return cart;
    }

    private async Task<int> GetDefaultUserIdAsync()
    {
        var user = await _db.Users.FirstAsync();
        return user.Id;
    }
}
