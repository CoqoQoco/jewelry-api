using jewelry.Model.Stock.Gem;
using Jewelry.Data.Context;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock
{
    public interface IGemStockService
    {
        List<SearchGemResponse> SearchGem(SearchGem request);
        IQueryable<SearchGemResponse> SearchGemData(SearchGem request);
    }
    public class GemStockService : IGemStockService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        public GemStockService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
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
                             Name = $"{item.Code}-{item.Description}-{item.Shape}-{item.SizeGem}-{item.Grade}",
                             Code = item.Code,
                             Price = item.Price.HasValue ? item.Price.Value : 0
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
                             Name = $"{item.Code}-{item.Description}-{item.Shape}-{item.SizeGem}-{item.Grade}",
                             Code = item.Code,
                             Description = item.Description,

                             Size = item.SizeGem,
                             Shape = item.Shape,
                             Grade = item.Grade,
                             Price = item.Price.HasValue ? item.Price.Value : 0
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

            return query.AsQueryable();
        }
    }
}
