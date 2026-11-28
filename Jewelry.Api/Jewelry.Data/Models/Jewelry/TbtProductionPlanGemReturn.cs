using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanGemReturn
{
    public int Id { get; set; }

    public int ProductionPlanId { get; set; }

    public int? HeaderId { get; set; }

    public string ItemNo { get; set; } = null!;

    public string? OutboundRunning { get; set; }

    public string GemCode { get; set; } = null!;

    public decimal ReturnQty { get; set; }

    public decimal ReturnWeight { get; set; }

    public string Mode { get; set; } = null!;

    public long? InboundTransectionId { get; set; }

    public string? InboundRunning { get; set; }

    public string Reason { get; set; } = null!;

    public string? Remark { get; set; }

    public string? CreateBy { get; set; }

    public DateTime CreateDate { get; set; }
}
