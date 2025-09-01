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

    public int NextStatus { get; set; }

    public string Running { get; set; } = null!;

    public bool IsNewProcess { get; set; }

    public virtual TbmProductMoldPlanStatus NextStatusNavigation { get; set; } = null!;

    public virtual TbmProductMoldPlanStatus StatusNavigation { get; set; } = null!;

    public virtual ICollection<TbtProductMold> TbtProductMold { get; set; } = new List<TbtProductMold>();

    public virtual ICollection<TbtProductMoldPlanCasting> TbtProductMoldPlanCasting { get; set; } = new List<TbtProductMoldPlanCasting>();

    public virtual ICollection<TbtProductMoldPlanCastingSilver> TbtProductMoldPlanCastingSilver { get; set; } = new List<TbtProductMoldPlanCastingSilver>();

    public virtual ICollection<TbtProductMoldPlanCutting> TbtProductMoldPlanCutting { get; set; } = new List<TbtProductMoldPlanCutting>();

    public virtual TbtProductMoldPlanDesign? TbtProductMoldPlanDesign { get; set; }

    public virtual ICollection<TbtProductMoldPlanGem> TbtProductMoldPlanGem { get; set; } = new List<TbtProductMoldPlanGem>();

    public virtual ICollection<TbtProductMoldPlanResin> TbtProductMoldPlanResin { get; set; } = new List<TbtProductMoldPlanResin>();

    public virtual ICollection<TbtProductMoldPlanStore> TbtProductMoldPlanStore { get; set; } = new List<TbtProductMoldPlanStore>();
}
