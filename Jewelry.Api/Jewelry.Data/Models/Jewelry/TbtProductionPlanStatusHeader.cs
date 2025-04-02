using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductionPlanStatusHeader
{
    public int Id { get; set; }

    public int ProductionPlanId { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public int Status { get; set; }

    public string? Remark1 { get; set; }

    public string? Remark2 { get; set; }

    public bool IsActive { get; set; }

    public decimal? WagesTotal { get; set; }

    public string? WorkerCode { get; set; }

    public string? WorkerName { get; set; }

    public virtual TbtProductionPlan ProductionPlan { get; set; } = null!;

    public virtual TbmProductionPlanStatus StatusNavigation { get; set; } = null!;

    public virtual ICollection<TbtProductionPlanStatusDetail> TbtProductionPlanStatusDetail { get; set; } = new List<TbtProductionPlanStatusDetail>();

    public virtual ICollection<TbtProductionPlanStatusDetailGem> TbtProductionPlanStatusDetailGem { get; set; } = new List<TbtProductionPlanStatusDetailGem>();
}
