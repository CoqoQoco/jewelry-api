using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.Receipt.Gem.Create;
using jewelry.Model.Receipt.Gem.Inbound;
using jewelry.Model.Receipt.Gem.List;
using jewelry.Model.Receipt.Gem.Outbound;
using jewelry.Model.Receipt.Gem.Scan;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Index.HPRtree;
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
                         });

            if (!string.IsNullOrEmpty(request.Status))
            {
                query = (from item in query
                         where item.Status == request.Status
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
    }
}
