using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockConvertHeader
{
    public string Running { get; set; } = null!;

    public string? SoNumber { get; set; }

    public string? SoLineKey { get; set; }

    public int Status { get; set; }

    public string? StatusName { get; set; }

    public decimal ConvertCost { get; set; }

    public string? Remark { get; set; }

    public string? CancelReason { get; set; }

    public DateTime? CompleteDate { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual ICollection<TbtStockConvertItem> TbtStockConvertItem { get; set; } = new List<TbtStockConvertItem>();
}
