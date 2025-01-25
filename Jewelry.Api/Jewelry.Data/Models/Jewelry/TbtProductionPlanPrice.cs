using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanPrice
{
    public string Running { get; set; } = null!;

    public int No { get; set; }

    public string Wo { get; set; } = null!;

    public int WoNumber { get; set; }

    public string WoText { get; set; } = null!;

    public int ProductionId { get; set; }

    public string Name { get; set; } = null!;

    public string NameDescription { get; set; } = null!;

    public string NameGroup { get; set; } = null!;

    public decimal Qty { get; set; }

    public decimal QtyPrice { get; set; }

    public decimal QtyWeight { get; set; }

    public decimal QtyWeightPrice { get; set; }

    public DateTime? Date { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public decimal TotalPrice { get; set; }

    public bool IsManualAdd { get; set; }

    public virtual TbtProductionPlan Production { get; set; } = null!;
}
