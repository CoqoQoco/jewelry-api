using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtWorkerGoldLossSlipReturn
{
    public long Id { get; set; }

    public long SlipId { get; set; }

    public string GoldSize { get; set; } = null!;

    public string? Gold { get; set; }

    public decimal Weight { get; set; }

    public decimal PricePerGram { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public virtual TbtWorkerGoldLossSlip Slip { get; set; } = null!;
}
