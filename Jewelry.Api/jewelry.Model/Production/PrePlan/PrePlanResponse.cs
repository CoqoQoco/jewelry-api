using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.PrePlan;

public class SearchPrePlanResponse
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = null!;
    public string JobLocation { get; set; } = null!;
    public string JobType { get; set; } = null!;
    public int ProductionRound { get; set; }
    public string MoldCode { get; set; } = null!;
    public string? ProductType { get; set; }
    public string GoldType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int? ProductQty { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string CreateBy { get; set; } = null!;
    public DateTime CreateDate { get; set; }
}

public class GetPrePlanResponse
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = null!;
    public string JobLocation { get; set; } = null!;
    public string JobType { get; set; } = null!;
    public int ProductionRound { get; set; }
    public string MoldCode { get; set; } = null!;
    public string? ProductType { get; set; }
    public string GoldType { get; set; } = null!;
    public string? MoldDetail { get; set; }
    public string? Remark { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string Status { get; set; } = null!;
    public int? ProductQty { get; set; }
    public string? RejectReason { get; set; }
    public int? LinkedProductionPlanId { get; set; }
    public string CreateBy { get; set; } = null!;
    public DateTime CreateDate { get; set; }
    public string? SubmitBy { get; set; }
    public DateTime? SubmitDate { get; set; }
    public string? ApproveBy { get; set; }
    public DateTime? ApproveDate { get; set; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }

    public IList<GetPrePlanMaterialResponse> Materials { get; set; } = new List<GetPrePlanMaterialResponse>();
}

public class GetPrePlanMaterialResponse
{
    public int Id { get; set; }
    public int PrePlanId { get; set; }
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
    public string CreateBy { get; set; } = null!;
    public DateTime CreateDate { get; set; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
}
