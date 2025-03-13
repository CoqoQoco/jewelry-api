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
    public string? Wo { get; set; }

    /// <summary>
    /// ลำดับใบจ่าย-รับงาน
    /// </summary>
    public int? WoNumber { get; set; }

    public string Type { get; set; } = null!;

    public string? Po { get; set; }

    public string? WoText { get; set; }

    public string? StockNumber { get; set; }

    /// <summary>
    /// รหัสประเภทสินค้า
    /// </summary>
    public string? ProductType { get; set; }

    /// <summary>
    /// ประเภทสินค้า
    /// </summary>
    public string? ProductTypeName { get; set; }

    /// <summary>
    /// Gold/Silver
    /// </summary>
    public string? ProductionType { get; set; }

    /// <summary>
    /// 10 K, 18K ....
    /// </summary>
    public string? ProductionTypeSize { get; set; }

    public string? Mold { get; set; }

    public DateTime? ReceiptDate { get; set; }

    public virtual TbtStockProductReceiptPlan RunningNavigation { get; set; } = null!;
}
