using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmNotificationType
{
    public string Code { get; set; } = null!;

    public string Module { get; set; } = null!;

    public string NameTh { get; set; } = null!;

    public string? NameEn { get; set; }

    public string? Icon { get; set; }

    public string DefaultSeverity { get; set; } = null!;

    public int? SeverityWarnDays { get; set; }

    public int? SeverityCritDays { get; set; }

    public int? EscalateDays { get; set; }

    public string? ActionRoute { get; set; }

    public string Source { get; set; } = null!;

    public bool IsActive { get; set; }

    public int? SortOrder { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual ICollection<TbtNotification> TbtNotification { get; set; } = new List<TbtNotification>();
}
