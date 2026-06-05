using System.Threading.Tasks;

namespace Jewelry.Service.Catalog
{
    public interface ICatalogService
    {
        jewelry.Model.Catalog.List.Response List(jewelry.Model.Catalog.List.Request request);
        jewelry.Model.Catalog.Get.Response Get(jewelry.Model.Catalog.Get.Request request);
        Task<string> Create(jewelry.Model.Catalog.Create.Request request);
        Task<string> Update(jewelry.Model.Catalog.Update.Request request);
        Task<string> Delete(jewelry.Model.Catalog.Delete.Request request);
        Task<string> AddProducts(jewelry.Model.Catalog.AddProducts.Request request);
        Task<string> RemoveProduct(jewelry.Model.Catalog.RemoveProduct.Request request);
    }
}
