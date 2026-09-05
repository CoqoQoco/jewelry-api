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
using System.Globalization;
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

                             Status = item.Header.ProductionPlan.Status,
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
                var woTextPattern = $"%{LikePattern.EscapeLikePattern(request.WoText)}%";
                query = query.Where(x => EF.Functions.ILike(x.WoText, woTextPattern));
            }
            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => request.Gold.Contains(x.Gold));
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                var productNumberPattern = $"%{LikePattern.EscapeLikePattern(request.ProductNumber)}%";
                query = query.Where(x => EF.Functions.ILike(x.ProductNumber, productNumberPattern));
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
                var searchPattern = $"%{LikePattern.EscapeLikePattern(request.Text)}%";
                query = query.Where(x => EF.Functions.ILike(x.Wo, searchPattern)
                                    || EF.Functions.ILike(x.WoText, searchPattern)
                                    || EF.Functions.ILike(x.Mold, searchPattern)
                                    || EF.Functions.ILike(x.ProductNumber, searchPattern)
                                    || EF.Functions.ILike(x.CustomerNumber, searchPattern));
            }

            // Status filter
            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.Status));
            }

            // Other filters
            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                var customerCodePattern = $"%{LikePattern.EscapeLikePattern(request.CustomerCode)}%";
                query = query.Where(x => EF.Functions.ILike(x.CustomerNumber, customerCodePattern));
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
                var moldPattern = $"%{LikePattern.EscapeLikePattern(request.Mold)}%";
                query = query.Where(x => EF.Functions.ILike(x.Mold, moldPattern));
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                var productNumberPattern = $"%{LikePattern.EscapeLikePattern(request.ProductNumber)}%";
                query = query.Where(x => EF.Functions.ILike(x.ProductNumber, productNumberPattern));
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
                var transferNumberPattern = $"%{LikePattern.EscapeLikePattern(request.TransferNumber)}%";
                query = query.Where(x => EF.Functions.ILike(x.TransferNumber, transferNumberPattern));
            }
            if (!string.IsNullOrEmpty(request.WoText))
            {
                var woTextPattern = $"%{LikePattern.EscapeLikePattern(request.WoText)}%";
                query = query.Where(x => EF.Functions.ILike(x.WoText, woTextPattern));
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
                var productNumberPattern = $"%{LikePattern.EscapeLikePattern(request.ProductNumber)}%";
                query = query.Where(x => EF.Functions.ILike(x.ProductNumber, productNumberPattern));
            }

            if (!string.IsNullOrEmpty(request.Mold))
            {
                var moldPattern = $"%{LikePattern.EscapeLikePattern(request.Mold)}%";
                query = query.Where(x => EF.Functions.ILike(x.Mold, moldPattern));
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
                .Include(x => x.TbtProductionPlanStatusHeader).ThenInclude(x => x.TbtProductionPlanStatusDetail)
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
            bool isAddDataFormGold = true;
            var NewStatusDetail = new List<TbtProductionPlanStatusDetail>();
            var formerStatusDetail = plan.TbtProductionPlanStatusHeader
                                      .Where(x => x.Status == request.FormerStatus)
                                      .SelectMany(x => x.TbtProductionPlanStatusDetail);

            if (formerStatusDetail.Any())
            {
                isAddDataFormGold = false;

               

                foreach (var item in formerStatusDetail)
                {
                    var GoldQtySend = item.GoldQtyCheck ?? item.GoldQtySend;
                    var GoldWeightSend = item.GoldWeightCheck ?? item.GoldWeightSend;

                    //ฝัง ไป ขัดชุบ
                    if (request.FormerStatus == ProductionPlanStatus.Embedd && request.TargetStatus == ProductionPlanStatus.Plated)
                    {
                         GoldQtySend = item.GoldQtySend;
                    }

                    var detail = new TbtProductionPlanStatusDetail
                    {
                        //HeaderId = headerId,
                        ProductionPlanId = plan.Id,
                        ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{plan.Id}-{request.TargetStatus}"),
                        IsActive = true,
                        RequestDate = dateNow,

                        Gold = item.Gold,

                        GoldQtySend = GoldQtySend,
                        GoldWeightSend = GoldWeightSend,

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

            if (isAddDataFormGold)
            {
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
            var todayThaiStartUtc = utcNow.AddHours(7).Date.AddHours(-7);
            var tomorrowThaiStartUtc = todayThaiStartUtc.AddDays(1);
            var yesterdayThaiStartUtc = todayThaiStartUtc.AddDays(-1);

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
                var searchPattern = $"%{LikePattern.EscapeLikePattern(request.Text)}%";
                query = query.Where(x => EF.Functions.ILike(x.Wo, searchPattern)
                                    || EF.Functions.ILike(x.WoText, searchPattern)
                                    || EF.Functions.ILike(x.Mold, searchPattern)
                                    || EF.Functions.ILike(x.ProductNumber, searchPattern)
                                    || EF.Functions.ILike(x.CustomerNumber, searchPattern));
            }

            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.Status));
            }

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                var customerCodePattern = $"%{LikePattern.EscapeLikePattern(request.CustomerCode)}%";
                query = query.Where(x => EF.Functions.ILike(x.CustomerNumber, customerCodePattern));
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
                var moldPattern = $"%{LikePattern.EscapeLikePattern(request.Mold)}%";
                query = query.Where(x => EF.Functions.ILike(x.Mold, moldPattern));
            }

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                var productNumberPattern = $"%{LikePattern.EscapeLikePattern(request.ProductNumber)}%";
                query = query.Where(x => EF.Functions.ILike(x.ProductNumber, productNumberPattern));
            }

            //ก่อน filter ทั้งหมด

            var planCountCompletedYesterday = await _jewelryContext.TbtProductionPlan
                .Where(x => x.IsActive == true
                       && x.Status == ProductionPlanStatus.Completed
                       && x.CompletedDate >= yesterdayThaiStartUtc
                       && x.CompletedDate < todayThaiStartUtc)
                .CountAsync();

            var completedToday = await _jewelryContext.TbtProductionPlan
                .Where(x => x.IsActive == true
                       && x.Status == ProductionPlanStatus.Completed
                       && x.CompletedDate >= todayThaiStartUtc
                       && x.CompletedDate < tomorrowThaiStartUtc)
                .CountAsync();

            var scopedQuery = query;

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

            var completedCount = scopedQuery.Count(x => x.Status == ProductionPlanStatus.Completed);
            var totalPlanCount = scopedQuery.Count();
            var percentageCompleted = totalPlanCount > 0
                                        ? Math.Round((decimal)completedCount * 100 / totalPlanCount, 2)
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
                    CompletedCount = completedCount,
                    TotalPlanCount = totalPlanCount,
                    StatusTrends = statusTrends.OrderBy(x => x.Status).ToList(),
                    ProductTypeSummary = productTypeSummary,
                    CustomerTypeSummary = customerTypeSummary
                }
            };
        }
        #endregion

        #region --- completed daily series ---
        public async Task<jewelry.Model.Production.Plan.CompletedDailySeries.Response> GetCompletedDailySeries(jewelry.Model.Production.Plan.CompletedDailySeries.Criteria request)
        {
            var utcNow = DateTime.UtcNow;

            var start = request.Start ?? new DateTimeOffset(new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc));
            var end = request.End ?? new DateTimeOffset(utcNow.Date.AddDays(1), TimeSpan.Zero);

            var query = _jewelryContext.TbtProductionPlan
                .Where(x => x.IsActive == true
                       && x.Status == ProductionPlanStatus.Completed
                       && x.CompletedDate.HasValue
                       && x.CompletedDate >= start.StartOfDayUtc()
                       && x.CompletedDate <= end.EndOfDayUtc());

            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => request.Gold.Contains(x.Type));
            }

            if (request.GoldSize != null && request.GoldSize.Any())
            {
                query = query.Where(x => request.GoldSize.Contains(x.TypeSize));
            }

            if (request.ProductType != null && request.ProductType.Any())
            {
                query = query.Where(x => request.ProductType.Contains(x.ProductType));
            }

            if (request.CustomerType != null && request.CustomerType.Any())
            {
                query = query.Where(x => request.CustomerType.Contains(x.CustomerType));
            }

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                var customerCodePattern = $"%{LikePattern.EscapeLikePattern(request.CustomerCode)}%";
                query = query.Where(x => EF.Functions.ILike(x.CustomerNumber, customerCodePattern));
            }

            if (!string.IsNullOrEmpty(request.Mold))
            {
                var moldPattern = $"%{LikePattern.EscapeLikePattern(request.Mold)}%";
                query = query.Where(x => EF.Functions.ILike(x.Mold, moldPattern));
            }

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                var productNumberPattern = $"%{LikePattern.EscapeLikePattern(request.ProductNumber)}%";
                query = query.Where(x => EF.Functions.ILike(x.ProductNumber, productNumberPattern));
            }

            if (!string.IsNullOrEmpty(request.Text))
            {
                var searchPattern = $"%{LikePattern.EscapeLikePattern(request.Text)}%";
                query = query.Where(x => EF.Functions.ILike(x.Wo, searchPattern)
                                    || EF.Functions.ILike(x.WoText, searchPattern)
                                    || EF.Functions.ILike(x.Mold, searchPattern)
                                    || EF.Functions.ILike(x.ProductNumber, searchPattern)
                                    || EF.Functions.ILike(x.CustomerNumber, searchPattern));
            }

            var grouped = await query
                .GroupBy(x => x.CompletedDate.Value.AddHours(7).Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var rows = grouped.Select(x => new jewelry.Model.Production.Plan.CompletedDailySeries.Row
            {
                Date = x.Date.ToString("yyyy-MM-dd"),
                Count = x.Count
            }).ToList();

            var total = grouped.Sum(x => x.Count);

            var today = utcNow.Date;
            var startDate = start.UtcDateTime.Date;
            var endDate = end.UtcDateTime.Date;
            var elapsedEndDate = endDate < today ? endDate : today;

            var daysElapsed = elapsedEndDate >= startDate ? (elapsedEndDate - startDate).Days + 1 : 0;
            var daysInPeriod = endDate >= startDate ? (endDate - startDate).Days + 1 : 0;

            return new jewelry.Model.Production.Plan.CompletedDailySeries.Response
            {
                Rows = rows,
                Total = total,
                DaysElapsed = daysElapsed,
                DaysInPeriod = daysInPeriod
            };
        }
        #endregion

        #region --- monthly success report ---
        public async Task<jewelry.Model.Production.Plan.MonthlyReport.Response> GetPlanSuccessMonthlyReport(jewelry.Model.Production.Plan.MonthlyReport.Criteria request)
        {
            var query = from productionPlan in _jewelryContext.TbtProductionPlan
                        join productType in _jewelryContext.TbmProductType on productionPlan.ProductType equals productType.Code into productTypeJoin
                        from pt in productTypeJoin.DefaultIfEmpty()
                        join customerType in _jewelryContext.TbmCustomerType on productionPlan.CustomerType equals customerType.Code into customerTypeJoin
                        from ct in customerTypeJoin.DefaultIfEmpty()
                        where productionPlan.IsActive == true
                        && productionPlan.Status == ProductionPlanStatus.Completed
                        && productionPlan.CompletedDate != null
                        && productionPlan.CompletedDate >= request.StartDate.StartOfDayUtc()
                        && productionPlan.CompletedDate <= request.EndDate.EndOfDayUtc()
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

            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => request.Gold.Contains(x.ProductionPlan.Type));
            }

            if (request.GoldSize != null && request.GoldSize.Any())
            {
                query = query.Where(x => request.GoldSize.Contains(x.ProductionPlan.TypeSize));
            }

            if (request.ProductType != null && request.ProductType.Any())
            {
                query = query.Where(x => request.ProductType.Contains(x.ProductionPlan.ProductType));
            }

            if (request.CustomerType != null && request.CustomerType.Any())
            {
                query = query.Where(x => request.CustomerType.Contains(x.ProductionPlan.CustomerType));
            }

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                var customerCodePattern = $"%{LikePattern.EscapeLikePattern(request.CustomerCode)}%";
                query = query.Where(x => EF.Functions.ILike(x.ProductionPlan.CustomerNumber, customerCodePattern));
            }

            if (!string.IsNullOrEmpty(request.Mold))
            {
                var moldPattern = $"%{LikePattern.EscapeLikePattern(request.Mold)}%";
                query = query.Where(x => EF.Functions.ILike(x.ProductionPlan.Mold, moldPattern));
            }

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                var productNumberPattern = $"%{LikePattern.EscapeLikePattern(request.ProductNumber)}%";
                query = query.Where(x => EF.Functions.ILike(x.ProductionPlan.ProductNumber, productNumberPattern));
            }

            if (!string.IsNullOrEmpty(request.Text))
            {
                var searchPattern = $"%{LikePattern.EscapeLikePattern(request.Text)}%";
                query = query.Where(x => EF.Functions.ILike(x.ProductionPlan.Wo, searchPattern)
                                    || EF.Functions.ILike(x.ProductionPlan.WoText, searchPattern)
                                    || EF.Functions.ILike(x.ProductionPlan.Mold, searchPattern)
                                    || EF.Functions.ILike(x.ProductionPlan.ProductNumber, searchPattern)
                                    || EF.Functions.ILike(x.ProductionPlan.CustomerNumber, searchPattern));
            }

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

        #region --- gold loss monthly report ---
        public async Task<jewelry.Model.Production.Plan.GoldLossMonthlyReport.SearchResponse> GetGoldLossMonthlyReport(jewelry.Model.Production.Plan.GoldLossMonthlyReport.SearchRequest request)
        {
            // 1. คำนวณช่วงวันที่ของเดือนที่เลือก
            var startDate = new DateTimeOffset(new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc));
            var endDate = startDate.AddMonths(1).AddSeconds(-1);

            // 2. Aggregate live data: ดึง status detail ของแผนกที่เลือก group by gold type
            var liveData = await (from detail in _jewelryContext.TbtProductionPlanStatusDetail
                                  join header in _jewelryContext.TbtProductionPlanStatusHeader
                                      on detail.HeaderId equals header.Id
                                  where header.Status == request.Status
                                      && header.IsActive == true
                                      && detail.IsActive == true
                                      && header.CreateDate >= startDate
                                      && header.CreateDate <= endDate
                                      && !string.IsNullOrEmpty(detail.Gold)
                                  group detail by detail.Gold into g
                                  select new
                                  {
                                      GoldType = g.Key,
                                      SumGoldWeightSend = g.Sum(x => x.GoldWeightSend ?? 0),
                                      SumGoldWeightCheck = g.Sum(x => x.GoldWeightCheck ?? 0),
                                  }).ToListAsync();

            // 3. ดึง saved data ของเดือนที่เลือก
            var savedData = await _jewelryContext.TbtGoldLossMonthlyReport
                .Where(x => x.Year == request.Year
                    && x.Month == request.Month
                    && x.Status == request.Status
                    && x.IsActive == true)
                .ToListAsync();

            var hasSavedData = savedData.Any();

            // 4. ถ้ายังไม่เคยบันทึก → ดึง default จากเดือนก่อนหน้า
            var previousDefaults = new List<Jewelry.Data.Models.Jewelry.TbtGoldLossMonthlyReport>();
            if (!hasSavedData)
            {
                var prevMonth = request.Month == 1 ? 12 : request.Month - 1;
                var prevYear = request.Month == 1 ? request.Year - 1 : request.Year;

                previousDefaults = await _jewelryContext.TbtGoldLossMonthlyReport
                    .Where(x => x.Year == prevYear
                        && x.Month == prevMonth
                        && x.Status == request.Status
                        && x.IsActive == true)
                    .ToListAsync();
            }

            // 5. ดึง master gold เพื่อ map ชื่อทอง
            var goldMaster = await _jewelryContext.TbmGold
                .Where(x => x.IsActive)
                .ToListAsync();

            // 6. สร้าง rows
            var rows = liveData.Select(item =>
            {
                var saved = savedData.FirstOrDefault(s => s.GoldType == item.GoldType);
                var prevDefault = previousDefaults.FirstOrDefault(p => p.GoldType == item.GoldType);
                var master = goldMaster.FirstOrDefault(m => m.Code == item.GoldType);

                var lossPercent = saved?.LossPercent ?? prevDefault?.LossPercent ?? 0m;
                var goldLossPrice = saved?.GoldLossPrice ?? prevDefault?.GoldLossPrice ?? 0m;
                var lossRemark = saved?.LossRemark ?? prevDefault?.LossRemark;

                var sumSend = item.SumGoldWeightSend;
                var sumCheck = item.SumGoldWeightCheck;
                var rawLoss = sumSend - sumCheck;
                var weightLossAllowed = sumCheck * (lossPercent / 100);
                var weightLossActual = Math.Round(weightLossAllowed - rawLoss, 4);
                var moneyDiff = Math.Round(weightLossActual, 2) * goldLossPrice;

                return new jewelry.Model.Production.Plan.GoldLossMonthlyReport.GoldLossMonthlyRow
                {
                    GoldType = item.GoldType ?? string.Empty,
                    GoldTypeName = master?.NameTh ?? item.GoldType ?? string.Empty,
                    SumGoldWeightSend = sumSend,
                    SumGoldWeightCheck = sumCheck,
                    RawLoss = rawLoss,
                    LossPercent = lossPercent,
                    GoldLossPrice = goldLossPrice,
                    WeightLossAllowed = weightLossAllowed,
                    WeightLossActual = weightLossActual,
                    MoneyDiff = moneyDiff,
                    LossRemark = lossRemark
                };
            }).OrderBy(x => x.GoldType).ToList();

            return new jewelry.Model.Production.Plan.GoldLossMonthlyReport.SearchResponse
            {
                Year = request.Year,
                Month = request.Month,
                Status = request.Status,
                HasSavedData = hasSavedData,
                TotalMoneyDiff = rows.Sum(r => r.MoneyDiff),
                Rows = rows
            };
        }

        public async Task<string> SaveGoldLossMonthlyReport(jewelry.Model.Production.Plan.GoldLossMonthlyReport.SaveRequest request)
        {
            // 1. คำนวณช่วงวันที่ของเดือนที่เลือก
            var startDate = new DateTimeOffset(new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc));
            var endDate = startDate.AddMonths(1).AddSeconds(-1);

            // 2. Re-aggregate live data
            var liveData = await (from detail in _jewelryContext.TbtProductionPlanStatusDetail
                                  join header in _jewelryContext.TbtProductionPlanStatusHeader
                                      on detail.HeaderId equals header.Id
                                  where header.Status == request.Status
                                      && header.IsActive == true
                                      && detail.IsActive == true
                                      && header.CreateDate >= startDate
                                      && header.CreateDate <= endDate
                                      && !string.IsNullOrEmpty(detail.Gold)
                                  group detail by detail.Gold into g
                                  select new
                                  {
                                      GoldType = g.Key,
                                      SumGoldWeightSend = g.Sum(x => x.GoldWeightSend ?? 0),
                                      SumGoldWeightCheck = g.Sum(x => x.GoldWeightCheck ?? 0),
                                  }).ToListAsync();

            // 3. ดึง existing saved records
            var existingRecords = await _jewelryContext.TbtGoldLossMonthlyReport
                .Where(x => x.Year == request.Year
                    && x.Month == request.Month
                    && x.Status == request.Status
                    && x.IsActive == true)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var currentUser = CurrentUsername;
            var newRecords = new List<Jewelry.Data.Models.Jewelry.TbtGoldLossMonthlyReport>();
            var updatedRecords = new List<Jewelry.Data.Models.Jewelry.TbtGoldLossMonthlyReport>();

            foreach (var item in request.Items)
            {
                var live = liveData.FirstOrDefault(l => l.GoldType == item.GoldType);
                var sumSend = live?.SumGoldWeightSend ?? 0;
                var sumCheck = live?.SumGoldWeightCheck ?? 0;
                var lossPercent = item.LossPercent ?? 0;
                var goldLossPrice = item.GoldLossPrice ?? 0;

                var rawLoss = sumSend - sumCheck;
                var weightLossAllowed = sumCheck * (lossPercent / 100);
                var weightLossActual = Math.Round(weightLossAllowed - rawLoss, 4);
                var moneyDiff = Math.Round(weightLossActual, 2) * goldLossPrice;

                var existing = existingRecords.FirstOrDefault(e => e.GoldType == item.GoldType);

                if (existing != null)
                {
                    existing.SumGoldWeightSend = sumSend;
                    existing.SumGoldWeightCheck = sumCheck;
                    existing.LossPercent = lossPercent;
                    existing.GoldLossPrice = goldLossPrice;
                    existing.RawLoss = rawLoss;
                    existing.WeightLossAllowed = weightLossAllowed;
                    existing.WeightLossActual = weightLossActual;
                    existing.MoneyDiff = moneyDiff;
                    existing.LossRemark = item.LossRemark;
                    existing.UpdateDate = now;
                    existing.UpdateBy = currentUser;
                    updatedRecords.Add(existing);
                }
                else
                {
                    newRecords.Add(new Jewelry.Data.Models.Jewelry.TbtGoldLossMonthlyReport
                    {
                        Year = request.Year,
                        Month = request.Month,
                        GoldType = item.GoldType,
                        Status = request.Status,
                        SumGoldWeightSend = sumSend,
                        SumGoldWeightCheck = sumCheck,
                        LossPercent = lossPercent,
                        GoldLossPrice = goldLossPrice,
                        RawLoss = rawLoss,
                        WeightLossAllowed = weightLossAllowed,
                        WeightLossActual = weightLossActual,
                        MoneyDiff = moneyDiff,
                        LossRemark = item.LossRemark,
                        IsActive = true,
                        CreateDate = now,
                        CreateBy = currentUser
                    });
                }
            }

            if (updatedRecords.Any())
            {
                _jewelryContext.TbtGoldLossMonthlyReport.UpdateRange(updatedRecords);
            }
            if (newRecords.Any())
            {
                await _jewelryContext.TbtGoldLossMonthlyReport.AddRangeAsync(newRecords);
            }

            await _jewelryContext.SaveChangesAsync();
            return "success";
        }
        #endregion

        #region --- gold loss by stage report ---
        public async Task<jewelry.Model.Production.Plan.GoldLossByStageReport.SearchResponse> GetGoldLossByStageReport(jewelry.Model.Production.Plan.GoldLossByStageReport.SearchRequest request)
        {
            var startDate = new DateTimeOffset(new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc));
            var endDate = startDate.AddMonths(1).AddSeconds(-1);

            var liveData = await (from detail in _jewelryContext.TbtProductionPlanStatusDetail
                                  join header in _jewelryContext.TbtProductionPlanStatusHeader
                                      on detail.HeaderId equals header.Id
                                  where header.IsActive == true
                                      && detail.IsActive == true
                                      && header.CreateDate >= startDate
                                      && header.CreateDate <= endDate
                                  group new { detail, header } by header.Status into g
                                  select new
                                  {
                                      StatusCode = g.Key,
                                      SumGoldWeightSend = g.Sum(x => x.detail.GoldWeightSend ?? 0),
                                      SumGoldWeightCheck = g.Sum(x => x.detail.GoldWeightCheck ?? 0),
                                      JobCount = g.Select(x => x.header.Id).Distinct().Count()
                                  }).ToListAsync();

            var statusMaster = await _jewelryContext.TbmProductionPlanStatus.ToListAsync();

            var rows = liveData.Select(item =>
            {
                var master = statusMaster.FirstOrDefault(m => m.Id == item.StatusCode);
                var rawLoss = item.SumGoldWeightSend - item.SumGoldWeightCheck;
                var rawLossPercent = item.SumGoldWeightSend > 0
                    ? Math.Round(rawLoss / item.SumGoldWeightSend * 100, 2)
                    : 0;

                return new jewelry.Model.Production.Plan.GoldLossByStageReport.GoldLossByStageRow
                {
                    StatusCode = item.StatusCode,
                    StatusName = master?.NameTh ?? string.Empty,
                    SumGoldWeightSend = item.SumGoldWeightSend,
                    SumGoldWeightCheck = item.SumGoldWeightCheck,
                    RawLoss = rawLoss,
                    RawLossPercent = rawLossPercent,
                    JobCount = item.JobCount
                };
            }).OrderBy(x => x.StatusCode).ToList();

            var totalSend = rows.Sum(x => x.SumGoldWeightSend);
            var totalCheck = rows.Sum(x => x.SumGoldWeightCheck);
            var totalRawLoss = totalSend - totalCheck;

            var total = new jewelry.Model.Production.Plan.GoldLossByStageReport.TotalRow
            {
                SumGoldWeightSend = totalSend,
                SumGoldWeightCheck = totalCheck,
                RawLoss = totalRawLoss,
                RawLossPercent = totalSend > 0 ? Math.Round(totalRawLoss / totalSend * 100, 2) : 0,
                JobCount = rows.Sum(x => x.JobCount)
            };

            return new jewelry.Model.Production.Plan.GoldLossByStageReport.SearchResponse
            {
                Year = request.Year,
                Month = request.Month,
                Rows = rows,
                Total = total
            };
        }
        #endregion

        #region --- gold loss by worker report ---
        public async Task<jewelry.Model.Production.Plan.GoldLossByWorkerReport.Response> GetGoldLossByWorkerReport(jewelry.Model.Production.Plan.GoldLossByWorkerReport.Criteria request)
        {
            var utcNow = DateTime.UtcNow;

            var start = request.Start ?? new DateTimeOffset(utcNow.AddMonths(-12), TimeSpan.Zero);
            var end = request.End ?? new DateTimeOffset(utcNow, TimeSpan.Zero);

            var statuses = (request.Status != null && request.Status.Length > 0)
                ? request.Status
                : new[]
                {
                    jewelry.Model.Constant.ProductionPlanStatus.Casting,
                    jewelry.Model.Constant.ProductionPlanStatus.Scrubb,
                    jewelry.Model.Constant.ProductionPlanStatus.Gems,
                    jewelry.Model.Constant.ProductionPlanStatus.Embedd,
                    jewelry.Model.Constant.ProductionPlanStatus.Plated
                };

            var minJobCount = request.MinJobCount ?? 10;

            var baseQuery = from detail in _jewelryContext.TbtProductionPlanStatusDetail
                            join header in _jewelryContext.TbtProductionPlanStatusHeader
                                on detail.HeaderId equals header.Id
                            where header.IsActive == true
                                && detail.IsActive == true
                                && detail.GoldWeightSend > 0
                                && statuses.Contains(header.Status)
                                && header.CreateDate >= start.StartOfDayUtc()
                                && header.CreateDate <= end.EndOfDayUtc()
                            select new
                            {
                                header.Id,
                                header.Status,
                                header.CreateDate,
                                detail.Worker,
                                detail.Gold,
                                GoldWeightSend = detail.GoldWeightSend ?? 0,
                                GoldWeightCheck = detail.GoldWeightCheck ?? 0,
                                IsReturned = detail.GoldWeightCheck.HasValue
                            };

            if (request.Gold != null && request.Gold.Length > 0)
            {
                baseQuery = baseQuery.Where(x => x.Gold != null && request.Gold.Contains(x.Gold));
            }

            var fullRows = await baseQuery.ToListAsync();

            var missingWorkerRows = fullRows.Where(x => string.IsNullOrWhiteSpace(x.Worker)).ToList();
            var knownRowsAll = fullRows.Where(x => !string.IsNullOrWhiteSpace(x.Worker)).ToList();

            // a row with GoldWeightCheck == NULL is work still with the worker (not yet returned) —
            // only rows where the work has actually been returned feed the loss aggregation, otherwise
            // in-progress gold is counted as 100% lost.
            var knownRowsReturned = knownRowsAll.Where(x => x.IsReturned).ToList();

            var knownRowsFiltered = knownRowsReturned;
            if (!string.IsNullOrWhiteSpace(request.WorkerCode))
            {
                knownRowsFiltered = knownRowsReturned
                    .Where(x => string.Equals(x.Worker!.Trim(), request.WorkerCode!.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var statusMaster = await _jewelryContext.TbmProductionPlanStatus.ToListAsync();
            var workerMaster = await _jewelryContext.TbmWorker.ToListAsync();

            string GetStatusName(int statusCode)
            {
                var master = statusMaster.FirstOrDefault(m => m.Id == statusCode);
                return master?.NameTh ?? statusCode.ToString();
            }

            string GetWorkerName(string workerCode)
            {
                var master = workerMaster.FirstOrDefault(w => string.Equals(w.Code, workerCode, StringComparison.OrdinalIgnoreCase));
                return master?.NameTh ?? workerCode;
            }

            // department (stage) level job volume — full department data set (worker known or not),
            // independent of the WorkerCode drill-down filter.
            var deptJobCounts = fullRows
                .GroupBy(x => x.Status)
                .Select(g => new
                {
                    StatusCode = g.Key,
                    JobCount = g.Select(x => x.Id).Distinct().Count()
                })
                .ToList();

            // department (stage) level loss average — worker-attributed, returned-only rows, so
            // unattributed volume (tracked separately via RowsMissingWorker*) and work still in
            // progress (tracked separately via RowsNotReturned*) never inflate the benchmark a
            // worker is compared against. Independent of the WorkerCode drill-down filter, so
            // drilling into one worker still compares them against their whole department.
            var deptWorkerAttributed = knownRowsReturned
                .GroupBy(x => x.Status)
                .Select(g =>
                {
                    var sumSend = Math.Round(g.Sum(x => x.GoldWeightSend), 4);
                    var sumCheck = Math.Round(g.Sum(x => x.GoldWeightCheck), 4);
                    var rawLoss = sumSend - sumCheck;
                    return new
                    {
                        StatusCode = g.Key,
                        SumGoldWeightSend = sumSend,
                        SumGoldWeightCheck = sumCheck,
                        RawLoss = rawLoss,
                        LossPercent = sumSend > 0 ? Math.Round(rawLoss / sumSend * 100, 2) : 0m,
                        WorkerCount = g.Select(x => x.Worker!.Trim().ToUpperInvariant()).Distinct().Count()
                    };
                })
                .ToList();

            var stageAggregates = deptJobCounts
                .Select(d =>
                {
                    var wa = deptWorkerAttributed.FirstOrDefault(w => w.StatusCode == d.StatusCode);
                    return new
                    {
                        StatusCode = d.StatusCode,
                        SumGoldWeightSend = wa?.SumGoldWeightSend ?? 0m,
                        SumGoldWeightCheck = wa?.SumGoldWeightCheck ?? 0m,
                        RawLoss = wa?.RawLoss ?? 0m,
                        LossPercent = wa?.LossPercent ?? 0m,
                        JobCount = d.JobCount,
                        WorkerCount = wa?.WorkerCount ?? 0
                    };
                })
                .ToList();

            // per worker x department rows
            var workerStageAggregates = knownRowsFiltered
                .GroupBy(x => new { Worker = x.Worker!.Trim(), x.Status })
                .Select(g =>
                {
                    var sumSend = Math.Round(g.Sum(x => x.GoldWeightSend), 4);
                    var sumCheck = Math.Round(g.Sum(x => x.GoldWeightCheck), 4);
                    var rawLoss = sumSend - sumCheck;
                    return new
                    {
                        g.Key.Worker,
                        g.Key.Status,
                        SumGoldWeightSend = sumSend,
                        SumGoldWeightCheck = sumCheck,
                        RawLoss = rawLoss,
                        LossPercent = sumSend > 0 ? Math.Round(rawLoss / sumSend * 100, 2) : 0m,
                        JobCount = g.Select(x => x.Id).Distinct().Count()
                    };
                })
                .ToList();

            var rows = workerStageAggregates.Select(item =>
            {
                var stage = stageAggregates.FirstOrDefault(s => s.StatusCode == item.Status);
                var stageAvg = stage?.LossPercent ?? 0m;
                var isBelowMinJobs = item.JobCount < minJobCount;

                return new jewelry.Model.Production.Plan.GoldLossByWorkerReport.WorkerStageRow
                {
                    WorkerCode = item.Worker,
                    WorkerName = GetWorkerName(item.Worker),
                    StatusCode = item.Status,
                    StatusName = GetStatusName(item.Status),
                    JobCount = item.JobCount,
                    SumGoldWeightSend = item.SumGoldWeightSend,
                    SumGoldWeightCheck = item.SumGoldWeightCheck,
                    RawLoss = item.RawLoss,
                    LossPercent = item.LossPercent,
                    StageAvgLossPercent = stageAvg,
                    DiffFromStageAvgPercent = Math.Round(item.LossPercent - stageAvg, 2),
                    IsBelowMinJobs = isBelowMinJobs,
                    RankInStage = 0
                };
            }).ToList();

            foreach (var stageGroup in rows.GroupBy(x => x.StatusCode))
            {
                var ranked = stageGroup
                    .Where(x => !x.IsBelowMinJobs)
                    .OrderByDescending(x => x.LossPercent)
                    .ToList();

                for (var i = 0; i < ranked.Count; i++)
                {
                    ranked[i].RankInStage = i + 1;
                }
            }

            rows = rows.OrderBy(x => x.StatusCode).ThenByDescending(x => x.LossPercent).ToList();

            // monthly (Thai local time) worker x department aggregates
            var monthlyAggregates = knownRowsFiltered
                .GroupBy(x => new
                {
                    Year = x.CreateDate.AddHours(7).Year,
                    Month = x.CreateDate.AddHours(7).Month,
                    Worker = x.Worker!.Trim(),
                    x.Status
                })
                .Select(g =>
                {
                    var sumSend = Math.Round(g.Sum(x => x.GoldWeightSend), 4);
                    var sumCheck = Math.Round(g.Sum(x => x.GoldWeightCheck), 4);
                    var rawLoss = sumSend - sumCheck;
                    return new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        g.Key.Worker,
                        g.Key.Status,
                        SumGoldWeightSend = sumSend,
                        SumGoldWeightCheck = sumCheck,
                        RawLoss = rawLoss,
                        LossPercent = sumSend > 0 ? Math.Round(rawLoss / sumSend * 100, 2) : 0m,
                        JobCount = g.Select(x => x.Id).Distinct().Count()
                    };
                })
                .ToList();

            var monthlyRows = monthlyAggregates
                .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Status).ThenByDescending(x => x.LossPercent)
                .Select(item => new jewelry.Model.Production.Plan.GoldLossByWorkerReport.MonthlyRow
                {
                    Year = item.Year,
                    Month = item.Month,
                    WorkerCode = item.Worker,
                    StatusCode = item.Status,
                    LossPercent = item.LossPercent,
                    JobCount = item.JobCount,
                    SumGoldWeightSend = item.SumGoldWeightSend,
                    SumGoldWeightCheck = item.SumGoldWeightCheck,
                    RawLoss = item.RawLoss
                }).ToList();

            var monthlyTop = monthlyAggregates
                .Where(x => x.JobCount >= minJobCount)
                .GroupBy(x => new { x.Year, x.Month, x.Status })
                .Select(g => g.OrderByDescending(x => x.LossPercent).First())
                .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Status)
                .Select(item => new jewelry.Model.Production.Plan.GoldLossByWorkerReport.MonthlyTopRow
                {
                    Year = item.Year,
                    Month = item.Month,
                    StatusCode = item.Status,
                    StatusName = GetStatusName(item.Status),
                    WorkerCode = item.Worker,
                    WorkerName = GetWorkerName(item.Worker),
                    LossPercent = item.LossPercent,
                    JobCount = item.JobCount
                }).ToList();

            var totalQualifyingCount = fullRows.Count;
            var missingWorkerCount = missingWorkerRows.Count;
            var notReturnedCount = fullRows.Count(x => !x.IsReturned);

            var summary = new jewelry.Model.Production.Plan.GoldLossByWorkerReport.SummaryRow
            {
                PeriodStart = start,
                PeriodEnd = end,
                WorkerCount = knownRowsAll.Select(x => x.Worker!.Trim().ToUpperInvariant()).Distinct().Count(),
                JobCount = fullRows.Select(x => x.Id).Distinct().Count(),
                RowsMissingWorkerCount = missingWorkerCount,
                RowsMissingWorkerPercent = totalQualifyingCount > 0
                    ? Math.Round((decimal)missingWorkerCount / totalQualifyingCount * 100, 2)
                    : 0m,
                RowsNotReturnedCount = notReturnedCount,
                RowsNotReturnedPercent = totalQualifyingCount > 0
                    ? Math.Round((decimal)notReturnedCount / totalQualifyingCount * 100, 2)
                    : 0m,
                StageSummaries = stageAggregates
                    .OrderBy(x => x.StatusCode)
                    .Select(x => new jewelry.Model.Production.Plan.GoldLossByWorkerReport.StageSummaryRow
                    {
                        StatusCode = x.StatusCode,
                        StatusName = GetStatusName(x.StatusCode),
                        AvgLossPercent = x.LossPercent,
                        JobCount = x.JobCount,
                        WorkerCount = x.WorkerCount
                    }).ToList()
            };

            return new jewelry.Model.Production.Plan.GoldLossByWorkerReport.Response
            {
                Rows = rows,
                MonthlyTop = monthlyTop,
                MonthlyRows = monthlyRows,
                Summary = summary
            };
        }
        #endregion

        #region --- lead time report ---
        public async Task<jewelry.Model.Production.Plan.LeadTimeReport.SearchResponse> GetLeadTimeReport(jewelry.Model.Production.Plan.LeadTimeReport.SearchRequest request)
        {
            var query = _jewelryContext.TbtProductionPlan
                .Where(x => x.IsActive == true
                    && x.Status == jewelry.Model.Constant.ProductionPlanStatus.Completed
                    && x.CompletedDate != null);

            if (request.CompletedStart.HasValue)
            {
                var start = request.CompletedStart.Value.StartOfDayUtc();
                query = query.Where(x => x.CompletedDate >= start);
            }
            if (request.CompletedEnd.HasValue)
            {
                var end = request.CompletedEnd.Value.EndOfDayUtc();
                query = query.Where(x => x.CompletedDate <= end);
            }
            if (request.ProductType != null && request.ProductType.Any())
            {
                query = query.Where(x => request.ProductType.Contains(x.ProductType));
            }
            if (request.CustomerType != null && request.CustomerType.Any())
            {
                query = query.Where(x => request.CustomerType.Contains(x.CustomerType));
            }

            var data = await query
                .Select(x => new
                {
                    x.ProductType,
                    x.CustomerType,
                    x.RequestDate,
                    x.CompletedDate
                })
                .ToListAsync();

            var groupByCustomerType = string.Equals(request.GroupBy, "customerType", StringComparison.OrdinalIgnoreCase);

            var invalidCount = data.Count(x => (x.CompletedDate!.Value - x.RequestDate).TotalDays < 0);
            var validData = data
                .Select(x => new
                {
                    x.ProductType,
                    x.CustomerType,
                    LeadDays = (x.CompletedDate!.Value - x.RequestDate).TotalDays
                })
                .Where(x => x.LeadDays >= 0)
                .ToList();

            var groups = groupByCustomerType
                ? validData.GroupBy(x => x.CustomerType)
                : validData.GroupBy(x => x.ProductType);

            var productTypeMaster = await _jewelryContext.TbmProductType.ToListAsync();
            var customerTypeMaster = await _jewelryContext.TbmCustomerType.ToListAsync();

            var rows = groups.Select(g =>
            {
                var leadDaysList = g.Select(x => x.LeadDays).ToList();
                var groupCode = g.Key ?? string.Empty;
                var groupName = groupByCustomerType
                    ? customerTypeMaster.FirstOrDefault(m => m.Code == groupCode)?.NameTh ?? groupCode
                    : productTypeMaster.FirstOrDefault(m => m.Code == groupCode)?.NameTh ?? groupCode;

                return new jewelry.Model.Production.Plan.LeadTimeReport.LeadTimeRow
                {
                    GroupCode = groupCode,
                    GroupName = groupName,
                    Count = leadDaysList.Count,
                    AvgDays = Math.Round((decimal)leadDaysList.Average(), 1),
                    MedianDays = Math.Round((decimal)GetMedian(leadDaysList), 1),
                    B0_30 = leadDaysList.Count(d => d <= 30),
                    B31_90 = leadDaysList.Count(d => d > 30 && d <= 90),
                    B91_180 = leadDaysList.Count(d => d > 90 && d <= 180),
                    B181_365 = leadDaysList.Count(d => d > 180 && d <= 365),
                    BGt365 = leadDaysList.Count(d => d > 365)
                };
            }).OrderByDescending(x => x.Count).ToList();

            var allLeadDays = validData.Select(x => x.LeadDays).ToList();
            var summary = new jewelry.Model.Production.Plan.LeadTimeReport.LeadTimeSummary
            {
                TotalCount = allLeadDays.Count,
                AvgDays = allLeadDays.Any() ? Math.Round((decimal)allLeadDays.Average(), 1) : 0,
                MedianDays = allLeadDays.Any() ? Math.Round((decimal)GetMedian(allLeadDays), 1) : 0,
                InvalidCount = invalidCount
            };

            return new jewelry.Model.Production.Plan.LeadTimeReport.SearchResponse
            {
                GroupBy = groupByCustomerType ? "customerType" : "productType",
                Rows = rows,
                Summary = summary
            };
        }

        private static double GetMedian(List<double> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            var sorted = values.OrderBy(x => x).ToList();
            var count = sorted.Count;
            var mid = count / 2;

            if (count % 2 == 0)
            {
                return (sorted[mid - 1] + sorted[mid]) / 2.0;
            }

            return sorted[mid];
        }
        #endregion

        #region --- capacity report ---

        private static readonly string[] ThaiMonthAbbr = new[]
        {
            "", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.",
            "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค."
        };

        public async Task<jewelry.Model.Production.Plan.CapacityReport.Response> GetCapacityReport(jewelry.Model.Production.Plan.CapacityReport.Criteria request)
        {
            var thaiOffset = TimeSpan.FromHours(7);
            var bucket = string.Equals(request.Bucket, "week", StringComparison.OrdinalIgnoreCase) ? "week" : "month";

            var validGroupBys = new[] { "gold", "goldSize", "productType", "customerType" };
            var groupBy = !string.IsNullOrEmpty(request.GroupBy)
                && validGroupBys.Any(x => string.Equals(x, request.GroupBy, StringComparison.OrdinalIgnoreCase))
                ? validGroupBys.First(x => string.Equals(x, request.GroupBy, StringComparison.OrdinalIgnoreCase))
                : "none";

            var thaiNow = DateTimeOffset.UtcNow.ToOffset(thaiOffset);
            var defaultEnd = new DateTimeOffset(thaiNow.Date, thaiOffset);
            var defaultStartDate = new DateTime(thaiNow.Year, thaiNow.Month, 1).AddMonths(-11);
            var defaultStart = new DateTimeOffset(defaultStartDate, thaiOffset);

            var start = request.Start ?? defaultStart;
            var end = request.End ?? defaultEnd;

            var startUtc = start.StartOfDayUtc();
            var endUtc = end.EndOfDayUtc();

            var query = _jewelryContext.TbtProductionPlan
                .Where(x => x.IsActive == true
                       && x.Status == ProductionPlanStatus.Completed
                       && x.CompletedDate.HasValue
                       && x.CompletedDate >= startUtc
                       && x.CompletedDate <= endUtc);

            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => request.Gold.Contains(x.Type));
            }

            if (request.GoldSize != null && request.GoldSize.Any())
            {
                query = query.Where(x => request.GoldSize.Contains(x.TypeSize));
            }

            if (request.ProductType != null && request.ProductType.Any())
            {
                query = query.Where(x => request.ProductType.Contains(x.ProductType));
            }

            if (request.CustomerType != null && request.CustomerType.Any())
            {
                query = query.Where(x => request.CustomerType.Contains(x.CustomerType));
            }

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                var customerCodePattern = $"%{LikePattern.EscapeLikePattern(request.CustomerCode)}%";
                query = query.Where(x => EF.Functions.ILike(x.CustomerNumber, customerCodePattern));
            }

            if (!string.IsNullOrEmpty(request.Mold))
            {
                var moldPattern = $"%{LikePattern.EscapeLikePattern(request.Mold)}%";
                query = query.Where(x => EF.Functions.ILike(x.Mold, moldPattern));
            }

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                var productNumberPattern = $"%{LikePattern.EscapeLikePattern(request.ProductNumber)}%";
                query = query.Where(x => EF.Functions.ILike(x.ProductNumber, productNumberPattern));
            }

            if (!string.IsNullOrEmpty(request.Text))
            {
                var searchPattern = $"%{LikePattern.EscapeLikePattern(request.Text)}%";
                query = query.Where(x => EF.Functions.ILike(x.Wo, searchPattern)
                                    || EF.Functions.ILike(x.WoText, searchPattern)
                                    || EF.Functions.ILike(x.Mold, searchPattern)
                                    || EF.Functions.ILike(x.ProductNumber, searchPattern)
                                    || EF.Functions.ILike(x.CustomerNumber, searchPattern));
            }

            var data = await query
                .Select(x => new
                {
                    x.CompletedDate,
                    x.ProductQty,
                    x.Type,
                    x.TypeSize,
                    x.ProductType,
                    x.CustomerType
                })
                .ToListAsync();

            var todayThaiDate = DateTimeOffset.UtcNow.ToOffset(thaiOffset).Date;

            var thaiStartDate = startUtc.UtcDateTime.AddHours(7).Date;
            var thaiEndDateRaw = endUtc.UtcDateTime.AddHours(7).Date;
            var thaiEndDate = thaiEndDateRaw > todayThaiDate ? todayThaiDate : thaiEndDateRaw;

            var anchors = BuildCapacityBucketAnchors(thaiStartDate, thaiEndDate, bucket, thaiOffset, todayThaiDate);

            var rowsWithAnchor = data.Select(x =>
            {
                var thaiDate = x.CompletedDate!.Value.AddHours(7).Date;
                var anchorDate = bucket == "week" ? GetIsoMonday(thaiDate) : new DateTime(thaiDate.Year, thaiDate.Month, 1);
                string? raw = groupBy switch
                {
                    "gold" => x.Type,
                    "goldSize" => x.TypeSize,
                    "productType" => x.ProductType,
                    "customerType" => x.CustomerType,
                    _ => null
                };

                return new
                {
                    AnchorDate = anchorDate,
                    x.ProductQty,
                    GroupCode = string.IsNullOrEmpty(raw) ? string.Empty : raw
                };
            }).ToList();

            var buckets = anchors.Select(a =>
            {
                var rowsInBucket = rowsWithAnchor.Where(x => x.AnchorDate == a.AnchorDate).ToList();
                return new jewelry.Model.Production.Plan.CapacityReport.BucketPoint
                {
                    Key = a.Key,
                    Label = a.Label,
                    Start = a.Start,
                    End = a.End,
                    PlanCount = rowsInBucket.Count,
                    PieceCount = rowsInBucket.Sum(x => x.ProductQty),
                    IsPartial = a.IsPartial
                };
            }).ToList();

            var series = new List<jewelry.Model.Production.Plan.CapacityReport.GroupSeries>();

            if (groupBy != "none")
            {
                Func<string, string> resolveGroupName = code => code;

                if (groupBy == "gold")
                {
                    var master = await _jewelryContext.TbmGold.ToListAsync();
                    resolveGroupName = code => master.FirstOrDefault(m => m.Code == code)?.NameTh ?? code;
                }
                else if (groupBy == "goldSize")
                {
                    var master = await _jewelryContext.TbmGoldSize.ToListAsync();
                    resolveGroupName = code => master.FirstOrDefault(m => m.Code == code)?.NameTh ?? code;
                }
                else if (groupBy == "productType")
                {
                    var master = await _jewelryContext.TbmProductType.ToListAsync();
                    resolveGroupName = code => master.FirstOrDefault(m => m.Code == code)?.NameTh ?? code;
                }
                else if (groupBy == "customerType")
                {
                    var master = await _jewelryContext.TbmCustomerType.ToListAsync();
                    resolveGroupName = code => master.FirstOrDefault(m => m.Code == code)?.NameTh ?? code;
                }

                series = rowsWithAnchor
                    .GroupBy(x => x.GroupCode)
                    .Select(g =>
                    {
                        var groupCode = g.Key;
                        var groupName = string.IsNullOrEmpty(groupCode) ? "ไม่ระบุ" : resolveGroupName(groupCode);

                        var points = anchors.Select(a =>
                        {
                            var rowsInBucket = g.Where(x => x.AnchorDate == a.AnchorDate).ToList();
                            return new jewelry.Model.Production.Plan.CapacityReport.GroupPoint
                            {
                                BucketKey = a.Key,
                                PlanCount = rowsInBucket.Count,
                                PieceCount = rowsInBucket.Sum(x => x.ProductQty)
                            };
                        }).ToList();

                        return new jewelry.Model.Production.Plan.CapacityReport.GroupSeries
                        {
                            GroupCode = groupCode,
                            GroupName = groupName,
                            Points = points
                        };
                    })
                    .OrderByDescending(x => x.Points.Sum(p => p.PlanCount))
                    .ToList();
            }

            var totalPlans = data.Count;
            var totalPieces = data.Sum(x => x.ProductQty);
            var completeBuckets = buckets.Where(x => !x.IsPartial).ToList();
            var completeBucketCount = completeBuckets.Count;
            var bestBucket = completeBuckets.OrderByDescending(x => x.PlanCount).FirstOrDefault();

            var summary = new jewelry.Model.Production.Plan.CapacityReport.CapacitySummary
            {
                TotalPlans = totalPlans,
                TotalPieces = totalPieces,
                AvgPlansPerBucket = completeBucketCount > 0 ? Math.Round((decimal)completeBuckets.Sum(x => x.PlanCount) / completeBucketCount, 2) : 0,
                AvgPiecesPerBucket = completeBucketCount > 0 ? Math.Round((decimal)completeBuckets.Sum(x => x.PieceCount) / completeBucketCount, 2) : 0,
                BestBucketKey = bestBucket?.Key ?? string.Empty,
                BestBucketLabel = bestBucket?.Label ?? string.Empty,
                BestBucketPlans = bestBucket?.PlanCount ?? 0
            };

            return new jewelry.Model.Production.Plan.CapacityReport.Response
            {
                Bucket = bucket,
                GroupBy = groupBy,
                Buckets = buckets,
                Series = series,
                Summary = summary
            };
        }

        private class CapacityBucketAnchor
        {
            public DateTime AnchorDate { get; set; }
            public string Key { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public DateTimeOffset Start { get; set; }
            public DateTimeOffset End { get; set; }
            public bool IsPartial { get; set; }
        }

        private static List<CapacityBucketAnchor> BuildCapacityBucketAnchors(DateTime thaiStartDate, DateTime thaiEndDate, string bucket, TimeSpan thaiOffset, DateTime todayThaiDate)
        {
            var result = new List<CapacityBucketAnchor>();

            if (bucket == "week")
            {
                var mondayCursor = GetIsoMonday(thaiStartDate);
                var mondayEnd = GetIsoMonday(thaiEndDate);

                while (mondayCursor <= mondayEnd)
                {
                    var sunday = mondayCursor.AddDays(6);
                    var isoYear = ISOWeek.GetYear(mondayCursor);
                    var weekNum = ISOWeek.GetWeekOfYear(mondayCursor);
                    var isPartial = todayThaiDate >= mondayCursor && todayThaiDate <= sunday;

                    result.Add(new CapacityBucketAnchor
                    {
                        AnchorDate = mondayCursor,
                        Key = $"{isoYear}-W{weekNum:D2}",
                        Label = FormatWeekLabel(mondayCursor, sunday),
                        Start = new DateTimeOffset(mondayCursor, thaiOffset),
                        End = new DateTimeOffset(sunday.AddHours(23).AddMinutes(59).AddSeconds(59), thaiOffset),
                        IsPartial = isPartial
                    });

                    mondayCursor = mondayCursor.AddDays(7);
                }
            }
            else
            {
                var monthCursor = new DateTime(thaiStartDate.Year, thaiStartDate.Month, 1);
                var monthEnd = new DateTime(thaiEndDate.Year, thaiEndDate.Month, 1);

                while (monthCursor <= monthEnd)
                {
                    var monthLastDay = monthCursor.AddMonths(1).AddSeconds(-1);
                    var monthLastDate = monthCursor.AddMonths(1).AddDays(-1);
                    var isPartial = todayThaiDate >= monthCursor && todayThaiDate <= monthLastDate;

                    result.Add(new CapacityBucketAnchor
                    {
                        AnchorDate = monthCursor,
                        Key = monthCursor.ToString("yyyy-MM"),
                        Label = $"{ThaiMonthAbbr[monthCursor.Month]} {monthCursor.Year}",
                        Start = new DateTimeOffset(monthCursor, thaiOffset),
                        End = new DateTimeOffset(monthLastDay, thaiOffset),
                        IsPartial = isPartial
                    });

                    monthCursor = monthCursor.AddMonths(1);
                }
            }

            return result;
        }

        private static DateTime GetIsoMonday(DateTime date)
        {
            var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.AddDays(-diff);
        }

        private static string FormatWeekLabel(DateTime monday, DateTime sunday)
        {
            if (monday.Year == sunday.Year && monday.Month == sunday.Month)
            {
                return $"{monday.Day}-{sunday.Day} {ThaiMonthAbbr[monday.Month]} {monday.Year}";
            }

            if (monday.Year == sunday.Year)
            {
                return $"{monday.Day} {ThaiMonthAbbr[monday.Month]}-{sunday.Day} {ThaiMonthAbbr[sunday.Month]} {sunday.Year}";
            }

            return $"{monday.Day} {ThaiMonthAbbr[monday.Month]} {monday.Year}-{sunday.Day} {ThaiMonthAbbr[sunday.Month]} {sunday.Year}";
        }
        #endregion

        #region --- stage lead time report ---

        public async Task<jewelry.Model.Production.Plan.StageLeadTimeReport.Response> GetStageLeadTimeReport(jewelry.Model.Production.Plan.StageLeadTimeReport.Criteria request)
        {
            var validLeadTimeGroupBys = new[] { "productType", "customerType", "gold", "goldSize" };
            var groupBy = !string.IsNullOrEmpty(request.GroupBy)
                && validLeadTimeGroupBys.Any(x => string.Equals(x, request.GroupBy, StringComparison.OrdinalIgnoreCase))
                ? validLeadTimeGroupBys.First(x => string.Equals(x, request.GroupBy, StringComparison.OrdinalIgnoreCase))
                : "none";

            var planQuery = _jewelryContext.TbtProductionPlan.AsQueryable();

            if (request.Gold != null && request.Gold.Any())
            {
                planQuery = planQuery.Where(x => request.Gold.Contains(x.Type));
            }

            if (request.GoldSize != null && request.GoldSize.Any())
            {
                planQuery = planQuery.Where(x => request.GoldSize.Contains(x.TypeSize));
            }

            if (request.ProductType != null && request.ProductType.Any())
            {
                planQuery = planQuery.Where(x => request.ProductType.Contains(x.ProductType));
            }

            if (request.CustomerType != null && request.CustomerType.Any())
            {
                planQuery = planQuery.Where(x => request.CustomerType.Contains(x.CustomerType));
            }

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                var customerCodePattern = $"%{LikePattern.EscapeLikePattern(request.CustomerCode)}%";
                planQuery = planQuery.Where(x => EF.Functions.ILike(x.CustomerNumber, customerCodePattern));
            }

            if (!string.IsNullOrEmpty(request.Mold))
            {
                var moldPattern = $"%{LikePattern.EscapeLikePattern(request.Mold)}%";
                planQuery = planQuery.Where(x => EF.Functions.ILike(x.Mold, moldPattern));
            }

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                var productNumberPattern = $"%{LikePattern.EscapeLikePattern(request.ProductNumber)}%";
                planQuery = planQuery.Where(x => EF.Functions.ILike(x.ProductNumber, productNumberPattern));
            }

            if (!string.IsNullOrEmpty(request.Text))
            {
                var searchPattern = $"%{LikePattern.EscapeLikePattern(request.Text)}%";
                planQuery = planQuery.Where(x => EF.Functions.ILike(x.Wo, searchPattern)
                                    || EF.Functions.ILike(x.WoText, searchPattern)
                                    || EF.Functions.ILike(x.Mold, searchPattern)
                                    || EF.Functions.ILike(x.ProductNumber, searchPattern)
                                    || EF.Functions.ILike(x.CustomerNumber, searchPattern));
            }

            DateTimeOffset? completedStartFilter = request.CompletedStart;
            DateTimeOffset? completedEndFilter = request.CompletedEnd;
            if (!completedStartFilter.HasValue && !completedEndFilter.HasValue)
            {
                completedEndFilter = DateTimeOffset.UtcNow;
                completedStartFilter = completedEndFilter.Value.AddMonths(-12);
            }

            var completedQuery = planQuery
                .Where(x => x.IsActive == true
                    && x.Status == jewelry.Model.Constant.ProductionPlanStatus.Completed
                    && x.CompletedDate != null);

            if (completedStartFilter.HasValue)
            {
                var start = completedStartFilter.Value.StartOfDayUtc();
                completedQuery = completedQuery.Where(x => x.CompletedDate >= start);
            }
            if (completedEndFilter.HasValue)
            {
                var end = completedEndFilter.Value.EndOfDayUtc();
                completedQuery = completedQuery.Where(x => x.CompletedDate <= end);
            }

            var completedPlans = await completedQuery
                .Select(x => new
                {
                    x.Id,
                    x.CompletedDate,
                    x.ProductType,
                    x.CustomerType,
                    x.Type,
                    x.TypeSize
                })
                .ToListAsync();

            var completedPlanIds = completedPlans.Select(x => x.Id).ToList();

            var completedHeaders = await _jewelryContext.TbtProductionPlanStatusHeader
                .Where(h => h.IsActive == true && completedPlanIds.Contains(h.ProductionPlanId))
                .Select(h => new
                {
                    h.ProductionPlanId,
                    h.Status,
                    h.CreateDate,
                    h.UpdateDate
                })
                .ToListAsync();

            var headersByPlan = completedHeaders
                .GroupBy(h => h.ProductionPlanId)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.CreateDate).ToList());

            var stageDwells = new Dictionary<int, List<double>>();
            var stageWorkDays = new Dictionary<int, List<double>>();
            var stageReliableCount = new Dictionary<int, int>();

            var totalLeadDaysList = new List<double>();
            var plansWithNoStageCount = 0;
            var groupAccumulators = new Dictionary<string, LeadTimeGroupAccumulator>();

            foreach (var plan in completedPlans)
            {
                if (!headersByPlan.TryGetValue(plan.Id, out var headers) || headers.Count == 0)
                {
                    plansWithNoStageCount++;
                    continue;
                }

                var workingStageCount = headers.Count(h => h.Status != jewelry.Model.Constant.ProductionPlanStatus.Price
                    && h.Status != jewelry.Model.Constant.ProductionPlanStatus.Completed
                    && h.Status != jewelry.Model.Constant.ProductionPlanStatus.Melted);
                if (workingStageCount == 0)
                {
                    plansWithNoStageCount++;
                }

                var firstHeader = headers[0];
                var leadDays = Math.Max(0, (plan.CompletedDate!.Value - firstHeader.CreateDate).TotalDays);
                totalLeadDaysList.Add(leadDays);

                LeadTimeGroupAccumulator? groupAcc = null;
                if (groupBy != "none")
                {
                    var rawGroupCode = groupBy switch
                    {
                        "gold" => plan.Type,
                        "goldSize" => plan.TypeSize,
                        "productType" => plan.ProductType,
                        "customerType" => plan.CustomerType,
                        _ => null
                    };
                    var groupCode = string.IsNullOrEmpty(rawGroupCode) ? string.Empty : rawGroupCode;

                    if (!groupAccumulators.TryGetValue(groupCode, out groupAcc))
                    {
                        groupAcc = new LeadTimeGroupAccumulator();
                        groupAccumulators[groupCode] = groupAcc;
                    }
                    groupAcc.TotalLeadDays.Add(leadDays);
                }

                for (var i = 0; i < headers.Count; i++)
                {
                    var current = headers[i];
                    var stageExit = i + 1 < headers.Count ? headers[i + 1].CreateDate : plan.CompletedDate!.Value;
                    var dwellDays = Math.Max(0, (stageExit - current.CreateDate).TotalDays);

                    if (current.Status == jewelry.Model.Constant.ProductionPlanStatus.Melted
                        || current.Status == jewelry.Model.Constant.ProductionPlanStatus.Completed)
                    {
                        continue;
                    }

                    if (!stageDwells.TryGetValue(current.Status, out var dwellList))
                    {
                        dwellList = new List<double>();
                        stageDwells[current.Status] = dwellList;
                        stageWorkDays[current.Status] = new List<double>();
                        stageReliableCount[current.Status] = 0;
                    }
                    dwellList.Add(dwellDays);

                    if (groupAcc != null)
                    {
                        if (!groupAcc.StageDwells.TryGetValue(current.Status, out var groupDwellList))
                        {
                            groupDwellList = new List<double>();
                            groupAcc.StageDwells[current.Status] = groupDwellList;
                        }
                        groupDwellList.Add(dwellDays);
                    }

                    if (current.UpdateDate.HasValue && current.UpdateDate.Value > current.CreateDate)
                    {
                        stageWorkDays[current.Status].Add((current.UpdateDate.Value - current.CreateDate).TotalDays);

                        if (current.UpdateDate.Value <= stageExit)
                        {
                            stageReliableCount[current.Status]++;
                        }
                    }
                }
            }

            var statusMaster = await _jewelryContext.TbmProductionPlanStatus.ToListAsync();
            var totalDwellSum = stageDwells.Values.Sum(list => list.Sum());

            var rows = stageDwells
                .Select(kvp =>
                {
                    var statusCode = kvp.Key;
                    var dwellList = kvp.Value;
                    var workDaysList = stageWorkDays[statusCode];
                    var reliableCount = stageReliableCount[statusCode];
                    var totalDaysRaw = dwellList.Sum();

                    return new jewelry.Model.Production.Plan.StageLeadTimeReport.StageRow
                    {
                        StatusCode = statusCode,
                        StatusName = statusMaster.FirstOrDefault(m => m.Id == statusCode)?.NameTh ?? statusCode.ToString(),
                        VisitCount = dwellList.Count,
                        AvgDays = Math.Round((decimal)dwellList.Average(), 1),
                        MedianDays = Math.Round((decimal)GetMedian(dwellList), 1),
                        P90Days = Math.Round((decimal)GetPercentile(dwellList, 90), 1),
                        TotalDays = Math.Round((decimal)totalDaysRaw, 1),
                        ShareOfTotalPercent = totalDwellSum > 0 ? Math.Round((decimal)(totalDaysRaw / totalDwellSum * 100), 2) : 0,
                        MedianWorkDays = workDaysList.Any() ? Math.Round((decimal)GetMedian(workDaysList), 1) : 0,
                        WorkDataReliabilityPercent = dwellList.Count > 0 ? Math.Round((decimal)reliableCount / dwellList.Count * 100, 2) : 0
                    };
                })
                .OrderBy(x => x.StatusCode)
                .ToList();

            var bottleneck = rows.OrderByDescending(x => x.ShareOfTotalPercent).FirstOrDefault();

            var summary = new jewelry.Model.Production.Plan.StageLeadTimeReport.StageLeadTimeSummary
            {
                CompletedPlanCount = completedPlans.Count,
                AvgTotalLeadDays = totalLeadDaysList.Any() ? Math.Round((decimal)totalLeadDaysList.Average(), 1) : 0,
                MedianTotalLeadDays = totalLeadDaysList.Any() ? Math.Round((decimal)GetMedian(totalLeadDaysList), 1) : 0,
                BottleneckStatusCode = bottleneck?.StatusCode ?? 0,
                BottleneckStatusName = bottleneck?.StatusName ?? string.Empty,
                PlansWithNoStageCount = plansWithNoStageCount
            };

            var breakdown = new List<jewelry.Model.Production.Plan.StageLeadTimeReport.BreakdownGroup>();

            if (groupBy != "none")
            {
                Func<string, string> resolveGroupName = code => code;

                if (groupBy == "gold")
                {
                    var master = await _jewelryContext.TbmGold.ToListAsync();
                    resolveGroupName = code => master.FirstOrDefault(m => m.Code == code)?.NameTh ?? code;
                }
                else if (groupBy == "goldSize")
                {
                    var master = await _jewelryContext.TbmGoldSize.ToListAsync();
                    resolveGroupName = code => master.FirstOrDefault(m => m.Code == code)?.NameTh ?? code;
                }
                else if (groupBy == "productType")
                {
                    var master = await _jewelryContext.TbmProductType.ToListAsync();
                    resolveGroupName = code => master.FirstOrDefault(m => m.Code == code)?.NameTh ?? code;
                }
                else if (groupBy == "customerType")
                {
                    var master = await _jewelryContext.TbmCustomerType.ToListAsync();
                    resolveGroupName = code => master.FirstOrDefault(m => m.Code == code)?.NameTh ?? code;
                }

                breakdown = groupAccumulators
                    .Select(kvp =>
                    {
                        var groupCode = kvp.Key;
                        var acc = kvp.Value;
                        var groupName = string.IsNullOrEmpty(groupCode) ? "ไม่ระบุ" : resolveGroupName(groupCode);

                        var stages = acc.StageDwells
                            .Select(s => new jewelry.Model.Production.Plan.StageLeadTimeReport.BreakdownStage
                            {
                                StatusCode = s.Key,
                                StatusName = statusMaster.FirstOrDefault(m => m.Id == s.Key)?.NameTh ?? s.Key.ToString(),
                                VisitCount = s.Value.Count,
                                AvgDays = Math.Round((decimal)s.Value.Average(), 1),
                                MedianDays = Math.Round((decimal)GetMedian(s.Value), 1)
                            })
                            .OrderBy(x => x.StatusCode)
                            .ToList();

                        return new jewelry.Model.Production.Plan.StageLeadTimeReport.BreakdownGroup
                        {
                            GroupCode = groupCode,
                            GroupName = groupName,
                            PlanCount = acc.TotalLeadDays.Count,
                            AvgTotalDays = acc.TotalLeadDays.Any() ? Math.Round((decimal)acc.TotalLeadDays.Average(), 1) : 0,
                            MedianTotalDays = acc.TotalLeadDays.Any() ? Math.Round((decimal)GetMedian(acc.TotalLeadDays), 1) : 0,
                            Stages = stages
                        };
                    })
                    .OrderByDescending(x => x.PlanCount)
                    .ToList();
            }

            var wipPlans = await planQuery
                .Where(x => x.IsActive == true
                    && x.Status != jewelry.Model.Constant.ProductionPlanStatus.Completed
                    && x.Status != jewelry.Model.Constant.ProductionPlanStatus.Melted)
                .Select(x => new
                {
                    x.Id,
                    x.Status,
                    x.WoText,
                    x.ProductName,
                    x.CustomerNumber,
                    x.RequestDate
                })
                .ToListAsync();

            var wipPlanIds = wipPlans.Select(x => x.Id).ToList();

            var wipHeaders = await _jewelryContext.TbtProductionPlanStatusHeader
                .Where(h => h.IsActive == true && wipPlanIds.Contains(h.ProductionPlanId))
                .Select(h => new
                {
                    h.ProductionPlanId,
                    h.Status,
                    h.CreateDate
                })
                .ToListAsync();

            var wipHeaderByPlanAndStatus = wipHeaders
                .GroupBy(h => (h.ProductionPlanId, h.Status))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.CreateDate).First());

            var nowUtc = DateTime.UtcNow;
            var customerCodes = wipPlans.Select(x => x.CustomerNumber).Distinct().ToList();
            var customerMaster = await _jewelryContext.TbmCustomer
                .Where(c => customerCodes.Contains(c.Code))
                .Select(c => new { c.Code, c.NameTh })
                .ToListAsync();

            var wipWithAge = new List<(int Id, int Status, string WoText, string ProductName, string? CustomerName, DateTime RequestDate, double AgeDays)>();

            foreach (var plan in wipPlans)
            {
                if (!wipHeaderByPlanAndStatus.TryGetValue((plan.Id, plan.Status), out var header))
                {
                    continue;
                }

                var ageDays = Math.Max(0, (nowUtc - header.CreateDate).TotalDays);
                var customerName = customerMaster.FirstOrDefault(c => c.Code == plan.CustomerNumber)?.NameTh;

                wipWithAge.Add((plan.Id, plan.Status, plan.WoText, plan.ProductName, customerName, plan.RequestDate, ageDays));
            }

            var wipRows = wipWithAge
                .GroupBy(x => x.Status)
                .Select(g => new jewelry.Model.Production.Plan.StageLeadTimeReport.WipRow
                {
                    StatusCode = g.Key,
                    StatusName = statusMaster.FirstOrDefault(m => m.Id == g.Key)?.NameTh ?? g.Key.ToString(),
                    WipCount = g.Count(),
                    AvgAgeDays = Math.Round((decimal)g.Average(x => x.AgeDays), 1),
                    MaxAgeDays = Math.Round((decimal)g.Max(x => x.AgeDays), 1)
                })
                .OrderBy(x => x.StatusCode)
                .ToList();

            var topStuckJobs = wipWithAge
                .OrderByDescending(x => x.AgeDays)
                .Take(10)
                .Select(x => new jewelry.Model.Production.Plan.StageLeadTimeReport.StuckJob
                {
                    ProductionPlanId = x.Id,
                    WoText = x.WoText,
                    StatusCode = x.Status,
                    StatusName = statusMaster.FirstOrDefault(m => m.Id == x.Status)?.NameTh ?? x.Status.ToString(),
                    AgeDays = Math.Round((decimal)x.AgeDays, 1),
                    ProductName = x.ProductName,
                    CustomerName = x.CustomerName,
                    RequestDate = x.RequestDate
                })
                .ToList();

            return new jewelry.Model.Production.Plan.StageLeadTimeReport.Response
            {
                Rows = rows,
                WipRows = wipRows,
                TopStuckJobs = topStuckJobs,
                Summary = summary,
                GroupBy = groupBy,
                Breakdown = breakdown
            };
        }

        private class LeadTimeGroupAccumulator
        {
            public List<double> TotalLeadDays { get; set; } = new List<double>();
            public Dictionary<int, List<double>> StageDwells { get; set; } = new Dictionary<int, List<double>>();
        }

        private static double GetPercentile(List<double> values, double percentile)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            var sorted = values.OrderBy(x => x).ToList();
            var rank = (percentile / 100.0) * (sorted.Count - 1);
            var lowerIndex = (int)Math.Floor(rank);
            var upperIndex = (int)Math.Ceiling(rank);

            if (lowerIndex == upperIndex)
            {
                return sorted[lowerIndex];
            }

            var fraction = rank - lowerIndex;
            return sorted[lowerIndex] + (sorted[upperIndex] - sorted[lowerIndex]) * fraction;
        }

        #endregion
    }
}
