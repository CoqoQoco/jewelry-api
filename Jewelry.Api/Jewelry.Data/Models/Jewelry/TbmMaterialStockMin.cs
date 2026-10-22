using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmMaterialStockMin
{
    public long Id { get; set; }

    public string MaterialKind { get; set; } = null!;

    public string ItemKey { get; set; } = null!;

    public decimal MinQty { get; set; }

    public int? LeadTimeDays { get; set; }

    public string? Note { get; set; }

    public bool IsActive { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }
}
