using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleMaterialItem
{
    public long Id { get; set; }

    public string Running { get; set; } = null!;

    public int ItemNo { get; set; }

    public string GemCode { get; set; } = null!;

    public string? GemName { get; set; }

    public string? GemGroup { get; set; }

    public string? GemShape { get; set; }

    public string? GemSize { get; set; }

    public string? GemGrade { get; set; }

    public string? Description { get; set; }

    public decimal QtyPiece { get; set; }

    public decimal QtyWeight { get; set; }

    public decimal PriceInclVat { get; set; }

    public decimal PriceExclVat { get; set; }

    public decimal Amount { get; set; }

    public decimal? RefStockPrice { get; set; }

    public string? Remark { get; set; }

    public virtual TbtSaleMaterialHeader RunningNavigation { get; set; } = null!;
}
