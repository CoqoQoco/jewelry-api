using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockProductReceiptPlan
{
    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public bool IsComplete { get; set; }

    public string Running { get; set; } = null!;

    /// <summary>
    /// เลขใบจ่าย-รับงาน
    /// </summary>
    public string? Wo { get; set; }

    /// <summary>
    /// ลำดับใบจ่าย-รับงาน
    /// </summary>
    public int? WoNumber { get; set; }

    public string? WoText { get; set; }

    public int? Qty { get; set; }

    public bool IsRunning { get; set; }

    public int QtyRunning { get; set; }

    public string? JsonDraft { get; set; }

    public string Type { get; set; } = null!;

    public string? PoNumber { get; set; }

    public virtual ICollection<TbtStockProductReceiptItem> TbtStockProductReceiptItem { get; set; } = new List<TbtStockProductReceiptItem>();
}
