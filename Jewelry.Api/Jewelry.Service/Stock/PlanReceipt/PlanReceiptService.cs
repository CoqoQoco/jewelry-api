using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.PlanReceipt
{
    public  class PlanReceiptService : IPlanReceiptService
    {
        private readonly string _admin = "@ADMIN";

        private readonly JewelryContext _jewelryContext;

        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public PlanReceiptService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }


        public IQueryable<jewelry.Model.Stock.Product.Plan.Receipt.List.Response> List(jewelry.Model.Stock.Product.Plan.Receipt.List.RequestSearch request)
        {
            var query = (from item in _jewelryContext.TbtStockProductReceiptPlan

                         join plan in _jewelryContext.TbtProductionPlan
                         .Include(o => o.ProductTypeNavigation)
                         .Include(o => o.CustomerTypeNavigation)
                         on item.ProductionPlanId equals plan.Id
                         //.Include(x => x.ProductionPlan)
                         //.Include(o => o.ProductionPlan.ProductTypeNavigation)
                         //.Include(o => o.ProductionPlan.CustomerTypeNavigation)
                         select new jewelry.Model.Stock.Product.Plan.Receipt.List.Response()
                         {
                             Id = plan.Id,
                             Wo = plan.Wo,
                             WoNumber = plan.WoNumber,
                             WoText = plan.WoText,

                             ReceiptNumber = item.Running,
                             ReceiptDate = item.CreateDate,

                             ProductNumber = plan.ProductNumber,
                             ProductTypeName = plan.ProductTypeNavigation.NameTh,
                             ProductType = plan.ProductType,
                             ProductQty = plan.ProductQty,

                             Mold = plan.Mold,
                             Gold = plan.Type,
                             GoldSize = plan.TypeSize,
                         });

            if (!string.IsNullOrEmpty(request.Running))
            {
                query = query.Where(x => x.ReceiptNumber.Contains(request.Running));
            }

            return query;
        }
    }
}
