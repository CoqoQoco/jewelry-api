using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPrePlanMaterial
{
    public int Id { get; set; }

    public int PrePlanId { get; set; }

    public string MaterialType { get; set; } = null!;

    public int? MasterId { get; set; }

    public string? MaterialCode { get; set; }

    public string? ShapeCode { get; set; }

    public string? Size { get; set; }

    public int Qty { get; set; }

    public string? Color { get; set; }

    public decimal? Weight { get; set; }

    public string? WeightUnit { get; set; }

    public bool IsLocked { get; set; }

    public string? Remark { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public virtual TbtProductionPrePlan PrePlan { get; set; } = null!;
}
