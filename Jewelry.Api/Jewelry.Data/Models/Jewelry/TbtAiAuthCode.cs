using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtAiAuthCode
{
    public string CodeHash { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public int UserId { get; set; }
    public string RedirectUri { get; set; } = null!;
    public string CodeChallenge { get; set; } = null!;
    public string Scopes { get; set; } = null!;
    public string? Resource { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
