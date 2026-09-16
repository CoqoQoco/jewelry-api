namespace jewelry.Model.Stock.StockConvert.PendingForSaleOrder;

public class Response
{
    public string Running { get; set; }
    public string? SoLineKey { get; set; }
    public string ResultStockNumber { get; set; }
    public string? ProductNumber { get; set; }
    public string? ProductNameEn { get; set; }
    public decimal QtyAvailable { get; set; }
    public DateTime? CompleteDate { get; set; }
}
