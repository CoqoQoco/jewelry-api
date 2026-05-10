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
    private readonly IRunningNumber _runningNumber;

    public ProductionPrePlanService(JewelryContext jewelryContext,
        IHttpContextAccessor httpContextAccessor,
        IRunningNumber runningNumber)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
        _runningNumber = runningNumber;
    }

    public async Task<IList<SearchPrePlanResponse>> Search(SearchPrePlanRequest request)
    {
        var query = _jewelryContext.TbtProductionPrePlan
            .Include(x => x.Items)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.MoldCode))
            query = query.Where(x => x.Items.Any(i => i.MoldCode == request.MoldCode));

        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(x => x.Status == request.Status);

        if (request.OrderDateFrom.HasValue)
            query = query.Where(x => x.OrderDate >= request.OrderDateFrom.Value.StartOfDayUtc());

        if (request.OrderDateTo.HasValue)
            query = query.Where(x => x.OrderDate <= request.OrderDateTo.Value.EndOfDayUtc());

        var list = await query
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync();

        var result = list.Select(x => new SearchPrePlanResponse
        {
            Id = x.Id,
            OrderNo = x.OrderNo,
            ProductionRound = x.ProductionRound,
            JobType = x.JobType,
            JobLocation = x.JobLocation,
            GoldType = x.GoldType,
            Status = x.Status,
            OrderDate = x.OrderDate,
            DeliveryDate = x.DeliveryDate,
            CreateBy = x.CreateBy,
            CreateDate = x.CreateDate,
            ItemCount = x.Items.Count,
            PrimaryMoldCode = x.Items.OrderBy(i => i.ItemNo).FirstOrDefault()?.MoldCode,
        }).ToList();

        return result;
    }

    public async Task<GetPrePlanResponse> Get(int id)
    {
        var entity = await _jewelryContext.TbtProductionPrePlan
            .Include(x => x.Items)
                .ThenInclude(i => i.Materials)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new Exception($"ไม่พบ Pre-Plan id: {id}");

        var response = new GetPrePlanResponse
        {
            Id = entity.Id,
            OrderNo = entity.OrderNo,
            ProductionRound = entity.ProductionRound,
            JobType = entity.JobType,
            JobLocation = entity.JobLocation,
            GoldType = entity.GoldType,
            OrderDate = entity.OrderDate,
            DeliveryDate = entity.DeliveryDate,
            Remark = entity.Remark,
            Status = entity.Status,
            RejectReason = entity.RejectReason,
            CreateBy = entity.CreateBy,
            CreateDate = entity.CreateDate,
            UpdateBy = entity.UpdateBy,
            UpdateDate = entity.UpdateDate,
            SubmitBy = entity.SubmitBy,
            SubmitDate = entity.SubmitDate,
            ApproveBy = entity.ApproveBy,
            ApproveDate = entity.ApproveDate,
            Items = entity.Items.OrderBy(i => i.ItemNo).Select(i => new GetPrePlanItemResponse
            {
                Id = i.Id,
                PrePlanId = i.PrePlanId,
                ItemNo = i.ItemNo,
                MoldCode = i.MoldCode,
                MoldDetail = i.MoldDetail,
                ProductType = i.ProductType,
                ProductQty = i.ProductQty,
                ProductQtyUnit = i.ProductQtyUnit,
                ProductDetail = i.ProductDetail,
                ProductImagePath = i.ProductImagePath,
                LinkedProductionPlanId = i.LinkedProductionPlanId,
                CreateBy = i.CreateBy,
                CreateDate = i.CreateDate,
                UpdateBy = i.UpdateBy,
                UpdateDate = i.UpdateDate,
                Materials = i.Materials.Select(m => new GetPrePlanMaterialResponse
                {
                    Id = m.Id,
                    PrePlanItemId = m.PrePlanItemId,
                    Gold = m.Gold,
                    GoldSize = m.GoldSize,
                    GoldQty = m.GoldQty,
                    Gem = m.Gem,
                    GemShape = m.GemShape,
                    GemQty = m.GemQty,
                    GemUnit = m.GemUnit,
                    GemSize = m.GemSize,
                    GemWeight = m.GemWeight,
                    GemWeightUnit = m.GemWeightUnit,
                    DiamondQty = m.DiamondQty,
                    DiamondUnit = m.DiamondUnit,
                    DiamondSize = m.DiamondSize,
                    DiamondWeight = m.DiamondWeight,
                    DiamondWeightUnit = m.DiamondWeightUnit,
                    DiamondQuality = m.DiamondQuality,
                    CreateBy = m.CreateBy,
                    CreateDate = m.CreateDate,
                }).ToList(),
            }).ToList(),
        };

        return response;
    }

    public async Task<string> Create(CreatePrePlanRequest request)
    {
        var orderNo = await _runningNumber.GeneratePrePlanNumber();

        var entity = new TbtProductionPrePlan
        {
            OrderNo = orderNo,
            ProductionRound = request.ProductionRound,
            JobType = request.JobType,
            JobLocation = request.JobLocation,
            GoldType = request.GoldType,
            OrderDate = request.OrderDate.UtcDateTime,
            DeliveryDate = request.DeliveryDate.UtcDateTime,
            Remark = request.Remark,
            Status = "Draft",
            CreateBy = CurrentUsername,
            CreateDate = DateTime.UtcNow,
        };

        int itemIdx = 0;
        foreach (var itemReq in request.Items)
        {
            var item = new TbtProductionPrePlanItem
            {
                ItemNo = itemIdx + 1,
                MoldCode = itemReq.MoldCode,
                MoldDetail = itemReq.MoldDetail,
                ProductType = itemReq.ProductType,
                ProductQty = itemReq.ProductQty,
                ProductQtyUnit = itemReq.ProductQtyUnit,
                ProductDetail = itemReq.ProductDetail,
                ProductImagePath = itemReq.ProductImagePath,
                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow,
            };

            foreach (var matReq in itemReq.Materials)
            {
                item.Materials.Add(new TbtProductionPrePlanMaterial
                {
                    Gold = matReq.Gold,
                    GoldSize = matReq.GoldSize,
                    GoldQty = matReq.GoldQty,
                    Gem = matReq.Gem,
                    GemShape = matReq.GemShape,
                    GemQty = matReq.GemQty,
                    GemUnit = matReq.GemUnit,
                    GemSize = matReq.GemSize,
                    GemWeight = matReq.GemWeight,
                    GemWeightUnit = matReq.GemWeightUnit,
                    DiamondQty = matReq.DiamondQty,
                    DiamondUnit = matReq.DiamondUnit,
                    DiamondSize = matReq.DiamondSize,
                    DiamondWeight = matReq.DiamondWeight,
                    DiamondWeightUnit = matReq.DiamondWeightUnit,
                    DiamondQuality = matReq.DiamondQuality,
                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                });
            }

            entity.Items.Add(item);
            itemIdx++;
        }

        await _jewelryContext.TbtProductionPrePlan.AddAsync(entity);
        await _jewelryContext.SaveChangesAsync();

        return entity.Id.ToString();
    }

    public async Task<string> Update(int id, UpdatePrePlanRequest request)
    {
        var entity = await _jewelryContext.TbtProductionPrePlan
            .Include(x => x.Items)
                .ThenInclude(i => i.Materials)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new Exception($"ไม่พบ Pre-Plan id: {id}");

        if (entity.Status != "Draft")
            throw new Exception($"ไม่สามารถแก้ไขได้ เนื่องจากสถานะปัจจุบันคือ '{entity.Status}' (แก้ไขได้เฉพาะสถานะ Draft)");

        entity.ProductionRound = request.ProductionRound;
        entity.JobType = request.JobType;
        entity.JobLocation = request.JobLocation;
        entity.GoldType = request.GoldType;
        entity.OrderDate = request.OrderDate.UtcDateTime;
        entity.DeliveryDate = request.DeliveryDate.UtcDateTime;
        entity.Remark = request.Remark;
        entity.UpdateBy = CurrentUsername;
        entity.UpdateDate = DateTime.UtcNow;

        _jewelryContext.TbtProductionPrePlanItem.RemoveRange(entity.Items);

        _jewelryContext.TbtProductionPrePlan.Update(entity);
        await _jewelryContext.SaveChangesAsync();

        int itemIdx = 0;
        foreach (var itemReq in request.Items)
        {
            var item = new TbtProductionPrePlanItem
            {
                PrePlanId = id,
                ItemNo = itemIdx + 1,
                MoldCode = itemReq.MoldCode,
                MoldDetail = itemReq.MoldDetail,
                ProductType = itemReq.ProductType,
                ProductQty = itemReq.ProductQty,
                ProductQtyUnit = itemReq.ProductQtyUnit,
                ProductDetail = itemReq.ProductDetail,
                ProductImagePath = itemReq.ProductImagePath,
                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow,
            };

            foreach (var matReq in itemReq.Materials)
            {
                item.Materials.Add(new TbtProductionPrePlanMaterial
                {
                    Gold = matReq.Gold,
                    GoldSize = matReq.GoldSize,
                    GoldQty = matReq.GoldQty,
                    Gem = matReq.Gem,
                    GemShape = matReq.GemShape,
                    GemQty = matReq.GemQty,
                    GemUnit = matReq.GemUnit,
                    GemSize = matReq.GemSize,
                    GemWeight = matReq.GemWeight,
                    GemWeightUnit = matReq.GemWeightUnit,
                    DiamondQty = matReq.DiamondQty,
                    DiamondUnit = matReq.DiamondUnit,
                    DiamondSize = matReq.DiamondSize,
                    DiamondWeight = matReq.DiamondWeight,
                    DiamondWeightUnit = matReq.DiamondWeightUnit,
                    DiamondQuality = matReq.DiamondQuality,
                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                });
            }

            await _jewelryContext.TbtProductionPrePlanItem.AddAsync(item);
            itemIdx++;
        }

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

    public async Task<string> Approve(int id, ApprovePrePlanRequest request)
    {
        var entity = await _jewelryContext.TbtProductionPrePlan
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new Exception("ไม่พบใบสั่งผลิต");

        if (entity.Status != "Submitted")
            throw new Exception("ใบสั่งผลิตต้องอยู่ในสถานะรออนุมัติเท่านั้น");

        entity.Status = "Approved";
        entity.ApproveBy = CurrentUsername;
        entity.ApproveDate = DateTime.UtcNow;

        _jewelryContext.TbtProductionPrePlan.Update(entity);
        await _jewelryContext.SaveChangesAsync();

        return "อนุมัติสำเร็จ";
    }

    public async Task<string> Reject(int id, RejectPrePlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RejectReason))
            throw new Exception("กรุณาระบุเหตุผลการปฏิเสธ");

        var entity = await _jewelryContext.TbtProductionPrePlan
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new Exception("ไม่พบใบสั่งผลิต");

        if (entity.Status != "Submitted")
            throw new Exception("ใบสั่งผลิตต้องอยู่ในสถานะรออนุมัติเท่านั้น");

        entity.Status = "Rejected";
        entity.RejectReason = request.RejectReason;
        entity.ApproveBy = CurrentUsername;
        entity.ApproveDate = DateTime.UtcNow;

        _jewelryContext.TbtProductionPrePlan.Update(entity);
        await _jewelryContext.SaveChangesAsync();

        return "ปฏิเสธสำเร็จ";
    }

    public async Task<string> Consume(int id, ConsumePrePlanRequest request)
    {
        var entity = await _jewelryContext.TbtProductionPrePlan
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new Exception("ไม่พบใบสั่งผลิต");

        if (entity.Status != "Approved")
            throw new Exception("ใบสั่งผลิตต้องอนุมัติก่อนส่งเข้าผลิต");

        entity.Status = "Consumed";
        entity.UpdateBy = CurrentUsername;
        entity.UpdateDate = DateTime.UtcNow;

        _jewelryContext.TbtProductionPrePlan.Update(entity);
        await _jewelryContext.SaveChangesAsync();

        return "ส่งเข้าผลิตสำเร็จ";
    }
}
