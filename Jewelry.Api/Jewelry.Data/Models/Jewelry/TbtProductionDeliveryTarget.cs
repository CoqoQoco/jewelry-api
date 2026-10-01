using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionDeliveryTarget
{
    public long Id { get; set; }

    public decimal TargetPercent { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public string? Remark { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;
}
