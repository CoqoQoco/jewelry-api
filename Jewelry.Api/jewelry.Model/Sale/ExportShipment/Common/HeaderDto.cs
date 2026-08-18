namespace jewelry.Model.Sale.ExportShipment.Common;

public class HeaderDto
{
    public string Running { get; set; }
    public string DocumentNumber { get; set; }
    public string? CustomNumber { get; set; }
    public DateTime DocumentDate { get; set; }
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
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreateBy { get; set; }
    public List<ItemDto> Items { get; set; } = new();
}
