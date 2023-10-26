using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanStatusDetail
{
    public int Id { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public int ProductionPlanId { get; set; }

    public int Status { get; set; }

    public DateTime AssignDate { get; set; }

    public string AssignBy { get; set; } = null!;

    public DateTime? ReceiveDate { get; set; }

    public string? ReceiveBy { get; set; }

    public string? Remark { get; set; }

    public string? AssignTo { get; set; }

    public string? AssignDetail { get; set; }

    public string? ReceiveDetail { get; set; }

    public bool IsActive { get; set; }

    public virtual TbtProductionPlan ProductionPlan { get; set; } = null!;

    public virtual TbmProductionPlanStatus StatusNavigation { get; set; } = null!;
}
