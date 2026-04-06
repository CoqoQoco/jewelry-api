using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockBasketItem
{
    public long Id { get; set; }

    public string BasketRunning { get; set; } = null!;

    public string BasketNumber { get; set; } = null!;

    public string StockNumber { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? StatusName { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbtStockBasket BasketRunningNavigation { get; set; } = null!;
}
