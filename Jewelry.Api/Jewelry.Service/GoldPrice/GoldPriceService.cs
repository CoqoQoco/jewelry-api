using jewelry.Model.GoldPrice;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.GoldPrice
{
    public class GoldPriceService : IGoldPriceService
    {
        public const string ClientName = "goldtraders";

        private const string TodayEndpoint = "/wp-admin/get_table_historical_gold_price_01_01.php";
        private const string HistoryEndpoint = "/wp-admin/get_table_historical_gold_price_02_02.php";
        private const int TodayFallbackLookbackDays = 14;

        private static readonly SemaphoreSlim HistoryConcurrency = new SemaphoreSlim(4);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GoldPriceService> _logger;

        public GoldPriceService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<GoldPriceService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<TodayGoldPriceResponse> Today(TodayGoldPriceRequest request)
        {
            try
            {
                var nowThai = DateTime.UtcNow.AddHours(7);
                var todayDate = nowThai.Date;

                var (rawItems, isStale) = await GetOrFetchAsync(
                    "GoldPrice:Today",
                    TimeSpan.FromMinutes(3),
                    () => FetchTodayDataAsync(todayDate));

                if (rawItems == null || rawItems.Count == 0)
                {
                    return new TodayGoldPriceResponse { IsStale = true };
                }

                var group = rawItems
                    .Select(x => new { Item = x, Dt = ParseAsTime(x.AsTime) })
                    .Where(x => x.Dt.HasValue && x.Dt.Value.Date <= todayDate)
                    .GroupBy(x => x.Dt!.Value.Date)
                    .OrderByDescending(g => g.Key)
                    .FirstOrDefault();

                if (group == null)
                {
                    return new TodayGoldPriceResponse { IsStale = true };
                }

                var asOfDate = group.Key;
                var dayItems = group.OrderBy(x => x.Dt).Select(x => x.Item).ToList();
                var latest = dayItems.OrderByDescending(x => x.PriceSeq).First();

                var prevClose = await GetPrevCloseAsync(asOfDate);
                var barSell = ParseDecimal(latest.BarSell);

                return new TodayGoldPriceResponse
                {
                    Date = asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Time = ParseAsTime(latest.AsTime)?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty,
                    PriceSeq = latest.PriceSeq,
                    BarBuy = ParseDecimal(latest.BarBuy),
                    BarSell = barSell,
                    OrnamentBuy = ParseDecimal(latest.OrnamentBuy),
                    OrnamentSell = ParseDecimal(latest.OrnamentSell),
                    GoldSpot = ParseDecimal(latest.GoldSpot),
                    BahtPerUsd = ParseDecimal(latest.BahtPerUsd),
                    ChangeFromPrevClose = prevClose.HasValue ? barSell - prevClose.Value : (decimal?)null,
                    Rounds = dayItems.Select(x => new GoldPriceRound
                    {
                        Time = ParseAsTime(x.AsTime)?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty,
                        PriceSeq = x.PriceSeq,
                        BarBuy = ParseDecimal(x.BarBuy),
                        BarSell = ParseDecimal(x.BarSell),
                        OrnamentBuy = ParseDecimal(x.OrnamentBuy),
                        OrnamentSell = ParseDecimal(x.OrnamentSell),
                        GoldSpot = ParseDecimal(x.GoldSpot),
                        BahtPerUsd = ParseDecimal(x.BahtPerUsd),
                        PriceDiff = ParseDecimal(x.PriceDiff)
                    }).ToList(),
                    IsStale = isStale
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GoldPriceService.Today failed unexpectedly");
                return new TodayGoldPriceResponse { IsStale = true };
            }
        }

        public async Task<HistoryGoldPriceResponse> History(HistoryGoldPriceRequest request)
        {
            try
            {
                var months = request?.Months ?? 24;
                if (months < 1) months = 1;
                if (months > 60) months = 60;

                var nowThai = DateTime.UtcNow.AddHours(7);
                var anchor = new DateTime(nowThai.Year, nowThai.Month, 1);

                var monthList = Enumerable.Range(0, months)
                    .Select(i => anchor.AddMonths(-(months - 1 - i)))
                    .ToList();

                var results = new (List<UpstreamHistoryItem>? items, bool isStale)[monthList.Count];

                var tasks = monthList.Select(async (dt, index) =>
                {
                    await HistoryConcurrency.WaitAsync();
                    try
                    {
                        results[index] = await GetMonthDataAsync(dt.Year, dt.Month);
                    }
                    finally
                    {
                        HistoryConcurrency.Release();
                    }
                }).ToList();

                await Task.WhenAll(tasks);

                var isStale = results.Any(r => r.isStale);

                var items = results
                    .Where(r => r.items != null)
                    .SelectMany(r => r.items!)
                    .Select(x => new { Dt = ParseHistoryDate(x.Date), Item = x })
                    .Where(x => x.Dt.HasValue)
                    .OrderBy(x => x.Dt)
                    .Select(x => new GoldPriceHistoryItem
                    {
                        Date = x.Dt!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Open = ParseDecimal(x.Item.Open),
                        High = ParseDecimal(x.Item.Highest),
                        Low = ParseDecimal(x.Item.Lowest),
                        Close = ParseDecimal(x.Item.Close),
                        Change = ParseDecimal(x.Item.Change)
                    })
                    .ToList();

                return new HistoryGoldPriceResponse
                {
                    Series = "bar-sell",
                    Items = items,
                    IsStale = isStale
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GoldPriceService.History failed unexpectedly");
                return new HistoryGoldPriceResponse
                {
                    Series = "bar-sell",
                    Items = new List<GoldPriceHistoryItem>(),
                    IsStale = true
                };
            }
        }

        private async Task<decimal?> GetPrevCloseAsync(DateTime asOfDate)
        {
            var monthsToCheck = new List<(int Year, int Month)> { (asOfDate.Year, asOfDate.Month) };
            if (asOfDate.Day <= 2)
            {
                var prevMonth = asOfDate.AddMonths(-1);
                monthsToCheck.Add((prevMonth.Year, prevMonth.Month));
            }

            var candidates = new List<(DateTime Date, string Close)>();
            foreach (var (year, month) in monthsToCheck)
            {
                var (items, _) = await GetMonthDataAsync(year, month);
                if (items == null) continue;

                foreach (var item in items)
                {
                    var dt = ParseHistoryDate(item.Date);
                    if (dt.HasValue && dt.Value.Date < asOfDate.Date)
                    {
                        candidates.Add((dt.Value, item.Close));
                    }
                }
            }

            if (candidates.Count == 0) return null;

            var best = candidates.OrderByDescending(x => x.Date).First();
            return ParseDecimal(best.Close);
        }

        private async Task<(List<UpstreamHistoryItem>? items, bool isStale)> GetMonthDataAsync(int year, int month)
        {
            var cacheKey = $"GoldPrice:Month:{year}-{month:D2}";
            var nowThai = DateTime.UtcNow.AddHours(7);
            var isCurrentMonth = year == nowThai.Year && month == nowThai.Month;
            var ttl = isCurrentMonth ? TimeSpan.FromMinutes(30) : TimeSpan.FromDays(7);

            return await GetOrFetchAsync(cacheKey, ttl, () => FetchMonthAsync(year, month));
        }

        private async Task<List<UpstreamTodayItem>> FetchTodayDataAsync(DateTime todayDate)
        {
            var todayItems = await CallTodayEndpoint(todayDate, todayDate);
            if (todayItems.Count > 0)
            {
                return todayItems;
            }

            // ยังไม่มีประกาศของวันนี้ (วันหยุด/ก่อนรอบแรก) - ขยายช่วงย้อนหลังเพื่อหาวันทำการล่าสุด
            var fallbackStart = todayDate.AddDays(-TodayFallbackLookbackDays);
            return await CallTodayEndpoint(fallbackStart, todayDate);
        }

        private async Task<List<UpstreamTodayItem>> CallTodayEndpoint(DateTime start, DateTime end)
        {
            var url = $"{TodayEndpoint}?dateStart={start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&dateEnd={end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
            var client = _httpClientFactory.CreateClient(ClientName);
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return new List<UpstreamTodayItem>();
            }

            var parsed = JsonConvert.DeserializeObject<UpstreamTodayResponse>(content);
            return parsed?.Data ?? new List<UpstreamTodayItem>();
        }

        private async Task<List<UpstreamHistoryItem>> FetchMonthAsync(int year, int month)
        {
            var url = $"{HistoryEndpoint}?year={year}&month={month}";
            var client = _httpClientFactory.CreateClient(ClientName);
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return new List<UpstreamHistoryItem>();
            }

            var parsed = JsonConvert.DeserializeObject<UpstreamHistoryResponse>(content);
            return parsed?.Data ?? new List<UpstreamHistoryItem>();
        }

        private async Task<(T? data, bool isStale)> GetOrFetchAsync<T>(string cacheKey, TimeSpan ttl, Func<Task<T>> fetcher) where T : class
        {
            if (_cache.TryGetValue<T>(cacheKey, out var cached) && cached != null)
            {
                return (cached, false);
            }

            try
            {
                var fresh = await fetcher();
                _cache.Set(cacheKey, fresh, ttl);
                _cache.Set(LastGoodKey(cacheKey), fresh);
                return (fresh, false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GoldPrice upstream fetch failed for cache key {CacheKey}", cacheKey);

                if (_cache.TryGetValue<T>(LastGoodKey(cacheKey), out var lastGood) && lastGood != null)
                {
                    return (lastGood, true);
                }

                return (null, true);
            }
        }

        private static string LastGoodKey(string cacheKey) => cacheKey + ":lastgood";

        private static decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0m;
        }

        private static DateTime? ParseAsTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                ? result
                : (DateTime?)null;
        }

        private static DateTime? ParseHistoryDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                ? result
                : (DateTime?)null;
        }
    }
}
