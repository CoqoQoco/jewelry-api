using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockBasket
{
    public string Running { get; set; } = null!;

    public string BasketNumber { get; set; } = null!;

    public string BasketName { get; set; } = null!;

    public DateTime? EventDate { get; set; }

    public string? Responsible { get; set; }

    public int Status { get; set; }

    public string? StatusName { get; set; }

    public string? Remark { get; set; }

    public DateTime? CheckoutDate { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual ICollection<TbtStockBasketItem> TbtStockBasketItem { get; set; } = new List<TbtStockBasketItem>();
}
