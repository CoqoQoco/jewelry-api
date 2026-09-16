using Kendo.DynamicLinqCore;

namespace Jewelry.Service.Stock.StockConvert;

public interface IStockConvertService
{
    Task<jewelry.Model.Stock.StockConvert.Create.Response> Create(jewelry.Model.Stock.StockConvert.Create.Request request);
    Task<jewelry.Model.Stock.StockConvert.Complete.Response> Complete(jewelry.Model.Stock.StockConvert.Complete.Request request);
    Task<jewelry.Model.Stock.StockConvert.Cancel.Response> Cancel(jewelry.Model.Stock.StockConvert.Cancel.Request request);
    Task<DataSourceResult> List(jewelry.Model.Stock.StockConvert.List.Request request);
    Task<jewelry.Model.Stock.StockConvert.Get.Response> Get(jewelry.Model.Stock.StockConvert.Get.Request request);
    Task<List<jewelry.Model.Stock.StockConvert.PendingForSaleOrder.Response>> PendingForSaleOrder(jewelry.Model.Stock.StockConvert.PendingForSaleOrder.Request request);
}
