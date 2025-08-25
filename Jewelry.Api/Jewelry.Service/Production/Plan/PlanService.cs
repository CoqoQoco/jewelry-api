using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.Production.Plan.Transfer;
using jewelry.Model.ProductionPlan.ProductionPlanStatus.Transfer;
using jewelry.Model.ProductionPlan.ProductionPlanStatusList;
using jewelry.Model.ProductionPlan.ProductionPlanTracking;
using jewelry.Model.ProductionPlanCost.GoldCostItem;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Index.HPRtree;
using NPOI.OpenXmlFormats;
using NPOI.OpenXmlFormats.Dml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static NPOI.HSSF.Util.HSSFColor;

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

                             ReceiveDate = item.Header.UpdateDate,
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

        #region --- list plan is success and ขัดชุบ gold ----
        public IQueryable<jewelry.Model.Production.Plan.ListComplete.Response> PlanCompleted(jewelry.Model.Production.Plan.ListComplete.Search request)
        {
            var query = (from statusDetail in _jewelryContext.TbtProductionPlanStatusDetail
                         .Include(x => x.Header)
                         .Include(x => x.Header.ProductionPlan)
                             .ThenInclude(x => x.StatusNavigation)
                         .Include(x => x.Header.ProductionPlan.TbtProductionPlanPrice)
                         .Include(x => x.Header.ProductionPlan.ProductTypeNavigation)
                         .Include(x => x.Header.ProductionPlan.CustomerTypeNavigation)
                         //.Include(x => x.Header.ProductionPlan.TbtProductionPlanStatusHeader
                         //    .Where(h => h.IsActive == true))

                             // Left joins using navigation properties instead of manual joins
                         where statusDetail.IsActive == true
                            && statusDetail.Header.Status == ProductionPlanStatus.Plated
                            && statusDetail.Header.IsActive == true
                            && (statusDetail.Header.ProductionPlan.Status == ProductionPlanStatus.Completed
                                || statusDetail.Header.ProductionPlan.Status == ProductionPlanStatus.Price)

                         let plan = statusDetail.Header.ProductionPlan
                         let customer = _jewelryContext.TbmCustomer
                             .FirstOrDefault(c => c.Code == plan.CustomerNumber)
                         let mold = _jewelryContext.TbtProductMold
                             .FirstOrDefault(m => m.Code == plan.Mold)
                         //let currentStatus = plan.TbtProductionPlanStatusHeader
                         //    .Where(x => x.IsActive == true && x.Status == plan.Status)
                         //    .OrderByDescending(x => x.UpdateDate)
                         //    .FirstOrDefault()

                         select new jewelry.Model.Production.Plan.ListComplete.Response()
                         {
                             Id = plan.Id,
                             Wo = plan.Wo,
                             WoNumber = plan.WoNumber,
                             WoText = plan.WoText,

                             Mold = plan.Mold,
                             MoldSub = mold != null && !string.IsNullOrEmpty(mold.ImageDraft1)
                                 ? $"{plan.Mold}-Sub" : string.Empty,

                             Status = plan.Status,
                             StatusName = plan.Status == ProductionPlanStatus.Completed && !plan.TbtProductionPlanPrice.Any()
                                 ? plan.StatusNavigation.Reference
                                 : statusDetail.Header.ProductionPlan.StatusNavigation.NameTh,

                             ProductNumber = plan.ProductNumber,
                             ProductQty = plan.ProductQty,

                             CustomerNumber = plan.CustomerNumber,
                             CustomerName = customer != null && !string.IsNullOrEmpty(customer.NameTh)
                                 ? customer.NameTh : null,

                             CustomerType = plan.CustomerType,
                             CustomerTypeName = plan.CustomerTypeNavigation.NameTh,

                             CreateDate = plan.CreateDate,
                             RequestDate = plan.RequestDate,
                             LastUpdateStatus = plan.UpdateDate,
                             //LastUpdateStatus = currentStatus != null
                             //    ? currentStatus.UpdateDate
                             //    : plan.UpdateDate,

                             IsSuccessWithoutCost = plan.Status == ProductionPlanStatus.Completed && !plan.TbtProductionPlanPrice.Any(),

                             ProductType = plan.ProductType,
                             ProductTypeName = plan.ProductTypeNavigation.NameTh,

                             Gold = plan.Type,
                             GoldSize = plan.TypeSize,

                             goldPlated = statusDetail.Gold,
                             GoldQtySend = statusDetail.GoldQtySend,
                             GoldWeightSend = statusDetail.GoldWeightSend,
                             GoldQtyCheck = statusDetail.GoldQtyCheck,
                             GoldWeightCheck = statusDetail.GoldWeightCheck,

                             Description = statusDetail.Description,
                         });

            // Date filters
            if (request.Start.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.Start.Value.StartOfDayUtc());
            }
            if (request.End.HasValue)
            {
                query = query.Where(x => x.CreateDate <= request.End.Value.EndOfDayUtc());
            }
            if (request.SendStart.HasValue)
            {
                query = query.Where(x => x.LastUpdateStatus >= request.SendStart.Value.StartOfDayUtc());
            }
            if (request.SendEnd.HasValue)
            {
                query = query.Where(x => x.LastUpdateStatus <= request.SendEnd.Value.EndOfDayUtc());
            }

            // IsOverPlan filter - ต้องเพิ่ม logic การคำนวณใน select ด้วย
            if (request.IsOverPlan.HasValue && request.IsOverPlan == 1)
            {
                // query = query.Where(x => x.IsOverPlan == true);
                // ต้องเพิ่ม logic การคำนวณ IsOverPlan ใน projection ด้านบน
            }

            // Text search
            if (!string.IsNullOrEmpty(request.Text))
            {
                var searchText = request.Text.ToUpper();
                query = query.Where(x =>
                    x.Wo.Contains(searchText) ||
                    x.WoText.Contains(request.Text) ||
                    x.Mold.Contains(request.Text) ||
                    x.ProductNumber.Contains(request.Text) ||
                    x.CustomerNumber.Contains(request.Text));
            }

            // Status filter
            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.Status));
            }

            // Other filters
            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                query = query.Where(x => x.CustomerNumber.Contains(request.CustomerCode));
            }
            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => request.Gold.Contains(x.Gold));
            }
            if (request.GoldSize != null && request.GoldSize.Any())
            {
                query = query.Where(x => request.GoldSize.Contains(x.GoldSize));
            }
            if (request.CustomerType != null && request.CustomerType.Any())
            {
                query = query.Where(x => request.CustomerType.Contains(x.CustomerType));
            }
            if (request.ProductType != null && request.ProductType.Any())
            {
                query = query.Where(x => request.ProductType.Contains(x.ProductType));
            }
            if (!string.IsNullOrEmpty(request.Mold))
            {
                query = query.Where(x => x.Mold.Contains(request.Mold));
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.ProductNumber.Contains(request.ProductNumber));
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

                             //WorkerCode = item.
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
            var plansCost = await GetProductionPlanCost(request.Plans.Select(x => $"{x.Wo}-{x.WoNumber}").ToArray());

            var response = new jewelry.Model.Production.Plan.Transfer.Response { Message = "success" };
            var transferData = await PrepareTransferData(request, plans, plansCost);

            //if (transferData.HasAnyValidPlans)
            //{
            //}
            await ProcessTransfer(transferData);

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

            if (request.TargetStatus == ProductionPlanStatus.Price && request.TargetStatusCvd.HasValue && request.TargetStatusCvd.Value)
            {
                var checkPermission = GetPermissionLevel("update_plan");
                if (!checkPermission)
                {
                    throw new HandleException($"{ErrorMessage.PermissionFail}");
                }
            }
        }
        private async Task<List<TbtProductionPlan>> GetProductionPlans(int[] planIds)
        {
            var plans = await _jewelryContext.TbtProductionPlan
                .Include(x => x.TbtProductionPlanStatusHeader)
                .Include(x => x.ProductTypeNavigation)
                .Include(x => x.TbtProductionPlanMaterial)
                .Where(item => planIds.Contains(item.Id))
                .ToListAsync();

            if (!plans.Any())
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            return plans;
        }
        private async Task<List<GoldCostItemResponse>> GetProductionPlanCost(string[] planNumbers)
        {
            var query = (from item in _jewelryContext.TbtProductionPlanCostGoldItem
                         .Include(x => x.TbtProductionPlanCostGold)
                         where item.TbtProductionPlanCostGold.IsActive == true
                         select new GoldCostItemResponse()
                         {
                             No = item.No,
                             BookNo = item.BookNo,
                             AssignDate = item.TbtProductionPlanCostGold.AssignDate,

                             GoldCode = item.TbtProductionPlanCostGold.GoldNavigation.Code,
                             GoldName = item.TbtProductionPlanCostGold.GoldNavigation.NameTh,
                             GoldSizeCode = item.TbtProductionPlanCostGold.GoldSizeNavigation.Code,
                             GoldSizeName = item.TbtProductionPlanCostGold.GoldSizeNavigation.NameTh,
                             GoldReceipt = item.TbtProductionPlanCostGold.GoldReceipt,

                             Zill = item.TbtProductionPlanCostGold.Zill,
                             ZillQty = item.TbtProductionPlanCostGold.ZillQty,

                             MeltDate = item.TbtProductionPlanCostGold.MeltDate,
                             MeltWeight = item.TbtProductionPlanCostGold.MeltWeight,
                             ReturnMeltWeight = item.TbtProductionPlanCostGold.ReturnMeltWeight,
                             ReturnMeltScrapWeight = item.TbtProductionPlanCostGold.ReturnMeltScrapWeight,
                             MeltWeightLoss = item.TbtProductionPlanCostGold.MeltWeightLoss,
                             MeltWeightOver = item.TbtProductionPlanCostGold.MeltWeightOver,

                             CastDate = item.TbtProductionPlanCostGold.CastDate,
                             CastWeight = item.TbtProductionPlanCostGold.CastWeight,
                             GemWeight = item.TbtProductionPlanCostGold.GemWeight,
                             ReturnCastWeight = item.TbtProductionPlanCostGold.ReturnCastWeight,
                             ReturnCastMoldWeight = item.TbtProductionPlanCostGold.ReturnCastMoldWeight,
                             ReturnCastBodyBrokenWeight = item.TbtProductionPlanCostGold.ReturnCastBodyBrokenedWeight,
                             ReturnCastBodyWeightTotal = item.TbtProductionPlanCostGold.ReturnCastBodyWeightTotal,
                             ReturnCastScrapWeight = item.TbtProductionPlanCostGold.ReturnCastScrapWeight,
                             ReturnCastPowderWeight = item.TbtProductionPlanCostGold.ReturnCastPowderWeight,
                             CastWeightLoss = item.TbtProductionPlanCostGold.CastWeightLoss,
                             CastWeightOver = item.TbtProductionPlanCostGold.CastWeightOver,

                             Cost = item.TbtProductionPlanCostGold.Cost,

                             AssignBy = item.TbtProductionPlanCostGold.AssignBy,
                             ReceiveBy = item.TbtProductionPlanCostGold.ReceiveBy,
                             RunningNumber = item.TbtProductionPlanCostGold.RunningNumber,
                             Remark1 = item.TbtProductionPlanCostGold.Remark,

                             ProductionPlanId = item.ProductionPlanId,
                             ReturnWeight = item.ReturnWeight,
                             ReturnQTY = item.ReturnQty.HasValue ? item.ReturnQty.Value : 0,
                             Remark2 = item.Remark,
                         });

            query = query.Where(x => planNumbers.Contains(x.ProductionPlanId));

            if (!planNumbers.Any())
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            return await query.ToListAsync();
        }

        private async Task<TransferData> PrepareTransferData(jewelry.Model.Production.Plan.Transfer.Request request,
            List<TbtProductionPlan> plans,
            List<GoldCostItemResponse> plansGoldCostItem)
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
                    var targetPlansGoldCostItem = plansGoldCostItem.Where(x => x.ProductionPlanId == $"{targetPlan.Wo}-{targetPlan.WoNumber}").ToList();
                    await AddValidPlanData(data, targetPlan, targetPlansGoldCostItem, request, receiptRunning, false);
                }
                else if (validationResult.ErrorMessage == ErrorMessage.StatusAlready
                        && request.TargetStatusCvd.HasValue
                        && request.TargetStatusCvd.Value
                        && request.TargetStatus == ProductionPlanStatus.Price)
                {
                    var targetPlan = plans.First(x => x.Id == planRequest.Id);
                    var targetPlansGoldCostItem = plansGoldCostItem.Where(x => x.ProductionPlanId == $"{targetPlan.Wo}-{targetPlan.WoNumber}").ToList();
                    await AddValidPlanData(data, targetPlan, targetPlansGoldCostItem, request, receiptRunning, true);
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
            List<GoldCostItemResponse> plansGoldCostItem,
            jewelry.Model.Production.Plan.Transfer.Request request,
            string receiptRunning,
            bool isSkibByCVD)
        {
            if (isSkibByCVD == false)
            {
                var newStatus = CreateNewStatus(plan, request, data.DateNow);
                data.NewStatuses.Add(newStatus);

                //new status detial
                var newStatusDetail = await CreateNewStatusDetial(plan, plansGoldCostItem, request, data.DateNow);
                data.NewStatusDetail.AddRange(newStatusDetail);

                var transferStatus = CreateTransferStatus(plan, request, data.TransferRunning, data.DateNow);
                data.TransferStatuses.Add(transferStatus);
            }

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
                plan.CompletedDate = data.DateNow;
                data.ReceiptRunning = receiptRunning;
            }

            bool isCvd = request.TargetStatusCvd.HasValue ? request.TargetStatusCvd.Value : false;

            plan.Status = currentStatus.GetWatingStatus(isCvd);
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

                WorkerName = request.WorkerName,
                WorkerCode = request.WorkerCode,

                IsActive = true,
                ProductionPlanId = plan.Id,
                Status = request.TargetStatus
            };
        }

        private async Task<List<TbtProductionPlanStatusDetail>> CreateNewStatusDetial(
            TbtProductionPlan plan,
            List<GoldCostItemResponse> plansGoldCostItem,
            jewelry.Model.Production.Plan.Transfer.Request request,
            DateTime dateNow)
        {
            var NewStatusDetail = new List<TbtProductionPlanStatusDetail>();

            if (plansGoldCostItem.Any())
            {
                foreach (var item in plansGoldCostItem)
                {
                    var detail = new TbtProductionPlanStatusDetail
                    {
                        //HeaderId = headerId,
                        ProductionPlanId = plan.Id,
                        ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{plan.Id}-{request.TargetStatus}"),
                        IsActive = true,
                        RequestDate = dateNow,

                        Gold = item.GoldCode,

                        GoldQtySend = item.ReturnQTY,
                        GoldWeightSend = item.ReturnWeight,

                        GoldQtyCheck = null,
                        GoldWeightCheck = null,

                        Worker = null,
                        WorkerSub = null,
                        Description = null,
                        Wages = 0,
                        TotalWages = 0
                    };
                    NewStatusDetail.Add(detail);
                }
            }
            else
            {
                if (plan.TbtProductionPlanMaterial.Any())
                {
                    foreach (var item in plan.TbtProductionPlanMaterial)
                    {
                        var detail = new TbtProductionPlanStatusDetail
                        {
                            //HeaderId = headerId,
                            ProductionPlanId = plan.Id,
                            ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{plan.Id}-{request.TargetStatus}"),
                            IsActive = true,
                            RequestDate = dateNow,
                            Gold = item.Gold,

                            GoldQtySend = item.GoldQty,
                            GoldWeightSend = null,

                            GoldQtyCheck = null,
                            GoldWeightCheck = null,

                            Worker = null,
                            WorkerSub = null,
                            Description = null,
                            Wages = 0,
                            TotalWages = 0
                        };
                        NewStatusDetail.Add(detail);
                    }
                }
            }


            return NewStatusDetail;
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
                TargetStatus = request.TargetStatus,

                WorkerCode = request.WorkerCode,
                WorkerName = request.WorkerName
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

            // บันทึก status detail
            if (data.NewStatusDetail.Any())
            {
                foreach (var item in data.NewStatusDetail)
                {
                    item.HeaderId = data.NewStatuses.First(x => x.ProductionPlanId == item.ProductionPlanId).Id;
                }

                await _jewelryContext.TbtProductionPlanStatusDetail.AddRangeAsync(data.NewStatusDetail);
                await _jewelryContext.SaveChangesAsync();
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
            public List<TbtProductionPlanStatusDetail> NewStatusDetail { get; } = new();


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

        #region --- daily report ---
        public async Task<jewelry.Model.Production.Plan.DailyPlan.Response> GetDailyReport(jewelry.Model.Production.Plan.DailyPlan.Criteria request)
        {
            var utcNow = DateTime.UtcNow;
            var yesterdayStart = utcNow.Date.AddDays(-1);
            var yesterdayEnd = utcNow.Date.AddMilliseconds(-1);

            // Define disabled status
            var disableStatus = new int[]
            {
                ProductionPlanStatus.Melted,
                ProductionPlanStatus.WaitCVD,
                ProductionPlanStatus.CVD,
            };

            // Get active status templates
            var activeStatus = await (from item in _jewelryContext.TbmProductionPlanStatus
                                      where !disableStatus.Contains(item.Id)
                                      select new jewelry.Model.Production.Plan.DailyPlan.ReortItem()
                                      {
                                          Status = item.Id,
                                          StatusNameEN = item.NameEn,
                                          StatusNameTH = item.NameTh,
                                          Description = item.Description,
                                          Reference = item.Reference,
                                          Count = 0
                                      }).ToListAsync();

            var successStatus = new List<int>
            {
                ProductionPlanStatus.Completed,
                ProductionPlanStatus.Melted,
                ProductionPlanStatus.WaitCVD,
                ProductionPlanStatus.CVD,
                //ProductionPlanStatus.Price 
            };

            // Main query for production plans
            var baseQuery = from item in _jewelryContext.TbtProductionPlan
                           .Include(x => x.StatusNavigation)
                           .Include(x => x.TbtProductionPlanStatusHeader.Where(o => o.IsActive == true).OrderByDescending(x => x.UpdateDate))
                           .Include(x => x.TbtProductionPlanPrice)
                           .Include(o => o.ProductTypeNavigation)
                           .Include(o => o.CustomerTypeNavigation)
                                //.Include(o => o.TypeNavigation)
                                //.Include(o => o.TypeSizeNavigation)

                            join customer in _jewelryContext.TbmCustomer on item.CustomerNumber equals customer.Code into customerJoin
                            from cj in customerJoin.DefaultIfEmpty()

                            join mold in _jewelryContext.TbtProductMold on item.Mold equals mold.Code into moldJoin
                            from m in moldJoin.DefaultIfEmpty()

                            where item.IsActive == true
                            let currentStatus = item.TbtProductionPlanStatusHeader.Where(x => x.IsActive == true && x.Status == item.Status).FirstOrDefault()

                            select new
                            {
                                Id = item.Id,
                                Wo = item.Wo,
                                WoNumber = item.WoNumber,
                                WoText = item.WoText,
                                CreateDate = item.CreateDate,
                                CreateBy = item.CreateBy,
                                UpdateDate = item.UpdateDate,
                                UpdateBy = item.UpdateBy,
                                RequestDate = item.RequestDate,

                                Mold = item.Mold,
                                MoldSub = m != null && !string.IsNullOrEmpty(m.ImageDraft1) ? $"{item.Mold}-Sub" : string.Empty,

                                Status = item.Status,
                                StatusName = item.Status == ProductionPlanStatus.Completed && item.TbtProductionPlanPrice.Any() == false ? item.StatusNavigation.Reference : item.StatusNavigation.NameTh,

                                ProductRunning = item.ProductRunning,
                                ProductNumber = item.ProductNumber,
                                ProductName = item.ProductName,
                                ProductDetail = item.ProductDetail,
                                ProductQty = item.ProductQty,
                                ProductQtyUnit = item.ProductQtyUnit,
                                //ProductWeight = item.ProductWeight,

                                CustomerNumber = item.CustomerNumber,
                                CustomerName = cj != null && !string.IsNullOrEmpty(cj.NameTh) ? cj.NameTh : null,

                                CustomerType = item.CustomerType,
                                CustomerTypeName = item.CustomerTypeNavigation.NameTh,

                                LastUpdateStatus = currentStatus != null ? currentStatus.UpdateDate : (item.UpdateDate ?? item.CreateDate),

                                IsOverPlan = item.RequestDate < utcNow && !successStatus.Contains(item.Status),
                                IsSuccessWithoutCost = item.Status == ProductionPlanStatus.Completed && item.TbtProductionPlanPrice.Any() == false,

                                ProductType = item.ProductType,
                                ProductTypeName = item.ProductTypeNavigation.NameTh,

                                Gold = item.Type,
                                //GoldName = item.TypeNavigation.NameTh,
                                GoldSize = item.TypeSize,
                                //GoldSizeName = item.TypeSizeNavigation.NameTh,

                                IsActive = item.IsActive,
                                Remark = item.Remark
                            };

            // Apply filters
            var query = baseQuery;

            if (request.Start.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.Start.Value.StartOfDayUtc());
            }
            if (request.End.HasValue)
            {
                query = query.Where(x => x.CreateDate <= request.End.Value.EndOfDayUtc());
            }

            if (request.SendStart.HasValue)
            {
                query = query.Where(x => x.LastUpdateStatus >= request.SendStart.Value.StartOfDayUtc());
            }
            if (request.SendEnd.HasValue)
            {
                query = query.Where(x => x.LastUpdateStatus <= request.SendEnd.Value.EndOfDayUtc());
            }

            if (request.IsOverPlan.HasValue && request.IsOverPlan == 1)
            {
                query = query.Where(x => x.IsOverPlan == true);
            }

            if (!string.IsNullOrEmpty(request.Text))
            {
                var searchText = request.Text.ToUpper();
                query = query.Where(x => x.Wo.Contains(searchText)
                                    || x.WoText.Contains(request.Text)
                                    || x.Mold.Contains(request.Text)
                                    || x.ProductNumber.Contains(request.Text)
                                    || x.CustomerNumber.Contains(request.Text));
            }

            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.Status));
            }

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                query = query.Where(x => x.CustomerNumber.Contains(request.CustomerCode));
            }

            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => request.Gold.Contains(x.Gold));
            }

            if (request.GoldSize != null && request.GoldSize.Any())
            {
                query = query.Where(x => request.GoldSize.Contains(x.GoldSize));
            }

            if (request.CustomerType != null && request.CustomerType.Any())
            {
                query = query.Where(x => request.CustomerType.Contains(x.CustomerType));
            }

            if (request.ProductType != null && request.ProductType.Any())
            {
                query = query.Where(x => request.ProductType.Contains(x.ProductType));
            }

            if (!string.IsNullOrEmpty(request.Mold))
            {
                query = query.Where(x => x.Mold.Contains(request.Mold));
            }

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.ProductNumber.Contains(request.ProductNumber));
            }

            //ก่อน filter ทั้งหมด

            var planCountCompletedYesterday = await _jewelryContext.TbtProductionPlan
                .Where(x => x.IsActive == true
                       && x.Status == ProductionPlanStatus.Completed
                       && x.CompletedDate >= yesterdayStart
                       && x.CompletedDate <= yesterdayEnd)
                .CountAsync();

            var completedToday = await _jewelryContext.TbtProductionPlan
                .Where(x => x.IsActive == true
                       && x.Status == ProductionPlanStatus.Completed
                       && x.CompletedDate >= utcNow.Date
                       && x.CompletedDate < utcNow.Date.AddDays(1))
                .CountAsync();

            var removeStatus = new List<int>
            {
                //ProductionPlanStatus.Completed,
                ProductionPlanStatus.Melted,
                ProductionPlanStatus.WaitCVD,
                ProductionPlanStatus.CVD,
                //ProductionPlanStatus.Price 
            };
            //remove seccoss 100%, melted, wait cvd, cvd
            query = query.Where(x => !removeStatus.Contains(x.Status));
            query = query.Where(x => !(x.Status == ProductionPlanStatus.Completed && !x.IsSuccessWithoutCost));

            //var tettt = query.ToList();

            // Calculate status counts efficiently
            var statusCounts = query.GroupBy(x => x.Status).ToDictionary(g => g.Key, g => g.Count());

            // Special handling for completed status (only count those without price)
            var completedWithoutPriceCount = query.Count(x => x.Status == ProductionPlanStatus.Completed && x.IsSuccessWithoutCost);

            // Update status counts
            foreach (var status in activeStatus)
            {
                if (status.Status == ProductionPlanStatus.Completed)
                {
                    status.Count = completedWithoutPriceCount;
                    status.StatusNameTH = status.Reference;
                }
                else
                {
                    status.Count = statusCounts.GetValueOrDefault(status.Status, 0);
                }
            }

            // Get recent activity (last 10 updated items)
            var recentActivity = query
                .Where(x => x.LastUpdateStatus.HasValue)
                .OrderByDescending(x => x.LastUpdateStatus)
                .Take(10)
                .Select(x => new jewelry.Model.Production.Plan.DailyPlan.RecentItem
                {
                    Id = x.Id,
                    Wo = x.Wo,
                    WoNumber = x.WoNumber,
                    WoText = x.WoText,
                    CreateDate = x.CreateDate,
                    CreateBy = x.CreateBy ?? "",
                    UpdateDate = x.UpdateDate,
                    UpdateBy = x.UpdateBy,
                    RequestDate = x.RequestDate,
                    Mold = x.Mold,
                    MoldSub = x.MoldSub,
                    ProductRunning = x.ProductRunning,
                    ProductName = x.ProductName ?? "",
                    ProductType = x.ProductType.ToString(),
                    ProductTypeName = x.ProductTypeName ?? "",
                    ProductNumber = x.ProductNumber,
                    ProductDetail = x.ProductDetail ?? "",
                    ProductQty = x.ProductQty,
                    ProductQtyUnit = x.ProductQtyUnit ?? "",
                    CustomerNumber = x.CustomerNumber,
                    CustomerName = x.CustomerName ?? "",
                    CustomerType = x.CustomerType,
                    CustomerTypeName = x.CustomerTypeName ?? "",
                    IsActive = x.IsActive,
                    Status = x.Status,
                    StatusName = x.StatusName ?? "",
                    Remark = x.Remark,
                    Gold = x.Gold,
                    GoldSize = x.GoldSize
                }).ToList();

            // Calculate dashboard metrics
            var planCountProcess = query.Count(x => !successStatus.Contains(x.Status));
            var planCountOverdue = query.Count(x => x.IsOverPlan && !successStatus.Contains(x.Status));
            var planCountTotal = query.Count();

            var percentageCompleted = planCountTotal > 0
                                        ? Math.Round((decimal)query.Count(x => x.Status == ProductionPlanStatus.Completed) * 100 / planCountTotal, 2)
                                        : 0;

            var pendingApproval = query.Count(x => x.Status == ProductionPlanStatus.Designed);

            // Calculate status trends
            var statusTrends = activeStatus.Select(status => new jewelry.Model.Production.Plan.DailyPlan.StatusTrend
            {
                Status = status.Status,
                StatusName = status.StatusNameTH ?? "",
                Count = status.Count,
                Percentage = planCountTotal > 0 ? Math.Round((decimal)status.Count * 100 / planCountTotal, 2) : 0,
                TrendDirection = "stable" // Could be enhanced with historical data comparison
            }).ToList();

            // Product type summary
            var productTypeSummary = query
                .GroupBy(x => new { x.ProductType, x.ProductTypeName })
                .Select(g => new jewelry.Model.Production.Plan.DailyPlan.ProductTypeSummary
                {
                    ProductType = g.Key.ProductType,
                    ProductTypeName = g.Key.ProductTypeName ?? "",
                    Count = g.Count(),
                    TotalQty = g.Sum(x => x.ProductQty),
                    //TotalWeight = g.Sum(x => x.ProductWeight)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Customer type summary
            var customerTypeSummary = query
                .GroupBy(x => new { x.CustomerType, x.CustomerTypeName })
                .Select(g => new jewelry.Model.Production.Plan.DailyPlan.CustomerTypeSummary
                {
                    CustomerType = g.Key.CustomerType ?? "",
                    CustomerTypeName = g.Key.CustomerTypeName ?? "",
                    Count = g.Count(),
                    TotalQty = g.Sum(x => x.ProductQty)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            return new jewelry.Model.Production.Plan.DailyPlan.Response()
            {
                Report = activeStatus.OrderBy(x => x.Status).ToList(),
                RecentActivity = recentActivity,
                PlanCountProcess = planCountProcess,
                PlanCountCompletedOnYesterday = planCountCompletedYesterday,
                PlanCountOverdue = planCountOverdue,
                PlanCountTotal = planCountTotal,
                Summary = new jewelry.Model.Production.Plan.DailyPlan.DashboardSummary
                {
                    TotalActiveProjects = planCountTotal,
                    CompletedToday = completedToday,
                    OverduePlans = planCountOverdue,
                    PendingApproval = pendingApproval,
                    PercentageCompleted = percentageCompleted,
                    StatusTrends = statusTrends.OrderBy(x => x.Status).ToList(),
                    ProductTypeSummary = productTypeSummary,
                    CustomerTypeSummary = customerTypeSummary
                }
            };
        }
        #endregion

        #region --- monthly success report ---
        public async Task<jewelry.Model.Production.Plan.MonthlyReport.Response> GetPlanSuccessMonthlyReport(jewelry.Model.Production.Plan.MonthlyReport.Criteria request)
        {
            var query = from statusHeader in _jewelryContext.TbtProductionPlanStatusHeader
                        join productionPlan in _jewelryContext.TbtProductionPlan on statusHeader.ProductionPlanId equals productionPlan.Id
                        join productType in _jewelryContext.TbmProductType on productionPlan.ProductType equals productType.Code into productTypeJoin
                        from pt in productTypeJoin.DefaultIfEmpty()
                        join customerType in _jewelryContext.TbmCustomerType on productionPlan.CustomerType equals customerType.Code into customerTypeJoin
                        from ct in customerTypeJoin.DefaultIfEmpty()
                        where statusHeader.Status == 100 // success status
                        && statusHeader.CreateDate >= request.StartDate.StartOfDayUtc()
                        && statusHeader.CreateDate <= request.EndDate.EndOfDayUtc()
                        && statusHeader.IsActive == true
                        && productionPlan.IsActive == true
                        select new
                        {
                            ProductionPlan = productionPlan,
                            ProductType = pt != null ? pt.Code : productionPlan.ProductType,
                            ProductTypeName = pt != null ? pt.NameTh : productionPlan.ProductType,
                            CustomerType = ct != null ? ct.Code : productionPlan.CustomerType,
                            CustomerTypeName = ct != null ? ct.NameTh : productionPlan.CustomerType,
                            Type = !string.IsNullOrEmpty(productionPlan.Type) ? productionPlan.Type : "ไม่ระบุ",
                            TypeName = !string.IsNullOrEmpty(productionPlan.Type) ? productionPlan.Type : "ไม่ระบุ"
                        };

            var data = await query.ToListAsync();
            var totalCount = data.Count;
            var totalQty = data.Sum(x => x.ProductionPlan.ProductQty);

            // 1. Plan finish by Type (Gold Type)
            var planFinishByType = data
                .Where(x => !string.IsNullOrEmpty(x.Type))
                .GroupBy(x => new { x.Type, x.TypeName })
                .Select(g => new jewelry.Model.Production.Plan.MonthlyReport.PlanFinishByType
                {
                    Type = g.Key.Type ?? string.Empty,
                    TypeName = g.Key.TypeName ?? string.Empty,
                    Count = g.Count(),
                    TotalQty = g.Sum(x => x.ProductionPlan.ProductQty),
                    Percentage = totalCount > 0 ? Math.Round((decimal)g.Count() * 100 / totalCount, 2) : 0
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // 2. Plan finish by ProductType
            var planFinishByProductType = data
                .GroupBy(x => new { x.ProductType, x.ProductTypeName })
                .Select(g => new jewelry.Model.Production.Plan.MonthlyReport.PlanFinishByProductType
                {
                    ProductType = g.Key.ProductType ?? string.Empty,
                    ProductTypeName = g.Key.ProductTypeName ?? string.Empty,
                    Count = g.Count(),
                    TotalQty = g.Sum(x => x.ProductionPlan.ProductQty),
                    Percentage = totalCount > 0 ? Math.Round((decimal)g.Count() * 100 / totalCount, 2) : 0
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // 3. Plan finish by CustomerType
            var planFinishByCustomerType = data
                .GroupBy(x => new { x.CustomerType, x.CustomerTypeName })
                .Select(g => new jewelry.Model.Production.Plan.MonthlyReport.PlanFinishByCustomerType
                {
                    CustomerType = g.Key.CustomerType ?? string.Empty,
                    CustomerTypeName = g.Key.CustomerTypeName ?? string.Empty,
                    Count = g.Count(),
                    TotalQty = g.Sum(x => x.ProductionPlan.ProductQty),
                    Percentage = totalCount > 0 ? Math.Round((decimal)g.Count() * 100 / totalCount, 2) : 0
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            return new jewelry.Model.Production.Plan.MonthlyReport.Response
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                PlanFinishByType = planFinishByType,
                PlanFinishByProductType = planFinishByProductType,
                PlanFinishByCustomerType = planFinishByCustomerType
            };
        }
        #endregion
    }
}
