using jewelry.Model.Exceptions;
using jewelry.Model.Worker.GoldLossSlip;
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
    public interface IWorkerGoldLossSlipService
    {
        Task<GoldLossSlipResponse> CreateSlip(CreateGoldLossSlipRequest request);
        List<GoldLossSlipSummaryResponse> ListSlips(ListGoldLossSlipRequest request);
        GoldLossSlipResponse GetSlip(long id);
        Task CancelSlip(long id);
    }

    public class WorkerGoldLossSlipService : BaseService, IWorkerGoldLossSlipService
    {
        private readonly JewelryContext _jewelryContext;

        public WorkerGoldLossSlipService(
            JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor,
            IHostEnvironment hostingEnvironment) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public async Task<GoldLossSlipResponse> CreateSlip(CreateGoldLossSlipRequest request)
        {
            var totalWeightLoss = request.Items.Sum(x => x.WeightLossActual ?? 0);
            var totalLossAmount = request.Items.Sum(x => x.MoneyDiff ?? 0);

            var returnItems = (request.GoldReturnItems ?? new List<GoldReturnItem>())
                .Select(r => new TbtWorkerGoldLossSlipReturn
                {
                    GoldSize = r.GoldSize,
                    Gold = r.Gold,
                    Weight = r.Weight,
                    PricePerGram = r.PricePerGram,
                    Amount = r.Weight * r.PricePerGram,
                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername,
                }).ToList();

            var totalGoldReturnAmount = returnItems.Sum(r => r.Amount);
            var totalReturnWeight = returnItems.Sum(r => r.Weight);
            var netWeightLoss = totalWeightLoss - totalReturnWeight;

            var documentNo = await GenerateGlsDocumentNo();

            var header = new TbtWorkerGoldLossSlip
            {
                DocumentNo = documentNo,
                WorkerCode = request.WorkerCode,
                WorkerName = request.WorkerName,
                RequestDateStart = request.RequestDateStart.UtcDateTime,
                RequestDateEnd = request.RequestDateEnd.UtcDateTime,
                TotalWeightLoss = totalWeightLoss,
                NetWeightLoss = netWeightLoss,
                TotalMoneyDiff = totalLossAmount,
                TotalGoldReturnAmount = totalGoldReturnAmount,
                Remark = request.Remark,
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername,
            };

            _jewelryContext.TbtWorkerGoldLossSlip.Add(header);
            await _jewelryContext.SaveChangesAsync();

            var planIds = request.Items.Select(x => x.ProductionPlanId).Distinct().ToList();
            var itemNos = request.Items.Select(x => x.ItemNo).Distinct().ToList();
            var loadedDetails = _jewelryContext.TbtProductionPlanStatusDetail
                .Include(d => d.Header)
                .Where(d => planIds.Contains(d.ProductionPlanId) && itemNos.Contains(d.ItemNo))
                .ToList();

            var items = request.Items.Select(x =>
            {
                var detail = loadedDetails.FirstOrDefault(d => d.ProductionPlanId == x.ProductionPlanId && d.ItemNo == x.ItemNo);
                return new TbtWorkerGoldLossSlipItem
                {
                    SlipId = header.Id,
                    Wo = x.Wo,
                    WoNumber = x.WoNumber,
                    ProductNumber = x.ProductNumber,
                    ProductName = x.ProductName,
                    Gold = x.Gold,
                    GoldSize = x.GoldSize,
                    JobDate = x.JobDate.HasValue ? x.JobDate.Value.UtcDateTime : (DateTime?)null,
                    GoldQtySend = x.GoldQtySend,
                    GoldWeightSend = x.GoldWeightSend,
                    GoldQtyCheck = x.GoldQtyCheck,
                    GoldWeightCheck = x.GoldWeightCheck,
                    LossPercent = x.LossPercent,
                    WeightLossAllowed = x.WeightLossAllowed,
                    WeightLossActual = x.WeightLossActual,
                    GoldLossPrice = x.GoldLossPrice,
                    MoneyDiff = x.MoneyDiff,
                    IsActive = true,
                };
            }).ToList();

            _jewelryContext.TbtWorkerGoldLossSlipItem.AddRange(items);
            await _jewelryContext.SaveChangesAsync();

            foreach (var r in returnItems)
            {
                r.SlipId = header.Id;
            }

            if (returnItems.Any())
            {
                _jewelryContext.TbtWorkerGoldLossSlipReturn.AddRange(returnItems);
                await _jewelryContext.SaveChangesAsync();
            }

            var detailsToStamp = new List<TbtProductionPlanStatusDetail>();
            foreach (var itemReq in request.Items)
            {
                var detail = loadedDetails.FirstOrDefault(d => d.ProductionPlanId == itemReq.ProductionPlanId && d.ItemNo == itemReq.ItemNo);
                if (detail != null)
                {
                    detail.WorkerGoldLossSlipId = header.Id;
                    detailsToStamp.Add(detail);
                }
            }

            if (detailsToStamp.Any())
            {
                _jewelryContext.TbtProductionPlanStatusDetail.UpdateRange(detailsToStamp);
                await _jewelryContext.SaveChangesAsync();
            }

            return MapToResponse(header, items, returnItems);
        }

        public List<GoldLossSlipSummaryResponse> ListSlips(ListGoldLossSlipRequest request)
        {
            var query = _jewelryContext.TbtWorkerGoldLossSlip
                .Where(x => x.IsActive);

            if (!string.IsNullOrEmpty(request.WorkerCode))
            {
                query = query.Where(x => x.WorkerCode == request.WorkerCode.ToUpper());
            }

            if (request.RequestDateStart.HasValue)
            {
                var startUtc = request.RequestDateStart.Value.StartOfDayUtc();
                query = query.Where(x => x.RequestDateEnd >= startUtc);
            }

            if (request.RequestDateEnd.HasValue)
            {
                var endUtc = request.RequestDateEnd.Value.EndOfDayUtc();
                query = query.Where(x => x.RequestDateStart <= endUtc);
            }

            var result = query
                .OrderByDescending(x => x.CreateDate)
                .Select(x => new GoldLossSlipSummaryResponse
                {
                    Id = x.Id,
                    DocumentNo = x.DocumentNo,
                    WorkerCode = x.WorkerCode,
                    WorkerName = x.WorkerName,
                    RequestDateStart = x.RequestDateStart,
                    RequestDateEnd = x.RequestDateEnd,
                    TotalWeightLoss = x.TotalWeightLoss,
                    NetWeightLoss = x.NetWeightLoss,
                    TotalMoneyDiff = x.TotalMoneyDiff,
                    TotalGoldReturnAmount = x.TotalGoldReturnAmount,
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

        public GoldLossSlipResponse GetSlip(long id)
        {
            var header = _jewelryContext.TbtWorkerGoldLossSlip
                .Include(x => x.TbtWorkerGoldLossSlipItem)
                .Include(x => x.TbtWorkerGoldLossSlipReturn)
                .FirstOrDefault(x => x.Id == id);

            if (header == null)
            {
                throw new HandleException($"ไม่พบ Gold Loss Slip Id: {id}");
            }

            return MapToResponse(header, header.TbtWorkerGoldLossSlipItem.ToList(), header.TbtWorkerGoldLossSlipReturn.ToList());
        }

        public async Task CancelSlip(long id)
        {
            var slip = await _jewelryContext.TbtWorkerGoldLossSlip
                .Include(x => x.TbtWorkerGoldLossSlipItem)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive == true);

            if (slip == null) throw new ArgumentException("Slip not found or already cancelled");

            slip.IsActive = false;
            slip.UpdateDate = DateTime.UtcNow;
            slip.UpdateBy = CurrentUsername;

            _jewelryContext.TbtWorkerGoldLossSlip.Update(slip);

            var details = await _jewelryContext.TbtProductionPlanStatusDetail
                .Where(d => d.WorkerGoldLossSlipId == id)
                .ToListAsync();

            foreach (var d in details)
            {
                d.WorkerGoldLossSlipId = null;
            }

            if (details.Any())
            {
                _jewelryContext.TbtProductionPlanStatusDetail.UpdateRange(details);
            }

            await _jewelryContext.SaveChangesAsync();
        }

        private async Task<string> GenerateGlsDocumentNo()
        {
            var now = DateTime.UtcNow;
            var dateStr = now.ToString("yyyyMM");
            var key = $"GLS-{dateStr}";

            var running = await _jewelryContext.TbtRunningNumber.FindAsync(key);
            if (running == null)
            {
                running = new Jewelry.Data.Models.Jewelry.TbtRunningNumber { Key = key, Number = 1 };
                _jewelryContext.TbtRunningNumber.Add(running);
            }
            else
            {
                running.Number += 1;
                _jewelryContext.TbtRunningNumber.Update(running);
            }
            await _jewelryContext.SaveChangesAsync();

            return $"GLS-{dateStr}-{running.Number:000}";
        }

        private GoldLossSlipResponse MapToResponse(TbtWorkerGoldLossSlip header, List<TbtWorkerGoldLossSlipItem> items, List<TbtWorkerGoldLossSlipReturn> returnItems)
        {
            var totalLossAmount = header.TotalMoneyDiff;
            var totalGoldReturnAmount = header.TotalGoldReturnAmount;
            var netPayAmount = (totalLossAmount ?? 0) + (totalGoldReturnAmount ?? 0);

            return new GoldLossSlipResponse
            {
                Id = header.Id,
                DocumentNo = header.DocumentNo,
                WorkerCode = header.WorkerCode,
                WorkerName = header.WorkerName,
                RequestDateStart = header.RequestDateStart,
                RequestDateEnd = header.RequestDateEnd,
                TotalWeightLoss = header.TotalWeightLoss,
                NetWeightLoss = header.NetWeightLoss,
                TotalLossAmount = totalLossAmount,
                TotalGoldReturnAmount = totalGoldReturnAmount,
                NetPayAmount = netPayAmount,
                Remark = header.Remark,
                IsActive = header.IsActive,
                CreateDate = header.CreateDate,
                CreateBy = header.CreateBy,
                UpdateDate = header.UpdateDate,
                UpdateBy = header.UpdateBy,
                Items = items.Select(i => new GoldLossSlipItemResponse
                {
                    Id = i.Id,
                    SlipId = i.SlipId,
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
                    LossPercent = i.LossPercent,
                    WeightLossAllowed = i.WeightLossAllowed,
                    WeightLossActual = i.WeightLossActual,
                    GoldLossPrice = i.GoldLossPrice,
                    MoneyDiff = i.MoneyDiff,
                }).ToList(),
                GoldReturnItems = returnItems.Select(r => new GoldReturnItemResponse
                {
                    Id = r.Id,
                    Gold = r.Gold,
                    GoldSize = r.GoldSize,
                    Weight = r.Weight,
                    PricePerGram = r.PricePerGram,
                    Amount = r.Amount,
                }).ToList(),
                TypeSummaries = BuildTypeSummaries(items, returnItems),
            };
        }

        private List<GoldLossTypeSummaryResponse> BuildTypeSummaries(List<TbtWorkerGoldLossSlipItem> items, List<TbtWorkerGoldLossSlipReturn> returnItems)
        {
            var goldTypes = items.Select(i => (Gold: i.Gold ?? "", GoldSize: i.GoldSize ?? ""))
                .Concat(returnItems.Select(r => (Gold: r.Gold ?? "", GoldSize: r.GoldSize ?? "")))
                .Distinct()
                .OrderBy(g => g.Gold).ThenBy(g => g.GoldSize)
                .ToList();

            return goldTypes.Select(key =>
            {
                var totalWeightLoss = items
                    .Where(i => (i.Gold ?? "") == key.Gold && (i.GoldSize ?? "") == key.GoldSize)
                    .Sum(i => i.WeightLossActual ?? 0);
                var totalMoneyLoss = items
                    .Where(i => (i.Gold ?? "") == key.Gold && (i.GoldSize ?? "") == key.GoldSize)
                    .Sum(i => i.MoneyDiff ?? 0);
                var returnWeight = returnItems
                    .Where(r => (r.Gold ?? "") == key.Gold && (r.GoldSize ?? "") == key.GoldSize)
                    .Sum(r => r.Weight);
                var returnAmount = returnItems
                    .Where(r => (r.Gold ?? "") == key.Gold && (r.GoldSize ?? "") == key.GoldSize)
                    .Sum(r => r.Amount);

                return new GoldLossTypeSummaryResponse
                {
                    Gold = key.Gold,
                    GoldSize = key.GoldSize == "" ? null : key.GoldSize,
                    TotalWeightLoss = totalWeightLoss,
                    TotalMoneyLoss = totalMoneyLoss,
                    ReturnWeight = returnWeight,
                    ReturnAmount = returnAmount,
                    NetWeight = totalWeightLoss - returnWeight,
                    NetAmount = totalMoneyLoss + returnAmount,
                };
            }).ToList();
        }
    }
}
