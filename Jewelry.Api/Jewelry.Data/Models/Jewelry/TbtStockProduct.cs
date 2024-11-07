using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockProduct
{
    public string Running { get; set; } = null!;

    public int ProductionPlanId { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string? ReceiptNumber { get; set; }

    public string? Location { get; set; }

    public bool IsReceipt { get; set; }

    public virtual TbtProductionPlan ProductionPlan { get; set; } = null!;

    public virtual TbtStockProductReceipt? ReceiptNumberNavigation { get; set; }
}
