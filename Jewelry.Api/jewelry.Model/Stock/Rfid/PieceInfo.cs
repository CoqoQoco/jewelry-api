namespace jewelry.Model.Stock.Rfid;

public class PieceInfo
{
    public bool Found { get; set; }

    public string? Epc { get; set; }

    public string? Tid { get; set; }

    public string? StockNumber { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductName { get; set; }

    public string? ProductNumber { get; set; }

    public string? Status { get; set; }

    public string? LocationCode { get; set; }

    public string? ImagePath { get; set; }

    public long? UnitId { get; set; }

    public int? UnitNo { get; set; }

    public string? UnitStatus { get; set; }

    public string? UnitLocationCode { get; set; }

    public string? UnitLocationName { get; set; }

    public decimal LotQty { get; set; }

    public bool SoldMismatch { get; set; }
}
