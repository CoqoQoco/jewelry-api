namespace jewelry.Model.Stock.Rfid.ListByReceipt;

public class Response
{
    public string? ReceiptNumber { get; set; }

    public int Total { get; set; }

    public int Bound { get; set; }

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

    public string? Epc { get; set; }

    public string? Tid { get; set; }

    public DateTime? BoundDate { get; set; }

    public string? BoundBy { get; set; }
}
