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

    public string? Remark { get; set; }

    public string? ImageUrl { get; set; }

    public string? RemarUpdate { get; set; }

    public decimal QtyReceive { get; set; }

    public decimal QtySend { get; set; }

    public string CategoryCode { get; set; } = null!;

    public string? DesignBy { get; set; }

    public string? ResinBy { get; set; }

    public bool IsNewProcess { get; set; }

    public virtual TbmProductType CategoryCodeNavigation { get; set; } = null!;

    public virtual TbtProductMoldPlan Plan { get; set; } = null!;
}
