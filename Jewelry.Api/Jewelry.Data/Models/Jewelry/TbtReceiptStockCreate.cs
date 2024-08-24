using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtReceiptStockCreate
{
    public int Id { get; set; }

    public int Type { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public bool IsActive { get; set; }

    public int Status { get; set; }

    public string Running { get; set; } = null!;
}
