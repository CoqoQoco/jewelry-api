using jewelry.Model.Exceptions;
using jewelry.Model.PublicProduct;
using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Jewelry.Service.PublicProduct
{
    public class PublicProductService : IPublicProductService
    {
        private static readonly object NotFoundSentinel = new object();

        private readonly JewelryContext _jewelryContext;
        private readonly IOptions<PublicProductConfig> _config;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PublicProductService> _logger;

        public PublicProductService(JewelryContext jewelryContext,
            IOptions<PublicProductConfig> config,
            IMemoryCache cache,
            ILogger<PublicProductService> logger)
        {
            _jewelryContext = jewelryContext;
            _config = config;
            _cache = cache;
            _logger = logger;
        }

        public jewelry.Model.PublicProduct.Get.Response Get(string token)
        {
            if (!_config.Value.Enabled || string.IsNullOrEmpty(_config.Value.TokenSecret) || string.IsNullOrWhiteSpace(token))
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var cacheKey = $"PublicProduct:Get:{token}";
            if (_cache.TryGetValue(cacheKey, out var cachedEntry))
            {
                if (ReferenceEquals(cachedEntry, NotFoundSentinel))
                {
                    throw new HandleException(ErrorMessage.NotFound);
                }

                return (jewelry.Model.PublicProduct.Get.Response)cachedEntry!;
            }

            try
            {
                var response = BuildResponse(token);
                _cache.Set(cacheKey, response, TimeSpan.FromSeconds(_config.Value.CacheSeconds));
                return response;
            }
            catch (HandleException)
            {
                _cache.Set(cacheKey, NotFoundSentinel, TimeSpan.FromSeconds(_config.Value.CacheSeconds));
                throw;
            }
        }

        public jewelry.Model.PublicProduct.Link.Response CreateLink(string stockNumber)
        {
            if (!_config.Value.Enabled || string.IsNullOrEmpty(_config.Value.TokenSecret) || string.IsNullOrWhiteSpace(stockNumber))
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var piece = _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Where(x => x.StockNumber == stockNumber)
                .Select(x => new { x.StockNumber, x.ProductCode })
                .FirstOrDefault();

            if (piece == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var sig = ComputeSig(piece.StockNumber, piece.ProductCode, _config.Value.TokenSecret!);
            var token = $"{piece.StockNumber}-{sig}";

            return new jewelry.Model.PublicProduct.Link.Response
            {
                Token = token,
                Path = $"/p/{token}"
            };
        }

        private jewelry.Model.PublicProduct.Get.Response BuildResponse(string token)
        {
            if (!TryParseToken(token, out var stockNumber, out var sig))
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var candidates = _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Where(x => x.StockNumber == stockNumber)
                .Select(x => new PieceProjection
                {
                    StockNumber = x.StockNumber,
                    StockNumberOrigin = x.StockNumberOrigin,
                    ProductCode = x.ProductCode,
                    SizeActual = x.SizeActual,
                    Status = x.Status,
                    Qty = x.Qty,
                    QtyReserved = x.QtyReserved,
                    ProductNumber = x.SkuCodeNavigation.ProductNumber,
                    ProductNameTh = x.SkuCodeNavigation.ProductNameTh,
                    ProductNameEn = x.SkuCodeNavigation.ProductNameEn,
                    ProductTypeName = x.SkuCodeNavigation.ProductTypeName,
                    ProductionTypeSize = x.SkuCodeNavigation.ProductionTypeSize,
                    Size = x.SkuCodeNavigation.Size,
                    EarringStemSize = x.SkuCodeNavigation.EarringStemSize,
                    ImagePath = x.SkuCodeNavigation.ImagePath,
                    ImageName = x.SkuCodeNavigation.ImageName,
                    DefaultPrice = x.SkuCodeNavigation.DefaultPrice,
                    TagPriceMultiplier = x.SkuCodeNavigation.TagPriceMultiplier,
                    IsActive = x.SkuCodeNavigation.IsActive,
                    Materials = x.TbtStockPieceMaterial.Select(m => new MaterialProjection
                    {
                        Type = m.Type,
                        TypeName = m.TypeName,
                        TypeCode = m.TypeCode,
                        Qty = m.Qty,
                        Weight = m.Weight,
                        WeightUnit = m.WeightUnit,
                        Size = m.Size,
                        Region = m.Region
                    }).ToList()
                })
                .ToList();

            var matched = candidates.FirstOrDefault(x =>
                string.Equals(ComputeSig(x.StockNumber, x.ProductCode, _config.Value.TokenSecret!), sig, StringComparison.OrdinalIgnoreCase));

            if (matched == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            if (!matched.IsActive)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var metalMaterials = matched.Materials.Where(m => m.Type == "Gold" || m.Type == "Silver").ToList();
            var metalColorCode = metalMaterials.Select(m => m.TypeCode).FirstOrDefault(c => !string.IsNullOrEmpty(c));
            decimal? metalWeight = metalMaterials.Any() ? metalMaterials.Sum(m => m.Weight ?? 0) : null;
            var metalWeightUnit = metalMaterials.Select(m => m.WeightUnit).FirstOrDefault(u => !string.IsNullOrEmpty(u)) ?? "g";

            var gemMaterials = matched.Materials.Where(m => m.Type == "Gem" || m.Type == "Diamond").ToList();
            var gems = gemMaterials
                .GroupBy(m => new { m.Type, Name = !string.IsNullOrEmpty(m.TypeName) ? m.TypeName : m.TypeCode })
                .Select(g => new jewelry.Model.PublicProduct.Get.Gem
                {
                    Type = g.Key.Type,
                    Name = g.Key.Name,
                    Code = g.Select(x => x.TypeCode).FirstOrDefault(c => !string.IsNullOrEmpty(c)),
                    Qty = g.Sum(x => x.Qty ?? 0),
                    Weight = g.Sum(x => x.Weight ?? 0),
                    WeightUnit = g.Select(x => x.WeightUnit).FirstOrDefault(u => !string.IsNullOrEmpty(u)),
                    Size = g.Select(x => x.Size).FirstOrDefault(s => !string.IsNullOrEmpty(s)),
                    Origin = _config.Value.ShowGemOrigin ? g.Select(x => x.Region).FirstOrDefault(r => !string.IsNullOrEmpty(r)) : null
                })
                .ToList();

            decimal? displayPrice = null;
            if (_config.Value.ShowPrice)
            {
                // หน้าสาธารณะไม่มีสกุลเงินของตัวเอง — ถือเป็น THB เสมอ ปัดครึ่งขึ้นแทน Math.Round เดิมที่เป็น banker's rounding (ToEven)
                var computed = MathHelper.RoundMoney((matched.DefaultPrice ?? 0) * (matched.TagPriceMultiplier ?? 1), "THB");
                displayPrice = computed > 0 ? computed : (decimal?)null;
            }

            var imagePath = BuildImagePath(matched.ImagePath, matched.ImageName);

            var availableQty = matched.Qty - matched.QtyReserved;
            if (availableQty < 0) availableQty = 0;

            return new jewelry.Model.PublicProduct.Get.Response
            {
                StockNumber = matched.StockNumber,
                StockNumberOrigin = matched.StockNumberOrigin,
                ProductNumber = matched.ProductNumber,
                ProductNameTh = matched.ProductNameTh,
                ProductNameEn = matched.ProductNameEn,
                ProductTypeName = matched.ProductTypeName,
                ImagePath = imagePath,
                MetalKarat = matched.ProductionTypeSize,
                MetalColorCode = metalColorCode,
                MetalWeight = metalWeight,
                MetalWeightUnit = metalWeightUnit,
                Size = matched.SizeActual ?? matched.Size,
                EarringStemSize = matched.EarringStemSize,
                Gems = gems,
                DisplayPrice = displayPrice,
                Currency = "THB",
                IsAvailable = _config.Value.ShowAvailability ? (bool?)(availableQty > 0) : null,
                AvailableQty = _config.Value.ShowAvailability ? (decimal?)availableQty : null
            };
        }

        private const string StockProductFolder = "Stock/Product";

        // กรณี legacy migrated SKU: ImagePath คือชื่อไฟล์ ("{NoCode}.jpg") อยู่ใต้โฟลเดอร์ Stock/Product เสมอ
        // กรณีอัปโหลด/แทนที่รูปผ่าน ProductImageService.Replace: ImagePath คือโฟลเดอร์เต็ม ("Stock/Product") + ImageName คือชื่อไฟล์
        private static string? BuildImagePath(string? imagePath, string? imageName)
        {
            if (string.IsNullOrEmpty(imagePath) && string.IsNullOrEmpty(imageName))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(imagePath) && imagePath.Contains('/'))
            {
                if (string.IsNullOrEmpty(imageName))
                {
                    return null;
                }

                return $"{imagePath.TrimEnd('/')}/{imageName}";
            }

            if (!string.IsNullOrEmpty(imagePath))
            {
                return $"{StockProductFolder}/{imagePath}";
            }

            var fileName = imageName!;
            if (!Path.HasExtension(fileName))
            {
                fileName = $"{fileName}.jpg";
            }

            return $"{StockProductFolder}/{fileName}";
        }

        private static bool TryParseToken(string token, out string stockNumber, out string sig)
        {
            stockNumber = string.Empty;
            sig = string.Empty;

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var idx = token.LastIndexOf('-');
            if (idx <= 0 || idx == token.Length - 1)
            {
                return false;
            }

            stockNumber = token.Substring(0, idx);
            sig = token.Substring(idx + 1);
            return true;
        }

        private static string ComputeSig(string stockNumber, string productCode, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{stockNumber}|{productCode}"));
            return ToBase32(hash).Substring(0, 8);
        }

        private static string ToBase32(byte[] data)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder();
            int bitBuffer = 0;
            int bitCount = 0;

            foreach (var b in data)
            {
                bitBuffer = (bitBuffer << 8) | b;
                bitCount += 8;

                while (bitCount >= 5)
                {
                    bitCount -= 5;
                    int index = (bitBuffer >> bitCount) & 0x1F;
                    result.Append(alphabet[index]);
                }
            }

            if (bitCount > 0)
            {
                int index = (bitBuffer << (5 - bitCount)) & 0x1F;
                result.Append(alphabet[index]);
            }

            return result.ToString();
        }

        private class PieceProjection
        {
            public string StockNumber { get; set; } = null!;
            public string? StockNumberOrigin { get; set; }
            public string ProductCode { get; set; } = null!;
            public string? SizeActual { get; set; }
            public string? Status { get; set; }
            public decimal Qty { get; set; }
            public decimal QtyReserved { get; set; }

            public string? ProductNumber { get; set; }
            public string ProductNameTh { get; set; } = null!;
            public string ProductNameEn { get; set; } = null!;
            public string? ProductTypeName { get; set; }
            public string? ProductionTypeSize { get; set; }
            public string? Size { get; set; }
            public string? EarringStemSize { get; set; }
            public string? ImagePath { get; set; }
            public string? ImageName { get; set; }
            public decimal? DefaultPrice { get; set; }
            public decimal? TagPriceMultiplier { get; set; }
            public bool IsActive { get; set; }

            public List<MaterialProjection> Materials { get; set; } = new List<MaterialProjection>();
        }

        private class MaterialProjection
        {
            public string Type { get; set; } = null!;
            public string? TypeName { get; set; }
            public string? TypeCode { get; set; }
            public decimal? Qty { get; set; }
            public decimal? Weight { get; set; }
            public string? WeightUnit { get; set; }
            public string? Size { get; set; }
            public string? Region { get; set; }
        }
    }
}
