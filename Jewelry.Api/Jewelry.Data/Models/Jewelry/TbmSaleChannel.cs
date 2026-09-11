using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmSaleChannel
{
    public string Code { get; set; } = null!;

    public string NameTh { get; set; } = null!;

    public string? NameEn { get; set; }

    public string Type { get; set; } = null!;

    public string? Venue { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public int? SortOrder { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }
}
