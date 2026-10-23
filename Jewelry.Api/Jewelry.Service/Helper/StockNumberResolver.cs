using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Helper
{
    // แปลงเลขที่ user พิมพ์/สแกน (มีขีดหรือไม่มีขีด) เป็นเลขจริงใน tbt_stock_piece: exact ก่อน ไม่เจอค่อย fallback ตัดขีด
    // ต้องเรียกก่อนใช้เลขไป lookup/lock/บันทึก — บันทึกลง DB ด้วยเลขจริงเสมอ
    public static class StockNumberResolver
    {
        // key = เลขที่ส่งเข้ามา (ตามที่ user ส่ง), value = เลขจริงใน DB; เลขที่หาไม่เจอจะไม่อยู่ใน dictionary
        public static async Task<Dictionary<string, string>> ResolveAsync(this JewelryContext context, IEnumerable<string?>? inputs)
        {
            var result = new Dictionary<string, string>();

            var requested = (inputs ?? Enumerable.Empty<string?>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .Distinct()
                .ToList();
            if (requested.Count == 0) return result;

            var exactCandidates = requested.Concat(requested.Select(s => s.Trim())).Distinct().ToList();
            var exactFound = await context.TbtStockPiece
                .AsNoTracking()
                .Where(p => exactCandidates.Contains(p.StockNumber))
                .Select(p => p.StockNumber)
                .Distinct()
                .ToListAsync();
            var exactSet = new HashSet<string>(exactFound);

            var pending = new List<string>();
            foreach (var input in requested)
            {
                if (exactSet.Contains(input)) result[input] = input;
                else if (exactSet.Contains(input.Trim())) result[input] = input.Trim();
                else pending.Add(input);
            }

            if (pending.Count == 0) return result;

            var normalizedSet = pending.Select(StockNumberSearch.Normalize).Where(n => n != "").Distinct().ToList();
            if (normalizedSet.Count == 0) return result;

            var fallbackFound = await context.TbtStockPiece
                .AsNoTracking()
                .Where(p => normalizedSet.Contains(p.StockNumber.Replace("-", "").ToUpper()))
                .Select(p => p.StockNumber)
                .Distinct()
                .ToListAsync();

            var byNormalized = fallbackFound
                .GroupBy(StockNumberSearch.Normalize)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x, StringComparer.Ordinal).First());

            foreach (var input in pending)
            {
                if (byNormalized.TryGetValue(StockNumberSearch.Normalize(input), out var actual))
                {
                    result[input] = actual;
                }
            }

            return result;
        }

        // sync version สำหรับ method ที่คืน IQueryable (ไม่ async)
        public static string? Resolve(this JewelryContext context, string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var trimmed = input.Trim();
            var exact = context.TbtStockPiece
                .AsNoTracking()
                .Where(p => p.StockNumber == input || p.StockNumber == trimmed)
                .Select(p => p.StockNumber)
                .FirstOrDefault();
            if (exact != null) return exact;

            var normalized = StockNumberSearch.Normalize(input);
            if (normalized == "") return null;

            return context.TbtStockPiece
                .AsNoTracking()
                .Where(p => p.StockNumber.Replace("-", "").ToUpper() == normalized)
                .OrderBy(p => p.StockNumber)
                .Select(p => p.StockNumber)
                .FirstOrDefault();
        }

        public static async Task<string?> ResolveAsync(this JewelryContext context, string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var map = await context.ResolveAsync(new[] { input });
            return map.TryGetValue(input, out var actual) ? actual : null;
        }
    }
}
