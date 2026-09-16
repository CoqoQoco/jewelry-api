namespace jewelry.Model.Stock.StockConvert.List;

public class Response
{
    public string Running { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public string? SoNumber { get; set; }
    public int SourceCount { get; set; }
    public string? SourceStockNumbers { get; set; }
    public string? ResultStockNumber { get; set; }
    public decimal ConvertCost { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreateBy { get; set; }
    public DateTime? CompleteDate { get; set; }
}
