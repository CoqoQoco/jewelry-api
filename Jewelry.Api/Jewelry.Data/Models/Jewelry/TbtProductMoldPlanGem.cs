using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductMoldPlanGem
{
    public int PlanId { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public int Id { get; set; }

    public string? Size { get; set; }

    public decimal? Qty { get; set; }

    public string GemCode { get; set; } = null!;

    public string GemShapeCode { get; set; } = null!;

    public virtual TbmGem GemCodeNavigation { get; set; } = null!;

    public virtual TbmGemShape GemShapeCodeNavigation { get; set; } = null!;

    public virtual TbtProductMoldPlan Plan { get; set; } = null!;
}
