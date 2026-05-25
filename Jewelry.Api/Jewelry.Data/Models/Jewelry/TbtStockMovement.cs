using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockMovement
{
    public long Id { get; set; }

    public DateTime MovementDate { get; set; }

    public string MovementType { get; set; } = null!;

    public string SkuCode { get; set; } = null!;

    public string? StockNumber { get; set; }

    public string? FromLocation { get; set; }

    public string? ToLocation { get; set; }

    public decimal Qty { get; set; }

    public string? RefDocType { get; set; }

    public string? RefDocNo { get; set; }

    public string? Remark { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public virtual TbtSku SkuCodeNavigation { get; set; } = null!;

    public virtual TbtStockPiece? StockNumberNavigation { get; set; }

    public virtual TbmStockLocation? FromLocationNavigation { get; set; }

    public virtual TbmStockLocation? ToLocationNavigation { get; set; }
}
