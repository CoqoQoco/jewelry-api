using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanImage
{
    public int Id { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public int ProductionPlanId { get; set; }

    public string Path { get; set; } = null!;

    public int Number { get; set; }

    public bool IsActive { get; set; }

    public virtual TbtProductionPlan ProductionPlan { get; set; } = null!;
}
