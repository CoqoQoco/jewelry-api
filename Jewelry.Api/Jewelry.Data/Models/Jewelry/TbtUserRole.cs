using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtUserRole
{
    public string Username { get; set; } = null!;

    public int Role { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public int UserId { get; set; }

    public virtual TbmUserRole RoleNavigation { get; set; } = null!;

    public virtual TbtUser User { get; set; } = null!;
}
