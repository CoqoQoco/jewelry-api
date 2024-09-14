using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockGemTransection
{
    public string Running { get; set; } = null!;

    public string Code { get; set; } = null!;

    public int Type { get; set; }

    public string? JobOrPo { get; set; }

    public decimal? SupplierCost { get; set; }

    public string? Remark1 { get; set; }

    public string? Remark2 { get; set; }

    public decimal Qty { get; set; }

    public string? SubpplierName { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime RequestDate { get; set; }

    public string Stastus { get; set; } = null!;

    public decimal QtyWeight { get; set; }

    public string? ProductionPlanWo { get; set; }

    public string? ProductionPlanMold { get; set; }

    public int? ProductionPlanWoNumber { get; set; }

    public string? ProductionPlanWoText { get; set; }

    public DateTime? ReturnDate { get; set; }

    public string? RefRunning { get; set; }

    public long Id { get; set; }
}
