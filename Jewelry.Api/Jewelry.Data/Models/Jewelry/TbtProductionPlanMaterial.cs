using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanMaterial
{
    public int Id { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public bool? IsActive { get; set; }

    public int ProductionPlanId { get; set; }

    public string Gold { get; set; } = null!;

    public string GoldSize { get; set; } = null!;

    public string? Gem { get; set; }

    public string? GemShape { get; set; }

    public int? GemQty { get; set; }

    public string? GemUnit { get; set; }

    public string? GemSize { get; set; }

    public int? DiamondQty { get; set; }

    public string? DiamondUnit { get; set; }

    public string? DiamondQuality { get; set; }

    public string? GemWeight { get; set; }

    public string? GemWeightUnit { get; set; }

    public string? DiamondWeight { get; set; }

    public string? DiamondWeightUnit { get; set; }

    public virtual TbmGem? GemNavigation { get; set; }

    public virtual TbmGemShape? GemShapeNavigation { get; set; }

    public virtual TbmGold GoldNavigation { get; set; } = null!;

    public virtual TbmGoldSize GoldSizeNavigation { get; set; } = null!;

    public virtual TbtProductionPlan ProductionPlan { get; set; } = null!;
}
