using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockPriceReviewAck
{
    public long Id { get; set; }

    public string StockNumber { get; set; } = null!;

    public decimal PriceAcked { get; set; }

    public string? Note { get; set; }

    public bool IsActive { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }
}
