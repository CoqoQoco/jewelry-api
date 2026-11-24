using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtAiToken
{
    public long Id { get; set; }
    public string Kind { get; set; } = null!;
    public int UserId { get; set; }
    public string? ClientId { get; set; }
    public string Name { get; set; } = null!;
    public string Scopes { get; set; } = null!;
    public string? TokenHash { get; set; }
    public string? RefreshHash { get; set; }
    public string? FamilyId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RefreshExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreateDate { get; set; }
}
