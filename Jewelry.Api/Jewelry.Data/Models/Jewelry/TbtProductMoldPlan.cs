using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductMoldPlan
{
    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public int Id { get; set; }

    public int Status { get; set; }

    public bool? IsActive { get; set; }

    public string? RemarkUpdate { get; set; }

    public virtual TbmProductMoldPlanStatus StatusNavigation { get; set; } = null!;

    public virtual ICollection<TbtProductMoldPlanDesign> TbtProductMoldPlanDesign { get; set; } = new List<TbtProductMoldPlanDesign>();
}
