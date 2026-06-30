using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtGoldLossTangSlipItem
{
    public long Id { get; set; }

    public long SlipId { get; set; }

    public int? ProductionPlanId { get; set; }

    public string? ItemNo { get; set; }

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

    public bool IsActive { get; set; }

    public virtual TbtGoldLossTangSlip Header { get; set; } = null!;
}
