using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockCostVersion
{
    public string Running { get; set; } = null!;

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public string StockNumber { get; set; } = null!;

    public string? CustomerName { get; set; }

    public string? CustomerCode { get; set; }

    public string? CustomerAddress { get; set; }

    public string? CustomerTel { get; set; }

    public string? CustomerEmail { get; set; }

    public string? Remark { get; set; }

    public string ProductCostDetail { get; set; } = null!;

    public virtual TbtStockProduct StockNumberNavigation { get; set; } = null!;
}
