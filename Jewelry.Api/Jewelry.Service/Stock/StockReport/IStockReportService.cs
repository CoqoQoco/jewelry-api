using System.Threading.Tasks;
using Summary = jewelry.Model.Stock.StockReport.Summary;
using ProductGroup = jewelry.Model.Stock.StockReport.ProductGroup;
using Aging = jewelry.Model.Stock.StockReport.Aging;
using AgingItems = jewelry.Model.Stock.StockReport.AgingItems;
using ProductionBalance = jewelry.Model.Stock.StockReport.ProductionBalance;
using DesignAlerts = jewelry.Model.Stock.StockReport.DesignAlerts;

namespace Jewelry.Service.Stock.StockReport
{
    public interface IStockReportService
    {
        Task<Summary.Response> Summary(Summary.Request request);
        Task<ProductGroup.Response> ProductGroup(ProductGroup.Request request);
        Task<Aging.Response> Aging(Aging.Request request);
        Task<AgingItems.Response> AgingItems(AgingItems.Request request);
        Task<ProductionBalance.Response> ProductionBalance(ProductionBalance.Request request);
        Task<DesignAlerts.Response> DesignAlerts(DesignAlerts.Request request);
    }
}
