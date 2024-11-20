using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmUserRole
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public int Level { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<TbtUserRole> TbtUserRole { get; set; } = new List<TbtUserRole>();
}
