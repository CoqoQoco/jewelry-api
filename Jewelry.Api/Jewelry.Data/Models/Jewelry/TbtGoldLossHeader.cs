using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtGoldLossHeader
{
    public int Id { get; set; }

    public string? DocumentNo { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int Status { get; set; }

    public string? Remark { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual ICollection<TbtGoldLossItem> TbtGoldLossItem { get; set; } = new List<TbtGoldLossItem>();
}
