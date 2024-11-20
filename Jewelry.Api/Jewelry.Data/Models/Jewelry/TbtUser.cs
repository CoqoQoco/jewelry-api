using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtUser
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string FirstNameTh { get; set; } = null!;

    public string LastNameTh { get; set; } = null!;

    public string PrefixNameTh { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string? Role { get; set; }

    public string Password { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public bool IsNew { get; set; }

    public virtual ICollection<TbtUserRole> TbtUserRole { get; set; } = new List<TbtUserRole>();
}
