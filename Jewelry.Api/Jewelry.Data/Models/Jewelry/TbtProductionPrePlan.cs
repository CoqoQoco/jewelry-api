using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPrePlan
{
    public int Id { get; set; }

    public string? OrderNo { get; set; }

    public int? ProductionRound { get; set; }

    public string? JobType { get; set; }

    public string? JobLocation { get; set; }

    public string? GoldType { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime DeliveryDate { get; set; }

    public string? Remark { get; set; }

    public string Status { get; set; } = null!;

    public string? RejectReason { get; set; }

    public string? CreateBy { get; set; }

    public DateTime? CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public string? SubmitBy { get; set; }

    public DateTime? SubmitDate { get; set; }

    public string? ApproveBy { get; set; }

    public DateTime? ApproveDate { get; set; }

    public string? SalesBy { get; set; }

    public string? ApprovedBy { get; set; }

    public string? ApprovedDocumentPath { get; set; }

    public virtual ICollection<TbtProductionPrePlanItem> Items { get; set; } = new List<TbtProductionPrePlanItem>();
}
