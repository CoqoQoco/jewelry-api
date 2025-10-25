using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleInvoiceVersion
{
    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string SoRunning { get; set; } = null!;

    public string Running { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public string Data { get; set; } = null!;

    public bool? IsActive { get; set; }

    public string InvoiceRunning { get; set; } = null!;
}
