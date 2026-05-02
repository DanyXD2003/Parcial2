namespace Parcial2.Models;

public class Sale
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
