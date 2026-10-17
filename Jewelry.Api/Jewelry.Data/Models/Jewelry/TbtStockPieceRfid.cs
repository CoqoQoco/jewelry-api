using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockPieceRfid
{
    public long Id { get; set; }

    public string Epc { get; set; } = null!;

    public string? Tid { get; set; }

    public string StockNumber { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }
}
