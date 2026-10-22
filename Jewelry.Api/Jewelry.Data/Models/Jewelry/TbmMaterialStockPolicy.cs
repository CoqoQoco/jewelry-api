using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmMaterialStockPolicy
{
    public string MaterialKind { get; set; } = null!;

    public int WindowMonths { get; set; }

    public int LeadTimeDays { get; set; }

    public int SafetyDays { get; set; }

    public decimal FloorQty { get; set; }

    public decimal NearPercent { get; set; }

    public int DeadStockDays { get; set; }

    public int ForecastMonths { get; set; }

    public int OnProcessStaleDays { get; set; }

    public decimal PriceChangePercent { get; set; }

    public decimal ConcentrationPercent { get; set; }

    public int MinHistoryMonths { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }
}
