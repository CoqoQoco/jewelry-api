using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtAiAccessLog
{
    public long Id { get; set; }
    public long? TokenId { get; set; }
    public int UserId { get; set; }
    public string Tool { get; set; } = null!;
    public string? Scopes { get; set; }
    public string? Params { get; set; }
    public int RowCount { get; set; }
    public int MaskedCount { get; set; }
    public int DurationMs { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime CreateDate { get; set; }
}
