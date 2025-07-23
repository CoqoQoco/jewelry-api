using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlanCost.GoldCostCreate;
using jewelry.Model.ProductionPlanCost.GoldCostItem;
using jewelry.Model.ProductionPlanCost.GoldCostList;
using jewelry.Model.ProductionPlanCost.GoldCostReport;
using jewelry.Model.ProductionPlanCost.GoldCostUpdate;
using jewelry.Model.ProductionPlanCost.ScrapWeightDashboard;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.Production.Plan;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NPOI.OpenXmlFormats.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static NPOI.HSSF.Util.HSSFColor;

namespace Jewelry.Service.ProductionPlan
{
    public interface IProductionPlanCostService
    {
        IQueryable<GoldCostListResponse> ListGoldCost(GoldCostList request);
        IQueryable<GoldCostListResponse> Report(GoldCostList request);
        GoldCostSummeryResponse SummeryReport(GoldCostList request);
        Task<string> CreateGoldCost(GoldCostCreateRequest request);
        Task<string> UpdateGoldCost(GoldCostUpdateRequest request);

        IQueryable<GoldCostItemResponse> ListGoldCostItem(GoldCostItemSearch request);
        ScrapWeightDashboardResponse GetScrapWeightDashboard();
    }
    public class ProductionPlanCostService : BaseService, IProductionPlanCostService
    {
        //private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        private readonly IPlanService _planService;
        public ProductionPlanCostService(JewelryContext JewelryContext,
            IHostEnvironment HostingEnvironment,
            IHttpContextAccessor httpContextAccessor,
            IRunningNumber runningNumberService,
            IPlanService planService) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
            _planService = planService;
        }

