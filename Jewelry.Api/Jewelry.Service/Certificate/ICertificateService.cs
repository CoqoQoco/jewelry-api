using System.Threading.Tasks;

namespace Jewelry.Service.Certificate
{
    public interface ICertificateService
    {
        Task<jewelry.Model.Certificate.UploadImage.Response> UploadImage(jewelry.Model.Certificate.UploadImage.Request request);

        Task<jewelry.Model.Certificate.CustomerBrand.Response> GetCustomerBrand(string? customerCode);

        Task<jewelry.Model.Certificate.Create.Response> Create(jewelry.Model.Certificate.Create.Request request);

        Task<jewelry.Model.Certificate.List.Response> List(jewelry.Model.Certificate.List.Request request);
    }
}
