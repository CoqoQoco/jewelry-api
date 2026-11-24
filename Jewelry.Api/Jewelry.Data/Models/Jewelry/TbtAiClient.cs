using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtAiClient
{
    public long Id { get; set; }
    public string ClientId { get; set; } = null!;
    public string ClientName { get; set; } = null!;
    public string Kind { get; set; } = null!;
    public string RedirectUris { get; set; } = "[]";
    public string? MetadataUrl { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? LastSeen { get; set; }
}
