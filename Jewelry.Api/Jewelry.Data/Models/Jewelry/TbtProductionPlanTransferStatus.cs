using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanTransferStatus
{
    public int Id { get; set; }

    public string Running { get; set; } = null!;

    public string Wo { get; set; } = null!;

    public int WoNumber { get; set; }

    public int ProductionPlanId { get; set; }

    public int FormerStatus { get; set; }

    public int TargetStatus { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public int TargetStatusId { get; set; }

    public virtual TbtProductionPlan ProductionPlan { get; set; } = null!;

    public virtual TbtProductionPlanStatusHeader TargetStatusNavigation { get; set; } = null!;
}
