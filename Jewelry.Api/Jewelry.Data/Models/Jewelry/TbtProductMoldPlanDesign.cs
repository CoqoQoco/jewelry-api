using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductMoldPlanDesign
{
    public int PlanId { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string CodePlan { get; set; } = null!;

    public decimal? SizeGem { get; set; }

    public decimal? SizeDiamond { get; set; }

    public decimal? QtyGem { get; set; }

    public decimal? QtyDiamond { get; set; }

    public decimal? QtyBeforeSend { get; set; }

    public decimal? QtyBeforeCasting { get; set; }

    public string? Remark { get; set; }

    public string? ImageUrl { get; set; }

    public virtual TbtProductMoldPlan Plan { get; set; } = null!;
}
