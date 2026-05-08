using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPrePlanMaterial
{
    public int Id { get; set; }

    public int PrePlanItemId { get; set; }

    public string? Gold { get; set; }

    public string? GoldSize { get; set; }

    public decimal? GoldQty { get; set; }

    public string? Gem { get; set; }

    public string? GemShape { get; set; }

    public decimal? GemQty { get; set; }

    public string? GemUnit { get; set; }

    public string? GemSize { get; set; }

    public decimal? GemWeight { get; set; }

    public string? GemWeightUnit { get; set; }

    public decimal? DiamondQty { get; set; }

    public string? DiamondUnit { get; set; }

    public string? DiamondSize { get; set; }

    public decimal? DiamondWeight { get; set; }

    public string? DiamondWeightUnit { get; set; }

    public string? DiamondQuality { get; set; }

    public string? CreateBy { get; set; }

    public DateTime? CreateDate { get; set; }

    public virtual TbtProductionPrePlanItem Item { get; set; } = null!;
}
