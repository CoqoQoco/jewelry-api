using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockPieceCostPlan
{
    public string Running { get; set; } = null!;

    public string StockNumber { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public string? StockNumberOrigin { get; set; }

    public string? VersionRunning { get; set; }

    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public bool? IsMobileActive { get; set; }

    public bool? IsActive { get; set; }

    public string? Remark { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbtStockPiece StockPieceNavigation { get; set; } = null!;

    public virtual TbtStockPieceCostVersion? VersionRunningNavigation { get; set; }
}
