using jewelry.Model.Production.PrePlan;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.PrePlan;

public class ProductionPrePlanService : BaseService, IProductionPrePlanService
{
    private readonly JewelryContext _jewelryContext;

    public ProductionPrePlanService(JewelryContext jewelryContext,
        IHttpContextAccessor httpContextAccessor)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
    }

    public async Task<IList<SearchPrePlanResponse>> Search(SearchPrePlanRequest request)
    {
        var query = _jewelryContext.TbtProductionPrePlan.AsQueryable();

        if (!string.IsNullOrEmpty(request.MoldCode))
            query = query.Where(x => x.MoldCode == request.MoldCode);

        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(x => x.Status == request.Status);

        if (request.OrderDateFrom.HasValue)
            query = query.Where(x => x.OrderDate >= request.OrderDateFrom.Value.StartOfDayUtc());

        if (request.OrderDateTo.HasValue)
            query = query.Where(x => x.OrderDate <= request.OrderDateTo.Value.EndOfDayUtc());

        var result = await query
            .OrderByDescending(x => x.CreateDate)
            .Select(x => new SearchPrePlanResponse
            {
                Id = x.Id,
                OrderNo = x.OrderNo,
                JobLocation = x.JobLocation,
                JobType = x.JobType,
                ProductionRound = x.ProductionRound,
                MoldCode = x.MoldCode,
                ProductType = x.ProductType,
                GoldType = x.GoldType,
                Status = x.Status,
                ProductQty = x.ProductQty,
                OrderDate = x.OrderDate,
                DeliveryDate = x.DeliveryDate,
                CreateBy = x.CreateBy,
                CreateDate = x.CreateDate,
            })
            .ToListAsync();

        return result;
    }

    public async Task<GetPrePlanResponse> Get(int id)
    {
        var entity = await _jewelryContext.TbtProductionPrePlan
            .Include(x => x.TbtProductionPrePlanMaterial)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new Exception($"ไม่พบ Pre-Plan id: {id}");

        var response = new GetPrePlanResponse
        {
            Id = entity.Id,
            OrderNo = entity.OrderNo,
            JobLocation = entity.JobLocation,
            JobType = entity.JobType,
            ProductionRound = entity.ProductionRound,
            MoldCode = entity.MoldCode,
            ProductType = entity.ProductType,
            GoldType = entity.GoldType,
            MoldDetail = entity.MoldDetail,
            Remark = entity.Remark,
            OrderDate = entity.OrderDate,
            DeliveryDate = entity.DeliveryDate,
            Status = entity.Status,
            ProductQty = entity.ProductQty,
            RejectReason = entity.RejectReason,
            LinkedProductionPlanId = entity.LinkedProductionPlanId,
            CreateBy = entity.CreateBy,
            CreateDate = entity.CreateDate,
            SubmitBy = entity.SubmitBy,
            SubmitDate = entity.SubmitDate,
            ApproveBy = entity.ApproveBy,
            ApproveDate = entity.ApproveDate,
            UpdateBy = entity.UpdateBy,
            UpdateDate = entity.UpdateDate,
            Materials = entity.TbtProductionPrePlanMaterial
                .Select(m => new GetPrePlanMaterialResponse
                {
                    Id = m.Id,
                    PrePlanId = m.PrePlanId,
                    MaterialType = m.MaterialType,
                    MasterId = m.MasterId,
                    MaterialCode = m.MaterialCode,
                    ShapeCode = m.ShapeCode,
                    Size = m.Size,
                    Qty = m.Qty,
                    Color = m.Color,
                    Weight = m.Weight,
                    WeightUnit = m.WeightUnit,
                    IsLocked = m.IsLocked,
                    Remark = m.Remark,
                    CreateBy = m.CreateBy,
                    CreateDate = m.CreateDate,
                    UpdateBy = m.UpdateBy,
                    UpdateDate = m.UpdateDate,
                })
                .ToList(),
        };

        return response;
    }

    public async Task<string> Create(CreatePrePlanRequest request)
    {
        var entity = new TbtProductionPrePlan
        {
            OrderNo = request.OrderNo,
            JobLocation = request.JobLocation,
            JobType = request.JobType,
            ProductionRound = request.ProductionRound,
            MoldCode = request.MoldCode,
            ProductType = request.ProductType,
            GoldType = request.GoldType,
            MoldDetail = request.MoldDetail,
            Remark = request.Remark,
            OrderDate = request.OrderDate.UtcDateTime,
            DeliveryDate = request.DeliveryDate.UtcDateTime,
            Status = "Draft",
            CreateBy = CurrentUsername,
            CreateDate = DateTime.UtcNow,
        };

        foreach (var m in request.Materials)
        {
            entity.TbtProductionPrePlanMaterial.Add(new TbtProductionPrePlanMaterial
            {
                MaterialType = m.MaterialType,
                MasterId = m.MasterId,
                MaterialCode = m.MaterialCode,
                ShapeCode = m.ShapeCode,
                Size = m.Size,
                Qty = m.Qty,
                Color = m.Color,
                Weight = m.Weight,
                WeightUnit = m.WeightUnit,
                IsLocked = m.IsLocked,
                Remark = m.Remark,
                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow,
            });
        }

        await _jewelryContext.TbtProductionPrePlan.AddAsync(entity);
        await _jewelryContext.SaveChangesAsync();

        return entity.Id.ToString();
    }

    public async Task<string> Update(int id, UpdatePrePlanRequest request)
    {
        var entity = await _jewelryContext.TbtProductionPrePlan
            .Include(x => x.TbtProductionPrePlanMaterial)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new Exception($"ไม่พบ Pre-Plan id: {id}");

        if (entity.Status != "Draft")
            throw new Exception($"ไม่สามารถแก้ไขได้ เนื่องจากสถานะปัจจุบันคือ '{entity.Status}' (แก้ไขได้เฉพาะสถานะ Draft)");

        entity.OrderNo = request.OrderNo;
        entity.JobLocation = request.JobLocation;
        entity.JobType = request.JobType;
        entity.ProductionRound = request.ProductionRound;
        entity.MoldCode = request.MoldCode;
        entity.ProductType = request.ProductType;
        entity.GoldType = request.GoldType;
        entity.MoldDetail = request.MoldDetail;
        entity.Remark = request.Remark;
        entity.OrderDate = request.OrderDate.UtcDateTime;
        entity.DeliveryDate = request.DeliveryDate.UtcDateTime;
        entity.UpdateBy = CurrentUsername;
        entity.UpdateDate = DateTime.UtcNow;

        _jewelryContext.TbtProductionPrePlanMaterial.RemoveRange(entity.TbtProductionPrePlanMaterial);

        var newMaterials = request.Materials.Select(m => new TbtProductionPrePlanMaterial
        {
            PrePlanId = id,
            MaterialType = m.MaterialType,
            MasterId = m.MasterId,
            MaterialCode = m.MaterialCode,
            ShapeCode = m.ShapeCode,
            Size = m.Size,
            Qty = m.Qty,
            Color = m.Color,
            Weight = m.Weight,
            WeightUnit = m.WeightUnit,
            IsLocked = m.IsLocked,
            Remark = m.Remark,
            CreateBy = CurrentUsername,
            CreateDate = DateTime.UtcNow,
        }).ToList();

        await _jewelryContext.TbtProductionPrePlanMaterial.AddRangeAsync(newMaterials);
        _jewelryContext.TbtProductionPrePlan.Update(entity);
        await _jewelryContext.SaveChangesAsync();

        return id.ToString();
    }

    public async Task<string> Submit(int id)
    {
        var entity = await _jewelryContext.TbtProductionPrePlan
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new Exception($"ไม่พบ Pre-Plan id: {id}");

        if (entity.Status != "Draft")
            throw new Exception($"ไม่สามารถ Submit ได้ เนื่องจากสถานะปัจจุบันคือ '{entity.Status}' (Submit ได้เฉพาะสถานะ Draft)");

        entity.Status = "Submitted";
        entity.SubmitBy = CurrentUsername;
        entity.SubmitDate = DateTime.UtcNow;

        _jewelryContext.TbtProductionPrePlan.Update(entity);
        await _jewelryContext.SaveChangesAsync();

        return id.ToString();
    }
}
