using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.Receipt.Gem.Create;
using jewelry.Model.Receipt.Gem.Inbound;
using jewelry.Model.Receipt.Gem.List;
using jewelry.Model.Receipt.Gem.Outbound;
using jewelry.Model.Receipt.Gem.Picklist;
using jewelry.Model.Receipt.Gem.PickOff;
using jewelry.Model.Receipt.Gem.Return;
using jewelry.Model.Receipt.Gem.Scan;
using jewelry.Model.Receipt.Gem.Update;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using NetTopologySuite.Index.HPRtree;
using NetTopologySuite.Triangulate.QuadEdge;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Receipt.Gem
{
    public interface IReceiptAndIssueStockGemService
    {
        Task<string> CreateGem(CreateRequest request);
        Task<string> UpdateGem(UpdateRequest request);
        Task<IQueryable<ScanResponse>> Scan(ScanRequest request);

        IQueryable<ListResponse> ListTransection(ListSearch request);
        Task<string> InboundGem(InboundRequest request);
        Task<string> OutboundGem(OutboundRequest request);

        IQueryable<PicklistResponse> Picklist(PicklistFilter request);
        Task<string> PickOffGem(PickOffRequest request);
        Task<PickReturnResponse> PickReturnGem(PickReturnRequest request);
    }
    public class ReceiptAndIssueStockGemService : BaseService, IReceiptAndIssueStockGemService
    {
        //private readonly string _admin = "@ADMIN";
        private readonly bool _valPass = true;
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public ReceiptAndIssueStockGemService(JewelryContext JewelryContext, 
            IHostEnvironment HostingEnvironment, 
            IHttpContextAccessor httpContextAccessor, 
            IRunningNumber runningNumberService) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public async Task<string> CreateGem(CreateRequest request)
        {
            var gem = (from item in _jewelryContext.TbtStockGem
                       where item.Code == request.Code.ToUpper().Trim()
                       select item);

            if (gem.Any())
            {
                throw new HandleException("รหัสเพชรเเละพลอยซ้ำ");
            }

            var newGem = new TbtStockGem()
            {
                Code = request.Code.ToUpper().Trim(),
                GroupName = request.GroupName.Trim(),

                Size = request.Size.Trim(),
                Shape = request.Shape,
                Grade = request.Grade,
                GradeCode = request.GradeCode,

                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername,

                Remark1 = request.Remark.Trim(),

                Price = 0,
                PriceQty = 0,
                Quantity = 0,
            };
            _jewelryContext.TbtStockGem.Add(newGem);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }
        public async Task<string> UpdateGem(UpdateRequest request)
        {
            var gem = (from item in _jewelryContext.TbtStockGem
                       where item.Code == request.Code.ToUpper().Trim()
                       select item).SingleOrDefault();

            if (gem == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            gem.GroupName = request.GroupName.Trim();
            gem.Size = request.Size.Trim();
            gem.Shape = request.Shape.Trim();


            gem.Grade = request.Grade.Trim();
            gem.GradeCode = request.GradeCode.Trim();

            gem.Remark1 = request.Remark;

            gem.UpdateDate = DateTime.UtcNow;
            gem.UpdateBy = CurrentUsername;

            _jewelryContext.TbtStockGem.Update(gem);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }




        public async Task<IQueryable<ScanResponse>> Scan(ScanRequest request)
        {
            var query = from item in _jewelryContext.TbtStockGem
                        select new ScanResponse()
                        {
                            Id = item.Id,
                            Name = $"{item.Code}-{item.GroupName}-{item.Shape}-{item.Size}-{item.Grade}",
                            Code = item.Code,
                            GroupName = item.GroupName,

                            Size = item.Size,
                            Shape = item.Shape,
                            Grade = item.Grade,

                            Quantity = item.Quantity,
                            QuantityOnProcess = item.QuantityOnProcess,
                            QuantityWeight = item.QuantityWeight,
                            QuantityWeightOnProcess = item.QuantityWeightOnProcess,

                            Price = item.Price,
                            PriceQty = item.PriceQty,
                            Unit = item.Unit,
                            UnitCode = item.UnitCode,

                            Remark1 = item.Remark1,
                            Remark2 = item.Remark2,

                            Daterec = item.Daterec,
                            Original = item.Original,
                        };

            if (request.Scans.Any())
            {
                var codes = request.Scans.Select(x => x.Code.ToUpperInvariant()).ToArray();

                if (request.ScanType == "S")
                {
                    //var code = codes[0];
                    query = (from item in query
                             where item.Code == codes[0]
                             select item);
                }
                else
                {
                    query = (from item in query
                             where codes.Contains(item.Code)
                             select item);
                }
            }

            var testtt = query.ToList();
            return query;
        }

        public IQueryable<ListResponse> ListTransection(ListSearch request)
        {
            var query = (from tran in _jewelryContext.TbtStockGemTransection
                             //where tran.RequestDate >= request.RequestDateStart.StartOfDayUtc()
                             //&& tran.RequestDate <= request.RequestDateEnd.EndOfDayUtc()
                         join gem in _jewelryContext.TbtStockGem on tran.Code equals gem.Code
                         select new ListResponse()
                         {
                             RequestDate = tran.RequestDate,
                             ReturnDate = tran.ReturnDate,
                             IsOverPick = tran.ReturnDate.HasValue ? tran.ReturnDate.Value < DateTime.UtcNow : false,

                             Running = tran.Running,
                             RefRunning1 = tran.RefRunning,
                             RefRunning2 = tran.Ref2Running,
                             Code = tran.Code,

                             Type = tran.Type,
                             JobOrPo = tran.JobOrPo,
                             SubpplierName = tran.SubpplierName,
                             Remark1 = tran.Remark1,

                             PreviousRemianQty = tran.PreviousRemainQty,
                             PreviousRemianQtyWeight = tran.PreviousRemianQtyWeight,
                             Qty = tran.Qty,
                             QtyWeight = tran.QtyWeight,
                             PointRemianQty = tran.PointRemianQty,
                             PointRemianQtyWeight = tran.PointRemianQtyWeight,

                             SupplierCost = tran.SupplierCost,
                             Remark2 = tran.Remark2,

                             CreateDate = tran.CreateDate,
                             CreateBy = tran.CreateBy,
                             UpdateDate = tran.UpdateDate,
                             UpdateBy = tran.UpdateBy,

                             Name = $"{gem.Code}-{gem.Shape}-{gem.Size}-{gem.Grade}-{gem.GroupName}",
                             GroupName = gem.GroupName,
                             Size = gem.Size,
                             Shape = gem.Shape,
                             Grade = gem.Grade,

                             Status = tran.Stastus,

                             WO = tran.ProductionPlanWo,
                             WONumber = tran.ProductionPlanWoNumber,
                             WOText = tran.ProductionPlanWoText,
                             Mold = tran.ProductionPlanMold,

                             Price = gem.Price,
                             PriceQty = gem.PriceQty,
                             Unit = gem.Unit,
                             UnitCode = gem.UnitCode,

                             OperatorBy = tran.OperatorBy,
                         });

            if (request.RequestDateStart.HasValue)
            {
                query = (from item in query
                         where item.RequestDate >= request.RequestDateStart.Value.StartOfDayUtc()
                         select item);
            }
            if (request.RequestDateEnd.HasValue)
            {
                query = (from item in query
                         where item.RequestDate <= request.RequestDateEnd.Value.EndOfDayUtc()
                         select item);
            }

            if (!string.IsNullOrEmpty(request.Running))
            {
                query = (from item in query
                         where item.Running == request.Running
                         select item);
            }

            if (!string.IsNullOrEmpty(request.RefRunning1))
            {
                query = (from item in query
                         where item.RefRunning1 == request.RefRunning1
                         select item);
            }

            if (!string.IsNullOrEmpty(request.RefRunning2))
            {
                query = (from item in query
                         where item.RefRunning2 == request.RefRunning2
                         select item);
            }

            if (request.Type != null && request.Type.Length > 0)
            {
                query = (from item in query
                         where request.Type.Contains(item.Type)
                         select item);
            }
            if (!string.IsNullOrEmpty(request.Code))
            {
                query = (from item in query
                         where item.Code.Contains(request.Code.ToUpper())
                         select item);
            }
            if (request.GroupName != null && request.GroupName.Length > 0)
            {
                query = (from item in query
                         where request.GroupName.Contains(item.GroupName)
                         select item);
            }
            if (request.Size != null && request.Size.Length > 0)
            {
                query = (from item in query
                         where request.Size.Contains(item.Size)
                         select item);
            }
            if (request.Shape != null && request.Shape.Length > 0)
            {
                query = (from item in query
                         where request.Shape.Contains(item.Shape)
                         select item);
            }
            if (request.Grade != null && request.Grade.Length > 0)
            {
                query = (from item in query
                         where request.Grade.Contains(item.Grade)
                         select item);
            }

            if (!string.IsNullOrEmpty(request.JobNoOrPONo))
            {
                query = (from item in query
                         where item.JobOrPo.Contains(request.JobNoOrPONo)
                         select item);
            }
            if (!string.IsNullOrEmpty(request.SupplierName))
            {
                query = (from item in query
                         where item.SubpplierName.Contains(request.SupplierName)
                         select item);
            }
            if (!string.IsNullOrEmpty(request.WO))
            {
                query = (from item in query
                         where item.WOText.Contains(request.WO)
                         select item);
            }

            if (!string.IsNullOrEmpty(request.Operator))
            {
                query = query.Where(item => item.OperatorBy.Contains(request.Operator));
            }

            if (!string.IsNullOrEmpty(request.CreateBy))
            {
                query = query.Where(item => item.CreateBy.Contains(request.CreateBy));
            }

            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(item => request.Status.Contains(item.Status));
            }

            if (request.ReturnDateStart.HasValue)
            {
                query = query.Where(item => item.ReturnDate >= request.ReturnDateStart.Value.StartOfDayUtc());
            }

            if (request.ReturnDateEnd.HasValue)
            {
                query = query.Where(item => item.ReturnDate <= request.ReturnDateEnd.Value.EndOfDayUtc());
            }

            return query;
        }
        public async Task<string> InboundGem(InboundRequest request)
        {
            var runningNo = string.Empty;

            //val pass
            if (_valPass)
            {
                var account = (from item in _jewelryContext.TbmAccount
                               where item.Username == "GR-GEM"
                               && item.TempPass == request.Pass
                               select item);

                if (!account.Any())
                {
                    throw new HandleException(ErrorMessage.PermissionFail);
                }
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var UpdateGems = new List<TbtStockGem>();
                var newInbounds = new List<TbtStockGemTransection>();

                var gems = (from item in _jewelryContext.TbtStockGem
                            where request.Gems.Select(x => x.Code).Contains(item.Code)
                            select item);
                //check gem
                if (!gems.Any())
                {
                    throw new HandleException(ErrorMessage.NotFound);
                }

                foreach (var gem in request.Gems)
                {
                    var gemData = gems.FirstOrDefault(x => x.Code == gem.Code);
                    if (gemData == null)
                    {
                        throw new HandleException($"{ErrorMessage.NotFound} --> {gem.Code}");
                    }

                    var previousQty = gemData.Quantity;
                    var previousQtyWeight = gemData.QuantityWeight;

                    gemData.Quantity += gem.ReceiveQty;
                    gemData.QuantityWeight += gem.ReceiveQtyWeight;

                    gemData.UpdateDate = DateTime.UtcNow;
                    gemData.UpdateBy = CurrentUsername;
                    UpdateGems.Add(gemData);

                    var newInbound = new TbtStockGemTransection()
                    {
                        Type = request.Type,
                        JobOrPo = request.JobNoOrPO,
                        SubpplierName = request.SubplierName,
                        Remark1 = request.Remark,

                        Code = gem.Code,
                        SupplierCost = gem.SupplierCost,
                        Remark2 = gem.Remark,

                        Qty = gem.ReceiveQty,
                        QtyWeight = gem.ReceiveQtyWeight,

                        PreviousRemainQty = previousQty,
                        PreviousRemianQtyWeight = previousQtyWeight,

                        PointRemianQty = gemData.Quantity,
                        PointRemianQtyWeight = gemData.QuantityWeight,

                        Stastus = "completed",

                        RequestDate = request.RequestDate.UtcDateTime,
                        CreateBy = CurrentUsername,
                        CreateDate = DateTime.UtcNow,
                    };
                    newInbounds.Add(newInbound);

                }


                if (UpdateGems.Any())
                {
                    _jewelryContext.TbtStockGem.UpdateRange(UpdateGems);
                }
                if (newInbounds.Any())
                {
                    //set all running number
                    runningNo = await _runningNumberService.GenerateRunningNumberForGold($"INB");
                    foreach (var item in newInbounds)
                    {
                        item.Running = runningNo;
                    }

                    _jewelryContext.TbtStockGemTransection.AddRange(newInbounds);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return runningNo;
        }
        public async Task<string> OutboundGem(OutboundRequest request)
        {
            var runningNo = string.Empty;

            //val pass
            if (_valPass)
            {
                var account = (from item in _jewelryContext.TbmAccount
                               where item.Username == "GI-GEM"
                               && item.TempPass == request.Pass
                               select item);

                if (!account.Any())
                {
                    throw new HandleException(ErrorMessage.PermissionFail);
                }
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var UpdateGems = new List<TbtStockGem>();
                var newInbounds = new List<TbtStockGemTransection>();

                var gems = (from item in _jewelryContext.TbtStockGem
                            where request.Gems.Select(x => x.Code).Contains(item.Code)
                            select item);
                //check gem
                if (!gems.Any())
                {
                    throw new HandleException(ErrorMessage.NotFound);
                }

                foreach (var gem in request.Gems)
                {
                    var gemData = gems.FirstOrDefault(x => x.Code == gem.Code);
                    if (gemData == null)
                    {
                        throw new HandleException($"{gem.Code} --> {ErrorMessage.NotFound}");
                    }

                    if (gem.IssueQty > gemData.Quantity)
                    {
                        throw new HandleException($"{gem.Code} --> {ErrorMessage.QtyLessThanAction}");
                    }
                    if (gem.IssueQtyWeight > gemData.QuantityWeight)
                    {
                        throw new HandleException($"{gem.Code} --> {ErrorMessage.QtyWeightLessThanAction}");
                    }

                    var previousQty = gemData.Quantity;
                    var previousQtyWeight = gemData.QuantityWeight;

                    gemData.Quantity -= gem.IssueQty;
                    gemData.QuantityWeight -= gem.IssueQtyWeight;

                    gemData.UpdateDate = DateTime.UtcNow;
                    gemData.UpdateBy = CurrentUsername;
                    UpdateGems.Add(gemData);

                    var newInbound = new TbtStockGemTransection()
                    {
                        Type = request.Type,
                        Remark1 = request.Remark,

                        Code = gem.Code,
                        Remark2 = gem.Remark,

                        PreviousRemainQty = previousQty,
                        PreviousRemianQtyWeight = previousQtyWeight,

                        Qty = gem.IssueQty,
                        QtyWeight = gem.IssueQtyWeight,

                        PointRemianQty = gemData.Quantity,
                        PointRemianQtyWeight = gemData.QuantityWeight,

                        Stastus = "completed",

                        RequestDate = request.RequestDate.UtcDateTime,
                        CreateBy = CurrentUsername,
                        CreateDate = DateTime.UtcNow,

                        ProductionPlanWo = gem.WO,
                        ProductionPlanWoNumber = gem.WONumber,
                        ProductionPlanWoText = gem.WOText,
                        ProductionPlanMold = gem.Mold,

                    };
                    newInbounds.Add(newInbound);

                }


                if (UpdateGems.Any())
                {
                    _jewelryContext.TbtStockGem.UpdateRange(UpdateGems);
                }
                if (newInbounds.Any())
                {
                    //set all running number
                    runningNo = await _runningNumberService.GenerateRunningNumberForGold($"OUT");
                    foreach (var item in newInbounds)
                    {
                        item.Running = runningNo;
                    }

                    _jewelryContext.TbtStockGemTransection.AddRange(newInbounds);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return runningNo;

        }

        public IQueryable<PicklistResponse> Picklist(PicklistFilter request)
        {

            var query = (from item in _jewelryContext.TbtStockGemTransection
                        select item);

            // Apply all filters before executing the query
            if (!string.IsNullOrEmpty(request.Running))
            {
                query = query.Where(item => item.Running.Contains(request.Running));
            }

            if (!string.IsNullOrEmpty(request.Operator))
            {
                query = query.Where(item => item.OperatorBy.Contains(request.Operator));
            }

            if (!string.IsNullOrEmpty(request.CreateBy))
            {
                query = query.Where(item => item.CreateBy.Contains(request.CreateBy));
            }

            if (request.Type != null && request.Type.Any())
            {
                query = query.Where(item => request.Type.Contains(item.Type));
            }

            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(item => request.Status.Contains(item.Stastus));
            }

            if (request.RequestDateStart.HasValue)
            {
                query = query.Where(item => item.RequestDate >= request.RequestDateStart.Value.StartOfDayUtc());
            }

            if (request.RequestDateEnd.HasValue)
            {
                query = query.Where(item => item.RequestDate <= request.RequestDateEnd.Value.EndOfDayUtc());
            }

            if (request.ReturnDateStart.HasValue)
            {
                query = query.Where(item => item.ReturnDate >= request.ReturnDateStart.Value.StartOfDayUtc());
            }

            if (request.ReturnDateEnd.HasValue)
            {
                query = query.Where(item => item.ReturnDate <= request.ReturnDateEnd.Value.EndOfDayUtc());
            }

            if (!string.IsNullOrEmpty(request.GetRunning))
            {
                query = query.Where(item => item.Running == request.GetRunning);
            }


            var response = (from item in query
                           join gem in _jewelryContext.TbtStockGem on item.Code equals gem.Code
                           group new { item, gem } by item.Running into grouped
                           select new PicklistResponse
                           {
                               Running = grouped.Key,
                               Type = grouped.First().item.Type,
                               RequestDate = grouped.First().item.RequestDate,
                               ReturnDate = grouped.First().item.ReturnDate,
                               Remark = grouped.First().item.Remark1,
                               Stastus = grouped.First().item.Stastus,
                               CreateBy = grouped.First().item.CreateBy,
                               CreateDate = grouped.First().item.CreateDate,
                               UpdateBy = grouped.First().item.UpdateBy,
                               UpdateDate = grouped.First().item.UpdateDate,
                               IsOverPick = grouped.First().item.ReturnDate != null && grouped.First().item.ReturnDate < DateTime.UtcNow,
                               OperatorBy = grouped.First().item.OperatorBy,
                               Items = grouped.Select(g => new PicklistItem
                               {
                                   Code = g.item.Code,
                                   GroupName = g.item.Code,
                                   Name = $"{g.item.Code}-{g.gem.Shape}-{g.gem.Size}-{g.gem.Grade}-{g.gem.GroupName}",
                                   Size = g.gem.Size,
                                   Shape = g.gem.Shape,
                                   Grade = g.gem.Grade,
                                   GradeDia = g.gem.GradeDia,
                                   Status = g.item.Stastus,
                                   RequestDate = g.item.RequestDate,
                                   Running = g.item.Running,
                                   Type = g.item.Type,
                                   JobOrPo = g.item.JobOrPo,
                                   SupplierCost = g.item.SupplierCost,
                                   Remark1 = g.item.Remark1,
                                   Remark2 = g.item.Remark2,
                                   Qty = g.item.Qty,
                                   QtyWeight = g.item.QtyWeight,
                                   SubpplierName = g.item.SubpplierName,
                                   CreateDate = g.item.CreateDate,
                                   CreateBy = g.item.CreateBy,
                                   UpdateDate = g.item.UpdateDate,
                                   UpdateBy = g.item.UpdateBy,

                                   WO = g.item.ProductionPlanWo,
                                   WONumber = g.item.ProductionPlanWoNumber,
                                   WOText = g.item.ProductionPlanWoText,
                                   Mold = g.item.ProductionPlanMold,

                                   Price = g.gem.Price,
                                   PriceQty = g.gem.PriceQty,
                                   Unit = g.gem.Unit,
                                   UnitCode = g.gem.UnitCode,
                                   OperatorBy = g.item.OperatorBy,
                               })
                           }).ToList();

            if (!string.IsNullOrEmpty(request.Code))
            {
                var upperCode = request.Code.ToUpper();
                response = response.Where(item => item.Items.Any(x => x.Code.Contains(upperCode))).ToList();
            }

            return response.AsQueryable();
        }
       
        public IQueryable<PicklistResponse> OldPicklist(PicklistFilter request)
        {

            var qury = (from item in _jewelryContext.TbtStockGemTransection
                        select item);

            var response = (from item in _jewelryContext.TbtStockGemTransection
                         join gem in _jewelryContext.TbtStockGem on item.Code equals gem.Code
                         group new { item, gem } by item.Running into grouped
                         select new PicklistResponse
                         {
                             Running = grouped.Key,
                             Type = grouped.First().item.Type,
                             RequestDate = grouped.First().item.RequestDate,
                             ReturnDate = grouped.First().item.ReturnDate,
                             Remark = grouped.First().item.Remark1,
                             Stastus = grouped.First().item.Stastus,
                             CreateBy = grouped.First().item.CreateBy,
                             CreateDate = grouped.First().item.CreateDate,
                             UpdateBy = grouped.First().item.UpdateBy,
                             UpdateDate = grouped.First().item.UpdateDate,
                             IsOverPick = grouped.First().item.ReturnDate != null && grouped.First().item.ReturnDate < DateTime.UtcNow,
                             OperatorBy = grouped.First().item.OperatorBy,
                             Items = grouped.Select(g => new PicklistItem
                             {
                                 Code = g.item.Code,
                                 GroupName = g.item.Code,
                                 Name = $"{g.item.Code}-{g.gem.Shape}-{g.gem.Size}-{g.gem.Grade}-{g.gem.GroupName}",
                                 Size = g.gem.Size,
                                 Shape = g.gem.Shape,
                                 Grade = g.gem.Grade,
                                 GradeDia = g.gem.GradeDia,
                                 Status = g.item.Stastus,
                                 RequestDate = g.item.RequestDate,
                                 Running = g.item.Running,
                                 Type = g.item.Type,
                                 JobOrPo = g.item.JobOrPo,
                                 SupplierCost = g.item.SupplierCost,
                                 Remark1 = g.item.Remark1,
                                 Remark2 = g.item.Remark2,
                                 Qty = g.item.Qty,
                                 QtyWeight = g.item.QtyWeight,
                                 SubpplierName = g.item.SubpplierName,
                                 CreateDate = g.item.CreateDate,
                                 CreateBy = g.item.CreateBy,
                                 UpdateDate = g.item.UpdateDate,
                                 UpdateBy = g.item.UpdateBy,
                                 WO = g.item.ProductionPlanWo,
                                 WONumber = g.item.ProductionPlanWoNumber,
                                 WOText = g.item.ProductionPlanWoText,
                                 Mold = g.item.ProductionPlanMold,

                                 Price = g.gem.Price,
                                 PriceQty = g.gem.PriceQty,
                                 Unit = g.gem.Unit,
                                 UnitCode = g.gem.UnitCode,

                                 OperatorBy = g.item.OperatorBy,
                             }),

                         }).ToList();

            if (!string.IsNullOrEmpty(request.Running))
            {
                response = (from item in response
                         where item.Running.Contains(request.Running)
                         select item).ToList();
            }
            if (request.Type != null && request.Type.Any())
            {
                response = (from item in response
                         where request.Type.Contains(item.Type)
                         select item).ToList();
            }
            if (request.Status != null && request.Status.Any())
            {
                response = (from item in response
                         where request.Status.Contains(item.Stastus)
                         select item).ToList();
            }

            if (request.RequestDateStart.HasValue)
            {
                response = (from item in response
                         where item.RequestDate >= request.RequestDateStart.Value.StartOfDayUtc()
                         select item).ToList();
            }
            if (request.RequestDateEnd.HasValue)
            {
                response = (from item in response
                         where item.RequestDate <= request.RequestDateEnd.Value.EndOfDayUtc()
                         select item).ToList();
            }

            if (request.ReturnDateStart.HasValue)
            {
                response = (from item in response
                         where item.ReturnDate >= request.ReturnDateStart.Value.StartOfDayUtc()
                         select item).ToList();
            }
            if (request.ReturnDateEnd.HasValue)
            {
                response = (from item in response
                         where item.ReturnDate <= request.ReturnDateEnd.Value.EndOfDayUtc()
                         select item).ToList();
            }

            if (!string.IsNullOrEmpty(request.GetRunning))
            {
                response = (from item in response
                         where item.Running == request.GetRunning
                         select item).ToList();
            }

            if (!string.IsNullOrEmpty(request.Code))
            {
                response = (from item in response
                         where item.Items.Any(x => x.Code.Contains(request.Code.ToUpper()))
                         //where item.Code.Contains(request.Code.ToUpper())
                         select item).ToList();
            }

            return response.AsQueryable();
        }
        public async Task<string> PickOffGem(PickOffRequest request)
        {
            var runningNo = string.Empty;

            //val pass
            if (_valPass)
            {
                var account = (from item in _jewelryContext.TbmAccount
                               where item.Username == "GI-GEM"
                               && item.TempPass == request.Pass
                               select item);

                if (!account.Any())
                {
                    throw new HandleException(ErrorMessage.PermissionFail);
                }
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var UpdateGems = new List<TbtStockGem>();
                var newPickOff = new List<TbtStockGemTransection>();

                var gems = (from item in _jewelryContext.TbtStockGem
                            where request.Gems.Select(x => x.Code).Contains(item.Code)
                            select item);
                //check gem
                if (!gems.Any())
                {
                    throw new HandleException(ErrorMessage.NotFound);
                }

                foreach (var gem in request.Gems)
                {
                    var gemData = gems.FirstOrDefault(x => x.Code == gem.Code);
                    if (gemData == null)
                    {
                        throw new HandleException($"{gem.Code} --> {ErrorMessage.NotFound}");
                    }

                    if (gem.IssueQty > gemData.Quantity)
                    {
                        throw new HandleException($"{gem.Code} --> {ErrorMessage.QtyLessThanAction}");
                    }
                    if (gem.IssueQtyWeight > gemData.QuantityWeight)
                    {
                        throw new HandleException($"{gem.Code} --> {ErrorMessage.QtyWeightLessThanAction}");
                    }

                    var previousQty = gemData.Quantity;
                    var previousQtyWeight = gemData.QuantityWeight;

                    gemData.Quantity -= gem.IssueQty;
                    gemData.QuantityWeight -= gem.IssueQtyWeight;

                    gemData.QuantityOnProcess += gem.IssueQty;
                    gemData.QuantityWeightOnProcess += gem.IssueQtyWeight;

                    gemData.UpdateDate = DateTime.UtcNow;
                    gemData.UpdateBy = CurrentUsername;
                    UpdateGems.Add(gemData);

                    var newInbound = new TbtStockGemTransection()
                    {
                        Type = request.Type,
                        Remark1 = request.Remark,

                        Code = gem.Code,
                        Remark2 = gem.Remark,

                        PreviousRemainQty = previousQty,
                        PreviousRemianQtyWeight = previousQtyWeight,

                        Qty = gem.IssueQty,
                        QtyWeight = gem.IssueQtyWeight,

                        PointRemianQty = gemData.Quantity,
                        PointRemianQtyWeight = gemData.QuantityWeight,

                        Stastus = "process",

                        RequestDate = request.RequestDate.UtcDateTime,
                        ReturnDate = request.ReturnDate.UtcDateTime,

                        CreateBy = CurrentUsername,
                        CreateDate = DateTime.UtcNow,

                        OperatorBy = request.OperatorBy,

                    };
                    newPickOff.Add(newInbound);

                }


                if (UpdateGems.Any())
                {
                    _jewelryContext.TbtStockGem.UpdateRange(UpdateGems);
                }
                if (newPickOff.Any())
                {
                    //set all running number
                    runningNo = await _runningNumberService.GenerateRunningNumberForGold($"PIF");
                    foreach (var item in newPickOff)
                    {
                        item.Running = runningNo;
                    }
                    _jewelryContext.TbtStockGemTransection.AddRange(newPickOff);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return runningNo;

        }
        public async Task<PickReturnResponse> PickReturnGem(PickReturnRequest request)
        {
            var runningNoPickReturn = string.Empty;
            var runningNoOutbound = string.Empty;

            var updateGems = new List<TbtStockGem>();
            var newTransection = new List<TbtStockGemTransection>();
            var updateTransection = new List<TbtStockGemTransection>();
            var updatePlan = new List<TbtProductionPlan>();
            var updatePlanHeader = new List<TbtProductionPlanStatusHeader>();
            var addStatusDetailGem = new List<TbtProductionPlanStatusDetailGem>();

            if (_valPass)
            {
                var account = (from item in _jewelryContext.TbmAccount
                               where item.Username == "GI-GEM"
                               && item.TempPass == request.Pass
                               select item);

                if (!account.Any())
                {
                    throw new HandleException(ErrorMessage.PermissionFail);
                }
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var gems = (from item in _jewelryContext.TbtStockGem
                            where request.GemsReturn.Select(x => x.Code).Contains(item.Code)
                            select item);

                var transection = (from item in _jewelryContext.TbtStockGemTransection
                                   select item);

                //already pick off
                var pickOffs = (from item in transection
                                where item.Running == request.PickOffRunning
                                select item);

                //already pick return
                var pickReturns = (from item in transection
                                   where item.RefRunning == pickOffs.First().Running && item.Type == 6
                                   select item);

                if (pickReturns.Any())
                {
                    throw new HandleException(ErrorMessage.PickReturned);
                }

                //already pick outbound
                var pickOutbounds = (from item in transection
                                     where item.Ref2Running == pickOffs.First().Running && item.Stastus == "completed" && item.Type == 7
                                     select item);

                var pickOutboundsTest = pickOutbounds.ToList();

                var aviStatus = new int[] { 10, 50, 55, 60, 70, 80, 90, 95 };
                var plan = (from item in _jewelryContext.TbtProductionPlan
                            .Include(x => x.TbtProductionPlanStatusHeader)
                            where aviStatus.Contains(item.Status)
                            select item);

                //check gem
                if (!gems.Any())
                {
                    throw new HandleException(ErrorMessage.NotFound);
                }

                int countError = 0;

                foreach (var gemsReturn in request.GemsReturn)
                {
                    //check gem pick off
                    var gemsPickOff = pickOffs.FirstOrDefault(x => x.Code == gemsReturn.Code);
                    if (gemsPickOff == null)
                    {
                        throw new HandleException(ErrorMessage.NotFound);
                    }
                    //check gem code
                    var gemData = gems.FirstOrDefault(x => x.Code == gemsReturn.Code);
                    if (gemData == null)
                    {
                        throw new HandleException($"{gemsReturn.Code} --> {ErrorMessage.NotFound}");
                    }
                    decimal previousQty = gemData.Quantity;
                    decimal previousQtyWeight = gemData.QuantityWeight;

                    //already outbound
                    var PickOutbounded = pickOutbounds.Where(x => x.Code == gemsReturn.Code);
                    decimal qtyOutbounded = 0;
                    decimal qtyWeightOutbounded = 0;
                    if (PickOutbounded.Any())
                    {
                        qtyOutbounded = PickOutbounded.Sum(x => x.Qty);
                        qtyWeightOutbounded = PickOutbounded.Sum(x => x.QtyWeight);
                    }

                    //sum can be qty returning 
                    decimal checkQty = gemsReturn.ReturnQty;
                    decimal checkQtyWeight = gemsReturn.ReturnQtyWeight;

                    //sum qry already outbound
                    checkQty += qtyOutbounded;
                    checkQtyWeight += qtyWeightOutbounded;

                    //sum qty outbounding
                    if (gemsReturn.GemsOutbound.Any())
                    {
                        checkQty += gemsReturn.GemsOutbound.Sum(x => x.IssueQty);
                        checkQtyWeight += gemsReturn.GemsOutbound.Sum(x => x.IssueQtyWeight);
                    }

                    if (checkQty != gemsPickOff.Qty || checkQtyWeight != gemsPickOff.QtyWeight)
                    {
                        throw new HandleException($"{gemsReturn.Code} --> {ErrorMessage.InvalidQty}");
                    }


                    //outbound transection
                    if (gemsReturn.GemsOutbound.Any())
                    {
                        foreach (var outbound in gemsReturn.GemsOutbound)
                        {
                            var matchPlan = plan.FirstOrDefault(x => x.Wo == outbound.WO && x.WoNumber == outbound.WONumber);
                            if (matchPlan == null)
                            {
                                throw new HandleException(ErrorMessage.NotFound);
                            }

                            var PickOutbound = new TbtStockGemTransection()
                            {
                                Type = 7,
                                Remark1 = request.Remark,

                                Code = gemsReturn.Code,
                                Remark2 = outbound.Remark,

                                PreviousRemainQty = previousQty,
                                PreviousRemianQtyWeight = previousQtyWeight,

                                Qty = outbound.IssueQty,
                                QtyWeight = outbound.IssueQtyWeight,

                                //PointRemianQty = (previousQty -= outbound.IssueQty),
                                //PointRemianQtyWeight = (previousQtyWeight -= outbound.IssueQtyWeight),

                                Stastus = "completed",

                                RequestDate = request.RequestDate.UtcDateTime,
                                CreateBy = CurrentUsername,
                                CreateDate = DateTime.UtcNow,

                                Ref2Running = request.PickOffRunning,

                                ProductionPlanWo = outbound.WO,
                                ProductionPlanWoNumber = outbound.WONumber,
                                ProductionPlanWoText = matchPlan.WoText,
                                ProductionPlanMold = outbound.Mold,

                                OperatorBy = request.OperatorBy,
                            };

                            //previousQty -= outbound.IssueQty;
                            //previousQtyWeight -= outbound.IssueQtyWeight;

                            PickOutbound.PointRemianQty = previousQty;
                            PickOutbound.PointRemianQtyWeight = previousQtyWeight;

                            newTransection.Add(PickOutbound);

                            //cal qty
                            //gemData.Quantity -= outbound.IssueQty;
                            //gemData.QuantityWeight -= outbound.IssueQtyWeight;

                            gemData.QuantityOnProcess -= outbound.IssueQty;
                            gemData.QuantityWeightOnProcess -= outbound.IssueQtyWeight;
                        }
                    }

                    if (request.IsFullReturn)
                    {
                        var pickReturn = new TbtStockGemTransection()
                        {
                            Type = request.Type,

                            Code = gemsReturn.Code,
                            Remark1 = request.Remark,

                            PreviousRemainQty = previousQty,
                            PreviousRemianQtyWeight = previousQtyWeight,

                            Qty = gemsReturn.ReturnQty,
                            QtyWeight = gemsReturn.ReturnQtyWeight,

                            //PointRemianQty = gemData.Quantity,
                            //PointRemianQtyWeight = gemData.QuantityWeight,

                            Stastus = "completed",
                            RequestDate = request.RequestDate.UtcDateTime,
                            CreateBy = CurrentUsername,
                            CreateDate = DateTime.UtcNow,

                            OperatorBy = request.OperatorBy,
                        };
                        newTransection.Add(pickReturn);

                        gemData.Quantity += gemsReturn.ReturnQty;
                        gemData.QuantityWeight += gemsReturn.ReturnQtyWeight;

                        pickReturn.PointRemianQty = gemData.Quantity;
                        pickReturn.PointRemianQtyWeight = gemData.QuantityWeight;

                        gemData.QuantityOnProcess -= gemsReturn.ReturnQty;
                        gemData.QuantityWeightOnProcess -= gemsReturn.ReturnQtyWeight;

                        gemsPickOff.Stastus = "completed";
                        gemsPickOff.UpdateDate = DateTime.UtcNow;
                        gemsPickOff.UpdateBy = CurrentUsername;


                        updateTransection.Add(gemsPickOff);
                        updateTransection.AddRange(pickOutbounds);
                    }

                    gemData.UpdateDate = DateTime.UtcNow;
                    gemData.UpdateBy = CurrentUsername;
                    updateGems.Add(gemData);
                }

                var getOutbound = newTransection.Where(x => x.Type == 7).ToList();
                if (getOutbound.Any())
                {
                    var groupOutbound = getOutbound.GroupBy(x => new { x.ProductionPlanWo, x.ProductionPlanWoNumber }).ToList();
                    foreach (var group in groupOutbound)
                    {
                        var headerId = 0;
                        var matchPlanGroup = plan.FirstOrDefault(x => x.Wo == group.Key.ProductionPlanWo && x.WoNumber == group.Key.ProductionPlanWoNumber);
                        if (matchPlanGroup == null)
                        {
                            throw new HandleException(ErrorMessage.NotFound);
                        }

                        var planGemPick = matchPlanGroup.TbtProductionPlanStatusHeader.FirstOrDefault(x => x.Status == ProductionPlanStatus.Gems);
                        if (planGemPick == null)
                        {
                            var addStatusHeader = new TbtProductionPlanStatusHeader
                            {
                                ProductionPlanId = matchPlanGroup.Id,
                                Status = ProductionPlanStatus.Gems,
                                IsActive = true,

                                SendDate = request.RequestDate.UtcDateTime,
                                CheckDate = request.RequestDate.UtcDateTime,

                                Remark1 = request.Remark,

                                CreateDate = DateTime.UtcNow,
                                CreateBy = CurrentUsername,
                                UpdateDate = DateTime.UtcNow,
                                UpdateBy = CurrentUsername,

                                WagesTotal = 0,
                            };
                            _jewelryContext.TbtProductionPlanStatusHeader.Add(addStatusHeader);
                            await _jewelryContext.SaveChangesAsync();

                            headerId = addStatusHeader.Id;
                            updatePlanHeader.Add(addStatusHeader);
                        }
                        else
                        {
                            headerId = planGemPick.Id;
                            planGemPick.UpdateDate = DateTime.UtcNow;
                            planGemPick.UpdateBy = CurrentUsername;
                        }

                        foreach (var gem in group)
                        {
                            var gemData = gems.FirstOrDefault(x => x.Code == gem.Code);
                            if (gemData == null)
                            {
                                throw new HandleException(ErrorMessage.NotFound);
                            }
                            var newGem = new TbtProductionPlanStatusDetailGem()
                            {
                                HeaderId = headerId,
                                ProductionPlanId = matchPlanGroup.Id,
                                ItemNo = await _runningNumberService.GenerateRunningNumberForGold($"G-{matchPlanGroup.Id}-{ProductionPlanStatus.Gems}"),
                                IsActive = true,

                                GemId = gemData.Id,
                                GemCode = gem.Code,
                                GemQty = gem.Qty,
                                GemWeight = gem.QtyWeight,
                                GemName = $"{gemData.Code}-{gemData.GroupName}-{gemData.Shape}-{gemData.Size}-{gemData.Grade}",
                                GemPrice = gemData.UnitCode == "K" ? gemData.Price : gemData.PriceQty,

                                RequestDate = request.RequestDate.UtcDateTime,
                            };
                            addStatusDetailGem.Add(newGem);

                            if (matchPlanGroup.Status == ProductionPlanStatus.Designed)
                            {
                                matchPlanGroup.Status = ProductionPlanStatus.Gems;
                                matchPlanGroup.UpdateDate = DateTime.UtcNow;
                                matchPlanGroup.UpdateBy = CurrentUsername;
                                updatePlan.Add(matchPlanGroup);
                            }
                        }
                    }
                }

                if (updateGems.Any())
                {
                    _jewelryContext.TbtStockGem.UpdateRange(updateGems);
                }
                if (newTransection.Any())
                {
                    var pickReturn = newTransection.Where(x => x.Type == 6).ToList();
                    if (pickReturn.Any())
                    {
                        runningNoPickReturn = await _runningNumberService.GenerateRunningNumberForGold($"PIR");
                        foreach (var item in pickReturn)
                        {
                            item.Running = runningNoPickReturn;

                            //ref pick off
                            item.RefRunning = request.PickOffRunning;
                        }
                    }

                    var pickOutbount = newTransection.Where(x => x.Type == 7).ToList();
                    if (pickOutbount.Any())
                    {
                        runningNoOutbound = await _runningNumberService.GenerateRunningNumberForGold($"PIO");
                        foreach (var item in pickOutbount)
                        {
                            item.Running = runningNoOutbound;

                            //ref pick return
                            item.RefRunning = runningNoPickReturn ?? string.Empty;

                            //ref pick off
                            item.Ref2Running = request.PickOffRunning;
                        }
                    }

                    _jewelryContext.TbtStockGemTransection.AddRange(newTransection);
                }
                if (updatePlan.Any())
                {
                    _jewelryContext.TbtProductionPlan.UpdateRange(updatePlan);
                }
                if (updatePlanHeader.Any())
                {
                    foreach (var item in updatePlanHeader)
                    {
                        //item.SendName = $"เลขที่เบิก: {runningNoOutbound}";
                        //item.CheckName = $"เลขที่เบิก: {runningNoOutbound}";
                        item.Remark2 = $"เลขที่เบิก: {runningNoOutbound}";
                    }
                    _jewelryContext.TbtProductionPlanStatusHeader.UpdateRange(updatePlanHeader);
                }
                if (addStatusDetailGem.Any())
                {
                    foreach (var item in addStatusDetailGem)
                    {
                        item.OutboundRunning = runningNoOutbound;
                        item.OutboundName = request.OperatorBy;
                        item.OutboundDate = request.RequestDate.UtcDateTime;
                    }
                    _jewelryContext.TbtProductionPlanStatusDetailGem.AddRange(addStatusDetailGem);
                }
                if (updateTransection.Any())
                {
                    foreach (var item in updateTransection)
                    {
                        //pick off
                        if (item.Type == 5)
                        {
                            item.RefRunning = runningNoPickReturn;
                        }
                        if (item.Type == 7)
                        {
                            item.RefRunning = runningNoPickReturn;

                            item.UpdateBy = CurrentUsername;
                            item.UpdateDate = DateTime.UtcNow;
                        }
                    }
                    _jewelryContext.TbtStockGemTransection.UpdateRange(updateTransection);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return new PickReturnResponse()
            {
                RunningPickReturn = runningNoPickReturn ?? string.Empty,
                RunningPickOutbound = runningNoOutbound,
            };
        }
    }
}
