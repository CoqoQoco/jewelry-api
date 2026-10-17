namespace jewelry.Model.Stock.Rfid.Bind;

public class Response
{
    public long Id { get; set; }

    public string Epc { get; set; } = null!;

    public string? Tid { get; set; }

    public string StockNumber { get; set; } = null!;

    public string ProductCode { get; set; } = null!;
}
