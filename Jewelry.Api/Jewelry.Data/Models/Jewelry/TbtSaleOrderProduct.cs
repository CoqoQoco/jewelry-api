using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleOrderProduct
{
    public string Running { get; set; } = null!;

    public string SoNumber { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string StockNumber { get; set; } = null!;

    public string? Stocknumberorigin { get; set; }

    public long Id { get; set; }

    /// <summary>
    /// Price in THB
    /// </summary>
    public decimal PriceOrigin { get; set; }

    public decimal? Discount { get; set; }

    public string? Remark { get; set; }

    public decimal? NetPrice { get; set; }

    public decimal Qty { get; set; }

    public string? Invoice { get; set; }

    public string? InvoiceItem { get; set; }
}
