namespace jewelry.Model.Stock.StockConvert.Get;

public class Response
{
    public string Running { get; set; }
    public string? SoNumber { get; set; }
    public string? SoLineKey { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public decimal ConvertCost { get; set; }
    public string? Remark { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CompleteDate { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? UpdateBy { get; set; }
    public List<ItemDto> Sources { get; set; } = new();
    public ItemDto? Result { get; set; }
}

public class ItemDto
{
    public long Id { get; set; }
    public string StockNumber { get; set; }
    public string? ProductCode { get; set; }
    public string? SkuCode { get; set; }
    public string? LocationCode { get; set; }
    public decimal Qty { get; set; }
    public decimal? ProductCost { get; set; }
    public string? ProductNumber { get; set; }
    public string? ProductNameEn { get; set; }
    public string? ProductNameTh { get; set; }
}
