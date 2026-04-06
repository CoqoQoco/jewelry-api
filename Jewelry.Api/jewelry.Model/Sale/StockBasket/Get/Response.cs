namespace jewelry.Model.Sale.StockBasket.Get;

public class Response
{
    public string Running { get; set; }
    public string BasketNumber { get; set; }
    public string BasketName { get; set; }
    public DateTime? EventDate { get; set; }
    public string? Responsible { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public string? Remark { get; set; }
    public DateTime? CheckoutDate { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreateBy { get; set; }
    public List<BasketItemResponse> Items { get; set; } = new();
}

public class BasketItemResponse
{
    public long Id { get; set; }
    public string StockNumber { get; set; }
    public string? ProductNumber { get; set; }
    public string? ProductNameTh { get; set; }
    public string? ProductNameEn { get; set; }
    public string? ProductType { get; set; }
    public string? ProductionTypeSize { get; set; }
    public string? ImagePath { get; set; }
    public string Status { get; set; }
    public string? StatusName { get; set; }
    public DateTime CreateDate { get; set; }
}
