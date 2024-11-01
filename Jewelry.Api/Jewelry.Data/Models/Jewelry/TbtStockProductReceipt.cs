using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockProductReceipt
{
    public string Running { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string Type { get; set; } = null!;

    public virtual ICollection<TbtStockProduct> TbtStockProduct { get; set; } = new List<TbtStockProduct>();
}
