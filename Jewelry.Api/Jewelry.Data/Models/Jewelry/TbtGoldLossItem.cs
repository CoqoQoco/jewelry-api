using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtGoldLossItem
{
    public int Id { get; set; }

    public int HeaderId { get; set; }

    public int ProductionPlanId { get; set; }

    public string ItemNo { get; set; } = null!;

    public string? Wo { get; set; }

    public int? WoNumber { get; set; }

    public string? WoText { get; set; }

    public string? WorkerCode { get; set; }

    public string? WorkerName { get; set; }

    public string? Gold { get; set; }

    public decimal? GoldQtySend { get; set; }

    public decimal? GoldWeightSend { get; set; }

    public decimal? GoldQtyCheck { get; set; }

    public decimal? GoldWeightCheck { get; set; }

    public decimal? LossPercent { get; set; }

    public decimal? GoldLossPrice { get; set; }

    public decimal? WeightLossAllowed { get; set; }

    public decimal? WeightLossActual { get; set; }

    public decimal? MoneyDiff { get; set; }

    public string? LossRemark { get; set; }

    public DateTime? RequestDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbtGoldLossHeader Header { get; set; } = null!;
}
