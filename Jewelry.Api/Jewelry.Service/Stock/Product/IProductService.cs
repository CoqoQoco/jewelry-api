using jewelry.Model.Stock.Product.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.Product
{
    public interface IProductService
    {
        IQueryable<jewelry.Model.Stock.Product.List.Response> List(jewelry.Model.Stock.Product.List.Search request);
        Task<jewelry.Model.Stock.Product.Get.Response> Get(jewelry.Model.Stock.Product.Get.Request request);
        IQueryable<jewelry.Model.Stock.Product.List.PriceTransection> GetStockCostDetail(string stockNumber);
        Task<string> Update(jewelry.Model.Stock.Product.Update.Request request);
        IQueryable<jewelry.Model.Stock.Product.ListName.Response> ListName(jewelry.Model.Stock.Product.ListName.Request request);

        Task<string> CreateProductCostDeatialPlan(jewelry.Model.Stock.Product.PlanPeoductCost.Request request);
        Task<string> AddProductCostDeatialVersion(jewelry.Model.Stock.Product.AddProductCost.Request request);
        IQueryable<jewelry.Model.Stock.Product.ListProductCost.Response> GetProductCostDetailVersion(string stockNumber);

        // Dashboard APIs
        Task<DashboardResponse> GetProductDashboard(DashboardRequest request);
        Task<TodayReportResponse> GetTodayReport(DashboardRequest request);
        Task<WeeklyReportResponse> GetWeeklyReport(DashboardRequest request);
        Task<MonthlyReportResponse> GetMonthlyReport(DashboardRequest request);
    }
}
