using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace jewelry.Model.Production.PrePlan;

public class SearchPrePlanRequest
{
    public string? MoldCode { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? OrderDateFrom { get; set; }
    public DateTimeOffset? OrderDateTo { get; set; }
}

public class CreatePrePlanRequest
{
    [Required]
    public string OrderNo { get; set; } = null!;

    [Required]
    public string JobLocation { get; set; } = null!;

    [Required]
    public string JobType { get; set; } = null!;

    public int ProductionRound { get; set; } = 1;

    [Required]
    public string MoldCode { get; set; } = null!;

    public string? ProductType { get; set; }

    [Required]
    public string GoldType { get; set; } = null!;

    public string? MoldDetail { get; set; }

    public string? Remark { get; set; }

    public DateTimeOffset OrderDate { get; set; }

    public DateTimeOffset DeliveryDate { get; set; }

    public IList<CreatePrePlanMaterialRequest> Materials { get; set; } = new List<CreatePrePlanMaterialRequest>();
}

public class CreatePrePlanMaterialRequest
{
    [Required]
    public string MaterialType { get; set; } = null!;

    public int? MasterId { get; set; }

    public string? MaterialCode { get; set; }

    public string? ShapeCode { get; set; }

    public string? Size { get; set; }

    public int Qty { get; set; }

    public string? Color { get; set; }

    public decimal? Weight { get; set; }

    public string? WeightUnit { get; set; }

    public bool IsLocked { get; set; }

    public string? Remark { get; set; }
}

public class UpdatePrePlanRequest : CreatePrePlanRequest
{
    public int Id { get; set; }
}

public class SubmitPrePlanRequest
{
    public int Id { get; set; }
}
