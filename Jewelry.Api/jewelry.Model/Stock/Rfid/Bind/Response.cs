namespace jewelry.Model.Stock.Rfid.Bind;

public class Response
{
    public long Id { get; set; }

    public string Epc { get; set; } = null!;

    public string? Tid { get; set; }

    public string StockNumber { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public long? UnitId { get; set; }

    public int? UnitNo { get; set; }

    public string? UnitStatus { get; set; }

    public string? UnitLocationCode { get; set; }

    public string? UnitLocationName { get; set; }

    public decimal LotQty { get; set; }
}
