namespace jewelry.Model.Sale.ExportShipment.Upsert;

public class Request
{
    public string? Running { get; set; }
    public string? CustomNumber { get; set; }
    public DateTimeOffset? DocumentDate { get; set; }
    public string? ConsigneeName { get; set; }
    public string? ConsigneeAddress { get; set; }
    public string? EventName { get; set; }
    public string? BoothNo { get; set; }
    public string? AttnName { get; set; }
    public string? AttnPassport { get; set; }
    public string? AttnTel { get; set; }
    public string? Incoterm { get; set; }
    public string? OriginCountry { get; set; }
    public string? Currency { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? PricePercent { get; set; }
    public int? ParcelCount { get; set; }
    public string? Remark { get; set; }
    public List<ItemRequest> Items { get; set; } = new();
}

public class ItemRequest
{
    public long? Id { get; set; }
    public string StockNumber { get; set; }
    public string? Description { get; set; }
    public decimal Qty { get; set; } = 1;
    public decimal? UnitPrice { get; set; }
    public int ParcelNo { get; set; } = 1;
    public int SortOrder { get; set; }
}
