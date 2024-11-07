using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.Product
{
    public class ProductService : IProductService
    {

        private readonly string _admin = "@ADMIN";

        private readonly JewelryContext _jewelryContext;

        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public ProductService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public IQueryable<jewelry.Model.Stock.Product.List.Response> List(jewelry.Model.Stock.Product.List.RequestSearch request)
        {
            var query = (from item in _jewelryContext.TbtStockProduct
                         .Include(x => x.ProductionPlan)
                         .Include(o => o.ProductionPlan.ProductTypeNavigation)
                         //.Include(o => o.ProductionPlan.CustomerTypeNavigation)
                         .Include(x => x.ReceiptNumberNavigation)
                         select new jewelry.Model.Stock.Product.List.Response()
                         {
                             Id = item.ProductionPlan.Id,
                             Wo = item.ProductionPlan.Wo,
                             WoNumber = item.ProductionPlan.WoNumber,

                             ReceiptNumber = item.ReceiptNumber,
                             ReceiptDate = item.ReceiptNumberNavigation.CreateDate,
                             StockNumber = item.Running,

                             ProductNumber = item.ProductionPlan.ProductNumber,
                             ProductTypeName = item.ProductionPlan.ProductTypeNavigation.NameTh,
                             ProductQty = item.ProductionPlan.ProductQty,

                             Mold = item.ProductionPlan.Mold,
                             Gold = item.ProductionPlan.Type,
                             GoldSize = item.ProductionPlan.TypeSize,
                         });

            if (request.RecieptStart.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.RecieptStart.Value.StartOfDayUtc());
            }
            if (request.ReceiptEnd.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.ReceiptEnd.Value.EndOfDayUtc());
            }

            if (!string.IsNullOrEmpty(request.ReceiptNumber))
            {
                query = query.Where(x => x.ReceiptNumber.Contains(request.ReceiptNumber));
            }
            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                query = query.Where(x => x.StockNumber.Contains(request.StockNumber));
            }

            return query;
        }
    }
}
