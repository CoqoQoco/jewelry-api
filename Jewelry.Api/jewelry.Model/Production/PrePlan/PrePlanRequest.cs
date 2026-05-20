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
    public int Take { get; set; } = 50;
    public int Skip { get; set; } = 0;
    public string? Sort { get; set; }
    public bool IncludeCompleted { get; set; } = false;
}

public class CreatePrePlanRequest
{
    public string? OrderNo { get; set; }

    public int ProductionRound { get; set; } = 1;

    public string? JobType { get; set; }

    public string? JobLocation { get; set; }

    public string? GoldType { get; set; }

    [Required]
    public DateTimeOffset OrderDate { get; set; }

    [Required]
    public DateTimeOffset DeliveryDate { get; set; }

    public string? Remark { get; set; }

    public string? SalesBy { get; set; }

    public string? ApprovedBy { get; set; }

    public List<CreatePrePlanItemRequest> Items { get; set; } = new List<CreatePrePlanItemRequest>();
}

public class CreatePrePlanItemRequest
{
    [Required]
    public string MoldCode { get; set; } = null!;

    public string? MoldDetail { get; set; }

    public string? ProductType { get; set; }

    public int? ProductQty { get; set; }

    public string? ProductQtyUnit { get; set; }

    public string? ProductDetail { get; set; }

    public string? ProductImagePath { get; set; }

    public List<CreatePrePlanMaterialRequest> Materials { get; set; } = new List<CreatePrePlanMaterialRequest>();
}

public class CreatePrePlanMaterialRequest
{
    public string? Gold { get; set; }
    public string? GoldSize { get; set; }
    public decimal? GoldQty { get; set; }
    public string? Gem { get; set; }
    public string? GemShape { get; set; }
    public decimal? GemQty { get; set; }
    public string? GemUnit { get; set; }
    public string? GemSize { get; set; }
    public decimal? GemWeight { get; set; }
    public string? GemWeightUnit { get; set; }
    public decimal? DiamondQty { get; set; }
    public string? DiamondUnit { get; set; }
    public string? DiamondSize { get; set; }
    public decimal? DiamondWeight { get; set; }
    public string? DiamondWeightUnit { get; set; }
    public string? DiamondQuality { get; set; }
}

public class UpdatePrePlanRequest : CreatePrePlanRequest
{
    public int Id { get; set; }
}

public class SubmitPrePlanRequest
{
    public int Id { get; set; }
}

public class ApprovePrePlanRequest
{
    public int Id { get; set; }
    public string? Remark { get; set; }
}

public class RejectPrePlanRequest
{
    public int Id { get; set; }
    public string? RejectReason { get; set; }
}

