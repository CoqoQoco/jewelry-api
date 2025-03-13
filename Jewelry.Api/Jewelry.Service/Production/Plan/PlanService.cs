using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.Production.Plan.Transfer;
using jewelry.Model.ProductionPlan.ProductionPlanStatus.Transfer;
using jewelry.Model.ProductionPlan.ProductionPlanStatusList;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Production.Plan
{
    public class PlanService : BaseService, IPlanService
    {

        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public PlanService(JewelryContext JewelryContext, IHttpContextAccessor httpContextAccessor,
            IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        #region --- list plan status detail ---
        public IQueryable<jewelry.Model.Production.Plan.StatusDetailList.Response> StatusDetailList(jewelry.Model.Production.Plan.StatusDetailList.RequestSearch request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlanStatusDetail
                        .Include(x => x.Header)
                        .Include(x => x.Header.ProductionPlan)

                         join _worker in _jewelryContext.TbmWorker on item.Worker equals _worker.Code into _workerJpined
                         from worker in _workerJpined.DefaultIfEmpty()

                         where item.IsActive == true
                         && item.Header.IsActive == true

                         select new jewelry.Model.Production.Plan.StatusDetailList.Response()
                         {
                             Wo = item.Header.ProductionPlan.Wo,
                             WoNumber = item.Header.ProductionPlan.WoNumber,
                             WoText = item.Header.ProductionPlan.WoText,
                             ProductNumber = item.Header.ProductionPlan.ProductNumber,
                             ProductName = item.Header.ProductionPlan.ProductName,
                             Mold = item.Header.ProductionPlan.Mold,

                             HeaderId = item.HeaderId,

                             WorkerCode = item.Worker,
                             WorkerName = worker != null ? worker.NameTh : "",

                             Status = item.Header.ProductionPlan.Id,
                             StatusName = item.Header.ProductionPlan.StatusNavigation.NameTh,


                             TypeStatus = item.Header.Status,
                             TypeStatusName = item.Header.StatusNavigation.NameTh,
                             TypeStatusDescription = item.Header.StatusNavigation.Description,

                             Gold = item.Gold,

                             GoldQtySend = item.GoldQtySend,
                             GoldWeightSend = item.GoldWeightSend,
                             GoldQtyCheck = item.GoldQtyCheck,
                             GoldWeightCheck = item.GoldWeightCheck,

                             Description = item.Description,
                             Wages = item.Wages,
                             TotalWages = item.TotalWages,
                             WagesStatus = item.Wages.HasValue && item.Wages.Value > 0 ? 100 : 10,

                             ReceiveDate = item.Header.SendDate,
                             ReceiveWorkDate = item.RequestDate,
                         });

            if (request.ReceivesDateStart.HasValue)
            {
                query = query.Where(x => x.ReceiveDate >= request.ReceivesDateStart.Value.StartOfDayUtc());
            }
            if (request.ReceiveDateEnd.HasValue)
            {
                query = query.Where(x => x.ReceiveDate >= request.ReceiveDateEnd.Value.EndOfDayUtc());
            }

            if (request.ReceiveWorkDateStart.HasValue)
            {
                query = query.Where(x => x.ReceiveWorkDate >= request.ReceiveWorkDateStart.Value.StartOfDayUtc());
            }
            if (request.ReceiveWorkDateEnd.HasValue)
            {
                query = query.Where(x => x.ReceiveWorkDate >= request.ReceiveWorkDateEnd.Value.EndOfDayUtc());
            }

            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.TypeStatus));
            }
            if (!string.IsNullOrEmpty(request.WoText))
            {
                query = query.Where(x => x.WoText.Contains(request.WoText.ToUpper()));
            }
            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => request.Gold.Contains(x.Gold));
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.WoText.Contains(request.ProductNumber));
            }

            return query;
        }
        #endregion

        #region --- list transfer transection ---
        public IQueryable<jewelry.Model.Production.Plan.TransferList.Response> TransferList(jewelry.Model.Production.Plan.TransferList.RequestSearch request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlanTransferStatus
                         .Include(x => x.ProductionPlan)
                         .Include(x => x.ProductionPlan.ProductTypeNavigation)
                         select new jewelry.Model.Production.Plan.TransferList.Response()
                         {
                             TransferNumber = item.Running,

                             Wo = item.Wo,
                             WoNumber = item.WoNumber,
                             WoText = item.ProductionPlan.WoText,

                             FormerStatus = item.FormerStatus,
                             TargetStatus = item.TargetStatus,

                             CreateDate = item.CreateDate,
                             CreateBy = item.CreateBy,

                             Mold = item.ProductionPlan.Mold,

                             ProductNumber = item.ProductionPlan.ProductNumber,
                             ProductQty = item.ProductionPlan.ProductQty,

                             ProductType = item.ProductionPlan.ProductType,
                             ProductTypeName = item.ProductionPlan.ProductTypeNavigation.NameTh,

                             Gold = item.ProductionPlan.Type,
                             GoldSize = item.ProductionPlan.TypeSize,
                         });

            if (request.Start.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.Start.Value.StartOfDayUtc());
            }
            if (request.End.HasValue)
            {
                query = query.Where(x => x.CreateDate <= request.End.Value.EndOfDayUtc());
            }

            if (!string.IsNullOrEmpty(request.TransferNumber))
            {
                query = query.Where(x => x.TransferNumber.Contains(request.TransferNumber));
            }
            if (!string.IsNullOrEmpty(request.WoText))
            {
                query = query.Where(x => x.WoText.Contains(request.WoText));
            }

            if (request.StatusFormer.HasValue)
            {
                query = query.Where(x => x.FormerStatus == request.StatusFormer.Value);
            }
            if (request.StatusTarget.HasValue)
            {
                query = query.Where(x => x.TargetStatus == request.StatusTarget.Value);
            }

            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => request.Gold.Contains(x.Gold));
            }
            if (request.GoldSize != null && request.GoldSize.Any())
            {
                query = query.Where(x => request.GoldSize.Contains(x.GoldSize));
            }

            if (request.ProductType != null && request.ProductType.Any())
            {
                query = query.Where(x => request.ProductType.Contains(x.ProductType));
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.ProductNumber.Contains(request.ProductNumber));
            }

            if (!string.IsNullOrEmpty(request.Mold))
            {
                query = query.Where(x => x.Mold.Contains(request.Mold));
            }

            return query;
        }
        #endregion

        #region --- Transfer Plan ---
        public async Task<jewelry.Model.Production.Plan.Transfer.Response> Transfer(jewelry.Model.Production.Plan.Transfer.Request request)
        {
            ValidateRequest(request);

            var plans = await GetProductionPlans(request.Plans.Select(x => x.Id).ToArray());
            var response = new jewelry.Model.Production.Plan.Transfer.Response { Message = "success" };

            var transferData = await PrepareTransferData(request, plans);

            if (transferData.HasAnyValidPlans)
            {
                //if (!string.IsNullOrEmpty(transferData.ReceiptRunning))
                //{

                //}

                await ProcessTransfer(transferData);
            }

            response.TransferNumber = transferData.TransferRunning;
            response.ReceiptNumber = transferData.ReceiptRunning;
            response.Errors.AddRange(transferData.Errors);
            return response;
        }
        private void ValidateRequest(jewelry.Model.Production.Plan.Transfer.Request request)
        {
            if (request.FormerStatus == request.TargetStatus)
            {
                throw new HandleException(ErrorMessage.InvalidRequest);
            }
        }
        private async Task<List<TbtProductionPlan>> GetProductionPlans(int[] planIds)
        {
            var plans = await _jewelryContext.TbtProductionPlan
                .Include(x => x.TbtProductionPlanStatusHeader)
                .Include(x => x.ProductTypeNavigation)
                .Where(item => planIds.Contains(item.Id))
                .ToListAsync();

            if (!plans.Any())
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            return plans;
        }
        private async Task<TransferData> PrepareTransferData(jewelry.Model.Production.Plan.Transfer.Request request, List<TbtProductionPlan> plans)
        {
            var data = new TransferData
            {
                DateNow = DateTime.UtcNow,
                TransferRunning = await _runningNumberService.GenerateRunningNumberForGold("PLT")
            };
            var receiptRunning = string.Empty;

            if (request.TargetStatus == ProductionPlanStatus.Completed)
            {
                receiptRunning = await _runningNumberService.GenerateRunningNumberForGold("REP");
            }

            foreach (var planRequest in request.Plans)
            {
                var validationResult = ValidatePlanForTransfer(planRequest, plans, request);

                if (validationResult.IsValid)
                {
                    var targetPlan = plans.First(x => x.Id == planRequest.Id);
                    await AddValidPlanData(data, targetPlan, request, receiptRunning);
                }
                else
                {
                    data.Errors.Add(new TransferResponseItem
                    {
                        Id = planRequest.Id,
                        Wo = planRequest.Wo,
                        WoNumber = planRequest.WoNumber,
                        Message = validationResult.ErrorMessage
                    });
                }
            }

            return data;
        }
        private (bool IsValid, string ErrorMessage) ValidatePlanForTransfer(
            jewelry.Model.Production.Plan.Transfer.RequestItem planRequest,
            List<TbtProductionPlan> plans,
            jewelry.Model.Production.Plan.Transfer.Request request)
        {
            var targetPlan = plans.FirstOrDefault(x => x.Id == planRequest.Id);

            if (targetPlan == null)
                return (false, ErrorMessage.NotFound);

            if (targetPlan.Status == ProductionPlanStatus.Completed)
                return (false, ErrorMessage.PlanCompleted);

            if (request.TargetStatus == ProductionPlanStatus.Completed && (targetPlan.Status != ProductionPlanStatus.Price))
                return (false, ErrorMessage.PlanNeedPrice);

            if (targetPlan.TbtProductionPlanStatusHeader.Any(x =>
                x.IsActive && x.Status == request.TargetStatus))
            {
                return (false, ErrorMessage.StatusAlready);
            }

            return (true, null);
        }
        private async Task AddValidPlanData(TransferData data,
            TbtProductionPlan plan,
            jewelry.Model.Production.Plan.Transfer.Request request,
            string receiptRunning)
        {

            var newStatus = CreateNewStatus(plan, request, data.DateNow);
            data.NewStatuses.Add(newStatus);

            var transferStatus = CreateTransferStatus(plan, request, data.TransferRunning, data.DateNow);
            data.TransferStatuses.Add(transferStatus);

            var currentStatus = (ProductionPlanStatusEnum)request.TargetStatus;

            //โอนของเข้าคลังสินค้า
            if (request.TargetStatus == ProductionPlanStatus.Completed)
            {
                //create new stock receipt plan
                var newStockReceiptPlan = await CreateNewStockReceiptPlan(plan, request, receiptRunning, data.DateNow);
                data.newStockReceiptPlan.Add(newStockReceiptPlan);

                //create new stock receipt item
                var newStockReceiptItem = await CreateNewStockReceiptItem(plan, request, receiptRunning, data.DateNow);
                data.newStockReceiptItem.AddRange(newStockReceiptItem);

                plan.IsReceipt = true;
                data.ReceiptRunning = receiptRunning;
            }

            plan.Status = currentStatus.GetWatingStatus();
            plan.UpdateDate = data.DateNow;
            plan.UpdateBy = CurrentUsername;
            data.UpdatePlans.Add(plan);
        }
        private TbtProductionPlanStatusHeader CreateNewStatus(
            TbtProductionPlan plan,
            jewelry.Model.Production.Plan.Transfer.Request request,
            DateTime dateNow)
        {
            return new TbtProductionPlanStatusHeader
            {
                CreateDate = dateNow,
                CreateBy = CurrentUsername,
                UpdateDate = dateNow,
                UpdateBy = CurrentUsername,
                IsActive = true,
                ProductionPlanId = plan.Id,
                Status = request.TargetStatus
            };
        }
        private TbtProductionPlanTransferStatus CreateTransferStatus(
            TbtProductionPlan plan,
            jewelry.Model.Production.Plan.Transfer.Request request,
            string running,
            DateTime dateNow)
        {
            return new TbtProductionPlanTransferStatus
            {
                Running = running,
                Wo = plan.Wo,
                WoNumber = plan.WoNumber,
                ProductionPlanId = plan.Id,
                CreateDate = dateNow,
                CreateBy = CurrentUsername,
                FormerStatus = request.FormerStatus,
                TargetStatus = request.TargetStatus
            };
        }
        private async Task<TbtStockProductReceiptPlan> CreateNewStockReceiptPlan(TbtProductionPlan plan,
            jewelry.Model.Production.Plan.Transfer.Request request,
            string running,
            DateTime dateNow)
        {
            return new TbtStockProductReceiptPlan
            {
                Running = running,
                Type = "production",

                CreateDate = dateNow,
                CreateBy = CurrentUsername,

                Wo = plan.Wo,
                WoNumber = plan.WoNumber,
                WoText = plan.WoText,

                Qty = plan.ProductQty,
                IsComplete = false,
                IsRunning = false,
            };
        }

        private async Task<List<TbtStockProductReceiptItem>> CreateNewStockReceiptItem(TbtProductionPlan plan,
           jewelry.Model.Production.Plan.Transfer.Request request,
           string running,
           DateTime dateNow)
        {
            var NewStockReceiptItem = new List<TbtStockProductReceiptItem>();

            for (int i = 1; i <= plan.ProductQty; i++)
            {
                var item = new TbtStockProductReceiptItem
                {
                    Running = running,
                    Type = "production",

                    Wo = plan.Wo,
                    WoNumber = plan.WoNumber,
                    WoText = plan.WoText,

                    Mold = plan.Mold,
                    StockReceiptNumber = await _runningNumberService.GenerateRunningNumberForGold("RPR"),
                    IsReceipt = false,

                    CreateDate = dateNow,
                    CreateBy = CurrentUsername,

                    ProductionType = plan.Type,
                    ProductionTypeSize = plan.TypeSize,

                    ProductType = plan.ProductType,
                    ProductTypeName = plan.ProductTypeNavigation.NameTh

                };

                NewStockReceiptItem.Add(item);
            }

            return NewStockReceiptItem;
        }
        private async Task ProcessTransfer(TransferData data)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            if (data.NewStatuses.Any())
            {
                await _jewelryContext.TbtProductionPlanStatusHeader.AddRangeAsync(data.NewStatuses);
                await _jewelryContext.SaveChangesAsync();

                // Link transfer statuses with new status headers
                foreach (var transfer in data.TransferStatuses)
                {
                    var match = data.NewStatuses.First(x => x.ProductionPlanId == transfer.ProductionPlanId);
                    transfer.TargetStatusId = match.Id;
                }
            }

            if (data.TransferStatuses.Any())
            {
                await _jewelryContext.TbtProductionPlanTransferStatus.AddRangeAsync(data.TransferStatuses);
                await _jewelryContext.SaveChangesAsync();
            }

            if (data.UpdatePlans.Any())
            {
                _jewelryContext.TbtProductionPlan.UpdateRange(data.UpdatePlans);
                await _jewelryContext.SaveChangesAsync();
            }

            if (data.newStockReceiptPlan != null && !string.IsNullOrEmpty(data.ReceiptRunning))
            {
                await _jewelryContext.TbtStockProductReceiptPlan.AddRangeAsync(data.newStockReceiptPlan);
                await _jewelryContext.SaveChangesAsync();
            }
            if (data.newStockReceiptItem.Any() && !string.IsNullOrEmpty(data.ReceiptRunning))
            {
                await _jewelryContext.TbtStockProductReceiptItem.AddRangeAsync(data.newStockReceiptItem);
                await _jewelryContext.SaveChangesAsync();
            }

            scope.Complete();
        }
        private class TransferData
        {
            public DateTime DateNow { get; set; }
            public string TransferRunning { get; set; }
            public string ReceiptRunning { get; set; }
            public List<TbtProductionPlanStatusHeader> NewStatuses { get; } = new();
            public List<TbtProductionPlanTransferStatus> TransferStatuses { get; } = new();
            public List<TbtProductionPlan> UpdatePlans { get; } = new();
            public List<TransferResponseItem> Errors { get; } = new();

            //new product to stock
            public List<TbtStockProductReceiptPlan> newStockReceiptPlan { get; } = new();
            public List<TbtStockProductReceiptItem> newStockReceiptItem { get; } = new();

            public bool HasAnyValidPlans => NewStatuses.Any();
        }
        #endregion

        #region --- restore plan ---
        public async Task<string> Restore(jewelry.Model.Production.Plan.Restore.Request request)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan
                        where item.Wo == request.Wo
                        && item.WoNumber == request.WoNumber
                        && item.Id == request.Id
                        select item).FirstOrDefault();


            return "success";
        }
        #endregion
    }
}
