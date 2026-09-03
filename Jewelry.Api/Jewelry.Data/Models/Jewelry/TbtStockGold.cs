using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockGold
{
    public long Id { get; set; }

    public string GoldCode { get; set; } = null!;

    public string GoldSizeCode { get; set; } = null!;

    public decimal Weight { get; set; }

    public decimal WeightOnProcess { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbmGold GoldCodeNavigation { get; set; } = null!;

    public virtual TbmGoldSize GoldSizeCodeNavigation { get; set; } = null!;
}
