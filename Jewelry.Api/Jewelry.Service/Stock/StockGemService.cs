using jewelry.Model.Stock.Gem.Option;
using jewelry.Model.Stock.Gem.Search;
using Jewelry.Data.Context;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Index.HPRtree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock
{
    public interface IStockGemService
    {
        List<SearchGemResponse> SearchGem(SearchGem request);
        IQueryable<SearchGemResponse> SearchGemData(SearchGem request);
        IQueryable<OptionResponse> GroupGemData(OptionRequest request);
    }
    public class StockGemService : IStockGemService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
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
                             Price = item.Price
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

            return query.AsQueryable();
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

    }
}
