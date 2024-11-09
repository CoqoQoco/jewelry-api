using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockProductReceiptItem
{
    public string StockReceiptNumber { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public bool IsReceipt { get; set; }

    public string Running { get; set; } = null!;

    /// <summary>
    /// เลขใบจ่าย-รับงาน
    /// </summary>
    public string Wo { get; set; } = null!;

    /// <summary>
    /// ลำดับใบจ่าย-รับงาน
    /// </summary>
    public int WoNumber { get; set; }

    public virtual TbtStockProductReceiptPlan TbtStockProductReceiptPlan { get; set; } = null!;
}
