namespace jewelry.Model.Stock.Rfid.ListRecent;

public class Response
{
    public long LastId { get; set; }

    public List<Item> Items { get; set; } = new();
}

public class Item
{
    public long Id { get; set; }

    public string Epc { get; set; } = null!;

    public string StockNumber { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public string? ProductName { get; set; }

    public string? ReceiptNumber { get; set; }

    public bool IsActive { get; set; }

    public long? UnitId { get; set; }

    public int? UnitNo { get; set; }

    public string? UnbindReason { get; set; }

    public string? BoundBy { get; set; }

    public DateTime BoundDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
