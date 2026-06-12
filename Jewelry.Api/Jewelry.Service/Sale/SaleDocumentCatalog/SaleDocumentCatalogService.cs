using jewelry.Model.Exceptions;
using jewelry.Model.Sale.SaleDocumentCatalog;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.SaleDocumentCatalog
{
    public class SaleDocumentCatalogService : BaseService, ISaleDocumentCatalogService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IAzureBlobStorageService _azureBlobService;

        public SaleDocumentCatalogService(
            JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor,
            IAzureBlobStorageService azureBlobService)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _azureBlobService = azureBlobService;
        }

        public async Task<long> Save(SaveRequest request)
        {
            var statusName = request.Status == 1 ? "Final" : "Draft";

            if (request.Id.HasValue)
            {
                var catalog = await _jewelryContext.TbtSaleDocumentCatalog
                    .Include(c => c.TbtSaleDocumentCatalogItem)
                        .ThenInclude(i => i.TbtSaleDocumentCatalogItemImage)
                    .FirstOrDefaultAsync(c => c.Id == request.Id.Value && c.IsActive);

                if (catalog == null)
                    throw new HandleException("ไม่พบเอกสาร");

                catalog.HeaderLabel = request.HeaderLabel;
                catalog.CollectionTitle = request.CollectionTitle;
                catalog.DocumentMonth = request.DocumentMonth;
                catalog.DocumentYear = request.DocumentYear;
                catalog.Tags = request.Tags;
                catalog.Remark = request.Remark;
                catalog.Status = request.Status;
                catalog.StatusName = statusName;
                catalog.UpdateBy = CurrentUsername ?? "system";
                catalog.UpdateDate = DateTime.UtcNow;

                var existingImages = catalog.TbtSaleDocumentCatalogItem
                    .SelectMany(i => i.TbtSaleDocumentCatalogItemImage)
                    .ToList();
                _jewelryContext.TbtSaleDocumentCatalogItemImage.RemoveRange(existingImages);

                var existingItems = catalog.TbtSaleDocumentCatalogItem.ToList();
                _jewelryContext.TbtSaleDocumentCatalogItem.RemoveRange(existingItems);

                _jewelryContext.TbtSaleDocumentCatalog.Update(catalog);
                await _jewelryContext.SaveChangesAsync();

                var newItems = BuildItems(catalog.Id, request.Items);
                _jewelryContext.TbtSaleDocumentCatalogItem.AddRange(newItems);
                await _jewelryContext.SaveChangesAsync();

                return catalog.Id;
            }
            else
            {
                var catalog = new TbtSaleDocumentCatalog
                {
                    HeaderLabel = request.HeaderLabel,
                    CollectionTitle = request.CollectionTitle,
                    DocumentMonth = request.DocumentMonth,
                    DocumentYear = request.DocumentYear,
                    Tags = request.Tags,
                    Remark = request.Remark,
                    Status = request.Status,
                    StatusName = statusName,
                    IsActive = true,
                    CreateBy = CurrentUsername ?? "system",
                    CreateDate = DateTime.UtcNow
                };

                _jewelryContext.TbtSaleDocumentCatalog.Add(catalog);
                await _jewelryContext.SaveChangesAsync();

                var newItems = BuildItems(catalog.Id, request.Items);
                _jewelryContext.TbtSaleDocumentCatalogItem.AddRange(newItems);
                await _jewelryContext.SaveChangesAsync();

                return catalog.Id;
            }
        }

        public async Task<GetResponse> Get(long id)
        {
            var catalog = await _jewelryContext.TbtSaleDocumentCatalog
                .Include(c => c.TbtSaleDocumentCatalogItem.OrderBy(i => i.SortOrder))
                    .ThenInclude(i => i.TbtSaleDocumentCatalogItemImage.OrderBy(img => img.SortOrder))
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (catalog == null)
                throw new HandleException("ไม่พบเอกสาร");

            return new GetResponse
            {
                Id = catalog.Id,
                HeaderLabel = catalog.HeaderLabel,
                CollectionTitle = catalog.CollectionTitle,
                DocumentMonth = catalog.DocumentMonth,
                DocumentYear = catalog.DocumentYear,
                Tags = catalog.Tags,
                Remark = catalog.Remark,
                Status = catalog.Status,
                StatusName = catalog.StatusName,
                CreateBy = catalog.CreateBy,
                CreateDate = catalog.CreateDate,
                UpdateBy = catalog.UpdateBy,
                UpdateDate = catalog.UpdateDate,
                Items = catalog.TbtSaleDocumentCatalogItem.Select(i => new GetItemResponse
                {
                    Id = i.Id,
                    ProductNumber = i.ProductNumber,
                    DescriptionLine1 = i.DescriptionLine1,
                    DescriptionLine2 = i.DescriptionLine2,
                    Dimension1 = i.Dimension1,
                    Dimension2 = i.Dimension2,
                    Dimension3 = i.Dimension3,
                    SortOrder = i.SortOrder,
                    Images = i.TbtSaleDocumentCatalogItemImage.Select(img => new GetItemImageResponse
                    {
                        Id = img.Id,
                        BlobPath = img.BlobPath,
                        SortOrder = img.SortOrder
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<List<ListResponse>> List(ListRequest request)
        {
            var query = _jewelryContext.TbtSaleDocumentCatalog
                .Include(c => c.TbtSaleDocumentCatalogItem)
                .Where(c => c.IsActive);

            if (request.DocumentMonth.HasValue)
                query = query.Where(c => c.DocumentMonth == request.DocumentMonth.Value);

            if (request.DocumentYear.HasValue)
                query = query.Where(c => c.DocumentYear == request.DocumentYear.Value);

            if (request.Status.HasValue)
                query = query.Where(c => c.Status == request.Status.Value);

            var items = await query
                .OrderByDescending(c => c.CreateDate)
                .Select(c => new ListResponse
                {
                    Id = c.Id,
                    HeaderLabel = c.HeaderLabel,
                    CollectionTitle = c.CollectionTitle,
                    DocumentMonth = c.DocumentMonth,
                    DocumentYear = c.DocumentYear,
                    Tags = c.Tags,
                    Remark = c.Remark,
                    Status = c.Status,
                    StatusName = c.StatusName,
                    ItemCount = c.TbtSaleDocumentCatalogItem.Count,
                    CreateBy = c.CreateBy,
                    CreateDate = c.CreateDate
                })
                .ToListAsync();

            return items;
        }

        public async Task Delete(long id)
        {
            var catalog = await _jewelryContext.TbtSaleDocumentCatalog
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (catalog == null)
                throw new HandleException("ไม่พบเอกสาร");

            catalog.IsActive = false;
            catalog.UpdateBy = CurrentUsername ?? "system";
            catalog.UpdateDate = DateTime.UtcNow;

            _jewelryContext.TbtSaleDocumentCatalog.Update(catalog);
            await _jewelryContext.SaveChangesAsync();
        }

        public async Task<UploadImageResponse> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new HandleException("กรุณาเลือกไฟล์รูปภาพ");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                throw new HandleException("รองรับเฉพาะไฟล์รูปภาพ (jpg, png, gif, webp) เท่านั้น");

            var now = DateTime.UtcNow;
            var folderName = "Sale/Document";
            var fileName = $"Catalog/{now.Year}/{now.Month:D2}/{Guid.NewGuid()}_{file.FileName}";

            using var stream = file.OpenReadStream();
            var uploadResult = await _azureBlobService.UploadImageAsync(stream, folderName, fileName);

            if (uploadResult == null || !uploadResult.Success)
                throw new HandleException(uploadResult?.ErrorMessage ?? "อัปโหลดรูปไม่สำเร็จ");

            return new UploadImageResponse
            {
                BlobPath = $"{folderName}/{fileName}"
            };
        }

        public async Task<(Stream stream, string contentType)> GetImage(string blobPath)
        {
            if (string.IsNullOrWhiteSpace(blobPath))
                throw new HandleException("blobPath ไม่ถูกต้อง");

            var parts = blobPath.Split('/');
            var fileName = parts.Last();
            var folderName = string.Join("/", parts.Take(parts.Length - 1));

            var stream = await _azureBlobService.DownloadImageAsync(folderName, fileName);

            var ext = Path.GetExtension(fileName).ToLower();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

            return (stream, contentType);
        }

        private static List<TbtSaleDocumentCatalogItem> BuildItems(long catalogId, List<ItemRequest> itemRequests)
        {
            var result = new List<TbtSaleDocumentCatalogItem>();
            foreach (var itemReq in itemRequests)
            {
                var item = new TbtSaleDocumentCatalogItem
                {
                    CatalogId = catalogId,
                    ProductNumber = itemReq.ProductNumber,
                    DescriptionLine1 = itemReq.DescriptionLine1,
                    DescriptionLine2 = itemReq.DescriptionLine2,
                    Dimension1 = itemReq.Dimension1,
                    Dimension2 = itemReq.Dimension2,
                    Dimension3 = itemReq.Dimension3,
                    SortOrder = itemReq.SortOrder,
                    TbtSaleDocumentCatalogItemImage = itemReq.ImageBlobPaths
                        .Select((bp, idx) => new TbtSaleDocumentCatalogItemImage
                        {
                            BlobPath = bp,
                            SortOrder = idx
                        }).ToList()
                };
                result.Add(item);
            }
            return result;
        }
    }
}
