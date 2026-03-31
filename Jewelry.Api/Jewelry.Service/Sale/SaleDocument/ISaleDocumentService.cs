using jewelry.Model.Sale.SaleDocument;

namespace Jewelry.Service.Sale.SaleDocument
{
    public interface ISaleDocumentService
    {
        Task<int> Upload(UploadRequest request);
        Task<List<ListResponse>> List(ListRequest request);
        Task<(Stream stream, string fileName, string contentType)> Download(int id);
        Task UpdateTag(UpdateTagRequest request);
        Task Delete(int id);
    }
}
