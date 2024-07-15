﻿using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductMoldPlanStore
{
    public int PlanId { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string CodePlan { get; set; } = null!;

    public string WorkerBy { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? Remark { get; set; }

    public decimal? QtyReceive { get; set; }

    public decimal? QtySend { get; set; }

    public string Code { get; set; } = null!;

    public string Location { get; set; } = null!;

    public virtual TbtProductMoldPlan Plan { get; set; } = null!;
}
