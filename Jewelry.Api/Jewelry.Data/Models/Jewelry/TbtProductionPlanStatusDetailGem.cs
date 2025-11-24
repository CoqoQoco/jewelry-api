using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanStatusDetailGem
{
    public int ProductionPlanId { get; set; }

    public string ItemNo { get; set; } = null!;

    public bool IsActive { get; set; }

    public int HeaderId { get; set; }

    public DateTime? RequestDate { get; set; }

    public int GemId { get; set; }

    public string GemCode { get; set; } = null!;

    public decimal? GemQty { get; set; }

    public decimal? GemWeight { get; set; }

    public string? GemName { get; set; }

    public decimal? GemPrice { get; set; }

    public string? Remark { get; set; }

    public string? OutboundRunning { get; set; }

    public string? OutboundName { get; set; }

    public DateTime? OutboundDate { get; set; }

    public decimal? GemLength { get; set; }

    public string? GemLengthUnit { get; set; }

    public string? GemWeightUnit { get; set; }

    public virtual TbtProductionPlanStatusHeader Header { get; set; } = null!;
}