        public IQueryable<GoldCostListResponse> ListGoldCost(GoldCostList request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlanCostGold
                         .Include(x => x.GoldNavigation)
                         .Include(x => x.GoldSizeNavigation)
                         where item.IsActive == true
                         select item);

            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query.Include(x => x.TbtProductionPlanCostGoldItem)
                             //where item.No.Contains(request.Text.ToUpper())
                             //|| item.BookNo.Contains(request.Text)
                         where item.AssignBy.Contains(request.Text)
                         || item.ReceiveBy.Contains(request.Text)
                         || item.Remark.Contains(request.Text)
                         select item);
            }
            if (!string.IsNullOrEmpty(request.BookNo))
            {
                query = (from item in query.Include(x => x.TbtProductionPlanCostGoldItem)
                         where item.BookNo.Contains(request.BookNo.ToUpper())
                         select item);
            }
            if (!string.IsNullOrEmpty(request.No))
            {
                query = (from item in query.Include(x => x.TbtProductionPlanCostGoldItem)
                         where item.No.Contains(request.No.ToUpper())
                         select item);
            }
            if (!string.IsNullOrEmpty(request.RunningNumber))
            {
                query = query.Where(x => x.RunningNumber.Contains(request.RunningNumber.ToUpper()));
            }
            if (request.CreateStart.HasValue)
            {
                query = query.Where(x => x.AssignDate >= request.CreateStart.Value.StartOfDayUtc());
            }
            if (request.CreateEnd.HasValue)
            {
                query = query.Where(x => x.AssignDate <= request.CreateEnd.Value.EndOfDayUtc());
            }


            var response = (from item in query
                            select new GoldCostListResponse()
                            {
                                No = item.No,
                                BookNo = item.BookNo,
                                AssignDate = item.AssignDate,

                                GoldCode = item.GoldNavigation.Code,
                                GoldName = item.GoldNavigation.NameTh,
                                GoldSizeCode = item.GoldSizeNavigation.Code,
                                GoldSizeName = item.GoldSizeNavigation.NameTh,
                                GoldReceipt = item.GoldReceipt,

                                Zill = item.Zill,
                                ZillQty = item.ZillQty,

                                MeltDate = item.MeltDate,
                                MeltWeight = item.MeltWeight,
                                ReturnMeltWeight = item.ReturnMeltWeight,
                                ReturnMeltScrapWeight = item.ReturnMeltScrapWeight,
                                ReturnMeltScrapWeightDate = item.ReturnMeltScrapWeightDate,
                                MeltWeightLoss = item.MeltWeightLoss,
                                MeltWeightOver = item.MeltWeightOver,

                                CastDate = item.CastDate,
                                CastWeight = item.CastWeight,
                                GemWeight = item.GemWeight,
                                ReturnCastWeight = item.ReturnCastWeight,
                                ReturnCastMoldWeight = item.ReturnCastMoldWeight,
                                ReturnCastBodyBrokenWeight = item.ReturnCastBodyBrokenedWeight,
                                ReturnCastBodyWeightTotal = item.ReturnCastBodyWeightTotal,
                                ReturnCastScrapWeight = item.ReturnCastScrapWeight,
                                ReturnCastScrapWeightDate = item.ReturnCastScrapWeightDate,
                                ReturnCastPowderWeight = item.ReturnCastPowderWeight,
                                CastWeightLoss = item.CastWeightLoss,
                                CastWeightOver = item.CastWeightOver,

                                Cost = item.Cost,

                                AssignBy = item.AssignBy,
                                ReceiveBy = item.ReceiveBy,
                                RunningNumber = item.RunningNumber,
                                Remark = item.Remark,
                                Items = (from subItem in item.TbtProductionPlanCostGoldItem
                                         select new GoldCostCreateItem()
                                         {
                                             ProductionPlanId = subItem.ProductionPlanId,
                                             Remark = subItem.Remark,
                                             ReturnWeight = subItem.ReturnWeight,
                                             ReturnQTY = subItem.ReturnQty.HasValue ? subItem.ReturnQty.Value : 0,
                                         }).OrderBy(x => x.ProductionPlanId).ToList()
                            });

            return response;
        }
        public IQueryable<GoldCostListResponse> Report(GoldCostList request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlanCostGold
                         .Include(x => x.GoldNavigation)
                         .Include(x => x.GoldSizeNavigation)
                         where item.IsActive == true
                         select item);

            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query.Include(x => x.TbtProductionPlanCostGoldItem)
                             //where item.No.Contains(request.Text.ToUpper())
                             //|| item.BookNo.Contains(request.Text)
                         where item.AssignBy.Contains(request.Text)
                         || item.ReceiveBy.Contains(request.Text)
                         || item.Remark.Contains(request.Text)
                         select item);
            }
            if (!string.IsNullOrEmpty(request.BookNo))
            {
                query = (from item in query.Include(x => x.TbtProductionPlanCostGoldItem)
                         where item.BookNo.Contains(request.BookNo.ToUpper())
                         select item);
            }
            if (!string.IsNullOrEmpty(request.No))
            {
                query = (from item in query.Include(x => x.TbtProductionPlanCostGoldItem)
                         where item.No.Contains(request.No.ToUpper())
                         select item);
            }
            if (!string.IsNullOrEmpty(request.RunningNumber))
            {
                query = query.Where(x => x.RunningNumber.Contains(request.RunningNumber.ToUpper()));
            }
            if (request.CreateStart.HasValue)
            {
                query = query.Where(x => x.AssignDate >= request.CreateStart.Value.StartOfDayUtc());
            }
            if (request.CreateEnd.HasValue)
            {
                query = query.Where(x => x.AssignDate <= request.CreateEnd.Value.EndOfDayUtc());
            }


            var response = (from item in query
                            select new GoldCostListResponse()
                            {
                                No = item.No,
                                BookNo = item.BookNo,
                                AssignDate = item.AssignDate,

                                GoldCode = item.GoldNavigation.Code,
                                GoldName = item.GoldNavigation.NameTh,
                                GoldSizeCode = item.GoldSizeNavigation.Code,
                                GoldSizeName = item.GoldSizeNavigation.NameTh,
                                GoldReceipt = item.GoldReceipt,

                                MeltDate = item.MeltDate,
                                MeltWeight = item.MeltWeight,
                                ReturnMeltWeight = item.ReturnMeltWeight,
                                ReturnMeltScrapWeight = item.ReturnMeltScrapWeight,
                                ReturnMeltScrapWeightDate = item.ReturnMeltScrapWeightDate,
                                MeltWeightLoss = item.MeltWeightLoss,
                                MeltWeightOver = item.MeltWeightOver,

                                CastDate = item.CastDate,
                                CastWeight = item.CastWeight,
                                GemWeight = item.GemWeight,
                                ReturnCastWeight = item.ReturnCastWeight,
                                ReturnCastMoldWeight = item.ReturnCastMoldWeight,
                                ReturnCastBodyBrokenWeight = item.ReturnCastBodyBrokenedWeight,
                                ReturnCastBodyWeightTotal = item.ReturnCastBodyWeightTotal,
                                ReturnCastScrapWeight = item.ReturnCastScrapWeight,
                                ReturnCastScrapWeightDate = item.ReturnCastScrapWeightDate,
                                ReturnCastPowderWeight = item.ReturnCastPowderWeight,
                                CastWeightLoss = item.CastWeightLoss,
                                CastWeightOver = item.CastWeightOver,

                                AssignBy = item.AssignBy,
                                ReceiveBy = item.ReceiveBy,
                                RunningNumber = item.RunningNumber,
                                Remark = item.Remark,
                                Items = (from subItem in item.TbtProductionPlanCostGoldItem
                                         select new GoldCostCreateItem()
                                         {
                                             ProductionPlanId = subItem.ProductionPlanId,
                                             Remark = subItem.Remark,
                                             ReturnWeight = subItem.ReturnWeight,
                                             ReturnQTY = subItem.ReturnQty.HasValue ? subItem.ReturnQty.Value : 0,
                                         }).OrderBy(x => x.ProductionPlanId).ToList()
                            });

            return response;
        }
        public GoldCostSummeryResponse SummeryReport(GoldCostList request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlanCostGold
                         .Include(x => x.GoldNavigation)
                         .Include(x => x.GoldSizeNavigation)
                         where item.IsActive == true
                         select item);

            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query.Include(x => x.TbtProductionPlanCostGoldItem)
                             //where item.No.Contains(request.Text.ToUpper())
                             //|| item.BookNo.Contains(request.Text)
                         where item.AssignBy.Contains(request.Text)
                         || item.ReceiveBy.Contains(request.Text)
                         || item.Remark.Contains(request.Text)
                         select item);
            }
            if (!string.IsNullOrEmpty(request.BookNo))
            {
                query = (from item in query.Include(x => x.TbtProductionPlanCostGoldItem)
                         where item.BookNo.Contains(request.BookNo.ToUpper())
                         select item);
            }
            if (!string.IsNullOrEmpty(request.No))
            {
                query = (from item in query.Include(x => x.TbtProductionPlanCostGoldItem)
                         where item.No.Contains(request.No.ToUpper())
                         select item);
            }
            if (!string.IsNullOrEmpty(request.RunningNumber))
            {
                query = query.Where(x => x.RunningNumber.Contains(request.RunningNumber.ToUpper()));
            }
            if (request.CreateStart.HasValue)
            {
                query = query.Where(x => x.AssignDate >= request.CreateStart.Value.StartOfDayUtc());
            }
            if (request.CreateEnd.HasValue)
            {
                query = query.Where(x => x.AssignDate <= request.CreateEnd.Value.EndOfDayUtc());
            }


            var response = (from item in query
                            select new GoldCostListResponse()
                            {
                                No = item.No,
                                BookNo = item.BookNo,
                                AssignDate = item.AssignDate,

                                GoldCode = item.GoldNavigation.Code,
                                GoldName = item.GoldNavigation.NameTh,
                                GoldSizeCode = item.GoldSizeNavigation.Code,
                                GoldSizeName = item.GoldSizeNavigation.NameTh,
                                GoldReceipt = item.GoldReceipt,

                                MeltDate = item.MeltDate,
                                MeltWeight = item.MeltWeight,
                                ReturnMeltWeight = item.ReturnMeltWeight,
                                ReturnMeltScrapWeight = item.ReturnMeltScrapWeight,
                                ReturnMeltScrapWeightDate = item.ReturnMeltScrapWeightDate,
                                MeltWeightLoss = item.MeltWeightLoss,
                                MeltWeightOver = item.MeltWeightOver,

                                CastDate = item.CastDate,
                                CastWeight = item.CastWeight,
                                GemWeight = item.GemWeight,
                                ReturnCastWeight = item.ReturnCastWeight,
                                ReturnCastMoldWeight = item.ReturnCastMoldWeight,
                                ReturnCastBodyBrokenWeight = item.ReturnCastBodyBrokenedWeight,
                                ReturnCastBodyWeightTotal = item.ReturnCastBodyWeightTotal,
                                ReturnCastScrapWeight = item.ReturnCastScrapWeight,
                                ReturnCastScrapWeightDate = item.ReturnCastScrapWeightDate,
                                ReturnCastPowderWeight = item.ReturnCastPowderWeight,
                                CastWeightLoss = item.CastWeightLoss,
                                CastWeightOver = item.CastWeightOver,

                                AssignBy = item.AssignBy,
                                ReceiveBy = item.ReceiveBy,
                                RunningNumber = item.RunningNumber,
                                Remark = item.Remark,
                                //Items = (from subItem in item.TbtProductionPlanCostGoldItem
                                //         select new GoldCostCreateItem()
                                //         {
                                //             ProductionPlanId = subItem.ProductionPlanId,
                                //             Remark = subItem.Remark,
                                //             ReturnWeight = subItem.ReturnWeight,
                                //             ReturnQTY = subItem.ReturnQty.HasValue ? subItem.ReturnQty.Value : 0,
                                //         }).OrderBy(x => x.ProductionPlanId).ToList()
                            });

            return new GoldCostSummeryResponse()
            {
                TotalCastWeight = response.Sum(x => x.CastWeight),
                TotalCastWeightLoss = response.Sum(x => x.CastWeightLoss),
                TotalCastWeightOver = response.Sum(x => x.CastWeightOver),
                TotalGemWeight = response.Sum(x => x.GemWeight),
                TotalMeltWeight = response.Sum(x => x.MeltWeight),
                TotalMeltWeightLoss = response.Sum(x => x.MeltWeightLoss),
                TotalMeltWeightOver = response.Sum(x => x.MeltWeightOver),
                TotalReturnCastBodyBrokenWeight = response.Sum(x => x.ReturnCastBodyBrokenWeight),
                TotalReturnCastBodyWeightTotal = response.Sum(x => x.ReturnCastBodyWeightTotal),
                TotalReturnCastMoldWeight = response.Sum(x => x.ReturnCastMoldWeight),
                TotalReturnCastPowderWeight = response.Sum(x => x.ReturnCastPowderWeight),
                TotalReturnCastScrapWeight = response.Sum(x => x.ReturnCastScrapWeight),
                TotalReturnCastWeight = response.Sum(x => x.ReturnCastWeight),
                TotalReturnMeltScrapWeight = response.Sum(x => x.ReturnMeltScrapWeight),
                TotalReturnMeltWeight = response.Sum(x => x.ReturnMeltWeight)
            };
        }
        public async Task<string> CreateGoldCost(GoldCostCreateRequest request)
        {

            var dub = (from item in _jewelryContext.TbtProductionPlanCostGold
                       where item.No == request.No.ToUpper() && item.BookNo == request.BookNo.ToUpper()
                       select item).SingleOrDefault();

            if (dub != null)
            {
                throw new HandleException($"ใบเบิกผสมทอง เลขที่:{request.No} เล่มที่:{request.BookNo} ทำซ้ำ กรุณาสร้างหมายเลขใหม่");
            }

            var running = await _runningNumberService.GenerateRunningNumberForGold("CAST");
            var requestTransfer = new jewelry.Model.Production.Plan.Transfer.Request()
            {
                FormerStatus = ProductionPlanStatus.Designed,
                TargetStatus = ProductionPlanStatus.Casting,
            };

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var create = new TbtProductionPlanCostGold()
                {
                    No = request.No.ToUpper(),
                    BookNo = request.BookNo.ToUpper(),
                    AssignDate = request.AssignDateFormat.UtcDateTime,

                    Gold = request.GoldCode,
                    GoldSize = request.GoldSizeCode,
                    GoldReceipt = request.GoldReceipt,

                    Zill = request.Zill,
                    ZillQty = request.ZillQty,

                    Remark = request.Remark,
                    AssignBy = request.AssignBy,
                    ReceiveBy = request.ReceiveBy,

                    MeltDate = request.MeltDateFormat.HasValue ? request.MeltDateFormat.Value.UtcDateTime : null,
                    MeltWeight = request.MeltWeight,
                    ReturnMeltWeight = request.ReturnMeltWeight,
                    ReturnMeltScrapWeight = request.ReturnMeltScrapWeight,
                    ReturnMeltScrapWeightDate = request.ReturnMeltScrapWeightDate.HasValue ? request.ReturnMeltScrapWeightDate.Value.UtcDateTime : null,
                    MeltWeightLoss = request.MeltWeightLoss,
                    MeltWeightOver = request.MeltWeightOver,

                    CastDate = request.CastDateFormat.HasValue ? request.CastDateFormat.Value.UtcDateTime : null,
                    CastWeight = request.CastWeight,
                    GemWeight = request.GemWeight,
                    ReturnCastWeight = request.ReturnCastWeight,
                    ReturnCastMoldWeight = request.ReturnCastMoldWeight,
                    ReturnCastBodyBrokenedWeight = request.ReturnCastBodyBrokenWeight,
                    ReturnCastBodyWeightTotal = request.Items.Sum(x => x.ReturnWeight),
                    ReturnCastScrapWeight = request.ReturnCastScrapWeight,
                    ReturnCastScrapWeightDate = request.ReturnCastScrapWeightDate.HasValue ? request.ReturnCastScrapWeightDate.Value.UtcDateTime : null,
                    ReturnCastPowderWeight = request.ReturnCastPowderWeight,
                    CastWeightLoss = request.CastWeightLoss,
                    CastWeightOver = request.CastWeightOver,

                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername,

                    RunningNumber = running,
                    Cost = request.Cost,

                    IsActive = true,
                };

                _jewelryContext.TbtProductionPlanCostGold.Add(create);
                //await _jewelryContext.SaveChangesAsync();

                var createItems = new List<TbtProductionPlanCostGoldItem>();

                if (request.Items != null && request.Items.Any())
                {

                    var duplicates = request.Items
                                    .GroupBy(x => x.ProductionPlanId)
                                    .Where(group => group.Count() > 1)
                                    .Select(group => group.Key);

                    if (duplicates.Any())
                    {
                        throw new HandleException($"ไม่สามารถบันทึกรายการคืนตัวเรือนซ้ำได้: {duplicates.First()}");
                    }

                    //get all item id
                    var planIds = request.Items.Select(x => x.Id).ToList();
                    var productionPlans = (from plan in _jewelryContext.TbtProductionPlan
                                           where planIds.Contains(plan.Id)
                                           select plan).ToList();

                    //var requestTransfer = new jewelry.Model.Production.Plan.Transfer.Request()
                    //{
                    //    FormerStatus = ProductionPlanStatus.Designed,
                    //    TargetStatus = ProductionPlanStatus.Casting,
                    //};

                    foreach (var item in request.Items)
                    {
                        var createItem = new TbtProductionPlanCostGoldItem()
                        {
                            No = request.No.ToUpper(),
                            BookNo = request.BookNo.ToUpper(),
                            ProductionPlanId = item.ProductionPlanId,
                            ReturnWeight = item.ReturnWeight,
                            ReturnQty = item.ReturnQTY,
                            Remark = item.Remark,


                            CreateDate = DateTime.UtcNow,
                            CreateBy = CurrentUsername,
                        };
                        createItems.Add(createItem);


                        // set auto status paln when plan is design
                        var getPlan = productionPlans.Where(x => x.Id == item.Id).FirstOrDefault();
                        if (getPlan == null)
                        {
                            throw new HandleException($"{ErrorMessage.NotFound} --> ไม่พบแผนผลิต");
                        }

                        if (getPlan.Status == ProductionPlanStatus.Designed)
                        {
                            var createheaderPlan = new jewelry.Model.Production.Plan.Transfer.RequestItem()
                            {
                                Id = item.Id,
                                Wo = getPlan.Wo,
                                WoNumber = getPlan.WoNumber
                            };
                            requestTransfer.Plans.Add(createheaderPlan);
                            //create header status wating cast
                            //var newHeaderWaitingCast = new TbtProductionPlanStatusHeader()
                            //{
                            //    CreateDate = DateTime.UtcNow,
                            //    CreateBy = $"{request.BookNo}- {request.No}/{request.ReceiveBy}",
                            //    UpdateDate = DateTime.UtcNow,
                            //    UpdateBy = $"{request.BookNo}- {request.No}/{request.ReceiveBy}",
                            //    IsActive = true,
                            //    ProductionPlanId = getPlan.Id,
                            //    Status = ProductionPlanStatus.WaitCasting
                            //};
                            //createHeaderStatus.Add(newHeaderWaitingCast);

                            //getPlan.UpdateDate = DateTime.UtcNow;
                            //getPlan.UpdateBy = request.ReceiveBy;
                            //getPlan.Status = ProductionPlanStatus.WaitCasting;
                            //updateproductionPlans.Add(getPlan);
                        }

                        #region --- old  method ---
                        //if (getPlan != null)
                        //{
                        //    var oldStatusHearder = (from heaader in _jewelryContext.TbtProductionPlanStatusHeader
                        //                            .Include(x => x.TbtProductionPlanStatusDetail)
                        //                            where heaader.ProductionPlanId == item.Id
                        //                            && heaader.Status == ProductionPlanStatus.Casting
                        //                            && heaader.IsActive
                        //                            select item).FirstOrDefault();

                        //    if (oldStatusHearder == null)
                        //    {
                        //        if (getPlan.Status == ProductionPlanStatus.Designed)
                        //        {
                        //            var addStatusHeader = new TbtProductionPlanStatusHeader
                        //            {
                        //                ProductionPlanId = item.Id,
                        //                Status = ProductionPlanStatus.Casting,
                        //                IsActive = true,

                        //                CreateDate = DateTime.UtcNow,
                        //                CreateBy = _admin,
                        //                UpdateDate = DateTime.UtcNow,
                        //                UpdateBy = _admin,

                        //                //SendName = request.AssignBy,
                        //                //SendDate = request.AssignDateFormat.UtcDateTime,
                        //                //CheckName = request.ReceiveBy,
                        //                //CheckDate = request.AssignDateFormat.UtcDateTime,

                        //                Remark1 = request.Remark,
                        //                Remark2 = string.Empty,
                        //                WagesTotal = 0,
                        //            };
                        //            _jewelryContext.TbtProductionPlanStatusHeader.Add(addStatusHeader);
                        //            await _jewelryContext.SaveChangesAsync();

                        //            var addStatusHeaderItem = new TbtProductionPlanStatusDetail()
                        //            {
                        //                HeaderId = addStatusHeader.Id,
                        //                ProductionPlanId = item.Id,
                        //                ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{item.Id}-{ProductionPlanStatus.Casting}"),
                        //                IsActive = true,

                        //                RequestDate = null,

                        //                Gold = request.GoldCode,
                        //                GoldQtySend = item.ReturnQTY,
                        //                GoldWeightSend = item.ReturnWeight,
                        //                GoldQtyCheck = null,
                        //                GoldWeightCheck = null,
                        //                Worker = null,
                        //                WorkerSub = null,
                        //                Description = request.Remark,

                        //                Wages = 0,
                        //                TotalWages = 0,

                        //            };
                        //            _jewelryContext.TbtProductionPlanStatusDetail.Add(addStatusHeaderItem);
                        //        }
                        //    }
                        //    else
                        //    {
                        //        if (getPlan.Status != ProductionPlanStatus.Completed)
                        //        {
                        //            var addStatusHeaderItem = new TbtProductionPlanStatusDetail()
                        //            {
                        //                HeaderId = oldStatusHearder.Id,
                        //                ProductionPlanId = item.Id,
                        //                ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{item.Id}-{ProductionPlanStatus.Casting}"),
                        //                IsActive = true,

                        //                RequestDate = null,

                        //                Gold = request.GoldCode,
                        //                GoldQtySend = item.ReturnQTY,
                        //                GoldWeightSend = item.ReturnWeight,
                        //                GoldQtyCheck = null,
                        //                GoldWeightCheck = null,
                        //                Worker = null,
                        //                WorkerSub = null,
                        //                Description = request.Remark,

                        //                Wages = 0,
                        //                TotalWages = 0,

                        //            };
                        //            _jewelryContext.TbtProductionPlanStatusDetail.Add(addStatusHeaderItem);
                        //        }
                        //    }

                        //    //update status plan
                        //    if (getPlan.Status == ProductionPlanStatus.Designed)
                        //    {
                        //        getPlan.Status = ProductionPlanStatus.Casting;
                        //        getPlan.UpdateBy = _admin;
                        //        getPlan.UpdateDate = DateTime.UtcNow;
                        //        updateproductionPlans.Add(getPlan);
                        //    }
                        //} 
                        #endregion
                    }

                }
                if (createItems.Any())
                {
                    _jewelryContext.TbtProductionPlanCostGoldItem.AddRange(createItems);
                }


                //throw new HandleException($"ไม่สามารถบันทึกรายการคืนตัวเรือนซ้ำได้");
                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            if (requestTransfer.Plans != null && requestTransfer.Plans.Count > 0)
            {
                var result = await _planService.Transfer(requestTransfer);
            }

            return "success";
        }
        public async Task<string> UpdateGoldCost(GoldCostUpdateRequest request)
        {

            var data = (from item in _jewelryContext.TbtProductionPlanCostGold.Include(x => x.TbtProductionPlanCostGoldItem)
                        where item.No == request.No.ToUpper() && item.BookNo == request.BookNo.ToUpper()
                        select item).SingleOrDefault();

            if (data == null)
            {
                throw new HandleException($"ไม่พบข้อมูลใบเบิกผสมทอง เลขที่:{request.No} เล่มที่:{request.BookNo} โปรดตรวจสอบความถูกต้องอีกครั้ง");
            }
            var requestTransfer = new jewelry.Model.Production.Plan.Transfer.Request()
            {
                FormerStatus = ProductionPlanStatus.Designed,
                TargetStatus = ProductionPlanStatus.Casting,
            };

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {

                data.No = request.No.ToUpper();
                data.BookNo = request.BookNo.ToUpper();
                data.AssignDate = request.AssignDateFormat.UtcDateTime;

                data.Gold = request.GoldCode;
                data.GoldSize = request.GoldSizeCode;
                data.GoldReceipt = request.GoldReceipt;

                data.Zill = request.Zill;
                data.ZillQty = request.ZillQty;

                data.Remark = request.Remark;
                data.AssignBy = request.AssignBy;
                data.ReceiveBy = request.ReceiveBy;

                data.MeltDate = request.MeltDateFormat.HasValue ? request.MeltDateFormat.Value.UtcDateTime : null;
                data.MeltWeight = request.MeltWeight;
                data.ReturnMeltWeight = request.ReturnMeltWeight;
                data.ReturnMeltScrapWeight = request.ReturnMeltScrapWeight;
                data.ReturnMeltScrapWeightDate = request.ReturnMeltScrapWeightDate.HasValue ? request.ReturnMeltScrapWeightDate.Value.UtcDateTime : null;
                data.MeltWeightLoss = request.MeltWeightLoss;
                data.MeltWeightOver = request.MeltWeightOver;

                data.CastDate = request.CastDateFormat.HasValue ? request.CastDateFormat.Value.UtcDateTime : null;
                data.CastWeight = request.CastWeight;
                data.GemWeight = request.GemWeight;
                data.ReturnCastWeight = request.ReturnCastWeight;
                data.ReturnCastMoldWeight = request.ReturnCastMoldWeight;
                data.ReturnCastBodyBrokenedWeight = request.ReturnCastBodyBrokenWeight;
                data.ReturnCastBodyWeightTotal = request.Items.Sum(x => x.ReturnWeight);
                data.ReturnCastScrapWeight = request.ReturnCastScrapWeight;
                data.ReturnCastScrapWeightDate = request.ReturnCastScrapWeightDate.HasValue ? request.ReturnCastScrapWeightDate.Value.UtcDateTime : null;
                data.ReturnCastPowderWeight = request.ReturnCastPowderWeight;
                data.CastWeightLoss = request.CastWeightLoss;
                data.CastWeightOver = request.CastWeightOver;

                data.UpdateDate = DateTime.UtcNow;
                data.UpdateBy = CurrentUsername;

                data.Cost = request.Cost;

                _jewelryContext.TbtProductionPlanCostGold.Update(data);

                if (data.TbtProductionPlanCostGoldItem.Any())
                {
                    _jewelryContext.TbtProductionPlanCostGoldItem.RemoveRange(data.TbtProductionPlanCostGoldItem);
                }

                var createItems = new List<TbtProductionPlanCostGoldItem>();
                if (request.Items != null && request.Items.Any())
                {

                    var duplicates = request.Items
                                    .GroupBy(x => x.ProductionPlanId)
                                    .Where(group => group.Count() > 1)
                                    .Select(group => group.Key);

                    if (duplicates.Any())
                    {
                        throw new HandleException($"ไม่สามารถบันทึกรายการคืนตัวเรือนซ้ำได้: {duplicates.First()}");
                    }

                    //get all item id
                    var planIds = request.Items.Select(x => x.Id).ToList();
                    var productionPlans = (from plan in _jewelryContext.TbtProductionPlan
                                           where planIds.Contains(plan.Id)
                                           select plan).ToList();

                    //var requestTransfer = new jewelry.Model.Production.Plan.Transfer.Request()
                    //{
                    //    FormerStatus = ProductionPlanStatus.Designed,
                    //    TargetStatus = ProductionPlanStatus.Casting,
                    //};

                    foreach (var item in request.Items)
                    {
                        //if (string.IsNullOrEmpty(item.ProductionPlanId))
                        //{ }
                        var createItem = new TbtProductionPlanCostGoldItem()
                        {
                            No = data.No.ToUpper(),
                            BookNo = data.BookNo.ToUpper(),
                            ProductionPlanId = item.ProductionPlanId,
                            ReturnWeight = item.ReturnWeight,
                            ReturnQty = item.ReturnQTY,
                            Remark = item.Remark,

                            CreateDate = DateTime.UtcNow,
                            CreateBy = CurrentUsername,
                        };
                        createItems.Add(createItem);

                        // set auto status paln when plan is design
                        var getPlan = productionPlans.Where(x => x.Id == item.Id).FirstOrDefault();
                        if (getPlan == null)
                        {
                            throw new HandleException($"{ErrorMessage.NotFound} --> ไม่พบแผนผลิต");
                        }

                        if (getPlan.Status == ProductionPlanStatus.Designed)
                        {
                            var createheaderPlan = new jewelry.Model.Production.Plan.Transfer.RequestItem()
                            {
                                Id = item.Id,
                                Wo = getPlan.Wo,
                                WoNumber = getPlan.WoNumber
                            };
                            requestTransfer.Plans.Add(createheaderPlan);
                        }
                    }
                }

                if (createItems.Any())
                {
                    _jewelryContext.TbtProductionPlanCostGoldItem.AddRange(createItems);
                }

                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            if (requestTransfer.Plans != null && requestTransfer.Plans.Count > 0)
            {
                var result = await _planService.Transfer(requestTransfer);
            }

            return "success";
        }

        public IQueryable<GoldCostItemResponse> ListGoldCostItem(GoldCostItemSearch request)
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
                             ReturnMeltScrapWeightDate = item.TbtProductionPlanCostGold.ReturnMeltScrapWeightDate,
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
                             ReturnCastScrapWeightDate = item.TbtProductionPlanCostGold.ReturnCastScrapWeightDate,
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

            if (!string.IsNullOrEmpty(request.ProductionPlanNumber))
            {
                query = query.Where(x => x.ProductionPlanId == request.ProductionPlanNumber);
            }

            return query;
        }

        public ScrapWeightDashboardResponse GetScrapWeightDashboard()
        {
            var currentYear = DateTime.UtcNow.Year;
            var startOfYear = new DateTime(currentYear, 1, 1).ToUniversalTime();
            var endOfYear = new DateTime(currentYear, 12, 31, 23, 59, 59).ToUniversalTime();

            // Get all TbtProductionPlanCostGold data for current year
            var query = (from item in _jewelryContext.TbtProductionPlanCostGold
                         .Include(x => x.GoldNavigation)
                         .Include(x => x.GoldSizeNavigation)
                         where item.IsActive == true
                         && ((item.ReturnMeltScrapWeightDate.HasValue && item.ReturnMeltScrapWeightDate.Value >= startOfYear && item.ReturnMeltScrapWeightDate.Value <= endOfYear)
                         || (item.ReturnCastScrapWeightDate.HasValue && item.ReturnCastScrapWeightDate.Value >= startOfYear && item.ReturnCastScrapWeightDate.Value <= endOfYear))
                         select item).ToList();

            var response = new ScrapWeightDashboardResponse();
            var monthNames = new string[] { "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", 
                                          "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };

            // Initialize 12 months for both datasets
            for (int month = 1; month <= 12; month++)
            {
                response.MeltScrapData.Add(new ScrapWeightMonthlyData
                {
                    Month = month,
                    MonthName = monthNames[month],
                    GoldCategories = new List<ScrapWeightGoldCategory>(),
                    TotalWeight = 0
                });

                response.CastScrapData.Add(new ScrapWeightMonthlyData
                {
                    Month = month,
                    MonthName = monthNames[month],
                    GoldCategories = new List<ScrapWeightGoldCategory>(),
                    TotalWeight = 0
                });
            }

            // Process Melt Scrap Weight Data
            var meltScrapData = query.Where(x => x.ReturnMeltScrapWeightDate.HasValue && x.ReturnMeltScrapWeight.HasValue && x.ReturnMeltScrapWeight.Value > 0)
                                   .GroupBy(x => new {
                                       Month = x.ReturnMeltScrapWeightDate.Value.Month,
                                       GoldCode = x.GoldNavigation.Code,
                                       GoldName = x.GoldNavigation.NameTh,
                                       GoldSizeCode = x.GoldSizeNavigation.Code,
                                       GoldSizeName = x.GoldSizeNavigation.NameTh
                                   })
                                   .Select(g => new {
                                       g.Key.Month,
                                       g.Key.GoldCode,
                                       g.Key.GoldName,
                                       g.Key.GoldSizeCode,
                                       g.Key.GoldSizeName,
                                       TotalWeight = g.Sum(x => x.ReturnMeltScrapWeight.Value),
                                       LastDate = g.Max(x => x.ReturnMeltScrapWeightDate.Value)
                                   });

            foreach (var item in meltScrapData)
            {
                var monthData = response.MeltScrapData.FirstOrDefault(x => x.Month == item.Month);
                if (monthData != null)
                {
                    monthData.GoldCategories.Add(new ScrapWeightGoldCategory
                    {
                        GoldCode = item.GoldCode,
                        GoldName = item.GoldName,
                        GoldSizeCode = item.GoldSizeCode,
                        GoldSizeName = item.GoldSizeName,
                        Weight = item.TotalWeight,
                        Date = item.LastDate
                    });
                    monthData.TotalWeight += item.TotalWeight;
                }
            }

            // Process Cast Scrap Weight Data
            var castScrapData = query.Where(x => x.ReturnCastScrapWeightDate.HasValue && x.ReturnCastScrapWeight.HasValue && x.ReturnCastScrapWeight.Value > 0)
                                   .GroupBy(x => new {
                                       Month = x.ReturnCastScrapWeightDate.Value.Month,
                                       GoldCode = x.GoldNavigation.Code,
                                       GoldName = x.GoldNavigation.NameTh,
                                       GoldSizeCode = x.GoldSizeNavigation.Code,
                                       GoldSizeName = x.GoldSizeNavigation.NameTh
                                   })
                                   .Select(g => new {
                                       g.Key.Month,
                                       g.Key.GoldCode,
                                       g.Key.GoldName,
                                       g.Key.GoldSizeCode,
                                       g.Key.GoldSizeName,
                                       TotalWeight = g.Sum(x => x.ReturnCastScrapWeight.Value),
                                       LastDate = g.Max(x => x.ReturnCastScrapWeightDate.Value)
                                   });

            foreach (var item in castScrapData)
            {
                var monthData = response.CastScrapData.FirstOrDefault(x => x.Month == item.Month);
                if (monthData != null)
                {
                    monthData.GoldCategories.Add(new ScrapWeightGoldCategory
                    {
                        GoldCode = item.GoldCode,
                        GoldName = item.GoldName,
                        GoldSizeCode = item.GoldSizeCode,
                        GoldSizeName = item.GoldSizeName,
                        Weight = item.TotalWeight,
                        Date = item.LastDate
                    });
                    monthData.TotalWeight += item.TotalWeight;
                }
            }

            return response;
        }
    }
}
