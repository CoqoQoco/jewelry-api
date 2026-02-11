using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtMyJob
{
    public int Id { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public bool? IsActive { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public string DataJob { get; set; } = null!;

    public string JobRunning { get; set; } = null!;

    public string JobTypeName { get; set; } = null!;

    public int JobTypeId { get; set; }
}
