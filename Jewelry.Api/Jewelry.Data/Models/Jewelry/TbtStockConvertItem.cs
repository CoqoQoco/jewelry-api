using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockConvertItem
{
    public long Id { get; set; }

    public string HeaderRunning { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string StockNumber { get; set; } = null!;

    public string? ProductCode { get; set; }

    public string? SkuCode { get; set; }

    public string? LocationCode { get; set; }

    public decimal Qty { get; set; }

    public decimal? ProductCost { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public virtual TbtStockConvertHeader HeaderRunningNavigation { get; set; } = null!;
}
