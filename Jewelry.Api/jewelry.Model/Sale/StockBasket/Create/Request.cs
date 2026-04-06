namespace jewelry.Model.Sale.StockBasket.Create;

public class Request
{
    public string? Running { get; set; }
    public string BasketName { get; set; }
    public DateTimeOffset? EventDate { get; set; }
    public string? Responsible { get; set; }
    public string? Remark { get; set; }
}
