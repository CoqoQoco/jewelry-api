using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtGoldLossMonthlyReport
{
    public int Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public string GoldType { get; set; } = null!;

    public int Status { get; set; }

    public decimal? SumGoldWeightSend { get; set; }

    public decimal? SumGoldWeightCheck { get; set; }

    public decimal? LossPercent { get; set; }

    public decimal? GoldLossPrice { get; set; }

    public decimal? RawLoss { get; set; }

    public decimal? WeightLossAllowed { get; set; }

    public decimal? WeightLossActual { get; set; }

    public decimal? MoneyDiff { get; set; }

    public string? LossRemark { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }
}
