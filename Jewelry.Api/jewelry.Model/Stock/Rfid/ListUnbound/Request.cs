namespace jewelry.Model.Stock.Rfid.ListUnbound;

public class Request
{
    public string? Search { get; set; }

    public string? LocationCode { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; } = 20;
}
