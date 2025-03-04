using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Gem.Option;
using jewelry.Model.Stock.Gem.Price;
using jewelry.Model.Stock.Gem.PriceEdit;
using jewelry.Model.Stock.Gem.Search;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Index.HPRtree;
using NPOI.HSSF.Record;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Stock
{
    public interface IStockGemService
    {
        List<SearchGemResponse> SearchGem(SearchGem request);
        IQueryable<SearchGemResponse> SearchGemData(SearchGem request);
        IQueryable<OptionResponse> GroupGemData(OptionRequest request);

        Task<string> Price(PriceEditRequest request);
        IQueryable<TbtStockGemTransectionPrice> PriceHistory(Price request);
    }
    public class StockGemService : IStockGemService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly bool _valPass = false;
        public StockGemService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
        }

        public List<SearchGemResponse> SearchGem(SearchGem request)
        {
            var query = (from item in _jewelryContext.TbtStockGem
                         select new SearchGemResponse()
                         {
                             Id = item.Id,
                             Name = $"{item.Code}-{item.GroupName}-{item.Shape}-{item.Size}-{item.Grade}",
                             Code = item.Code,

                             Price = item.Price,
                             PriceQty = item.PriceQty,
                             Unit = item.Unit,

                         }).ToList();

            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query
                         where item.Name.Contains(request.Text)
                         select item).ToList();
            }
            if (request.Id.HasValue)
            {
                query = (from item in query
                         where item.Id == request.Id.Value
                         select item).ToList();
            }

            return query;
        }

        public IQueryable<SearchGemResponse> SearchGemData(SearchGem request)
        {
            var query = (from item in _jewelryContext.TbtStockGem
                         select item).AsNoTracking();
           
            if (request.Id.HasValue)
            {
                query = (from item in query
                         where item.Id == request.Id.Value
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

            if (request.TypeCheck != null && request.TypeCheck.Length > 0)
            {
                var typeCheckLower = request.TypeCheck.Select(tc => tc.ToLower()).ToArray();

                if (typeCheckLower.Contains("qty-remain"))
                {
                    query = query.Where(item => item.Quantity > 0);
                }

                if (typeCheckLower.Contains("qty-process-remain"))
                {
                    query = query.Where(item => item.QuantityOnProcess > 0);
                }

                if (typeCheckLower.Contains("qty-weight-remain"))
                {
                    query = query.Where(item => item.QuantityWeight > 0);
                }

                if (typeCheckLower.Contains("qty-weight-process-remain"))
                {
                    query = query.Where(item => item.QuantityWeightOnProcess > 0);
                }
            }

            var response = (from item in query
                            select new SearchGemResponse()
                            {
                                Id = item.Id,
                                Name = $"{item.Code}-{item.Shape}-{item.Size}-{item.Grade}",
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
                            });

            return response;
        }
        public IQueryable<OptionResponse> GroupGemData(OptionRequest request)
        {
            var result = new List<OptionResponse>().AsQueryable();

            var query = (from item in _jewelryContext.TbtStockGem
                         select item);

            if (request.Type == "GROUPGEM")
            {
                result = (from item in query
                          group item by item.GroupName into g
                          select new OptionResponse()
                          {
                              Value = g.Key,
                          });
            }
            if (request.Type == "GRADE")
            {
                result = (from item in query
                          group item by item.Grade into g
                          select new OptionResponse()
                          {
                              Value = g.Key,
                          });
            }
            if (request.Type == "SHAPE")
            {
                result = (from item in query
                          group item by item.Shape into g
                          select new OptionResponse()
                          {
                              Value = g.Key,
                          });
            }
            if (request.Type == "SIZE")
            {
                result = (from item in query
                          group item by item.Size into g
                          select new OptionResponse()
                          {
                              Value = g.Key,
                          });
            }

            if (request.Value != null && request.Value.Length > 0)
            {
                result = (from item in result
                          where request.Value.Contains(item.Value)
                          select item);
            }

            return result.OrderBy(x => x.Value);
        }

        public async Task<string> Price(PriceEditRequest request)
        {
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

            var gem = (from _gem in _jewelryContext.TbtStockGem
                       where request.Code == _gem.Code && _gem.Id == request.Id
                       select _gem).FirstOrDefault();

            if (gem == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var priceTtransection = new TbtStockGemTransectionPrice()
                {
                    Code = gem.Code,

                    PreviousPrice = gem.Price,
                    NewPrice = request.NewPrice,

                    PreviousPriceUnit = gem.PriceQty,
                    NewPriceUnit = request.NewPriceUnit,

                    Unit = request.Unit,
                    UnitCode = request.UnitCode,

                    Remark = request.Pass,

                    CreateBy = _admin,
                    CreateDate = DateTime.UtcNow
                };

                gem.Price = request.NewPrice;
                gem.PriceQty = request.NewPriceUnit;

                gem.Unit = request.Unit;
                gem.UnitCode = request.UnitCode;

                gem.UpdateBy = _admin;
                gem.UpdateDate = DateTime.UtcNow;

                _jewelryContext.TbtStockGemTransectionPrice.Add(priceTtransection);
                _jewelryContext.TbtStockGem.Update(gem);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            return "success";
        }
        public IQueryable<TbtStockGemTransectionPrice> PriceHistory(Price request)
        {
            var query = (from item in _jewelryContext.TbtStockGemTransectionPrice
                         where item.Code == request.Code
                         select item);

            return query.OrderByDescending(x => x.CreateDate);
        }

    }
}
