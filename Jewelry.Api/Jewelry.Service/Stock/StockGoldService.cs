using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Stock
{
    public interface IStockGoldService
    {
        Task<jewelry.Model.Stock.Gold.Inbound.Response> Inbound(jewelry.Model.Stock.Gold.Inbound.Request request);
        Task<jewelry.Model.Stock.Gold.OpeningBalance.Response> OpeningBalance(jewelry.Model.Stock.Gold.OpeningBalance.Request request);
        Task<jewelry.Model.Stock.Gold.Adjust.Response> Adjust(jewelry.Model.Stock.Gold.Adjust.Request request);

        IQueryable<jewelry.Model.Stock.Gold.Balance.Response> Balance(jewelry.Model.Stock.Gold.Balance.Search request);
        IQueryable<jewelry.Model.Stock.Gold.Transection.Response> Transection(jewelry.Model.Stock.Gold.Transection.Search request);

        // เมธอดกลาง — Phase 3 (ผูกกับใบเบิกผสมทอง) จะเรียกใช้ตรงนี้
        Task<string> PostMovement(
            string goldCode,
            string goldSizeCode,
            int type,
            decimal weight,
            string? refDocType = null,
            string? refDocNo = null,
            string? productionPlanWo = null,
            int? productionPlanWoNumber = null,
            string? refRunning = null,
            DateTimeOffset? requestDate = null,
            DateTimeOffset? returnDate = null,
            string? remark = null,
            string? status = null,
            bool allowNegative = false);

        Task ReverseByRefDoc(string refDocType, string refDocNo);
    }

    public class StockGoldService : BaseService, IStockGoldService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IRunningNumber _runningNumberService;

        public StockGoldService(JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor,
            IRunningNumber runningNumberService) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _runningNumberService = runningNumberService;
        }

        public async Task<jewelry.Model.Stock.Gold.Inbound.Response> Inbound(jewelry.Model.Stock.Gold.Inbound.Request request)
        {
            await EnsureGoldMasterExists(request.GoldCode, request.GoldSizeCode);

            if (request.Weight <= 0)
            {
                throw new HandleException(ErrorMessage.InvalidQty);
            }

            var running = await PostMovement(
                request.GoldCode,
                request.GoldSizeCode,
                GoldStockTransactionType.Inbound,
                request.Weight,
                requestDate: request.RequestDate,
                remark: request.Remark);

            var remainWeight = await GetCurrentWeight(request.GoldCode, request.GoldSizeCode);

            return new jewelry.Model.Stock.Gold.Inbound.Response
            {
                Running = running,
                Weight = request.Weight,
                RemainWeight = remainWeight,
            };
        }

        public async Task<jewelry.Model.Stock.Gold.OpeningBalance.Response> OpeningBalance(jewelry.Model.Stock.Gold.OpeningBalance.Request request)
        {
            await EnsureGoldMasterExists(request.GoldCode, request.GoldSizeCode);

            if (request.Weight <= 0)
            {
                throw new HandleException(ErrorMessage.InvalidQty);
            }

            var running = await PostMovement(
                request.GoldCode,
                request.GoldSizeCode,
                GoldStockTransactionType.OpeningBalance,
                request.Weight,
                requestDate: request.RequestDate,
                remark: request.Remark);

            var remainWeight = await GetCurrentWeight(request.GoldCode, request.GoldSizeCode);

            return new jewelry.Model.Stock.Gold.OpeningBalance.Response
            {
                Running = running,
                Weight = request.Weight,
                RemainWeight = remainWeight,
            };
        }

        public async Task<jewelry.Model.Stock.Gold.Adjust.Response> Adjust(jewelry.Model.Stock.Gold.Adjust.Request request)
        {
            if (request.Type != GoldStockTransactionType.AdjustIncrease && request.Type != GoldStockTransactionType.AdjustDecrease)
            {
                throw new HandleException(ErrorMessage.InvalidRequest);
            }

            if (string.IsNullOrWhiteSpace(request.Remark))
            {
                throw new HandleException("กรุณาระบุเหตุผลในการปรับยอด");
            }

            await EnsureGoldMasterExists(request.GoldCode, request.GoldSizeCode);

            if (request.Weight <= 0)
            {
                throw new HandleException(ErrorMessage.InvalidQty);
            }

            var running = await PostMovement(
                request.GoldCode,
                request.GoldSizeCode,
                request.Type,
                request.Weight,
                requestDate: request.RequestDate,
                remark: request.Remark);

            var remainWeight = await GetCurrentWeight(request.GoldCode, request.GoldSizeCode);

            return new jewelry.Model.Stock.Gold.Adjust.Response
            {
                Running = running,
                Weight = request.Weight,
                RemainWeight = remainWeight,
            };
        }

        public IQueryable<jewelry.Model.Stock.Gold.Balance.Response> Balance(jewelry.Model.Stock.Gold.Balance.Search request)
        {
            var query = _jewelryContext.TbtStockGold.AsQueryable();

            if (request?.GoldCode != null && request.GoldCode.Any())
            {
                query = query.Where(x => request.GoldCode.Contains(x.GoldCode));
            }
            if (request?.GoldSizeCode != null && request.GoldSizeCode.Any())
            {
                query = query.Where(x => request.GoldSizeCode.Contains(x.GoldSizeCode));
            }

            return query
                .OrderBy(x => x.GoldCode).ThenBy(x => x.GoldSizeCode)
                .Select(x => new jewelry.Model.Stock.Gold.Balance.Response
                {
                    Id = x.Id,
                    GoldCode = x.GoldCode,
                    GoldNameTh = x.GoldCodeNavigation.NameTh,
                    GoldNameEn = x.GoldCodeNavigation.NameEn,
                    GoldSizeCode = x.GoldSizeCode,
                    GoldSizeNameTh = x.GoldSizeCodeNavigation.NameTh,
                    GoldSizeNameEn = x.GoldSizeCodeNavigation.NameEn,
                    GoldPercent = x.GoldSizeCodeNavigation.GoldPercent,
                    Weight = x.Weight,
                    WeightOnProcess = x.WeightOnProcess,
                    CreateDate = x.CreateDate,
                    CreateBy = x.CreateBy,
                    UpdateDate = x.UpdateDate,
                    UpdateBy = x.UpdateBy,
                });
        }

        public IQueryable<jewelry.Model.Stock.Gold.Transection.Response> Transection(jewelry.Model.Stock.Gold.Transection.Search request)
        {
            var query = _jewelryContext.TbtStockGoldTransection.AsQueryable();

            if (request?.GoldCode != null && request.GoldCode.Any())
            {
                query = query.Where(x => request.GoldCode.Contains(x.GoldCode));
            }
            if (request?.GoldSizeCode != null && request.GoldSizeCode.Any())
            {
                query = query.Where(x => request.GoldSizeCode.Contains(x.GoldSizeCode));
            }
            if (request?.Type != null && request.Type.Any())
            {
                query = query.Where(x => request.Type.Contains(x.Type));
            }
            if (!string.IsNullOrEmpty(request?.RefDocType))
            {
                query = query.Where(x => x.RefDocType == request.RefDocType);
            }
            if (!string.IsNullOrEmpty(request?.RefDocNo))
            {
                query = query.Where(x => x.RefDocNo == request.RefDocNo);
            }
            if (request?.DateFrom != null)
            {
                query = query.Where(x => x.CreateDate >= request.DateFrom.Value.StartOfDayUtc());
            }
            if (request?.DateTo != null)
            {
                query = query.Where(x => x.CreateDate <= request.DateTo.Value.EndOfDayUtc());
            }

            return query
                .OrderByDescending(x => x.CreateDate)
                .Select(x => new jewelry.Model.Stock.Gold.Transection.Response
                {
                    Id = x.Id,
                    Running = x.Running,
                    GoldCode = x.GoldCode,
                    GoldNameTh = x.GoldCodeNavigation.NameTh,
                    GoldSizeCode = x.GoldSizeCode,
                    GoldSizeNameTh = x.GoldSizeCodeNavigation.NameTh,
                    GoldPercent = x.GoldSizeCodeNavigation.GoldPercent,
                    Type = x.Type,
                    TypeName = x.Type == GoldStockTransactionType.Inbound ? "รับเข้าคลัง [ซื้อ/รับใหม่]"
                        : x.Type == GoldStockTransactionType.OpeningBalance ? "ตั้งยอดยกมา"
                        : x.Type == GoldStockTransactionType.ReturnIn ? "คืนเข้าคลัง [จากใบเบิกผสมทอง]"
                        : x.Type == GoldStockTransactionType.Outbound ? "เบิกออกคลัง [ใบเบิกผสมทอง]"
                        : x.Type == GoldStockTransactionType.AdjustIncrease ? "ปรับยอดเพิ่ม"
                        : x.Type == GoldStockTransactionType.AdjustDecrease ? "ปรับยอดลด"
                        : x.Type == GoldStockTransactionType.ReversalIncrease ? "กลับรายการเพิ่ม [แก้ไขรายการ]"
                        : x.Type == GoldStockTransactionType.ReversalDecrease ? "กลับรายการลด [แก้ไขรายการ]"
                        : "อื่นๆ",
                    Weight = x.Weight,
                    PreviousRemainWeight = x.PreviousRemainWeight,
                    PointRemainWeight = x.PointRemainWeight,
                    RefDocType = x.RefDocType,
                    RefDocNo = x.RefDocNo,
                    ProductionPlanWo = x.ProductionPlanWo,
                    ProductionPlanWoNumber = x.ProductionPlanWoNumber,
                    RefRunning = x.RefRunning,
                    RequestDate = x.RequestDate,
                    ReturnDate = x.ReturnDate,
                    Status = x.Status,
                    Remark = x.Remark,
                    CreateDate = x.CreateDate,
                    CreateBy = x.CreateBy,
                    UpdateDate = x.UpdateDate,
                    UpdateBy = x.UpdateBy,
                });
        }

        public async Task<string> PostMovement(
            string goldCode,
            string goldSizeCode,
            int type,
            decimal weight,
            string? refDocType = null,
            string? refDocNo = null,
            string? productionPlanWo = null,
            int? productionPlanWoNumber = null,
            string? refRunning = null,
            DateTimeOffset? requestDate = null,
            DateTimeOffset? returnDate = null,
            string? remark = null,
            string? status = null,
            bool allowNegative = false)
        {
            if (!GoldStockTransactionType.IsValid(type))
            {
                throw new HandleException(ErrorMessage.InvalidRequest);
            }
            if (weight <= 0)
            {
                throw new HandleException(ErrorMessage.InvalidQty);
            }

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var balance = await _jewelryContext.TbtStockGold
                    .FirstOrDefaultAsync(x => x.GoldCode == goldCode && x.GoldSizeCode == goldSizeCode);

                var isNew = balance == null;
                if (balance == null)
                {
                    balance = new TbtStockGold
                    {
                        GoldCode = goldCode,
                        GoldSizeCode = goldSizeCode,
                        Weight = 0,
                        WeightOnProcess = 0,
                        CreateDate = DateTime.UtcNow,
                        CreateBy = CurrentUsername,
                    };
                }

                var previousWeight = balance.Weight;

                if (GoldStockTransactionType.IsInbound(type))
                {
                    balance.Weight = previousWeight + weight;
                }
                else
                {
                    if (!allowNegative && weight > previousWeight)
                    {
                        throw new HandleException($"น้ำหนักทองคงเหลือไม่เพียงพอ (คงเหลือ {previousWeight:0.####} กรัม)");
                    }
                    balance.Weight = previousWeight - weight;
                }

                balance.UpdateDate = DateTime.UtcNow;
                balance.UpdateBy = CurrentUsername;

                if (isNew)
                {
                    _jewelryContext.TbtStockGold.Add(balance);
                }
                else
                {
                    _jewelryContext.TbtStockGold.Update(balance);
                }

                var running = await _runningNumberService.GenerateRunningNumberForGold(GetRunningKey(type));

                var transection = new TbtStockGoldTransection
                {
                    Running = running,
                    GoldCode = goldCode,
                    GoldSizeCode = goldSizeCode,
                    Type = type,
                    Weight = weight,
                    PreviousRemainWeight = previousWeight,
                    PointRemainWeight = balance.Weight,
                    RefDocType = refDocType,
                    RefDocNo = refDocNo,
                    ProductionPlanWo = productionPlanWo,
                    ProductionPlanWoNumber = productionPlanWoNumber,
                    RefRunning = refRunning,
                    RequestDate = (requestDate ?? DateTimeOffset.UtcNow).UtcDateTime,
                    ReturnDate = returnDate?.UtcDateTime,
                    Status = status ?? GoldStockTransactionStatus.Completed,
                    Remark = remark,
                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername,
                };

                _jewelryContext.TbtStockGoldTransection.Add(transection);

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();

                return running;
            }
        }

        public async Task ReverseByRefDoc(string refDocType, string refDocNo)
        {
            if (string.IsNullOrWhiteSpace(refDocType) || string.IsNullOrWhiteSpace(refDocNo))
            {
                throw new HandleException(ErrorMessage.InvalidRequest);
            }

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var transections = await _jewelryContext.TbtStockGoldTransection
                    .Where(x => x.RefDocType == refDocType && x.RefDocNo == refDocNo
                        && x.Status != GoldStockTransactionStatus.Reversed
                        && x.Type != GoldStockTransactionType.ReversalIncrease
                        && x.Type != GoldStockTransactionType.ReversalDecrease)
                    .OrderBy(x => x.Id)
                    .ToListAsync();

                foreach (var trans in transections)
                {
                    var reverseType = GoldStockTransactionType.IsInbound(trans.Type)
                        ? GoldStockTransactionType.ReversalDecrease
                        : GoldStockTransactionType.ReversalIncrease;

                    await PostMovement(
                        trans.GoldCode,
                        trans.GoldSizeCode,
                        reverseType,
                        trans.Weight,
                        refDocType: trans.RefDocType,
                        refDocNo: trans.RefDocNo,
                        productionPlanWo: trans.ProductionPlanWo,
                        productionPlanWoNumber: trans.ProductionPlanWoNumber,
                        refRunning: trans.Running,
                        remark: $"ยกเลิกรายการ {trans.Running} ({StockGoldServiceStatic.GetTransactionTypeName(trans.Type)})",
                        allowNegative: true);

                    trans.Status = GoldStockTransactionStatus.Reversed;
                    trans.UpdateDate = DateTime.UtcNow;
                    trans.UpdateBy = CurrentUsername;
                    _jewelryContext.TbtStockGoldTransection.Update(trans);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }
        }

        private async Task EnsureGoldMasterExists(string goldCode, string goldSizeCode)
        {
            var goldExists = await _jewelryContext.TbmGold.AnyAsync(x => x.Code == goldCode);
            if (!goldExists)
            {
                throw new HandleException($"{ErrorMessage.NotFound} --> {goldCode}");
            }

            var goldSizeExists = await _jewelryContext.TbmGoldSize.AnyAsync(x => x.Code == goldSizeCode);
            if (!goldSizeExists)
            {
                throw new HandleException($"{ErrorMessage.NotFound} --> {goldSizeCode}");
            }
        }

        private async Task<decimal> GetCurrentWeight(string goldCode, string goldSizeCode)
        {
            var balance = await _jewelryContext.TbtStockGold
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.GoldCode == goldCode && x.GoldSizeCode == goldSizeCode);
            return balance?.Weight ?? 0;
        }

        private static string GetRunningKey(int type) => type switch
        {
            GoldStockTransactionType.Inbound => "GDIN",
            GoldStockTransactionType.OpeningBalance => "GDOB",
            GoldStockTransactionType.ReturnIn => "GDRT",
            GoldStockTransactionType.Outbound => "GDOT",
            GoldStockTransactionType.AdjustIncrease => "GDAP",
            GoldStockTransactionType.AdjustDecrease => "GDAM",
            GoldStockTransactionType.ReversalIncrease => "GDRI",
            GoldStockTransactionType.ReversalDecrease => "GDRD",
            _ => "GDST",
        };
    }

    public static class StockGoldServiceStatic
    {
        public static string GetTransactionTypeName(int type)
        {
            return type switch
            {
                GoldStockTransactionType.Inbound => "รับเข้าคลัง [ซื้อ/รับใหม่]",
                GoldStockTransactionType.OpeningBalance => "ตั้งยอดยกมา",
                GoldStockTransactionType.ReturnIn => "คืนเข้าคลัง [จากใบเบิกผสมทอง]",
                GoldStockTransactionType.Outbound => "เบิกออกคลัง [ใบเบิกผสมทอง]",
                GoldStockTransactionType.AdjustIncrease => "ปรับยอดเพิ่ม",
                GoldStockTransactionType.AdjustDecrease => "ปรับยอดลด",
                GoldStockTransactionType.ReversalIncrease => "กลับรายการเพิ่ม [แก้ไขรายการ]",
                GoldStockTransactionType.ReversalDecrease => "กลับรายการลด [แก้ไขรายการ]",
                _ => "อื่นๆ",
            };
        }
    }
}
