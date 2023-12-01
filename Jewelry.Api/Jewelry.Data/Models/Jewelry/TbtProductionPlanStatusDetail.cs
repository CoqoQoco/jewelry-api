using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanStatusDetail
{
    public int ProductionPlanId { get; set; }

    public string ItemNo { get; set; } = null!;

    public string? Gold { get; set; }

    public decimal? GoldWeightSend { get; set; }

    public string? Worker { get; set; }

    public decimal? GoldWeightCheck { get; set; }

    public decimal? GoldWeightDiff { get; set; }

    public decimal? GoldWeightDiffPercent { get; set; }

    public bool IsActive { get; set; }

    public decimal? GoldQtySend { get; set; }

    public decimal? GoldQtyCheck { get; set; }

    public int HeaderId { get; set; }

    public virtual TbtProductionPlanStatusHeader Header { get; set; } = null!;
}
