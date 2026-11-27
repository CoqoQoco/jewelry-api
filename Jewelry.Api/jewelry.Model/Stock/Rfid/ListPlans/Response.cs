namespace jewelry.Model.Stock.Rfid.ListPlans;

public class Response
{
    public int Total { get; set; }

    public List<Item> Items { get; set; } = new();
}

public class Item
{
    public string Wo { get; set; } = null!;

    public int WoNumber { get; set; }

    public string WoText { get; set; } = null!;

    public string? ProductNameTh { get; set; }

    public string? ProductNameEn { get; set; }

    public string? ImagePath { get; set; }

    public string? Mold { get; set; }

    public int LotCount { get; set; }

    public int TotalUnits { get; set; }

    public int UnboundUnits { get; set; }

    public DateTime? LastReceiptDate { get; set; }
}
