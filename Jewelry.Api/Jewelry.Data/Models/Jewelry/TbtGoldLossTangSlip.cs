using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtGoldLossTangSlip
{
    public long Id { get; set; }

    public string DocumentNo { get; set; } = null!;

    public string WorkerCode { get; set; } = null!;

    public string? WorkerName { get; set; }

    public DateTime? RequestDateStart { get; set; }

    public DateTime? RequestDateEnd { get; set; }

    public decimal? LossPercent { get; set; }

    public decimal? PricePerGram { get; set; }

    public decimal? IssuedTotal { get; set; }

    public decimal? ReturnedTotal { get; set; }

    public decimal? RawLoss { get; set; }

    public decimal? AllowedLoss { get; set; }

    public decimal? DiffLoss { get; set; }

    public decimal? TotalMoneyDiff { get; set; }

    public string? Remark { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual ICollection<TbtGoldLossTangSlipItem> TbtGoldLossTangSlipItem { get; set; } = new List<TbtGoldLossTangSlipItem>();

    public virtual ICollection<TbtGoldLossTangSlipExtra> TbtGoldLossTangSlipExtra { get; set; } = new List<TbtGoldLossTangSlipExtra>();
}
