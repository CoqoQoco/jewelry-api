namespace jewelry.Model.Stock.Rfid.ListUnbound;

public class Response
{
    public int Total { get; set; }

    public List<Item> Items { get; set; } = new();
}

public class Item
{
    public string StockNumber { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public string? ProductName { get; set; }

    public string? ProductNumber { get; set; }

    public string? Status { get; set; }

    public string? LocationCode { get; set; }

    public string? ImagePath { get; set; }

    public string? ReceiptNumber { get; set; }
}
