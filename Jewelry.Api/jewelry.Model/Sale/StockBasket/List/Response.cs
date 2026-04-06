namespace jewelry.Model.Sale.StockBasket.List;

public class Response
{
    public string Running { get; set; }
    public string BasketNumber { get; set; }
    public string BasketName { get; set; }
    public DateTime? EventDate { get; set; }
    public string? Responsible { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public int TotalItems { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreateBy { get; set; }
}
