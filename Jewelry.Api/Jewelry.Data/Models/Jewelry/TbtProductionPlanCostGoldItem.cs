using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanCostGoldItem
{
    public string No { get; set; } = null!;

    public string BookNo { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string ProductionPlanId { get; set; } = null!;

    public decimal ReturnWeight { get; set; }

    public string? Remark { get; set; }

    public virtual TbtProductionPlanCostGold TbtProductionPlanCostGold { get; set; } = null!;
}
