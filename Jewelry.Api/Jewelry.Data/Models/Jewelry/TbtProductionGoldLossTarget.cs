using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionGoldLossTarget
{
    public long Id { get; set; }

    public string Scope { get; set; } = null!;

    public int WorkerType { get; set; }

    public string Metal { get; set; } = null!;

    public decimal TargetPercent { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public string? Remark { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;
}
