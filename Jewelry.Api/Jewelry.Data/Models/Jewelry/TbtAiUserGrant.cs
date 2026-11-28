using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtAiUserGrant
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Scope { get; set; } = null!;
    public string GrantedBy { get; set; } = null!;
    public DateTime GrantedAt { get; set; }
    public string? RevokedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
}
