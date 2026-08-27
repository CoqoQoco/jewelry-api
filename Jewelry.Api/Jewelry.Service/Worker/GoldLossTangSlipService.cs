using jewelry.Model.Exceptions;
using jewelry.Model.Worker.GoldLossTangSlip;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Worker
{
    public interface IGoldLossTangSlipService
    {
        List<SearchGoldLossTangJobsResponse> SearchJobs(SearchGoldLossTangJobsRequest request);
        Task<GoldLossTangSlipResponse> CreateSlip(CreateGoldLossTangSlipRequest request);
        Task<GoldLossTangSlipResponse> UpdateSlip(UpdateGoldLossTangSlipRequest request);
        List<GoldLossTangSlipSummaryResponse> ListSlips(ListGoldLossTangSlipRequest request);
        GoldLossTangSlipResponse GetSlip(long id);
        Task CancelSlip(long id);
        IQueryable<ReportGoldLossTangByWorkerResponse> ReportByWorker(ReportGoldLossTangByWorkerSearch request);
        GoldLossTangLineOptionsResponse GetLineOptions();
        ReportGoldLossTangMonthlyResponse ReportMonthly(ReportGoldLossTangMonthlyRequest request);
    }

    public class GoldLossTangSlipService : BaseService, IGoldLossTangSlipService
    {
        private readonly JewelryContext _jewelryContext;

        public GoldLossTangSlipService(
            JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor,
            IHostEnvironment hostingEnvironment) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public List<SearchGoldLossTangJobsResponse> SearchJobs(SearchGoldLossTangJobsRequest request)
        {
            var startUtc = request.RequestDateStart.HasValue
                ? request.RequestDateStart.Value.StartOfDayUtc()
                : (DateTimeOffset?)null;
            var endUtc = request.RequestDateEnd.HasValue
                ? request.RequestDateEnd.Value.EndOfDayUtc()
                : (DateTimeOffset?)null;

            var query = from item in _jewelryContext.TbtProductionPlanStatusDetail
                            .Include(x => x.Header)
                            .ThenInclude(x => x.ProductionPlan)
                        join slip in _jewelryContext.TbtGoldLossTangSlip
                            on item.GoldLossTangSlipId equals slip.Id into slipJoined
                        from slipJ in slipJoined.DefaultIfEmpty()
                        where item.IsActive
                           && item.Header.IsActive
                           && item.Header.Status == 50
                           && (item.Header.WorkerCode == request.WorkerCode.ToUpper()
                               || item.Worker == request.WorkerCode.ToUpper()
                               || item.WorkerSub == request.WorkerCode.ToUpper())
                        select new
                        {
                            item.ProductionPlanId,
                            item.ItemNo,
                            item.Header.ProductionPlan.Wo,
                            item.Header.ProductionPlan.WoNumber,
                            item.Header.ProductionPlan.WoText,
                            item.Header.ProductionPlan.ProductNumber,
                            item.Header.ProductionPlan.ProductName,
                            GoldSize = item.Header.ProductionPlan.TypeSize,
                            item.Gold,
                            item.RequestDate,
                            item.GoldQtySend,
                            item.GoldWeightSend,
                            item.GoldQtyCheck,
                            item.GoldWeightCheck,
                            item.GoldLossTangSlipId,
                            GoldLossTangSlipDocumentNo = slipJ != null ? slipJ.DocumentNo : null,
                        };

            if (startUtc.HasValue)
            {
                query = query.Where(x => x.RequestDate >= startUtc.Value.UtcDateTime);
            }

            if (endUtc.HasValue)
            {
                query = query.Where(x => x.RequestDate <= endUtc.Value.UtcDateTime);
            }

            if (!string.IsNullOrEmpty(request.Wo))
            {
                query = query.Where(x => x.Wo == request.Wo);
            }

            if (request.GoldSize != null && request.GoldSize.Any())
            {
                query = query.Where(x => request.GoldSize.Contains(x.GoldSize));
            }

            return query
                .OrderBy(x => x.RequestDate)
                .ThenBy(x => x.Wo)
                .ToList()
                .Select(x => new SearchGoldLossTangJobsResponse
                {
                    ProductionPlanId = x.ProductionPlanId,
                    ItemNo = x.ItemNo,
                    Wo = x.Wo,
                    WoNumber = x.WoNumber,
                    WoText = x.WoText,
                    ProductNumber = x.ProductNumber,
                    ProductName = x.ProductName,
                    Gold = x.Gold,
                    GoldSize = x.GoldSize,
                    JobDate = x.RequestDate,
                    GoldQtySend = x.GoldQtySend,
                    GoldWeightSend = x.GoldWeightSend,
                    GoldQtyCheck = x.GoldQtyCheck,
                    GoldWeightCheck = x.GoldWeightCheck,
                    GoldLossTangSlipId = x.GoldLossTangSlipId,
                    GoldLossTangSlipDocumentNo = x.GoldLossTangSlipDocumentNo,
                })
                .ToList();
        }

        public async Task<GoldLossTangSlipResponse> CreateSlip(CreateGoldLossTangSlipRequest request)
        {
            // Validate: must have at least 1 job or 1 custom line
            var hasItems = request.Items != null && request.Items.Any();
            var hasCustomLines = (request.IssuedLines != null && request.IssuedLines.Any())
                              || (request.ReturnedLines != null && request.ReturnedLines.Any());

            if (!hasItems && !hasCustomLines)
            {
                throw new HandleException("กรุณาเลือกงานหรือเพิ่มรายการเบิก/คืนอย่างน้อย 1 รายการ");
            }

            // Validate custom line weights >= 0
            foreach (var line in request.IssuedLines ?? new List<GoldLossTangExtraLine>())
            {
                if (line.Weight < 0)
                    throw new HandleException($"น้ำหนักรายการเบิก '{line.Name}' ต้องไม่ติดลบ");
            }
            foreach (var line in request.ReturnedLines ?? new List<GoldLossTangExtraLine>())
            {
                if (line.Weight < 0)
                    throw new HandleException($"น้ำหนักรายการคืน '{line.Name}' ต้องไม่ติดลบ");
            }

            // Re-read job data from DB — do NOT trust request weights
            List<TbtProductionPlanStatusDetail> loadedDetails = new List<TbtProductionPlanStatusDetail>();
            if (hasItems)
            {
                var planIds = request.Items.Select(x => x.ProductionPlanId).Distinct().ToList();
                var itemNos = request.Items.Select(x => x.ItemNo).Distinct().ToList();

                loadedDetails = _jewelryContext.TbtProductionPlanStatusDetail
                    .Include(d => d.Header)
                    .ThenInclude(h => h.ProductionPlan)
                    .Where(d => planIds.Contains(d.ProductionPlanId) && itemNos.Contains(d.ItemNo) && d.IsActive)
                    .ToList();

                // Guard: reject if any detail already assigned to a tang slip
                foreach (var itemReq in request.Items)
                {
                    var detail = loadedDetails.FirstOrDefault(d =>
                        d.ProductionPlanId == itemReq.ProductionPlanId && d.ItemNo == itemReq.ItemNo);
                    if (detail != null && detail.GoldLossTangSlipId != null)
                    {
                        throw new HandleException($"งานนี้ถูกใช้ในใบอื่นแล้ว (WO: {detail.Header?.ProductionPlan?.Wo}, ItemNo: {detail.ItemNo})");
                    }
                }
            }

            // Compute totals from re-read DB values
            decimal issuedFromJobs = loadedDetails.Sum(d => d.GoldWeightSend ?? 0);
            decimal returnedFromJobs = loadedDetails.Sum(d => d.GoldWeightCheck ?? 0);

            decimal issuedFromCustom = (request.IssuedLines ?? new List<GoldLossTangExtraLine>())
                .Where(l => l.CountInCalc).Sum(l => l.Weight);
            decimal returnedFromCustom = (request.ReturnedLines ?? new List<GoldLossTangExtraLine>())
                .Where(l => l.CountInCalc).Sum(l => l.Weight);

            decimal issuedTotal = issuedFromJobs + issuedFromCustom;
            decimal returnedTotal = returnedFromJobs + returnedFromCustom;

            // Formula (round-half-up, matching frontend Math.round)
            decimal rawLoss = issuedTotal - returnedTotal;
            // allowedLoss คิดจากฐานคืนตัวงาน (returnedFromJobs) ไม่รวม add-on
            decimal allowedLoss = RoundHalfUp(returnedFromJobs * request.LossPercent / 100m, 4);
            decimal diffLoss = RoundHalfUp(allowedLoss - rawLoss, 4);
            decimal totalMoneyDiff = RoundHalfUp(diffLoss, 2) * request.PricePerGram;

            var documentNo = await GenerateGltDocumentNo();

            var header = new TbtGoldLossTangSlip
            {
                DocumentNo = documentNo,
                WorkerCode = request.WorkerCode,
                WorkerName = request.WorkerName,
                RequestDateStart = request.RequestDateStart.UtcDateTime,
                RequestDateEnd = request.RequestDateEnd.UtcDateTime,
                LossPercent = request.LossPercent,
                PricePerGram = request.PricePerGram,
                IssuedTotal = issuedTotal,
                ReturnedTotal = returnedTotal,
                RawLoss = rawLoss,
                AllowedLoss = allowedLoss,
                DiffLoss = diffLoss,
                TotalMoneyDiff = totalMoneyDiff,
                Remark = request.Remark,
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername,
            };

            _jewelryContext.TbtGoldLossTangSlip.Add(header);
            await _jewelryContext.SaveChangesAsync();

            // Insert item snapshots
            var items = new List<TbtGoldLossTangSlipItem>();
            foreach (var itemReq in request.Items ?? new List<CreateGoldLossTangSlipItem>())
            {
                var detail = loadedDetails.FirstOrDefault(d =>
                    d.ProductionPlanId == itemReq.ProductionPlanId && d.ItemNo == itemReq.ItemNo);

                items.Add(new TbtGoldLossTangSlipItem
                {
                    SlipId = header.Id,
                    ProductionPlanId = itemReq.ProductionPlanId,
                    ItemNo = itemReq.ItemNo,
                    Wo = detail?.Header?.ProductionPlan?.Wo,
                    WoNumber = detail?.Header?.ProductionPlan?.WoNumber,
                    ProductNumber = detail?.Header?.ProductionPlan?.ProductNumber,
                    ProductName = detail?.Header?.ProductionPlan?.ProductName,
                    Gold = detail?.Gold,
                    GoldSize = detail?.Header?.ProductionPlan?.TypeSize,
                    JobDate = detail?.RequestDate,
                    GoldQtySend = detail?.GoldQtySend,
                    GoldWeightSend = detail?.GoldWeightSend,
                    GoldQtyCheck = detail?.GoldQtyCheck,
                    GoldWeightCheck = detail?.GoldWeightCheck,
                    IsActive = true,
                });
            }

            if (items.Any())
            {
                _jewelryContext.TbtGoldLossTangSlipItem.AddRange(items);
                await _jewelryContext.SaveChangesAsync();
            }

            // Insert extra lines
            var extras = new List<TbtGoldLossTangSlipExtra>();
            foreach (var line in request.IssuedLines ?? new List<GoldLossTangExtraLine>())
            {
                extras.Add(new TbtGoldLossTangSlipExtra
                {
                    SlipId = header.Id,
                    Kind = 1,
                    Name = line.Name,
                    Weight = line.Weight,
                    CountInCalc = line.CountInCalc,
                    IsActive = true,
                });
            }
            foreach (var line in request.ReturnedLines ?? new List<GoldLossTangExtraLine>())
            {
                extras.Add(new TbtGoldLossTangSlipExtra
                {
                    SlipId = header.Id,
                    Kind = 2,
                    Name = line.Name,
                    Weight = line.Weight,
                    CountInCalc = line.CountInCalc,
                    IsActive = true,
                });
            }

            if (extras.Any())
            {
                _jewelryContext.TbtGoldLossTangSlipExtra.AddRange(extras);
                await _jewelryContext.SaveChangesAsync();
            }

            // Stamp GoldLossTangSlipId on status details
            var detailsToStamp = new List<TbtProductionPlanStatusDetail>();
            foreach (var itemReq in request.Items ?? new List<CreateGoldLossTangSlipItem>())
            {
                var detail = loadedDetails.FirstOrDefault(d =>
                    d.ProductionPlanId == itemReq.ProductionPlanId && d.ItemNo == itemReq.ItemNo);
                if (detail != null)
                {
                    detail.GoldLossTangSlipId = header.Id;
                    detailsToStamp.Add(detail);
                }
            }

            if (detailsToStamp.Any())
            {
                _jewelryContext.TbtProductionPlanStatusDetail.UpdateRange(detailsToStamp);
                await _jewelryContext.SaveChangesAsync();
            }

            return MapToResponse(header, items, extras);
        }

        public async Task<GoldLossTangSlipResponse> UpdateSlip(UpdateGoldLossTangSlipRequest request)
        {
            var slip = await _jewelryContext.TbtGoldLossTangSlip
                .Include(x => x.TbtGoldLossTangSlipItem)
                .Include(x => x.TbtGoldLossTangSlipExtra)
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.IsActive);

            if (slip == null)
                throw new HandleException("ไม่พบใบงาน หรือถูกยกเลิกแล้ว");

            // Validate: must have at least 1 job or 1 custom line
            var hasItems = request.Items != null && request.Items.Any();
            var hasCustomLines = (request.IssuedLines != null && request.IssuedLines.Any())
                              || (request.ReturnedLines != null && request.ReturnedLines.Any());

            if (!hasItems && !hasCustomLines)
            {
                throw new HandleException("กรุณาเลือกงานหรือเพิ่มรายการเบิก/คืนอย่างน้อย 1 รายการ");
            }

            // Validate custom line weights >= 0
            foreach (var line in request.IssuedLines ?? new List<GoldLossTangExtraLine>())
            {
                if (line.Weight < 0)
                    throw new HandleException($"น้ำหนักรายการเบิก '{line.Name}' ต้องไม่ติดลบ");
            }
            foreach (var line in request.ReturnedLines ?? new List<GoldLossTangExtraLine>())
            {
                if (line.Weight < 0)
                    throw new HandleException($"น้ำหนักรายการคืน '{line.Name}' ต้องไม่ติดลบ");
            }

            // Un-stamp all details currently linked to this slip (restore)
            var oldStampedDetails = await _jewelryContext.TbtProductionPlanStatusDetail
                .Where(d => d.GoldLossTangSlipId == slip.Id)
                .ToListAsync();

            foreach (var d in oldStampedDetails)
            {
                d.GoldLossTangSlipId = null;
            }

            // Re-read new selected details from DB
            List<TbtProductionPlanStatusDetail> loadedDetails = new List<TbtProductionPlanStatusDetail>();
            if (hasItems)
            {
                var planIds = request.Items.Select(x => x.ProductionPlanId).Distinct().ToList();
                var itemNos = request.Items.Select(x => x.ItemNo).Distinct().ToList();

                loadedDetails = _jewelryContext.TbtProductionPlanStatusDetail
                    .Include(d => d.Header)
                    .ThenInclude(h => h.ProductionPlan)
                    .Where(d => planIds.Contains(d.ProductionPlanId) && itemNos.Contains(d.ItemNo) && d.IsActive)
                    .ToList();

                // Guard: reject if any detail is stamped to ANOTHER slip (un-stamp above already cleared this slip's own)
                foreach (var itemReq in request.Items)
                {
                    var detail = loadedDetails.FirstOrDefault(d =>
                        d.ProductionPlanId == itemReq.ProductionPlanId && d.ItemNo == itemReq.ItemNo);
                    if (detail != null && detail.GoldLossTangSlipId != null && detail.GoldLossTangSlipId != slip.Id)
                    {
                        throw new HandleException($"งานนี้ถูกใช้ในใบอื่นแล้ว (WO: {detail.Header?.ProductionPlan?.Wo}, ItemNo: {detail.ItemNo})");
                    }
                }
            }

            // Compute totals from re-read DB values
            decimal issuedFromJobs = loadedDetails.Sum(d => d.GoldWeightSend ?? 0);
            decimal returnedFromJobs = loadedDetails.Sum(d => d.GoldWeightCheck ?? 0);

            decimal issuedFromCustom = (request.IssuedLines ?? new List<GoldLossTangExtraLine>())
                .Where(l => l.CountInCalc).Sum(l => l.Weight);
            decimal returnedFromCustom = (request.ReturnedLines ?? new List<GoldLossTangExtraLine>())
                .Where(l => l.CountInCalc).Sum(l => l.Weight);

            decimal issuedTotal = issuedFromJobs + issuedFromCustom;
            decimal returnedTotal = returnedFromJobs + returnedFromCustom;

            decimal rawLoss = issuedTotal - returnedTotal;
            // allowedLoss คิดจากฐานคืนตัวงาน (returnedFromJobs) ไม่รวม add-on
            decimal allowedLoss = RoundHalfUp(returnedFromJobs * request.LossPercent / 100m, 4);
            decimal diffLoss = RoundHalfUp(allowedLoss - rawLoss, 4);
            decimal totalMoneyDiff = RoundHalfUp(diffLoss, 2) * request.PricePerGram;

            // Update header fields
            slip.WorkerCode = request.WorkerCode;
            slip.WorkerName = request.WorkerName;
            slip.RequestDateStart = request.RequestDateStart.UtcDateTime;
            slip.RequestDateEnd = request.RequestDateEnd.UtcDateTime;
            slip.LossPercent = request.LossPercent;
            slip.PricePerGram = request.PricePerGram;
            slip.Remark = request.Remark;
            slip.IssuedTotal = issuedTotal;
            slip.ReturnedTotal = returnedTotal;
            slip.RawLoss = rawLoss;
            slip.AllowedLoss = allowedLoss;
            slip.DiffLoss = diffLoss;
            slip.TotalMoneyDiff = totalMoneyDiff;
            slip.UpdateDate = DateTime.UtcNow;
            slip.UpdateBy = CurrentUsername;

            _jewelryContext.TbtGoldLossTangSlip.Update(slip);

            // Soft-delete old children
            var oldItems = slip.TbtGoldLossTangSlipItem.ToList();
            var oldExtras = slip.TbtGoldLossTangSlipExtra.ToList();

            foreach (var oi in oldItems) oi.IsActive = false;
            foreach (var oe in oldExtras) oe.IsActive = false;

            if (oldItems.Any()) _jewelryContext.TbtGoldLossTangSlipItem.UpdateRange(oldItems);
            if (oldExtras.Any()) _jewelryContext.TbtGoldLossTangSlipExtra.UpdateRange(oldExtras);

            // Build new item snapshots
            var newItems = new List<TbtGoldLossTangSlipItem>();
            foreach (var itemReq in request.Items ?? new List<CreateGoldLossTangSlipItem>())
            {
                var detail = loadedDetails.FirstOrDefault(d =>
                    d.ProductionPlanId == itemReq.ProductionPlanId && d.ItemNo == itemReq.ItemNo);

                newItems.Add(new TbtGoldLossTangSlipItem
                {
                    SlipId = slip.Id,
                    ProductionPlanId = itemReq.ProductionPlanId,
                    ItemNo = itemReq.ItemNo,
                    Wo = detail?.Header?.ProductionPlan?.Wo,
                    WoNumber = detail?.Header?.ProductionPlan?.WoNumber,
                    ProductNumber = detail?.Header?.ProductionPlan?.ProductNumber,
                    ProductName = detail?.Header?.ProductionPlan?.ProductName,
                    Gold = detail?.Gold,
                    GoldSize = detail?.Header?.ProductionPlan?.TypeSize,
                    JobDate = detail?.RequestDate,
                    GoldQtySend = detail?.GoldQtySend,
                    GoldWeightSend = detail?.GoldWeightSend,
                    GoldQtyCheck = detail?.GoldQtyCheck,
                    GoldWeightCheck = detail?.GoldWeightCheck,
                    IsActive = true,
                });
            }

            // Build new extra lines
            var newExtras = new List<TbtGoldLossTangSlipExtra>();
            foreach (var line in request.IssuedLines ?? new List<GoldLossTangExtraLine>())
            {
                newExtras.Add(new TbtGoldLossTangSlipExtra
                {
                    SlipId = slip.Id,
                    Kind = 1,
                    Name = line.Name,
                    Weight = line.Weight,
                    CountInCalc = line.CountInCalc,
                    IsActive = true,
                });
            }
            foreach (var line in request.ReturnedLines ?? new List<GoldLossTangExtraLine>())
            {
                newExtras.Add(new TbtGoldLossTangSlipExtra
                {
                    SlipId = slip.Id,
                    Kind = 2,
                    Name = line.Name,
                    Weight = line.Weight,
                    CountInCalc = line.CountInCalc,
                    IsActive = true,
                });
            }

            if (newItems.Any()) _jewelryContext.TbtGoldLossTangSlipItem.AddRange(newItems);
            if (newExtras.Any()) _jewelryContext.TbtGoldLossTangSlipExtra.AddRange(newExtras);

            // Un-stamp old details (already set null above) — mark for update
            if (oldStampedDetails.Any())
            {
                _jewelryContext.TbtProductionPlanStatusDetail.UpdateRange(oldStampedDetails);
            }

            await _jewelryContext.SaveChangesAsync();

            // Re-stamp new details
            var detailsToStamp = new List<TbtProductionPlanStatusDetail>();
            foreach (var itemReq in request.Items ?? new List<CreateGoldLossTangSlipItem>())
            {
                var detail = loadedDetails.FirstOrDefault(d =>
                    d.ProductionPlanId == itemReq.ProductionPlanId && d.ItemNo == itemReq.ItemNo);
                if (detail != null)
                {
                    detail.GoldLossTangSlipId = slip.Id;
                    detailsToStamp.Add(detail);
                }
            }

            if (detailsToStamp.Any())
            {
                _jewelryContext.TbtProductionPlanStatusDetail.UpdateRange(detailsToStamp);
                await _jewelryContext.SaveChangesAsync();
            }

            return MapToResponse(slip, newItems, newExtras);
        }

        public List<GoldLossTangSlipSummaryResponse> ListSlips(ListGoldLossTangSlipRequest request)
        {
            var query = _jewelryContext.TbtGoldLossTangSlip
                .Where(x => x.IsActive);

            if (!string.IsNullOrEmpty(request.WorkerCode))
            {
                query = query.Where(x => x.WorkerCode == request.WorkerCode.ToUpper());
            }

            if (!string.IsNullOrEmpty(request.DocumentNo))
            {
                query = query.Where(x => x.DocumentNo.Contains(request.DocumentNo.ToUpper()));
            }

            if (request.RequestDateStart.HasValue)
            {
                var startUtc = request.RequestDateStart.Value.StartOfDayUtc();
                query = query.Where(x => x.RequestDateEnd >= startUtc.UtcDateTime);
            }

            if (request.RequestDateEnd.HasValue)
            {
                var endUtc = request.RequestDateEnd.Value.EndOfDayUtc();
                query = query.Where(x => x.RequestDateStart <= endUtc.UtcDateTime);
            }

            var result = query
                .OrderByDescending(x => x.CreateDate)
                .Select(x => new GoldLossTangSlipSummaryResponse
                {
                    Id = x.Id,
                    DocumentNo = x.DocumentNo,
                    WorkerCode = x.WorkerCode,
                    WorkerName = x.WorkerName,
                    RequestDateStart = x.RequestDateStart,
                    RequestDateEnd = x.RequestDateEnd,
                    LossPercent = x.LossPercent,
                    PricePerGram = x.PricePerGram,
                    IssuedTotal = x.IssuedTotal,
                    ReturnedTotal = x.ReturnedTotal,
                    DiffLoss = x.DiffLoss,
                    TotalMoneyDiff = x.TotalMoneyDiff,
                    Remark = x.Remark,
                    IsActive = x.IsActive,
                    CreateDate = x.CreateDate,
                    CreateBy = x.CreateBy,
                    UpdateDate = x.UpdateDate,
                    UpdateBy = x.UpdateBy,
                });

            if (request.Skip.HasValue)
            {
                result = result.Skip(request.Skip.Value);
            }

            if (request.Take.HasValue)
            {
                result = result.Take(request.Take.Value);
            }

            return result.ToList();
        }

        public GoldLossTangSlipResponse GetSlip(long id)
        {
            var header = _jewelryContext.TbtGoldLossTangSlip
                .Include(x => x.TbtGoldLossTangSlipItem)
                .Include(x => x.TbtGoldLossTangSlipExtra)
                .FirstOrDefault(x => x.Id == id);

            if (header == null)
            {
                throw new HandleException($"ไม่พบ Gold Loss Tang Slip Id: {id}");
            }

            return MapToResponse(
                header,
                header.TbtGoldLossTangSlipItem.ToList(),
                header.TbtGoldLossTangSlipExtra.ToList());
        }

        public async Task CancelSlip(long id)
        {
            var slip = await _jewelryContext.TbtGoldLossTangSlip
                .Include(x => x.TbtGoldLossTangSlipItem)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive == true);

            if (slip == null)
                throw new HandleException("ไม่พบใบงาน หรือถูกยกเลิกแล้ว");

            slip.IsActive = false;
            slip.UpdateDate = DateTime.UtcNow;
            slip.UpdateBy = CurrentUsername;

            _jewelryContext.TbtGoldLossTangSlip.Update(slip);

            // Clear stamp on linked status details
            var details = await _jewelryContext.TbtProductionPlanStatusDetail
                .Where(d => d.GoldLossTangSlipId == id)
                .ToListAsync();

            foreach (var d in details)
            {
                d.GoldLossTangSlipId = null;
            }

            if (details.Any())
            {
                _jewelryContext.TbtProductionPlanStatusDetail.UpdateRange(details);
            }

            await _jewelryContext.SaveChangesAsync();
        }

        public IQueryable<ReportGoldLossTangByWorkerResponse> ReportByWorker(ReportGoldLossTangByWorkerSearch request)
        {
            var query = _jewelryContext.TbtGoldLossTangSlip
                .Where(x => x.IsActive);

            if (request.RequestDateStart.HasValue)
            {
                var startUtc = request.RequestDateStart.Value.StartOfDayUtc();
                query = query.Where(x => x.RequestDateEnd >= startUtc.UtcDateTime);
            }

            if (request.RequestDateEnd.HasValue)
            {
                var endUtc = request.RequestDateEnd.Value.EndOfDayUtc();
                query = query.Where(x => x.RequestDateEnd <= endUtc.UtcDateTime);
            }

            if (!string.IsNullOrEmpty(request.WorkerCode))
            {
                query = query.Where(x => x.WorkerCode == request.WorkerCode.ToUpper());
            }

            if (request.GroupByMonth)
            {
                return query
                    .Select(x => new
                    {
                        x.WorkerCode,
                        x.WorkerName,
                        Local = x.RequestDateEnd.Value.AddHours(7),
                        x.IssuedTotal,
                        x.ReturnedTotal,
                        x.RawLoss,
                        x.AllowedLoss,
                        x.DiffLoss,
                        x.TotalMoneyDiff,
                    })
                    .GroupBy(x => new { x.WorkerCode, x.WorkerName, Year = x.Local.Date.Year, Month = x.Local.Date.Month })
                    .Select(g => new ReportGoldLossTangByWorkerResponse
                    {
                        WorkerCode = g.Key.WorkerCode,
                        WorkerName = g.Key.WorkerName,
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        SlipCount = g.Count(),
                        TotalIssued = g.Sum(x => x.IssuedTotal),
                        TotalReturned = g.Sum(x => x.ReturnedTotal),
                        TotalRawLoss = g.Sum(x => x.RawLoss),
                        TotalAllowedLoss = g.Sum(x => x.AllowedLoss),
                        TotalDiffLoss = g.Sum(x => x.DiffLoss),
                        TotalMoneyDiff = g.Sum(x => x.TotalMoneyDiff),
                    });
            }

            return query
                .GroupBy(x => new { x.WorkerCode, x.WorkerName })
                .Select(g => new ReportGoldLossTangByWorkerResponse
                {
                    WorkerCode = g.Key.WorkerCode,
                    WorkerName = g.Key.WorkerName,
                    SlipCount = g.Count(),
                    TotalIssued = g.Sum(x => x.IssuedTotal),
                    TotalReturned = g.Sum(x => x.ReturnedTotal),
                    TotalRawLoss = g.Sum(x => x.RawLoss),
                    TotalAllowedLoss = g.Sum(x => x.AllowedLoss),
                    TotalDiffLoss = g.Sum(x => x.DiffLoss),
                    TotalMoneyDiff = g.Sum(x => x.TotalMoneyDiff),
                });
        }

        public ReportGoldLossTangMonthlyResponse ReportMonthly(ReportGoldLossTangMonthlyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WorkerCode))
            {
                throw new HandleException("กรุณาระบุรหัสช่าง");
            }

            var workerCode = request.WorkerCode.ToUpper();

            var query = _jewelryContext.TbtGoldLossTangSlip
                .Include(x => x.TbtGoldLossTangSlipItem)
                .Include(x => x.TbtGoldLossTangSlipExtra)
                .AsSplitQuery()
                .Where(x => x.IsActive && x.WorkerCode == workerCode);

            if (request.RequestDateStart.HasValue)
            {
                var startUtc = request.RequestDateStart.Value.StartOfDayUtc();
                query = query.Where(x => x.RequestDateEnd >= startUtc.UtcDateTime);
            }

            if (request.RequestDateEnd.HasValue)
            {
                var endUtc = request.RequestDateEnd.Value.EndOfDayUtc();
                query = query.Where(x => x.RequestDateEnd <= endUtc.UtcDateTime);
            }

            var slips = query
                .OrderBy(x => x.RequestDateStart)
                .ThenBy(x => x.DocumentNo)
                .ToList();

            var response = new ReportGoldLossTangMonthlyResponse
            {
                WorkerCode = workerCode,
                RequestDateStart = request.RequestDateStart,
                RequestDateEnd = request.RequestDateEnd,
                SlipCount = slips.Count,
            };

            if (!slips.Any())
            {
                var worker = _jewelryContext.TbmWorker.FirstOrDefault(w => w.Code == workerCode);
                response.WorkerName = worker?.NameTh;
                return response;
            }

            response.WorkerName = slips.First().WorkerName;

            var slipDtos = slips.Select(slip =>
            {
                var items = slip.TbtGoldLossTangSlipItem
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.JobDate)
                    .ThenBy(i => i.Wo)
                    .ThenBy(i => i.WoNumber)
                    .ToList();
                var extras = slip.TbtGoldLossTangSlipExtra
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.Kind)
                    .ThenBy(e => e.Id)
                    .ToList();

                var lossPercent = slip.LossPercent ?? 0;

                return new ReportGoldLossTangMonthlySlip
                {
                    Id = slip.Id,
                    DocumentNo = slip.DocumentNo,
                    RequestDateStart = slip.RequestDateStart,
                    RequestDateEnd = slip.RequestDateEnd,
                    GoldSize = items.FirstOrDefault()?.GoldSize,
                    LossPercent = slip.LossPercent,
                    PricePerGram = slip.PricePerGram,
                    IssuedTotal = slip.IssuedTotal,
                    ReturnedTotal = slip.ReturnedTotal,
                    RawLoss = slip.RawLoss,
                    AllowedLoss = slip.AllowedLoss,
                    DiffLoss = slip.DiffLoss,
                    TotalMoneyDiff = slip.TotalMoneyDiff,
                    Items = items.Select(i => new ReportGoldLossTangMonthlyItem
                    {
                        JobDate = i.JobDate,
                        Wo = i.Wo,
                        WoNumber = i.WoNumber,
                        ProductNumber = i.ProductNumber,
                        ProductName = i.ProductName,
                        Gold = i.Gold,
                        GoldSize = i.GoldSize,
                        GoldQtyCheck = i.GoldQtyCheck,
                        GoldWeightSend = i.GoldWeightSend,
                        GoldWeightCheck = i.GoldWeightCheck,
                        WeightLossAllowed = (i.GoldWeightCheck ?? 0) * lossPercent / 100m,
                    }).ToList(),
                    Extras = extras.Select(e => new ReportGoldLossTangMonthlyExtra
                    {
                        Kind = e.Kind,
                        Name = e.Name,
                        Weight = e.Weight,
                        CountInCalc = e.CountInCalc,
                    }).ToList(),
                };
            }).ToList();

            response.Slips = slipDtos;

            response.GoldTypeSummaries = slipDtos
                .GroupBy(s => s.GoldSize)
                .Select(g => new ReportGoldLossTangMonthlyGoldTypeSummary
                {
                    GoldSize = g.Key,
                    PricePerGram = g.First().PricePerGram,
                    IssuedTotal = g.Sum(s => s.IssuedTotal ?? 0),
                    ReturnedTotal = g.Sum(s => s.ReturnedTotal ?? 0),
                    RawLoss = g.Sum(s => s.RawLoss ?? 0),
                    AllowedLoss = g.Sum(s => s.AllowedLoss ?? 0),
                    DiffLoss = g.Sum(s => s.DiffLoss ?? 0),
                    TotalMoneyDiff = g.Sum(s => s.TotalMoneyDiff ?? 0),
                }).ToList();

            response.TotalIssued = slipDtos.Sum(s => s.IssuedTotal ?? 0);
            response.TotalReturned = slipDtos.Sum(s => s.ReturnedTotal ?? 0);
            response.TotalRawLoss = slipDtos.Sum(s => s.RawLoss ?? 0);
            response.TotalAllowedLoss = slipDtos.Sum(s => s.AllowedLoss ?? 0);
            response.TotalDiffLoss = slipDtos.Sum(s => s.DiffLoss ?? 0);
            response.NetPayAmount = slipDtos.Sum(s => s.TotalMoneyDiff ?? 0);

            return response;
        }

        public GoldLossTangLineOptionsResponse GetLineOptions()
        {
            var issued = _jewelryContext.TbtGoldLossTangSlipExtra
                .Where(x => x.IsActive && x.Kind == 1 && x.Name != null && x.Name != "")
                .GroupBy(x => x.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key!)
                .ToList();
            var returned = _jewelryContext.TbtGoldLossTangSlipExtra
                .Where(x => x.IsActive && x.Kind == 2 && x.Name != null && x.Name != "")
                .GroupBy(x => x.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key!)
                .ToList();
            return new GoldLossTangLineOptionsResponse { Issued = issued, Returned = returned };
        }

        private async Task<string> GenerateGltDocumentNo()
        {
            var now = DateTime.UtcNow;
            var dateStr = now.ToString("yyyyMM");
            var key = $"GLT-{dateStr}";

            var running = await _jewelryContext.TbtRunningNumber.FindAsync(key);
            if (running == null)
            {
                running = new TbtRunningNumber { Key = key, Number = 1 };
                _jewelryContext.TbtRunningNumber.Add(running);
            }
            else
            {
                running.Number += 1;
                _jewelryContext.TbtRunningNumber.Update(running);
            }
            await _jewelryContext.SaveChangesAsync();

            return $"GLT-{dateStr}-{running.Number:000}";
        }

        private GoldLossTangSlipResponse MapToResponse(
            TbtGoldLossTangSlip header,
            List<TbtGoldLossTangSlipItem> items,
            List<TbtGoldLossTangSlipExtra> extras)
        {
            var issuedLines = extras.Where(e => e.Kind == 1 && e.IsActive).ToList();
            var returnedLines = extras.Where(e => e.Kind == 2 && e.IsActive).ToList();

            return new GoldLossTangSlipResponse
            {
                Id = header.Id,
                DocumentNo = header.DocumentNo,
                WorkerCode = header.WorkerCode,
                WorkerName = header.WorkerName,
                RequestDateStart = header.RequestDateStart,
                RequestDateEnd = header.RequestDateEnd,
                LossPercent = header.LossPercent,
                PricePerGram = header.PricePerGram,
                IssuedTotal = header.IssuedTotal,
                ReturnedTotal = header.ReturnedTotal,
                RawLoss = header.RawLoss,
                AllowedLoss = header.AllowedLoss,
                DiffLoss = header.DiffLoss,
                TotalMoneyDiff = header.TotalMoneyDiff,
                Remark = header.Remark,
                IsActive = header.IsActive,
                CreateDate = header.CreateDate,
                CreateBy = header.CreateBy,
                UpdateDate = header.UpdateDate,
                UpdateBy = header.UpdateBy,
                Items = items.Where(i => i.IsActive).Select(i => new GoldLossTangSlipItemResponse
                {
                    Id = i.Id,
                    SlipId = i.SlipId,
                    ProductionPlanId = i.ProductionPlanId,
                    ItemNo = i.ItemNo,
                    Wo = i.Wo,
                    WoNumber = i.WoNumber,
                    ProductNumber = i.ProductNumber,
                    ProductName = i.ProductName,
                    Gold = i.Gold,
                    GoldSize = i.GoldSize,
                    JobDate = i.JobDate,
                    GoldQtySend = i.GoldQtySend,
                    GoldWeightSend = i.GoldWeightSend,
                    GoldQtyCheck = i.GoldQtyCheck,
                    GoldWeightCheck = i.GoldWeightCheck,
                }).ToList(),
                IssuedLines = issuedLines.Select(e => new GoldLossTangExtraLineResponse
                {
                    Id = e.Id,
                    Kind = e.Kind,
                    Name = e.Name,
                    Weight = e.Weight,
                    CountInCalc = e.CountInCalc,
                }).ToList(),
                ReturnedLines = returnedLines.Select(e => new GoldLossTangExtraLineResponse
                {
                    Id = e.Id,
                    Kind = e.Kind,
                    Name = e.Name,
                    Weight = e.Weight,
                    CountInCalc = e.CountInCalc,
                }).ToList(),
                TypeSummaries = BuildTypeSummaries(items.Where(i => i.IsActive).ToList()),
            };
        }

        private static decimal RoundHalfUp(decimal value, int decimals)
        {
            decimal factor = 1m;
            for (int i = 0; i < decimals; i++) factor *= 10m;
            return Math.Floor(value * factor + 0.5m) / factor;
        }

        private static string PurityKey(string? gold, string? goldSize)
            => gold == "SV" ? "SILVER" : (!string.IsNullOrEmpty(goldSize) ? goldSize : (gold ?? ""));

        private List<GoldLossTangTypeSummaryResponse> BuildTypeSummaries(List<TbtGoldLossTangSlipItem> items)
        {
            var purityKeys = items
                .Select(i => PurityKey(i.Gold, i.GoldSize))
                .Distinct()
                .OrderBy(k => k)
                .ToList();

            return purityKeys.Select(purity => new GoldLossTangTypeSummaryResponse
            {
                Gold = null,
                GoldSize = purity == "" ? null : purity,
                IssuedWeight = items
                    .Where(i => PurityKey(i.Gold, i.GoldSize) == purity)
                    .Sum(i => i.GoldWeightSend ?? 0),
                ReturnedWeight = items
                    .Where(i => PurityKey(i.Gold, i.GoldSize) == purity)
                    .Sum(i => i.GoldWeightCheck ?? 0),
            }).ToList();
        }
    }
}
