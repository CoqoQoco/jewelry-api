using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmProductMoldPlanStatus
{
    public string? Description { get; set; }

    public int Id { get; set; }

    public string NameEn { get; set; } = null!;

    public string NameTh { get; set; } = null!;

    public virtual ICollection<TbtProductMoldPlan> TbtProductMoldPlan { get; set; } = new List<TbtProductMoldPlan>();
}
