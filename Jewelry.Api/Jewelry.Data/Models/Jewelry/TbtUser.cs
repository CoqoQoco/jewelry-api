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

    public byte[] PasswordHash { get; set; } = null!;

    public byte[] PasswordSalt { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string Position { get; set; } = null!;

    public int PermissionLevel { get; set; }
}
