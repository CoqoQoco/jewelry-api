using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.PrePlan;

public class SearchPrePlanResponse
{
    public int Id { get; set; }
    public string? OrderNo { get; set; }
    public int? ProductionRound { get; set; }
    public string? JobType { get; set; }
    public string? JobLocation { get; set; }
    public string? GoldType { get; set; }
    public string Status { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string? CreateBy { get; set; }
    public DateTime? CreateDate { get; set; }
    public int ItemCount { get; set; }
    public string? PrimaryMoldCode { get; set; }
    public int LinkedItemCount { get; set; }
    public string? ApprovedDocumentPath { get; set; }
    public List<SearchPrePlanItemResponse> Items { get; set; } = new();
}

public class SearchPrePlanItemResponse
{
    public int Id { get; set; }
    public int ItemNo { get; set; }
    public string MoldCode { get; set; } = null!;
    public string? ProductType { get; set; }
    public int? ProductQty { get; set; }
    public string? ProductQtyUnit { get; set; }
    public string? Wo { get; set; }
    public int? WoNumber { get; set; }
    public string? WoText { get; set; }
    public int? LinkedProductionPlanId { get; set; }
    public string? PlanStatus { get; set; }
    public bool IsCancelled { get; set; }
    public List<SearchPrePlanMaterialBrief> Materials { get; set; } = new();
}

public class SearchPrePlanMaterialBrief
{
    public string? Gold { get; set; }
    public decimal? GoldQty { get; set; }
    public string? Gem { get; set; }
    public string? GemShape { get; set; }
    public decimal? GemQty { get; set; }
}

public class GetPrePlanResponse
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

    public List<GetPrePlanItemResponse> Items { get; set; } = new List<GetPrePlanItemResponse>();
}

public class GetPrePlanItemResponse
{
    public int Id { get; set; }
    public int PrePlanId { get; set; }
    public int ItemNo { get; set; }
    public string MoldCode { get; set; } = null!;
    public string? MoldDetail { get; set; }
    public string? ProductType { get; set; }
    public int? ProductQty { get; set; }
    public string? ProductQtyUnit { get; set; }
    public string? ProductDetail { get; set; }
    public string? ProductImagePath { get; set; }
    public int? LinkedProductionPlanId { get; set; }
    public string? Wo { get; set; }
    public int? WoNumber { get; set; }
    public string? WoText { get; set; }
    public string? CreateBy { get; set; }
    public DateTime? CreateDate { get; set; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public bool IsCancelled { get; set; }

    public List<GetPrePlanMaterialResponse> Materials { get; set; } = new List<GetPrePlanMaterialResponse>();
}

public class AvailableForPlanResponse
{
    public int PrePlanId { get; set; }
    public string? OrderNo { get; set; }
    public string? GoldType { get; set; }
    public string? JobType { get; set; }
    public string? JobLocation { get; set; }
    public DateTime DeliveryDate { get; set; }
    public int ItemId { get; set; }
    public int ItemNo { get; set; }
    public string MoldCode { get; set; } = null!;
    public string? MoldDetail { get; set; }
    public string? ProductType { get; set; }
    public int? ProductQty { get; set; }
    public string? ProductQtyUnit { get; set; }
    public string? ProductDetail { get; set; }
    public List<GetPrePlanMaterialResponse> Materials { get; set; } = new List<GetPrePlanMaterialResponse>();
}

public class UploadApproveDocumentResponse
{
    public string Path { get; set; } = null!;
}

public class UploadProductImageResponse
{
    public string ImagePath { get; set; } = null!;
}

public class GetPrePlanMaterialResponse
{
    public int Id { get; set; }
    public int PrePlanItemId { get; set; }
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
    public string? CreateBy { get; set; }
    public DateTime? CreateDate { get; set; }
}
