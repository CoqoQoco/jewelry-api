namespace jewelry.Model.Sale.ExportShipment.Common;

public class ItemDto
{
    public long Id { get; set; }
    public int ItemNo { get; set; }
    public int SortOrder { get; set; }
    public string StockNumber { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductNumber { get; set; }
    public string? Description { get; set; }
    public decimal? GoldWeight { get; set; }
    public decimal? StoneWeight { get; set; }
    public decimal? DiamondWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? Qty { get; set; }
    public decimal? TagPrice { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Amount { get; set; }
    public string? ImagePath { get; set; }
    public int? ParcelNo { get; set; }
}
