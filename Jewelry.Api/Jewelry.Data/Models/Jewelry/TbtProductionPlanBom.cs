using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanBom
{
    public string Running { get; set; } = null!;

    public string Wo { get; set; } = null!;

    public int WoNumber { get; set; }

    public string WoText { get; set; } = null!;

    public int ProductionId { get; set; }

    public string OriginCode { get; set; } = null!;

    public string OriginName { get; set; } = null!;

    public string Type { get; set; } = null!;

    public decimal Qty { get; set; }

    public DateTime? Date { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public int No { get; set; }

    public string? MatchCode { get; set; }

    public string? MatchName { get; set; }

    public decimal? Price { get; set; }

    public string DisplayName { get; set; } = null!;

    public string? Unit { get; set; }

    public virtual TbtProductionPlan Production { get; set; } = null!;
}
