using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockProductMaterial
{
    public int Id { get; set; }

    public string StockNumber { get; set; } = null!;

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public string Type { get; set; } = null!;

    public string? TypeName { get; set; }

    public string? TypeCode { get; set; }

    public string? TypeBarcode { get; set; }

    public string? Size { get; set; }

    public decimal? Price { get; set; }

    public decimal? Weight { get; set; }

    public decimal? Qty { get; set; }

    public string? QtyUnit { get; set; }

    public string? WeightUnit { get; set; }

    public string? Region { get; set; }

    public virtual TbtStockProduct StockNumberNavigation { get; set; } = null!;
}
