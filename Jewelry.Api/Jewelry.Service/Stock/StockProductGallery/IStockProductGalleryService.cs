namespace Jewelry.Service.Stock.StockProductGallery;

public interface IStockProductGalleryService
{
    Task<jewelry.Model.Stock.StockProductGallery.Get.Response> Get(jewelry.Model.Stock.StockProductGallery.Get.Request request);
    Task<jewelry.Model.Stock.StockProductGallery.Item> Upload(jewelry.Model.Stock.StockProductGallery.Upload.Request request);
    Task Reorder(jewelry.Model.Stock.StockProductGallery.Reorder.Request request);
    Task Delete(jewelry.Model.Stock.StockProductGallery.Delete.Request request);
}
