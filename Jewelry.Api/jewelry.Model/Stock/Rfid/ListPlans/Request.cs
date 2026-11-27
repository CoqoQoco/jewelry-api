namespace jewelry.Model.Stock.Rfid.ListPlans;

public class Request
{
    public string? Search { get; set; }

    public DateTimeOffset? ReceiptDateFrom { get; set; }

    public DateTimeOffset? ReceiptDateTo { get; set; }

    public bool OnlyIncomplete { get; set; } = true;

    public int Skip { get; set; }

    public int Take { get; set; } = 50;
}
