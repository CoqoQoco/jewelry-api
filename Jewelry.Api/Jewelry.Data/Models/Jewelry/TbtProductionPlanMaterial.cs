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

    public string Gem { get; set; } = null!;

    public string GemShape { get; set; } = null!;

    public int GemQty { get; set; }

    public string GemUnit { get; set; } = null!;

    public string GemSize { get; set; } = null!;

    public virtual TbmGem GemNavigation { get; set; } = null!;

    public virtual TbmGemShape GemShapeNavigation { get; set; } = null!;

    public virtual TbmGold GoldNavigation { get; set; } = null!;

    public virtual TbmGoldSize GoldSizeNavigation { get; set; } = null!;

    public virtual TbtProductionPlan ProductionPlan { get; set; } = null!;
}
