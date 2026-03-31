using jewelry.Model.Exceptions;
using jewelry.Model.Sale.SaleDocument;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Sale.SaleDocument
{
    public class SaleDocumentService : BaseService, ISaleDocumentService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IAzureBlobStorageService _azureBlobService;

        public SaleDocumentService(
            JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor,
            IAzureBlobStorageService azureBlobService)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _azureBlobService = azureBlobService;
        }

        public async Task<int> Upload(UploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                throw new HandleException("กรุณาเลือกไฟล์ PDF");

            if (!request.File.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                throw new HandleException("รองรับเฉพาะไฟล์ PDF เท่านั้น");

            if (request.DocumentMonth < 1 || request.DocumentMonth > 12)
                throw new HandleException("เดือนไม่ถูกต้อง (1-12)");

            if (request.DocumentYear < 2000)
                throw new HandleException("ปีไม่ถูกต้อง");

            var blobFileName = $"{Guid.NewGuid()}_{request.File.FileName}";
            var folderName = $"Sale/Document/{request.DocumentYear}/{request.DocumentMonth:D2}";

            using var stream = request.File.OpenReadStream();
            await _azureBlobService.UploadImageAsync(stream, folderName, blobFileName);

            var document = new TbtSaleDocument
            {
                FileName = request.File.FileName,
                BlobPath = $"{folderName}/{blobFileName}",
                DocumentMonth = request.DocumentMonth,
                DocumentYear = request.DocumentYear,
                Tags = request.Tags,
                Remark = request.Remark,
                IsActive = true,
                CreateBy = CurrentUsername ?? "system",
                CreateDate = DateTime.UtcNow
            };

            _jewelryContext.TbtSaleDocument.Add(document);
            await _jewelryContext.SaveChangesAsync();

            return document.Id;
        }

        public async Task<List<ListResponse>> List(ListRequest request)
        {
            var query = _jewelryContext.TbtSaleDocument
                .Where(x => x.IsActive);

            if (request.DocumentMonth.HasValue)
                query = query.Where(x => x.DocumentMonth == request.DocumentMonth.Value);

            if (request.DocumentYear.HasValue)
                query = query.Where(x => x.DocumentYear == request.DocumentYear.Value);

            var items = await query
                .OrderByDescending(x => x.CreateDate)
                .Select(x => new ListResponse
                {
                    Id = x.Id,
                    FileName = x.FileName,
                    BlobPath = x.BlobPath,
                    DocumentMonth = x.DocumentMonth,
                    DocumentYear = x.DocumentYear,
                    Tags = x.Tags,
                    Remark = x.Remark,
                    CreateBy = x.CreateBy,
                    CreateDate = x.CreateDate
                })
                .ToListAsync();

            return items;
        }

        public async Task<(Stream stream, string fileName, string contentType)> Download(int id)
        {
            var document = await _jewelryContext.TbtSaleDocument
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (document == null)
                throw new HandleException("ไม่พบเอกสาร");

            // BlobPath = "Sale/Document/{year}/{month}/{blobFileName}"
            var parts = document.BlobPath.Split('/');
            var fileName = parts.Last();
            var folderName = string.Join("/", parts.Take(parts.Length - 1));

            var stream = await _azureBlobService.DownloadImageAsync(folderName, fileName);

            return (stream, document.FileName, "application/pdf");
        }

        public async Task UpdateTag(UpdateTagRequest request)
        {
            var document = await _jewelryContext.TbtSaleDocument
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.IsActive);

            if (document == null)
                throw new HandleException("ไม่พบเอกสาร");

            document.Tags = request.Tags;
            document.Remark = request.Remark;
            document.UpdateBy = CurrentUsername ?? "system";
            document.UpdateDate = DateTime.UtcNow;

            _jewelryContext.TbtSaleDocument.Update(document);
            await _jewelryContext.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var document = await _jewelryContext.TbtSaleDocument
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (document == null)
                throw new HandleException("ไม่พบเอกสาร");

            document.IsActive = false;
            document.UpdateBy = CurrentUsername ?? "system";
            document.UpdateDate = DateTime.UtcNow;

            _jewelryContext.TbtSaleDocument.Update(document);
            await _jewelryContext.SaveChangesAsync();
        }
    }
}
