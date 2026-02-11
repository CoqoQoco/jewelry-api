using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockCostPlan
{
    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? Remark { get; set; }

    public string Running { get; set; } = null!;

    public string StockNumber { get; set; } = null!;

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public string? VersionRunning { get; set; }

    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public bool? IsMobileActive { get; set; }

    public bool? IsActive { get; set; }

    public virtual TbtStockCostVersion? TbtStockCostVersion { get; set; }
}
