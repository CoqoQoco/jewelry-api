using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtGoldLossTangSlipExtra
{
    public long Id { get; set; }

    public long SlipId { get; set; }

    public int Kind { get; set; }

    public string? Name { get; set; }

    public decimal? Weight { get; set; }

    public bool IsActive { get; set; }

    public bool CountInCalc { get; set; }

    public virtual TbtGoldLossTangSlip Header { get; set; } = null!;
}
