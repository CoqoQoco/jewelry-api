using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtWorkerGoldLossSlip
{
    public long Id { get; set; }

    public string DocumentNo { get; set; } = null!;

    public string WorkerCode { get; set; } = null!;

    public string? WorkerName { get; set; }

    public DateTime RequestDateStart { get; set; }

    public DateTime RequestDateEnd { get; set; }

    public decimal? GoldReturn { get; set; }

    public decimal? TotalWeightLoss { get; set; }

    public decimal? NetWeightLoss { get; set; }

    public decimal? TotalMoneyDiff { get; set; }

    public string? Remark { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual ICollection<TbtWorkerGoldLossSlipItem> TbtWorkerGoldLossSlipItem { get; set; } = new List<TbtWorkerGoldLossSlipItem>();
}
