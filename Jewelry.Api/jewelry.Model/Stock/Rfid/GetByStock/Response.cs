namespace jewelry.Model.Stock.Rfid.GetByStock;

public class Response
{
    public bool Found { get; set; }

    public string? Epc { get; set; }

    public string? Tid { get; set; }

    public DateTime? BoundDate { get; set; }

    public decimal LotQty { get; set; }

    public List<Unit> Units { get; set; } = new();
}

public class Unit
{
    public long UnitId { get; set; }

    public int UnitNo { get; set; }

    public string Status { get; set; } = null!;

    public string? LocationCode { get; set; }

    public string? LocationName { get; set; }

    public string? ReceiptNumber { get; set; }

    public DateTime? ReceiptDate { get; set; }

    public string? SoldRef { get; set; }

    public DateTime? SoldDate { get; set; }

    public string? ReservedRefType { get; set; }

    public string? ReservedRef { get; set; }

    public long? ReservedLineId { get; set; }

    public string? Epc { get; set; }

    public string? Tid { get; set; }

    public DateTime? BoundDate { get; set; }

    public string? BoundBy { get; set; }
}
