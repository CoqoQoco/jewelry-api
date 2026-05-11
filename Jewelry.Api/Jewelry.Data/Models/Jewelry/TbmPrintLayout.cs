using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmPrintLayout
{
    public string LayoutKey { get; set; } = null!;

    public string LayoutJson { get; set; } = null!;

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }
}
