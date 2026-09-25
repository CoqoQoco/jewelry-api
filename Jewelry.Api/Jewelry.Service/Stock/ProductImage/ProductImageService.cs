using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Stock.ProductImage
{
    public class ProductImageService : BaseService, IProductImageService
    {
        private readonly JewelryContext _jewelryContext;

        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        private readonly IAzureBlobStorageService _azureBlobService;
        public ProductImageService(JewelryContext JewelryContext,
            IHostEnvironment HostingEnvironment,
            IRunningNumber runningNumberService,
            IAzureBlobStorageService azureBlobService,
            IHttpContextAccessor httpContextAccessor) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
            _azureBlobService = azureBlobService;
        }

        public async Task<string> Create(jewelry.Model.Stock.Product.Image.Create.Request request)
        {
            var name = request.Name.ToUpper();
            var namePath = $"{name}.jpg";

            var existing = _jewelryContext.TbtStockProductImage
                .FirstOrDefault(x => x.Name == name && x.Year == DateTime.UtcNow.Year && x.IsActive == true);
            if (existing != null)
            {
                throw new HandleException($"มีรูปภาพชื่อ {name} ในปี {DateTime.UtcNow.Year} อยู่แล้ว");
            }

            using var stream = request.Image.OpenReadStream();
            var result = await _azureBlobService.UploadImageAsync(
                stream,
                "Stock/Product",
                namePath
            );

            if (!result.Success)
            {
                throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
            }

            var nextId = (await _jewelryContext.TbtStockProductImage.MaxAsync(x => (int?)x.Id) ?? 0) + 1;

            var newImage = new TbtStockProductImage
            {
                Id = nextId,
                Name = name,
                Year = DateTime.UtcNow.Year,
                NamePath = namePath,
                Remark = request.Description ?? string.Empty,
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername,
            };
            _jewelryContext.TbtStockProductImage.Add(newImage);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }

        public IQueryable<jewelry.Model.Stock.Product.Image.List.Response> List(jewelry.Model.Stock.Product.Image.List.Search request)
        {
            var query = (from item in _jewelryContext.TbtStockProductImage
                         where item.IsActive == true
                         select new jewelry.Model.Stock.Product.Image.List.Response()
                         {
                             Id = item.Id,
                             Name = item.Name,
                             Year = item.Year,
                             CreateBy = item.CreateBy,
                             CreateDate = item.CreateDate,
                             UpdateBy = item.UpdateBy,
                             UpdateDate = item.UpdateDate,
                             IsActive = item.IsActive,
                             Remark = item.Remark,
                             NamePath = item.NamePath,
                         }).AsNoTracking();

            if (!string.IsNullOrEmpty(request.Name))
            {
                query = query.Where(x => x.Name.Contains(request.Name.ToUpper()));
            }
            if (request.Year.HasValue)
            {
                query = query.Where(x => x.Year == request.Year.Value);
            }
            return query;
        }

        public async Task<jewelry.Model.Stock.Product.Image.Replace.Response> Replace(jewelry.Model.Stock.Product.Image.Replace.Request request)
        {
            var piece = await _jewelryContext.TbtStockPiece
                .FirstOrDefaultAsync(x => x.StockNumber == request.StockNumber);

            if (piece == null)
            {
                throw new HandleException($"ไม่พบสินค้าในสต็อก StockNumber: {request.StockNumber}");
            }

            var sku = await _jewelryContext.TbtSku
                .FirstOrDefaultAsync(x => x.SkuCode == piece.SkuCode);

            if (sku == null)
            {
                throw new HandleException($"ไม่พบข้อมูล SKU สำหรับ SkuCode: {piece.SkuCode}");
            }

            var newBaseName = $"{piece.SkuCode}-{DateTime.UtcNow:yyyyMMddHHmmss}".ToUpper();
            var newFileName = $"{newBaseName}.jpg";

            using var stream = request.Image.OpenReadStream();
            var result = await _azureBlobService.UploadImageAsync(stream, "Stock/Product", newFileName);

            if (!result.Success)
            {
                throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
            }

            var oldImageName = sku.ImageName;

            if (!string.IsNullOrEmpty(oldImageName))
            {
                try
                {
                    await _azureBlobService.DeleteImageAsync("Stock/Product", oldImageName);
                }
                catch
                {
                    // non-critical — ignore deletion failure
                }
            }

            sku.ImageName = newFileName;
            sku.ImagePath = "Stock/Product";
            sku.UpdateDate = DateTime.UtcNow;
            sku.UpdateBy = CurrentUsername;

            _jewelryContext.TbtSku.Update(sku);
            await _jewelryContext.SaveChangesAsync();

            return new jewelry.Model.Stock.Product.Image.Replace.Response
            {
                ImageName = newFileName,
                ImagePath = "Stock/Product"
            };
        }

        private const int MaxBulkStockNumbers = 200;

        private static string? GetMoldKey(TbtSku sku)
        {
            return !string.IsNullOrEmpty(sku.Mold) ? sku.Mold : sku.MoldDesign;
        }

        private static List<(string Normalized, string Raw)> NormalizeBulkInputs(List<string> stockNumbers)
        {
            var result = new List<(string Normalized, string Raw)>();
            var seen = new HashSet<string>();

            foreach (var raw in stockNumbers ?? new List<string>())
            {
                var trimmed = (raw ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                var normalized = StockNumberSearch.Normalize(trimmed);
                if (string.IsNullOrEmpty(normalized) || !seen.Add(normalized))
                {
                    continue;
                }

                result.Add((normalized, trimmed));
            }

            return result;
        }

        public async Task<List<jewelry.Model.Stock.Product.Image.BulkPreview.Response>> BulkPreview(jewelry.Model.Stock.Product.Image.BulkPreview.Request request)
        {
            var inputs = NormalizeBulkInputs(request.StockNumbers);
            var normalizedSet = inputs.Select(x => x.Normalized).ToList();

            var pieces = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Include(x => x.SkuCodeNavigation)
                .Where(x => normalizedSet.Contains(x.StockNumber.Replace("-", "").ToUpper()))
                .ToListAsync();

            if (request.IncludeSameMoldInReceipt)
            {
                var moldPairs = pieces
                    .Where(p => !string.IsNullOrEmpty(p.ReceiptNumber) && !string.IsNullOrEmpty(GetMoldKey(p.SkuCodeNavigation)))
                    .Select(p => new { p.ReceiptNumber, Mold = GetMoldKey(p.SkuCodeNavigation) })
                    .Distinct()
                    .ToList();

                var foundStockNumbers = new HashSet<string>(pieces.Select(p => p.StockNumber));

                foreach (var pair in moldPairs)
                {
                    var siblings = await _jewelryContext.TbtStockPiece
                        .AsNoTracking()
                        .Include(x => x.SkuCodeNavigation)
                        .Where(x => x.ReceiptNumber == pair.ReceiptNumber)
                        .ToListAsync();

                    foreach (var sibling in siblings)
                    {
                        if (foundStockNumbers.Contains(sibling.StockNumber))
                        {
                            continue;
                        }

                        if (GetMoldKey(sibling.SkuCodeNavigation) != pair.Mold)
                        {
                            continue;
                        }

                        pieces.Add(sibling);
                        foundStockNumbers.Add(sibling.StockNumber);
                    }
                }
            }

            var candidateNames = pieces.Select(p => p.StockNumber.ToUpper()).Distinct().ToList();
            var imageNames = await _jewelryContext.TbtStockProductImage
                .AsNoTracking()
                .Where(x => x.IsActive == true && candidateNames.Contains(x.Name))
                .Select(x => x.Name)
                .Distinct()
                .ToListAsync();
            var hasImageSet = new HashSet<string>(imageNames);

            var response = new List<jewelry.Model.Stock.Product.Image.BulkPreview.Response>();
            var matchedNormalized = new HashSet<string>();

            foreach (var piece in pieces)
            {
                matchedNormalized.Add(StockNumberSearch.Normalize(piece.StockNumber));

                response.Add(new jewelry.Model.Stock.Product.Image.BulkPreview.Response
                {
                    StockNumber = piece.StockNumber,
                    ProductCode = piece.ProductCode,
                    Mold = GetMoldKey(piece.SkuCodeNavigation),
                    ReceiptNumber = piece.ReceiptNumber,
                    Found = true,
                    HasImage = hasImageSet.Contains(piece.StockNumber.ToUpper()),
                    ImagePath = StockImagePath.Build(piece.SkuCodeNavigation.ImagePath, piece.SkuCodeNavigation.ImageName),
                });
            }

            foreach (var input in inputs)
            {
                if (matchedNormalized.Contains(input.Normalized))
                {
                    continue;
                }

                response.Add(new jewelry.Model.Stock.Product.Image.BulkPreview.Response
                {
                    StockNumber = input.Raw,
                    ProductCode = null,
                    Mold = null,
                    ReceiptNumber = null,
                    Found = false,
                    HasImage = false,
                    ImagePath = null,
                });
            }

            return response;
        }

        public async Task<jewelry.Model.Stock.Product.Image.CreateBulk.Response> CreateBulk(jewelry.Model.Stock.Product.Image.CreateBulk.Request request)
        {
            var stockNumbers = request.StockNumbers ?? new List<string>();
            if (stockNumbers.Count > MaxBulkStockNumbers)
            {
                throw new HandleException($"อัปโหลดได้สูงสุด {MaxBulkStockNumbers} เลขสต็อกต่อครั้ง (ส่งมา {stockNumbers.Count} รายการ)");
            }

            var inputs = NormalizeBulkInputs(stockNumbers);
            var normalizedSet = inputs.Select(x => x.Normalized).ToList();

            var pieces = await _jewelryContext.TbtStockPiece
                .Include(x => x.SkuCodeNavigation)
                .Where(x => normalizedSet.Contains(x.StockNumber.Replace("-", "").ToUpper()))
                .ToListAsync();

            var pieceByNormalized = pieces.ToDictionary(p => StockNumberSearch.Normalize(p.StockNumber));

            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await request.Image.OpenReadStream().CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            var currentYear = DateTime.UtcNow.Year;
            var nextId = await _jewelryContext.TbtStockProductImage.MaxAsync(x => (int?)x.Id) ?? 0;

            var response = new jewelry.Model.Stock.Product.Image.CreateBulk.Response();
            var uploadFailures = new List<string>();

            foreach (var input in inputs)
            {
                if (!pieceByNormalized.TryGetValue(input.Normalized, out var piece))
                {
                    response.NotFound.Add(input.Raw);
                    continue;
                }

                var name = piece.StockNumber.ToUpper();
                var namePath = $"{name}.jpg";

                var existingImage = await _jewelryContext.TbtStockProductImage
                    .FirstOrDefaultAsync(x => x.Name == name && x.Year == currentYear && x.IsActive == true);

                if (existingImage != null && !request.Overwrite)
                {
                    response.Skipped.Add(piece.StockNumber);
                    continue;
                }

                using (var uploadStream = new MemoryStream(imageBytes))
                {
                    var result = await _azureBlobService.UploadImageAsync(uploadStream, "Stock/Product", namePath);
                    if (!result.Success)
                    {
                        uploadFailures.Add($"{piece.StockNumber}: {result.ErrorMessage}");
                        continue;
                    }
                }

                if (existingImage != null)
                {
                    existingImage.UpdateBy = CurrentUsername;
                    existingImage.UpdateDate = DateTime.UtcNow;
                    existingImage.Remark = request.Description ?? existingImage.Remark ?? string.Empty;
                    _jewelryContext.TbtStockProductImage.Update(existingImage);
                    response.Overwritten.Add(piece.StockNumber);
                }
                else
                {
                    nextId++;
                    var newImage = new TbtStockProductImage
                    {
                        Id = nextId,
                        Name = name,
                        Year = currentYear,
                        NamePath = namePath,
                        Remark = request.Description ?? string.Empty,
                        IsActive = true,
                        CreateDate = DateTime.UtcNow,
                        CreateBy = CurrentUsername,
                    };
                    _jewelryContext.TbtStockProductImage.Add(newImage);
                    response.Created.Add(piece.StockNumber);
                }

                var sku = piece.SkuCodeNavigation;
                var skuHasImage = !string.IsNullOrEmpty(sku.ImagePath) || !string.IsNullOrEmpty(sku.ImageName);
                if (!skuHasImage || request.Overwrite)
                {
                    sku.ImageName = name;
                    sku.ImagePath = namePath;
                    sku.UpdateBy = CurrentUsername;
                    sku.UpdateDate = DateTime.UtcNow;
                    _jewelryContext.TbtSku.Update(sku);
                }
            }

            if (uploadFailures.Any())
            {
                throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้บางรายการ: {string.Join(", ", uploadFailures)}");
            }

            await _jewelryContext.SaveChangesAsync();

            return response;
        }
    }
}
