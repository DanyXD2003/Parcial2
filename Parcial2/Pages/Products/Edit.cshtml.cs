using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Parcial2.Data;

namespace Parcial2.Pages.Products;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db) => _db = db;

    [BindProperty] public EditInput Input { get; set; } = new();
    public int CurrentStock { get; set; }

    public class EditInput
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría es obligatoria")]
        public string Category { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Price { get; set; }

        public int LowStockThreshold { get; set; } = 5;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null || !product.IsActive) return NotFound();

        CurrentStock = product.Stock;
        Input = new EditInput
        {
            Name              = product.Name,
            Description       = product.Description,
            Category          = product.Category,
            Price             = product.Price,
            LowStockThreshold = product.LowStockThreshold
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid) return Page();

        var product = await _db.Products.FindAsync(id);
        if (product == null || !product.IsActive) return NotFound();

        product.Name              = Input.Name;
        product.Description       = Input.Description;
        product.Category          = Input.Category;
        product.Price             = Input.Price;
        product.LowStockThreshold = Input.LowStockThreshold;
        product.UpdatedAt         = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Producto '{product.Name}' actualizado";
        return RedirectToPage("Index");
    }
}
