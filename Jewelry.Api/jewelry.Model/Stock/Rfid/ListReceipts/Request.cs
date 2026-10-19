namespace jewelry.Model.Stock.Rfid.ListReceipts;

public class Request
{
    public string? Search { get; set; }

    public bool OnlyIncomplete { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; } = 20;
}
