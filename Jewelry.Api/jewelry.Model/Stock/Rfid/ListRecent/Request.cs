namespace jewelry.Model.Stock.Rfid.ListRecent;

public class Request
{
    public long? SinceId { get; set; }

    public int Take { get; set; } = 50;
}
