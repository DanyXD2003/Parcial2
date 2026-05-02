using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Parcial2.Data;
using Parcial2.Models;

namespace Parcial2.Pages.Sales;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public List<Sale> Sales { get; set; } = new();

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public DateTime? FilterDate { get; set; }

    public async Task OnGetAsync()
    {
        var query = _db.Sales
            .Include(s => s.User)
            .Include(s => s.SaleItems).ThenInclude(si => si.Product)
            .AsQueryable();

        if (FilterDate.HasValue)
        {
            var day = FilterDate.Value.Date;
            query = query.Where(s => s.SaleDate >= day && s.SaleDate < day.AddDays(1));
        }

        Sales = await query.OrderByDescending(s => s.SaleDate).Take(100).ToListAsync();
    }
}
