namespace jewelry.Model.Stock.Rfid.ListUnits;

public class Response
{
    public int Total { get; set; }

    public List<Item> Items { get; set; } = new();
}

public class Item
{
    public long UnitId { get; set; }

    public int UnitNo { get; set; }

    public decimal LotQty { get; set; }

    public string UnitStatus { get; set; } = null!;

    public string StockNumber { get; set; } = null!;

    public string? StockNumberOrigin { get; set; }

    public string ProductCode { get; set; } = null!;

    public string? ProductNumber { get; set; }

    public string? ProductNameTh { get; set; }

    public string? ProductNameEn { get; set; }

    public string? ProductType { get; set; }

    public string? ProductTypeName { get; set; }

    public string LocationCode { get; set; } = null!;

    public string? LocationName { get; set; }

    public string? ImagePath { get; set; }

    public string? ReceiptNumber { get; set; }

    public DateTime? ReceiptDate { get; set; }

    public string? Wo { get; set; }

    public int? WoNumber { get; set; }

    public string? Epc { get; set; }

    public DateTime? BoundDate { get; set; }
}
