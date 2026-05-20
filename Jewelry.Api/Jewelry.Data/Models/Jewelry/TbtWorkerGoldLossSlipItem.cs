using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtWorkerGoldLossSlipItem
{
    public long Id { get; set; }

    public long SlipId { get; set; }

    public string? Wo { get; set; }

    public int? WoNumber { get; set; }

    public string? ProductNumber { get; set; }

    public string? ProductName { get; set; }

    public string? Gold { get; set; }

    public string? GoldSize { get; set; }

    public DateTime? JobDate { get; set; }

    public decimal? GoldQtySend { get; set; }

    public decimal? GoldWeightSend { get; set; }

    public decimal? GoldQtyCheck { get; set; }

    public decimal? GoldWeightCheck { get; set; }

    public decimal? LossPercent { get; set; }

    public decimal? WeightLossAllowed { get; set; }

    public decimal? WeightLossActual { get; set; }

    public decimal? GoldLossPrice { get; set; }

    public decimal? MoneyDiff { get; set; }

    public bool IsActive { get; set; }

    public virtual TbtWorkerGoldLossSlip Header { get; set; } = null!;
}
