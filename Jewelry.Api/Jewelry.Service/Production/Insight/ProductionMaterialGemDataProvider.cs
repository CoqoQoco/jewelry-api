using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    public class ProductionMaterialGemDataProvider : IProductionMaterialGemDataProvider
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IMemoryCache _cache;

        private const string StockCacheKey = "ProductionInsight:Material:StockGems";
        private const string GemNamesCacheKey = "ProductionInsight:Material:GemNames";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public ProductionMaterialGemDataProvider(JewelryContext jewelryContext, IMemoryCache cache)
        {
            _jewelryContext = jewelryContext;
            _cache = cache;
        }

        public async Task<List<ProductionMaterialGemEvaluator.StockGemRow>> GetStockGemsAsync()
        {
            if (_cache.TryGetValue<List<ProductionMaterialGemEvaluator.StockGemRow>>(StockCacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var result = await _jewelryContext.TbtStockGem
                .AsNoTracking()
                .Select(s => new ProductionMaterialGemEvaluator.StockGemRow
                {
                    Code = s.Code,
                    GroupName = s.GroupName,
                    Shape = s.Shape,
                    Grade = s.Grade,
                    Size = s.Size,
                    Quantity = s.Quantity
                })
                .ToListAsync();

            _cache.Set(StockCacheKey, result, CacheTtl);
            return result;
        }

        public async Task<Dictionary<string, string>> GetGemNamesAsync()
        {
            if (_cache.TryGetValue<Dictionary<string, string>>(GemNamesCacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var rows = await _jewelryContext.TbmGem
                .AsNoTracking()
                .Select(g => new { g.Code, g.NameEn })
                .ToListAsync();

            var result = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Code))
                .GroupBy(r => r.Code.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().NameEn);

            _cache.Set(GemNamesCacheKey, result, CacheTtl);
            return result;
        }
    }
}
