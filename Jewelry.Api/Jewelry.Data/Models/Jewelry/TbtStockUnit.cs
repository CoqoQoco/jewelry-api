using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockUnit
{
    public long Id { get; set; }

    public string StockNumber { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public int UnitNo { get; set; }

    public string LocationCode { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? ReceiptNumber { get; set; }

    public DateTime? ReceiptDate { get; set; }

    public string? SoldRef { get; set; }

    public DateTime? SoldDate { get; set; }

    public string? ReservedRefType { get; set; }

    public string? ReservedRef { get; set; }

    public long? ReservedLineId { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }
}
