using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Parcial2.Data;
using Parcial2.Models;

namespace Parcial2.Pages.Products;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty] public CreateInput Input { get; set; } = new();

    public class CreateInput
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría es obligatoria")]
        public string Category { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; }

        public int LowStockThreshold { get; set; } = 5;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var product = new Product
        {
            Name              = Input.Name,
            Description       = Input.Description,
            Category          = Input.Category,
            Price             = Input.Price,
            Stock             = Input.Stock,
            LowStockThreshold = Input.LowStockThreshold
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Producto '{product.Name}' creado exitosamente";
        return RedirectToPage("Index");
    }
}
