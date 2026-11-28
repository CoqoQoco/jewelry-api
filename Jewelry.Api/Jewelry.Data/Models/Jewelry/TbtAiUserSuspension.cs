using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtAiUserSuspension
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Source { get; set; } = null!;
    public string ReasonCode { get; set; } = null!;
    public string? ReasonDetail { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? LiftedBy { get; set; }
    public DateTime? LiftedAt { get; set; }
}
