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

    public string Material { get; set; } = null!;

    public string MaterialType { get; set; } = null!;

    public string MaterialSize { get; set; } = null!;

    public string MaterialQty { get; set; } = null!;

    public string? MaterialRemark { get; set; }

    public int ProductionPlanId { get; set; }

    public string MaterialShape { get; set; } = null!;

    public virtual TbtProductionPlan ProductionPlan { get; set; } = null!;
}
