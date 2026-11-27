namespace jewelry.Model.Stock.Rfid.ListUnits;

public class Request
{
    public string? StockNumber { get; set; }

    public string? StockNumberOrigin { get; set; }

    public string? ProductNumber { get; set; }

    public string[]? LocationCodes { get; set; }

    public string[]? ProductType { get; set; }

    public string[]? Gold { get; set; }

    public string[]? GoldSize { get; set; }

    public string[]? Materials { get; set; }

    public bool? MaterialMatchAll { get; set; }

    public string[]? ReceiptType { get; set; }

    public string? ReceiptNumber { get; set; }

    public DateTimeOffset? ReceiptDateFrom { get; set; }

    public DateTimeOffset? ReceiptDateTo { get; set; }

    public string? Mold { get; set; }

    public string? ProductName { get; set; }

    public string? WoText { get; set; }

    public string? Wo { get; set; }

    public int? WoNumber { get; set; }

    public string? Size { get; set; }

    // unbound (default) | bound | all
    public string? TagStatus { get; set; }

    public long[]? UnitIds { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; } = 50;
}
