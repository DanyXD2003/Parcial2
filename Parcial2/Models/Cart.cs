namespace Parcial2.Models;

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
