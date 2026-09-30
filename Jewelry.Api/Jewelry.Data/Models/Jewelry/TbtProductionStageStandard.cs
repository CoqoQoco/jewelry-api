using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionStageStandard
{
    public long Id { get; set; }

    public string DeptKey { get; set; } = null!;

    public decimal StandardDays { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public string? Remark { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;
}
