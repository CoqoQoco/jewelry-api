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
            var netWeightLoss = totalWeightLoss + request.GoldReturn;
            var totalMoneyDiff = request.Items.Sum(x => x.MoneyDiff ?? 0);

            var documentNo = await GenerateGlsDocumentNo();

            var header = new TbtWorkerGoldLossSlip
            {
                DocumentNo = documentNo,
                WorkerCode = request.WorkerCode,
                WorkerName = request.WorkerName,
                RequestDateStart = request.RequestDateStart.UtcDateTime,
                RequestDateEnd = request.RequestDateEnd.UtcDateTime,
                GoldReturn = request.GoldReturn,
                TotalWeightLoss = totalWeightLoss,
                NetWeightLoss = netWeightLoss,
                TotalMoneyDiff = totalMoneyDiff,
                Remark = request.Remark,
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername,
            };

            _jewelryContext.TbtWorkerGoldLossSlip.Add(header);
            await _jewelryContext.SaveChangesAsync();

            var items = request.Items.Select(x => new TbtWorkerGoldLossSlipItem
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
            }).ToList();

            _jewelryContext.TbtWorkerGoldLossSlipItem.AddRange(items);
            await _jewelryContext.SaveChangesAsync();

            var detailsToStamp = new List<TbtProductionPlanStatusDetail>();
            foreach (var itemReq in request.Items)
            {
                var detail = _jewelryContext.TbtProductionPlanStatusDetail
                    .FirstOrDefault(d => d.ProductionPlanId == itemReq.ProductionPlanId && d.ItemNo == itemReq.ItemNo);
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

            return MapToResponse(header, items);
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
                    GoldReturn = x.GoldReturn,
                    TotalWeightLoss = x.TotalWeightLoss,
                    NetWeightLoss = x.NetWeightLoss,
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

        public GoldLossSlipResponse GetSlip(long id)
        {
            var header = _jewelryContext.TbtWorkerGoldLossSlip
                .Include(x => x.TbtWorkerGoldLossSlipItem)
                .FirstOrDefault(x => x.Id == id);

            if (header == null)
            {
                throw new HandleException($"ไม่พบ Gold Loss Slip Id: {id}");
            }

            return MapToResponse(header, header.TbtWorkerGoldLossSlipItem.ToList());
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

        private GoldLossSlipResponse MapToResponse(TbtWorkerGoldLossSlip header, List<TbtWorkerGoldLossSlipItem> items)
        {
            return new GoldLossSlipResponse
            {
                Id = header.Id,
                DocumentNo = header.DocumentNo,
                WorkerCode = header.WorkerCode,
                WorkerName = header.WorkerName,
                RequestDateStart = header.RequestDateStart,
                RequestDateEnd = header.RequestDateEnd,
                GoldReturn = header.GoldReturn,
                TotalWeightLoss = header.TotalWeightLoss,
                NetWeightLoss = header.NetWeightLoss,
                TotalMoneyDiff = header.TotalMoneyDiff,
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
            };
        }
    }
}
