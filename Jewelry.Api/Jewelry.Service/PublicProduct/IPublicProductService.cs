namespace Jewelry.Service.PublicProduct
{
    public interface IPublicProductService
    {
        jewelry.Model.PublicProduct.Get.Response Get(string token);
        jewelry.Model.PublicProduct.Link.Response CreateLink(string stockNumber);
    }
}
