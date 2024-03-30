using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlanCost.GoldCostCreate;
using jewelry.Model.ProductionPlanCost.GoldCostList;
using jewelry.Model.ProductionPlanCost.GoldCostReport;
using jewelry.Model.ProductionPlanCost.GoldCostUpdate;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
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
    }
    public class ProductionPlanCostService : IProductionPlanCostService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public ProductionPlanCostService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
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

                                MeltDate = item.MeltDate,
                                MeltWeight = item.MeltWeight,
                                ReturnMeltWeight = item.ReturnMeltWeight,
                                ReturnMeltScrapWeight = item.ReturnMeltScrapWeight,
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

                    Remark = request.Remark,
                    AssignBy = request.AssignBy,
                    ReceiveBy = request.ReceiveBy,

                    MeltDate = request.MeltDateFormat.HasValue ? request.MeltDateFormat.Value.UtcDateTime : null,
                    MeltWeight = request.MeltWeight,
                    ReturnMeltWeight = request.ReturnMeltWeight,
                    ReturnMeltScrapWeight = request.ReturnMeltScrapWeight,
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
                    ReturnCastPowderWeight = request.ReturnCastPowderWeight,
                    CastWeightLoss = request.CastWeightLoss,
                    CastWeightOver = request.CastWeightOver,

                    CreateDate = DateTime.UtcNow,
                    CreateBy = _admin,

                    RunningNumber = running,

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

                    foreach (var item in request.Items)
                    {
                        //if (string.IsNullOrEmpty(item.ProductionPlanId))
                        //{ }
                        var createItem = new TbtProductionPlanCostGoldItem()
                        {
                            No = request.No.ToUpper(),
                            BookNo = request.BookNo.ToUpper(),
                            ProductionPlanId = item.ProductionPlanId,
                            ReturnWeight = item.ReturnWeight,
                            ReturnQty = item.ReturnQTY,
                            Remark = item.Remark,


                            CreateDate = DateTime.UtcNow,
                            CreateBy = _admin,
                        };
                        createItems.Add(createItem);
                    }
                }

                if (createItems.Any())
                {
                    _jewelryContext.TbtProductionPlanCostGoldItem.AddRange(createItems);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
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

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {

                data.No = request.No.ToUpper();
                data.BookNo = request.BookNo.ToUpper();
                data.AssignDate = request.AssignDateFormat.UtcDateTime;

                data.Gold = request.GoldCode;
                data.GoldSize = request.GoldSizeCode;
                data.GoldReceipt = request.GoldReceipt;

                data.Remark = request.Remark;
                data.AssignBy = request.AssignBy;
                data.ReceiveBy = request.ReceiveBy;

                data.MeltDate = request.MeltDateFormat.HasValue ? request.MeltDateFormat.Value.UtcDateTime : null;
                data.MeltWeight = request.MeltWeight;
                data.ReturnMeltWeight = request.ReturnMeltWeight;
                data.ReturnMeltScrapWeight = request.ReturnMeltScrapWeight;
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
                data.ReturnCastPowderWeight = request.ReturnCastPowderWeight;
                data.CastWeightLoss = request.CastWeightLoss;
                data.CastWeightOver = request.CastWeightOver;

                data.UpdateDate = DateTime.UtcNow;
                data.UpdateBy = _admin;

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
                            CreateBy = _admin,
                        };
                        createItems.Add(createItem);
                    }
                }

                if (createItems.Any())
                {
                    _jewelryContext.TbtProductionPlanCostGoldItem.AddRange(createItems);
                }

                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            return "success";
        }
    }
}
