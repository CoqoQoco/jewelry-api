namespace jewelry.Model.Sale.StockBasket.List;

public class Request
{
    public int Take { get; set; } = 10;
    public int Skip { get; set; } = 0;
    public string? BasketNumber { get; set; }
    public string? BasketName { get; set; }
    public int? Status { get; set; }
    public string? Responsible { get; set; }
    public DateTimeOffset? EventDateStart { get; set; }
    public DateTimeOffset? EventDateEnd { get; set; }
}
