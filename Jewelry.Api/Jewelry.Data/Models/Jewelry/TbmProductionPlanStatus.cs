using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmProductionPlanStatus
{
    public int Id { get; set; }

    public string NameEn { get; set; } = null!;

    public string NameTh { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<TbtProductionPlan> TbtProductionPlan { get; set; } = new List<TbtProductionPlan>();

    public virtual ICollection<TbtProductionPlanStatusDetail> TbtProductionPlanStatusDetail { get; set; } = new List<TbtProductionPlanStatusDetail>();
}
