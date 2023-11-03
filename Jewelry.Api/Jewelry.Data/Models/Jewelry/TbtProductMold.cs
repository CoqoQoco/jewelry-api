using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductMold
{
    public string Code { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string? Description { get; set; }

    public string Image { get; set; } = null!;

    public bool IsActive { get; set; }

    public string Category { get; set; } = null!;
}
