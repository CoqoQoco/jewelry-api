using jewelry.Model.Sale.SaleDocumentCatalog;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.SaleDocumentCatalog
{
    public interface ISaleDocumentCatalogService
    {
        Task<long> Save(SaveRequest request);
        Task<GetResponse> Get(long id);
        Task<List<ListResponse>> List(ListRequest request);
        Task Delete(long id);
        Task<UploadImageResponse> UploadImage(IFormFile file);
        Task<(Stream stream, string contentType)> GetImage(string blobPath);
    }
}
