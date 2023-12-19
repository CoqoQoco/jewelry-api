using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmWorker
{
    public string Code { get; set; } = null!;

    public string NameTh { get; set; } = null!;

    public string? NameEn { get; set; }

    public int TypeId { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public bool IsActive { get; set; }
}
