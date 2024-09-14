﻿using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.Receipt.Gem.Create;
using jewelry.Model.Receipt.Gem.Inbound;
using jewelry.Model.Receipt.Gem.List;
using jewelry.Model.Receipt.Gem.Outbound;
using jewelry.Model.Receipt.Gem.Picklist;
using jewelry.Model.Receipt.Gem.PickOff;
using jewelry.Model.Receipt.Gem.Return;
using jewelry.Model.Receipt.Gem.Scan;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using NetTopologySuite.Index.HPRtree;
using NetTopologySuite.Triangulate.QuadEdge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Receipt.Gem
{
    public interface IReceiptAndIssueStockGemService
    {
        Task<string> CreateGem(CreateRequest request);
        Task<IQueryable<ScanResponse>> Scan(ScanRequest request);

        IQueryable<ListResponse> ListTransection(ListSearch request);
        Task<string> InboundGem(InboundRequest request);
        Task<string> OutboundGem(OutboundRequest request);

        IQueryable<PicklistResponse> Picklist(PicklistFilter request);
        Task<string> PickOffGem(PickOffRequest request);
        Task<PickReturnResponse> PickReturnGem(PickReturnRequest request);
    }
    public class ReceiptAndIssueStockGemService : IReceiptAndIssueStockGemService
    {
        private readonly string _admin = "@ADMIN";
        private readonly bool _valPass = true;
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public ReceiptAndIssueStockGemService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService)
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
                CreateBy = _admin,

                Remark1 = request.Remark.Trim(),

                Price = 0,
                PriceQty = 0,
                Quantity = 0,
            };
            _jewelryContext.TbtStockGem.Add(newGem);
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
                         where tran.RequestDate >= request.RequestDateStart.StartOfDayUtc()
                         && tran.RequestDate <= request.RequestDateEnd.EndOfDayUtc()
                         join gem in _jewelryContext.TbtStockGem on tran.Code equals gem.Code
                         select new ListResponse()
                         {
                             RequestDate = tran.RequestDate,
                             Running = tran.Running,
                             RefRunning = tran.RefRunning,
                             Code = tran.Code,

                             Type = tran.Type,
                             JobOrPo = tran.JobOrPo,
                             SubpplierName = tran.SubpplierName,
                             Remark1 = tran.Remark1,

                             Qty = tran.Qty,
                             QtyWeight = tran.QtyWeight,

                             SupplierCost = tran.SupplierCost,
                             Remark2 = tran.Remark2,

                             CreateDate = tran.CreateDate,
                             CreateBy = tran.CreateBy,
                             UpdateDate = tran.UpdateDate,
                             UpdateBy = tran.UpdateBy,

                             Name = $"{gem.Code}-{gem.Shape}-{gem.Size}-{gem.Grade}",
                             GroupName = gem.GroupName,
                             Size = gem.Size,
                             Shape = gem.Shape,
                             Grade = gem.Grade,

                             Status = tran.Stastus,

                             WO = tran.ProductionPlanWo,
                             WONumber = tran.ProductionPlanWoNumber,
                             WOText = tran.ProductionPlanWoText,
                             Mold = tran.ProductionPlanMold,
                         });

            //if (!string.IsNullOrEmpty(request.Status))
            //{
            //    query = (from item in query
            //             where item.Status == request.Status
            //             select item);
            //}
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

                    gemData.Quantity += gem.ReceiveQty;
                    gemData.QuantityWeight += gem.ReceiveQtyWeight;

                    gemData.UpdateDate = DateTime.UtcNow;
                    gemData.UpdateBy = _admin;
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

                        Stastus = "completed",

                        RequestDate = request.RequestDate.UtcDateTime,
                        CreateBy = _admin,
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
                    runningNo = await _runningNumberService.GenerateRunningNumber($"INB");
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

                    gemData.Quantity -= gem.IssueQty;
                    gemData.QuantityWeight -= gem.IssueQtyWeight;

                    gemData.UpdateDate = DateTime.UtcNow;
                    gemData.UpdateBy = _admin;
                    UpdateGems.Add(gemData);

                    var newInbound = new TbtStockGemTransection()
                    {
                        Type = request.Type,
                        Remark1 = request.Remark,

                        Code = gem.Code,
                        Remark2 = gem.Remark,

                        Qty = gem.IssueQty,
                        QtyWeight = gem.IssueQtyWeight,

                        Stastus = "completed",

                        RequestDate = request.RequestDate.UtcDateTime,
                        CreateBy = _admin,
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
                    runningNo = await _runningNumberService.GenerateRunningNumber($"OUT");
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
                             Items = grouped.Select(g => new PicklistItem
                             {
                                 Code = g.item.Code,
                                 GroupName = g.item.Code,
                                 Name = $"{g.item.Code}-{g.gem.Shape}-{g.gem.Size}-{g.gem.Grade}",
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
                             })
                         });

            if (!string.IsNullOrEmpty(request.Running))
            {
                query = (from item in query
                         where item.Running.Contains(request.Running)
                         select item);
            }
            if (request.Type != null && request.Type.Any())
            {
                query = (from item in query
                         where request.Type.Contains(item.Type)
                         select item);
            }
            if (request.Status != null && request.Status.Any())
            {
                query = (from item in query
                         where request.Status.Contains(item.Stastus)
                         select item);
            }

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

            if (request.ReturnDateStart.HasValue)
            {
                query = (from item in query
                         where item.ReturnDate >= request.ReturnDateStart.Value.StartOfDayUtc()
                         select item);
            }
            if (request.ReturnDateEnd.HasValue)
            {
                query = (from item in query
                         where item.ReturnDate <= request.ReturnDateEnd.Value.EndOfDayUtc()
                         select item);
            }

            if (!string.IsNullOrEmpty(request.GetRunning))
            {
                query = (from item in query
                         where item.Running == request.GetRunning
                         select item);
            }

            return query;
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

                    gemData.Quantity -= gem.IssueQty;
                    gemData.QuantityWeight -= gem.IssueQtyWeight;

                    gemData.QuantityOnProcess += gem.IssueQty;
                    gemData.QuantityWeightOnProcess += gem.IssueQtyWeight;

                    gemData.UpdateDate = DateTime.UtcNow;
                    gemData.UpdateBy = _admin;
                    UpdateGems.Add(gemData);

                    var newInbound = new TbtStockGemTransection()
                    {
                        Type = request.Type,
                        Remark1 = request.Remark,

                        Code = gem.Code,
                        Remark2 = gem.Remark,

                        Qty = gem.IssueQty,
                        QtyWeight = gem.IssueQtyWeight,

                        Stastus = "process",

                        RequestDate = request.RequestDate.UtcDateTime,
                        ReturnDate = request.ReturnDate.UtcDateTime,

                        CreateBy = _admin,
                        CreateDate = DateTime.UtcNow,

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
                    runningNo = await _runningNumberService.GenerateRunningNumber($"PIF");
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
            var updatePlan = new List<TbtProductionPlan>();
            var updatePlanHeader = new List<TbtProductionPlanStatusHeader>();
            var addStatusDetailGem = new List<TbtProductionPlanStatusDetailGem>();
            var updateTransection = new List<TbtStockGemTransection>();

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

                var pickOffs = (from item in _jewelryContext.TbtStockGemTransection
                                    where item.Running == request.ReferenceRunning
                                    select item);

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

                foreach (var gemsReturn in request.GemsReturn)
                {
                    var gemData = gems.FirstOrDefault(x => x.Code == gemsReturn.Code);
                    if (gemData == null)
                    {
                        throw new HandleException($"{gemsReturn.Code} --> {ErrorMessage.NotFound}");
                    }

                    decimal checkQty = gemsReturn.ReturnQty;
                    decimal checkQtyWeight = gemsReturn.ReturnQtyWeight;

                    var gemsPickOff = pickOffs.FirstOrDefault(x => x.Code == gemsReturn.Code);
                    if (gemsPickOff == null)
                    {
                        throw new HandleException(ErrorMessage.NotFound);
                    }

                    if (gemsReturn.GemsOutbound.Any())
                    {
                        checkQty += gemsReturn.GemsOutbound.Sum(x => x.IssueQty);
                        checkQtyWeight += gemsReturn.GemsOutbound.Sum(x => x.IssueQtyWeight);
                    }

                    if (checkQty != gemsPickOff.Qty || checkQtyWeight != gemsPickOff.QtyWeight)
                    {
                        throw new HandleException(ErrorMessage.InvalidQty);
                    }

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

                                Qty = outbound.IssueQty,
                                QtyWeight = outbound.IssueQtyWeight,

                                Stastus = "completed",

                                RequestDate = request.RequestDate.UtcDateTime,
                                CreateBy = _admin,
                                CreateDate = DateTime.UtcNow,

                                ProductionPlanWo = outbound.WO,
                                ProductionPlanWoNumber = outbound.WONumber,
                                ProductionPlanWoText = matchPlan.WoText,
                                ProductionPlanMold = outbound.Mold,
                            };
                            newTransection.Add(PickOutbound);
                        }
                    }

                    var pickReturn = new TbtStockGemTransection()
                    {
                        Type = request.Type,

                        Code = gemsReturn.Code,
                        Remark1 = request.Remark,

                        Qty = gemsReturn.ReturnQty,
                        QtyWeight = gemsReturn.ReturnQtyWeight,

                        Stastus = "completed",
                        RequestDate = request.RequestDate.UtcDateTime,
                        CreateBy = _admin,
                        CreateDate = DateTime.UtcNow,
                    };
                    newTransection.Add(pickReturn);

                    gemData.Quantity += gemsReturn.ReturnQty;
                    gemData.QuantityOnProcess -= gemsPickOff.Qty;
                    gemData.QuantityWeight += gemsReturn.ReturnQtyWeight;
                    gemData.QuantityWeightOnProcess -= gemsPickOff.QtyWeight;

                    gemData.UpdateDate = DateTime.UtcNow;
                    gemData.UpdateBy = _admin;
                    updateGems.Add(gemData);

                    gemsPickOff.Stastus = "completed";
                    gemsPickOff.UpdateDate = DateTime.UtcNow;
                    gemsPickOff.UpdateBy = _admin;
                    updateTransection.Add(gemsPickOff);

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

                        var planGemPick = matchPlanGroup.TbtProductionPlanStatusHeader.FirstOrDefault(x => x.Status == ProductionPlanStatus.GemPick);
                        if (planGemPick == null)
                        {
                            var addStatusHeader = new TbtProductionPlanStatusHeader
                            {
                                ProductionPlanId = matchPlanGroup.Id,
                                Status = ProductionPlanStatus.GemPick,
                                IsActive = true,

                                SendDate = request.RequestDate.UtcDateTime,
                                CheckDate = request.RequestDate.UtcDateTime,

                                Remark1 = request.Remark,

                                CreateDate = DateTime.UtcNow,
                                CreateBy = _admin,
                                UpdateDate = DateTime.UtcNow,
                                UpdateBy = _admin,

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
                            planGemPick.UpdateBy = _admin;
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
                                ItemNo = await _runningNumberService.GenerateRunningNumber($"G-{matchPlanGroup.Id}-{ProductionPlanStatus.GemPick}"),
                                IsActive = true,

                                GemId = gemData.Id,
                                GemCode = gem.Code,
                                GemQty = gem.Qty,
                                GemWeight = gem.QtyWeight,
                                GemName = $"{gemData.Code}-{gemData.GroupName}-{gemData.Shape}-{gemData.Size}-{gemData.Grade}",
                               
                                RequestDate = request.RequestDate.UtcDateTime,
                            };
                            addStatusDetailGem.Add(newGem);

                            if (matchPlanGroup.Status == ProductionPlanStatus.Designed)
                            {
                                matchPlanGroup.Status = ProductionPlanStatus.GemPick;
                                matchPlanGroup.UpdateDate = DateTime.UtcNow;
                                matchPlanGroup.UpdateBy = _admin;
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
                    //set all running number
                    runningNoPickReturn = await _runningNumberService.GenerateRunningNumber($"PIR");
                    runningNoOutbound = await _runningNumberService.GenerateRunningNumber($"PIO");
                    foreach (var item in newTransection)
                    {
                        if (item.Type == 6)
                        {
                            item.Running = runningNoPickReturn;
                            item.RefRunning = request.ReferenceRunning;
                        }
                        else if (item.Type == 7)
                        {
                            item.Running = runningNoOutbound;
                            item.RefRunning = runningNoPickReturn;
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
                        item.SendName = $"เลขที่เบิก: {runningNoOutbound}";
                        item.CheckName = $"เลขที่เบิก: {runningNoOutbound}";
                        item.Remark2 = $"เลขที่เบิก: {runningNoOutbound}";
                    }
                    _jewelryContext.TbtProductionPlanStatusHeader.UpdateRange(updatePlanHeader);
                }
                if (addStatusDetailGem.Any())
                { 
                    foreach (var item in addStatusDetailGem)
                    {
                        //item.re = request.RequestDate.UtcDateTime;
                        item.Remark = $"เลขที่เบิก: {runningNoOutbound}";
                    }
                    _jewelryContext.TbtProductionPlanStatusDetailGem.AddRange(addStatusDetailGem);
                }
                if (updateTransection.Any())
                {
                    foreach (var item in updateTransection)
                    { 
                        item.RefRunning = runningNoPickReturn;
                    }
                    _jewelryContext.TbtStockGemTransection.UpdateRange(updateTransection);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return new PickReturnResponse()
            {
                RunningPickReturn = runningNoPickReturn,
                RunningPickOutbound = runningNoOutbound,
            };
        }
    }
}
