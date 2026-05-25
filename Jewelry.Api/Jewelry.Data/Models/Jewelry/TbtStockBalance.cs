using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockBalance
{
    public long Id { get; set; }

    public string SkuCode { get; set; } = null!;

    public string LocationCode { get; set; } = null!;

    public decimal QtyOnHand { get; set; }

    public decimal QtyReserved { get; set; }

    public decimal? QtyAvailable { get; set; }

    public DateTime? LastMovementAt { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbtSku SkuCodeNavigation { get; set; } = null!;

    public virtual TbmStockLocation LocationCodeNavigation { get; set; } = null!;
}
