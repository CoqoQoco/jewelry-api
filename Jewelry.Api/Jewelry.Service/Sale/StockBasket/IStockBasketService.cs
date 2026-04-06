namespace Jewelry.Service.Sale.StockBasket;

public interface IStockBasketService
{
    Task<string> Upsert(jewelry.Model.Sale.StockBasket.Create.Request request);
    Task<jewelry.Model.Sale.StockBasket.Get.Response> Get(jewelry.Model.Sale.StockBasket.Get.Request request);
    Task<(IList<jewelry.Model.Sale.StockBasket.List.Response> Data, int Total)> List(jewelry.Model.Sale.StockBasket.List.Request request);
    Task<string> GenerateNumber();
    Task<jewelry.Model.Sale.StockBasket.AddItems.Response> AddItems(jewelry.Model.Sale.StockBasket.AddItems.Request request);
    Task RemoveItem(jewelry.Model.Sale.StockBasket.RemoveItem.Request request);
    Task SubmitApproval(jewelry.Model.Sale.StockBasket.UpdateStatus.Request request);
    Task Approve(jewelry.Model.Sale.StockBasket.UpdateStatus.Request request);
    Task Checkout(jewelry.Model.Sale.StockBasket.UpdateStatus.Request request);
}
