namespace Parcial2.DTOs.Sales;

public class CreateSaleDto
{
    public List<SaleItemDto> Items { get; set; } = new();
}
