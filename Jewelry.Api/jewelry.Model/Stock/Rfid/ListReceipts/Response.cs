namespace jewelry.Model.Stock.Rfid.ListReceipts;

public class Response
{
    public int Total { get; set; }

    public List<Item> Items { get; set; } = new();
}

public class Item
{
    public string ReceiptNumber { get; set; } = null!;

    public DateTime? ReceiptDate { get; set; }

    public int TotalPieces { get; set; }

    public int BoundPieces { get; set; }

    public int RemainingPieces { get; set; }
}
