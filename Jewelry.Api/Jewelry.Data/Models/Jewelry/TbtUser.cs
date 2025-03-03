using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtUser
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string Password { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public bool IsNew { get; set; }

    public DateTime? LastLogin { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<TbtUserRole> TbtUserRole { get; set; } = new List<TbtUserRole>();
}
