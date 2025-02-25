using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockProductImage
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Year { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string? Remark { get; set; }

    public bool? IsActive { get; set; }

    public string NamePath { get; set; } = null!;
}
