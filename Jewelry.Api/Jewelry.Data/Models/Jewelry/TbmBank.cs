using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmBank
{
    public string Code { get; set; } = null!;

    public string? NameTh { get; set; }

    public string? NameEn { get; set; }

    public bool IsActive { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }
}
