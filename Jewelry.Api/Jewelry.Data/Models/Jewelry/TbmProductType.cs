using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmProductType
{
    public int Id { get; set; }

    public string NameEn { get; set; } = null!;

    public string NameTh { get; set; } = null!;

    public string Code { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public virtual ICollection<TbtProductMoldPlanDesign> TbtProductMoldPlanDesign { get; set; } = new List<TbtProductMoldPlanDesign>();

    public virtual ICollection<TbtProductionPlan> TbtProductionPlan { get; set; } = new List<TbtProductionPlan>();
}
