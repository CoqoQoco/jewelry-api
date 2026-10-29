namespace jewelry.Model.Stock.Rfid.Unbind;

public class Request
{
    public string? Epc { get; set; }

    public string? StockNumber { get; set; }

    public string? ProductCode { get; set; }

    public long? UnitId { get; set; }
}
