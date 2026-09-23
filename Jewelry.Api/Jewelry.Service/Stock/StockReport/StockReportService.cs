using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Summary = jewelry.Model.Stock.StockReport.Summary;
using ProductGroup = jewelry.Model.Stock.StockReport.ProductGroup;
using Aging = jewelry.Model.Stock.StockReport.Aging;
using AgingItems = jewelry.Model.Stock.StockReport.AgingItems;
using ProductionBalance = jewelry.Model.Stock.StockReport.ProductionBalance;
using DesignAlerts = jewelry.Model.Stock.StockReport.DesignAlerts;

namespace Jewelry.Service.Stock.StockReport
{
    public class StockReportService : BaseService, IStockReportService
    {
        private readonly JewelryContext _jewelryContext;

        private const string UnknownGroupKey = "__UNKNOWN__";
        private const int DefaultAgingThresholdYears = 2;
        private static readonly int[] AllowedAgingThresholdYears = { 1, 2, 3, 5 };
        private static readonly string[] AgingBucketOrder = { "lt6m", "m6to12", "y1to2", "y2to3", "y3to5", "gt5y" };

        public StockReportService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        // ฐานสต็อกร่วมของทุก endpoint: IN_STOCK/RESERVED เท่านั้น (ห้ามใช้ "Available" ไม่มีจริงใน DB)
        // ProductionDate ใช้เป็นวันที่อ้างอิงอายุสต็อกเสมอ (ไม่ใช่ CreateDate/ReceiptDate)
        private class ResolvedStockLine
        {
            public string StockNumber { get; set; } = null!;
            public string? StockNumberOrigin { get; set; }
            public string SkuCode { get; set; } = string.Empty;
            public string? ProductNumber { get; set; }
            public string? ProductNameTh { get; set; }
            public string? ProductType { get; set; }
            public string? ProductTypeName { get; set; }
            public string? ProductionType { get; set; }
            public string? ProductionTypeSize { get; set; }
            public string Design { get; set; } = string.Empty;
            public string? ImagePath { get; set; }
            public string? ImageName { get; set; }
            public string LocationCode { get; set; } = string.Empty;
            public string? LocationName { get; set; }
            public string? Status { get; set; }
            public decimal Qty { get; set; }
            public decimal QtyReserved { get; set; }
            public DateTime? ProductionDate { get; set; }
            public int AgeDays { get; set; }
        }

        // ยอดขายที่ resolve กลับไปหา SKU ผ่าน tbt_stock_piece (composite PK stock_number+product_code —
        // group by StockNumber แล้ว First() เสมอ ตาม pattern BuildFilteredSaleLines ของ SaleReportService)
        private class ResolvedSoldLine
        {
            public string StockNumber { get; set; } = null!;
            public string SkuCode { get; set; } = string.Empty;
            public string? ProductNumber { get; set; }
            public string? ProductNameTh { get; set; }
            public string? ProductType { get; set; }
            public string? ProductTypeName { get; set; }
            public string? ProductionType { get; set; }
            public string? ProductionTypeSize { get; set; }
            public string Design { get; set; } = string.Empty;
            public string? ImagePath { get; set; }
            public string? ImageName { get; set; }
            public decimal Qty { get; set; }
            public DateTime InvoiceDate { get; set; }
        }

        // ไทยไม่มี DST — +7 ชม. คงที่แปลง UTC เป็นวันที่ปฏิทินไทยได้เสมอ (เหมือน SaleReportService)
        private static DateTime ThaiToday() => DateTime.UtcNow.AddHours(7).Date;

        // key ของ "แบบ": COALESCE(NULLIF(sku.MoldDesign,''), sku.Mold) — fallback สุดท้ายไป SkuCode
        // กันกรณี Mold/MoldDesign ว่างทั้งคู่ไม่ให้ทุกชิ้นถูกยุบรวมกันเป็นกลุ่มเดียว
        private static string ResolveDesignKey(string? moldDesign, string? mold, string skuCode)
        {
            if (!string.IsNullOrEmpty(moldDesign))
            {
                return moldDesign;
            }

            if (!string.IsNullOrEmpty(mold))
            {
                return mold;
            }

            return skuCode;
        }

        private static string GroupKey(string groupBy, string? productType, string? productionType, string? productionTypeSize)
        {
            return groupBy switch
            {
                "gold" => string.IsNullOrEmpty(productionType) ? UnknownGroupKey : productionType!,
                "goldsize" => string.IsNullOrEmpty(productionTypeSize) ? UnknownGroupKey : productionTypeSize!,
                "combined" => (string.IsNullOrEmpty(productType) && string.IsNullOrEmpty(productionType) && string.IsNullOrEmpty(productionTypeSize))
                    ? UnknownGroupKey
                    : $"{productType}|{productionType}|{productionTypeSize}",
                _ => string.IsNullOrEmpty(productType) ? UnknownGroupKey : productType!
            };
        }

        private static string? GroupLabel(string groupBy, string key, string? productTypeName, string? productionType, string? productionTypeSize)
        {
            if (key == UnknownGroupKey)
            {
                return null;
            }

            return groupBy switch
            {
                "gold" => key,
                "goldsize" => key,
                "combined" => $"{productTypeName} · {productionType} · {productionTypeSize}",
                _ => productTypeName
            };
        }

        private static int NormalizeThresholdYears(int requested)
        {
            return AllowedAgingThresholdYears.Contains(requested) ? requested : DefaultAgingThresholdYears;
        }

        // ดึงสต็อกทั้งหมด (IN_STOCK/RESERVED) กรองด้วย locationCodes เท่านั้น — ยังไม่กรอง productTypes/golds/goldSizes
        // เพื่อให้ผู้เรียก (Summary) คำนวณ FilterOptions ได้ก่อนกรองต่อ ตาม pattern BuildFilteredSaleLines
        private async Task<List<ResolvedStockLine>> BuildResolvedStockLinesAsync(List<string>? locationCodes)
        {
            var query = _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Where(x => x.Status == "IN_STOCK" || x.Status == "RESERVED");

            if (locationCodes != null && locationCodes.Count > 0)
            {
                query = query.Where(x => locationCodes.Contains(x.LocationCode));
            }

            var rows = await query
                .Select(x => new
                {
                    x.StockNumber,
                    x.StockNumberOrigin,
                    x.SkuCode,
                    x.LocationCode,
                    x.Status,
                    x.Qty,
                    x.QtyReserved,
                    x.ProductionDate,
                    ProductNumber = x.SkuCodeNavigation.ProductNumber,
                    ProductNameTh = x.SkuCodeNavigation.ProductNameTh,
                    ProductType = x.SkuCodeNavigation.ProductType,
                    ProductTypeName = x.SkuCodeNavigation.ProductTypeName,
                    Mold = x.SkuCodeNavigation.Mold,
                    MoldDesign = x.SkuCodeNavigation.MoldDesign,
                    ProductionType = x.SkuCodeNavigation.ProductionType,
                    ProductionTypeSize = x.SkuCodeNavigation.ProductionTypeSize,
                    ImagePath = x.SkuCodeNavigation.ImagePath,
                    ImageName = x.SkuCodeNavigation.ImageName,
                    LocationNameTh = x.LocationCodeNavigation.NameTh,
                    LocationNameEn = x.LocationCodeNavigation.NameEn
                })
                .ToListAsync();

            var today = ThaiToday();

            return rows.Select(x => new ResolvedStockLine
            {
                StockNumber = x.StockNumber,
                StockNumberOrigin = x.StockNumberOrigin,
                SkuCode = x.SkuCode,
                ProductNumber = x.ProductNumber,
                ProductNameTh = x.ProductNameTh,
                ProductType = x.ProductType,
                ProductTypeName = x.ProductTypeName,
                ProductionType = x.ProductionType,
                ProductionTypeSize = x.ProductionTypeSize,
                Design = ResolveDesignKey(x.MoldDesign, x.Mold, x.SkuCode),
                ImagePath = x.ImagePath,
                ImageName = x.ImageName,
                LocationCode = x.LocationCode,
                LocationName = !string.IsNullOrEmpty(x.LocationNameTh)
                    ? x.LocationNameTh
                    : (!string.IsNullOrEmpty(x.LocationNameEn) ? x.LocationNameEn : x.LocationCode),
                Status = x.Status,
                Qty = x.Qty,
                QtyReserved = x.QtyReserved,
                ProductionDate = x.ProductionDate,
                AgeDays = x.ProductionDate.HasValue ? (int)(today - x.ProductionDate.Value.Date).TotalDays : 0
            }).ToList();
        }

        private static List<ResolvedStockLine> ApplyTypeFilters(
            IEnumerable<ResolvedStockLine> lines, List<string>? productTypes, List<string>? golds, List<string>? goldSizes)
        {
            var result = lines;

            if (productTypes != null && productTypes.Count > 0)
            {
                result = result.Where(x => x.ProductType != null && productTypes.Contains(x.ProductType));
            }

            if (golds != null && golds.Count > 0)
            {
                result = result.Where(x => x.ProductionType != null && golds.Contains(x.ProductionType));
            }

            if (goldSizes != null && goldSizes.Count > 0)
            {
                result = result.Where(x => x.ProductionTypeSize != null && goldSizes.Contains(x.ProductionTypeSize));
            }

            return result.ToList();
        }

        private static Summary.FilterOptionsData BuildFilterOptions(List<ResolvedStockLine> lines)
        {
            return new Summary.FilterOptionsData
            {
                Locations = lines
                    .GroupBy(x => x.LocationCode)
                    .Select(g => new Summary.LocationOption { Code = g.Key, Name = g.First().LocationName ?? g.Key })
                    .OrderBy(x => x.Name)
                    .ToList(),
                ProductTypes = lines
                    .Where(x => !string.IsNullOrEmpty(x.ProductType) && !string.IsNullOrEmpty(x.ProductTypeName))
                    .GroupBy(x => x.ProductType!)
                    .Select(g => new Summary.ProductTypeOption { Code = g.Key, Name = g.First().ProductTypeName! })
                    .OrderBy(x => x.Name)
                    .ToList(),
                Golds = lines
                    .Where(x => !string.IsNullOrEmpty(x.ProductionType))
                    .Select(x => x.ProductionType!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                GoldSizes = lines
                    .Where(x => !string.IsNullOrEmpty(x.ProductionTypeSize))
                    .Select(x => x.ProductionTypeSize!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
            };
        }

        // ช่วงวันที่แบบ half-open เวลาไทย เลียนแบบ FilterInvoicesByDateAndChannel ของ SaleReportService เป๊ะ ๆ
        // ห้ามใช้ EndOfDayUtc() — ของเดิมทำให้ช่วงเกินไป 1 วัน
        private IQueryable<TbtSaleInvoiceHeader> FilterInvoicesBySalesRange(DateTimeOffset? start, DateTimeOffset? end)
        {
            var query = _jewelryContext.TbtSaleInvoiceHeader.AsNoTracking().Where(x => x.IsDelete == false);

            if (start.HasValue)
            {
                var fromThaiDate = start.Value.UtcDateTime.AddHours(7).Date;
                query = query.Where(x => x.CreateDate >= fromThaiDate.AddHours(-7));
            }

            if (end.HasValue)
            {
                var toThaiDate = end.Value.UtcDateTime.AddHours(7).Date;
                query = query.Where(x => x.CreateDate < toThaiDate.AddDays(1).AddHours(-7));
            }

            return query;
        }

        // ยอดขาย resolve กลับไปหา SKU ผ่าน stock piece ปัจจุบัน (LocationCode ใช้กรองด้วยเพื่อให้ scope
        // เดียวกับฝั่งสต็อก) แล้วกรอง productTypes/golds/goldSizes ซ้ำเหมือนฝั่งสต็อก
        private async Task<List<ResolvedSoldLine>> BuildResolvedSoldLinesAsync(
            DateTimeOffset? salesStart, DateTimeOffset? salesEnd,
            List<string>? locationCodes, List<string>? productTypes, List<string>? golds, List<string>? goldSizes)
        {
            var invoiceRows = await FilterInvoicesBySalesRange(salesStart, salesEnd)
                .Select(x => new { x.Running, x.CreateDate })
                .ToListAsync();

            if (invoiceRows.Count == 0)
            {
                return new List<ResolvedSoldLine>();
            }

            var invoiceDateByRunning = invoiceRows.ToDictionary(x => x.Running, x => x.CreateDate);
            var runningNumbers = invoiceRows.Select(x => x.Running).ToList();

            var lineRows = await _jewelryContext.TbtSaleOrderProduct
                .AsNoTracking()
                .Where(x => x.Invoice != null && runningNumbers.Contains(x.Invoice))
                .Select(x => new { x.Invoice, x.StockNumber, x.Qty })
                .ToListAsync();

            if (lineRows.Count == 0)
            {
                return new List<ResolvedSoldLine>();
            }

            var stockNumbers = lineRows.Select(x => x.StockNumber).Distinct().ToList();

            // tbt_stock_piece PK เป็น composite (stock_number, product_code) — ห้าม ToDictionary ด้วย
            // StockNumber ตรง ๆ ต้อง GroupBy แล้ว First() เสมอ
            var pieceRows = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Where(x => stockNumbers.Contains(x.StockNumber))
                .Select(x => new { x.StockNumber, x.SkuCode, x.LocationCode })
                .ToListAsync();

            var pieceByStockNumber = pieceRows
                .GroupBy(x => x.StockNumber)
                .ToDictionary(g => g.Key, g => g.First());

            var skuCodes = pieceByStockNumber.Values.Select(x => x.SkuCode).Distinct().ToList();

            var skuRows = await _jewelryContext.TbtSku
                .AsNoTracking()
                .Where(x => skuCodes.Contains(x.SkuCode))
                .Select(x => new
                {
                    x.SkuCode,
                    x.ProductNumber,
                    x.ProductNameTh,
                    x.ProductType,
                    x.ProductTypeName,
                    x.Mold,
                    x.MoldDesign,
                    x.ProductionType,
                    x.ProductionTypeSize,
                    x.ImagePath,
                    x.ImageName
                })
                .ToListAsync();

            var skuBySkuCode = skuRows.GroupBy(x => x.SkuCode).ToDictionary(g => g.Key, g => g.First());

            var resolved = new List<ResolvedSoldLine>();

            foreach (var line in lineRows)
            {
                if (!pieceByStockNumber.TryGetValue(line.StockNumber, out var piece))
                {
                    continue;
                }

                if (locationCodes != null && locationCodes.Count > 0 && !locationCodes.Contains(piece.LocationCode))
                {
                    continue;
                }

                var sku = skuBySkuCode.TryGetValue(piece.SkuCode, out var s) ? s : null;

                resolved.Add(new ResolvedSoldLine
                {
                    StockNumber = line.StockNumber,
                    SkuCode = piece.SkuCode,
                    ProductNumber = sku?.ProductNumber,
                    ProductNameTh = sku?.ProductNameTh,
                    ProductType = sku?.ProductType,
                    ProductTypeName = sku?.ProductTypeName,
                    ProductionType = sku?.ProductionType,
                    ProductionTypeSize = sku?.ProductionTypeSize,
                    Design = ResolveDesignKey(sku?.MoldDesign, sku?.Mold, piece.SkuCode),
                    ImagePath = sku?.ImagePath,
                    ImageName = sku?.ImageName,
                    Qty = line.Qty,
                    InvoiceDate = line.Invoice != null && invoiceDateByRunning.TryGetValue(line.Invoice, out var d) ? d : default
                });
            }

            if (productTypes != null && productTypes.Count > 0)
            {
                resolved = resolved.Where(x => x.ProductType != null && productTypes.Contains(x.ProductType)).ToList();
            }

            if (golds != null && golds.Count > 0)
            {
                resolved = resolved.Where(x => x.ProductionType != null && golds.Contains(x.ProductionType)).ToList();
            }

            if (goldSizes != null && goldSizes.Count > 0)
            {
                resolved = resolved.Where(x => x.ProductionTypeSize != null && goldSizes.Contains(x.ProductionTypeSize)).ToList();
            }

            return resolved;
        }

        public async Task<Summary.Response> Summary(Summary.Request request)
        {
            var allStockLines = await BuildResolvedStockLinesAsync(request.LocationCodes);
            var filterOptions = BuildFilterOptions(allStockLines);

            var filteredLines = ApplyTypeFilters(allStockLines, request.ProductTypes, request.Golds, request.GoldSizes);

            var pieceQty = filteredLines.Sum(x => x.Qty);
            var pieceRowCount = filteredLines.Count;
            var reservedQty = filteredLines.Where(x => x.Status == "RESERVED").Sum(x => x.Qty);
            var designCount = filteredLines.Select(x => x.Design).Distinct().Count();

            var today = ThaiToday();
            var agedCutoff = today.AddYears(-DefaultAgingThresholdYears);
            var agedQty = filteredLines.Where(x => (x.ProductionDate?.Date ?? today) < agedCutoff).Sum(x => x.Qty);
            var agedPercent = pieceQty > 0 ? Math.Round(agedQty / pieceQty * 100, 1, MidpointRounding.AwayFromZero) : 0;

            var soldLines = await BuildResolvedSoldLinesAsync(
                request.SalesStart, request.SalesEnd,
                request.LocationCodes, request.ProductTypes, request.Golds, request.GoldSizes);

            var stockByDesign = filteredLines.GroupBy(x => x.Design).ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));
            var soldByDesign = soldLines.GroupBy(x => x.Design).ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var allDesignKeys = stockByDesign.Keys.Union(soldByDesign.Keys).ToList();

            var overProducedDesignCount = 0;
            var lowStockDesignCount = 0;

            foreach (var key in allDesignKeys)
            {
                var stockQty = stockByDesign.TryGetValue(key, out var sq) ? sq : 0;
                var soldQty = soldByDesign.TryGetValue(key, out var sdq) ? sdq : 0;

                if (stockQty >= 10 && soldQty == 0)
                {
                    overProducedDesignCount++;
                }

                if (soldQty > 0 && stockQty <= 2)
                {
                    lowStockDesignCount++;
                }
            }

            return new Summary.Response
            {
                PieceQty = pieceQty,
                PieceRowCount = pieceRowCount,
                ReservedQty = reservedQty,
                DesignCount = designCount,
                AgedOver2YearsQty = agedQty,
                AgedOver2YearsPercent = agedPercent,
                OverProducedDesignCount = overProducedDesignCount,
                LowStockDesignCount = lowStockDesignCount,
                FilterOptions = filterOptions,
                DataAsOf = DateTime.UtcNow
            };
        }

        public async Task<ProductGroup.Response> ProductGroup(ProductGroup.Request request)
        {
            var stockLines = await BuildResolvedStockLinesAsync(request.LocationCodes);
            var filtered = ApplyTypeFilters(stockLines, request.ProductTypes, request.Golds, request.GoldSizes);

            var groupBy = (request.GroupBy ?? string.Empty).Trim().ToLowerInvariant();

            var groups = filtered
                .GroupBy(x => GroupKey(groupBy, x.ProductType, x.ProductionType, x.ProductionTypeSize))
                .Select(g =>
                {
                    var list = g.ToList();
                    var first = list.First();
                    return new ProductGroup.GroupData
                    {
                        Key = g.Key,
                        Label = GroupLabel(groupBy, g.Key, first.ProductTypeName, first.ProductionType, first.ProductionTypeSize),
                        PieceQty = list.Sum(x => x.Qty),
                        PieceRowCount = list.Count,
                        AvgAgeDays = (int)Math.Round(list.Average(x => x.AgeDays))
                    };
                })
                .ToList();

            var totalPieceQty = filtered.Sum(x => x.Qty);
            var totalPieceRowCount = filtered.Count;

            foreach (var group in groups)
            {
                group.Percent = totalPieceQty > 0 ? Math.Round(group.PieceQty / totalPieceQty * 100, 1, MidpointRounding.AwayFromZero) : 0;
            }

            groups = groups.OrderByDescending(x => x.PieceQty).ToList();

            return new ProductGroup.Response
            {
                Groups = groups,
                TotalPieceQty = totalPieceQty,
                TotalPieceRowCount = totalPieceRowCount
            };
        }

        public async Task<Aging.Response> Aging(Aging.Request request)
        {
            var stockLines = await BuildResolvedStockLinesAsync(request.LocationCodes);
            var filtered = ApplyTypeFilters(stockLines, request.ProductTypes, request.Golds, request.GoldSizes);

            var thresholdYears = NormalizeThresholdYears(request.ThresholdYears);

            var today = ThaiToday();
            var sixMonthsAgo = today.AddMonths(-6);
            var oneYearAgo = today.AddYears(-1);
            var twoYearsAgo = today.AddYears(-2);
            var threeYearsAgo = today.AddYears(-3);
            var fiveYearsAgo = today.AddYears(-5);

            string BucketOf(ResolvedStockLine line)
            {
                var date = line.ProductionDate?.Date ?? today;
                if (date >= sixMonthsAgo) return "lt6m";
                if (date >= oneYearAgo) return "m6to12";
                if (date >= twoYearsAgo) return "y1to2";
                if (date >= threeYearsAgo) return "y2to3";
                if (date >= fiveYearsAgo) return "y3to5";
                return "gt5y";
            }

            var bucketGroups = filtered.GroupBy(BucketOf).ToDictionary(g => g.Key, g => g.ToList());

            var buckets = AgingBucketOrder.Select(key => new Aging.BucketData
            {
                Key = key,
                Label = null,
                PieceQty = bucketGroups.TryGetValue(key, out var list) ? list.Sum(x => x.Qty) : 0,
                PieceRowCount = bucketGroups.TryGetValue(key, out var list2) ? list2.Count : 0
            }).ToList();

            var agedCutoff = today.AddYears(-thresholdYears);
            var agedLines = filtered.Where(x => (x.ProductionDate?.Date ?? today) < agedCutoff).ToList();
            var agedQty = agedLines.Sum(x => x.Qty);
            var agedRowCount = agedLines.Count;
            var totalPieceQty = filtered.Sum(x => x.Qty);

            var groupBy = (request.GroupBy ?? string.Empty).Trim().ToLowerInvariant();

            var topGroups = filtered
                .GroupBy(x => GroupKey(groupBy, x.ProductType, x.ProductionType, x.ProductionTypeSize))
                .Select(g =>
                {
                    var list = g.ToList();
                    var first = list.First();
                    var pieceQty = list.Sum(x => x.Qty);
                    var agedQtyGroup = list.Where(x => (x.ProductionDate?.Date ?? today) < agedCutoff).Sum(x => x.Qty);
                    return new Aging.TopGroupData
                    {
                        Key = g.Key,
                        Label = GroupLabel(groupBy, g.Key, first.ProductTypeName, first.ProductionType, first.ProductionTypeSize),
                        PieceQty = pieceQty,
                        AgedQty = agedQtyGroup,
                        AgedPercent = pieceQty > 0 ? Math.Round(agedQtyGroup / pieceQty * 100, 1, MidpointRounding.AwayFromZero) : 0
                    };
                })
                .OrderByDescending(x => x.AgedQty)
                .Take(15)
                .ToList();

            return new Aging.Response
            {
                Buckets = buckets,
                TopGroups = topGroups,
                AgedQty = agedQty,
                AgedRowCount = agedRowCount,
                ThresholdYears = thresholdYears,
                TotalPieceQty = totalPieceQty
            };
        }

        public async Task<AgingItems.Response> AgingItems(AgingItems.Request request)
        {
            var stockLines = await BuildResolvedStockLinesAsync(request.LocationCodes);
            var filtered = ApplyTypeFilters(stockLines, request.ProductTypes, request.Golds, request.GoldSizes);

            var thresholdYears = NormalizeThresholdYears(request.ThresholdYears);
            var today = ThaiToday();
            var agedCutoff = today.AddYears(-thresholdYears);

            var agedLines = filtered.Where(x => (x.ProductionDate?.Date ?? today) < agedCutoff).ToList();
            var ordered = agedLines.OrderBy(x => x.ProductionDate ?? DateTime.MaxValue).ToList();

            var take = request.Take <= 0 ? 50 : Math.Min(request.Take, 500);
            var skip = Math.Max(request.Skip, 0);

            var page = ordered.Skip(skip).Take(take).Select(x => new AgingItems.ItemData
            {
                StockNumber = x.StockNumber,
                StockNumberOrigin = x.StockNumberOrigin,
                Design = x.Design,
                ProductNumber = x.ProductNumber,
                ProductNameTh = x.ProductNameTh,
                ProductTypeName = x.ProductTypeName,
                ProductionType = x.ProductionType,
                ProductionTypeSize = x.ProductionTypeSize,
                ImageBlobPath = StockImagePath.Build(x.ImagePath, x.ImageName),
                LocationCode = x.LocationCode,
                LocationName = x.LocationName,
                ProductionDate = x.ProductionDate,
                AgeDays = x.AgeDays,
                Qty = x.Qty
            }).ToList();

            return new AgingItems.Response
            {
                Data = page,
                Total = ordered.Count
            };
        }

        public async Task<ProductionBalance.Response> ProductionBalance(ProductionBalance.Request request)
        {
            var groupBy = (request.GroupBy ?? string.Empty).Trim().ToLowerInvariant();

            var stockLines = await BuildResolvedStockLinesAsync(request.LocationCodes);
            var filteredStock = ApplyTypeFilters(stockLines, request.ProductTypes, request.Golds, request.GoldSizes);

            var soldLines = await BuildResolvedSoldLinesAsync(
                request.SalesStart, request.SalesEnd,
                request.LocationCodes, request.ProductTypes, request.Golds, request.GoldSizes);

            var stockGroups = filteredStock
                .GroupBy(x => GroupKey(groupBy, x.ProductType, x.ProductionType, x.ProductionTypeSize))
                .ToDictionary(g => g.Key, g => new
                {
                    Qty = g.Sum(x => x.Qty),
                    Label = GroupLabel(groupBy, g.Key, g.First().ProductTypeName, g.First().ProductionType, g.First().ProductionTypeSize)
                });

            var soldGroups = soldLines
                .GroupBy(x => GroupKey(groupBy, x.ProductType, x.ProductionType, x.ProductionTypeSize))
                .ToDictionary(g => g.Key, g => new
                {
                    Qty = g.Sum(x => x.Qty),
                    Label = GroupLabel(groupBy, g.Key, g.First().ProductTypeName, g.First().ProductionType, g.First().ProductionTypeSize)
                });

            var allKeys = stockGroups.Keys.Union(soldGroups.Keys).ToList();

            var totalStockQty = filteredStock.Sum(x => x.Qty);
            var totalSoldQty = soldLines.Sum(x => x.Qty);

            var groups = new List<ProductionBalance.GroupData>();

            foreach (var key in allKeys)
            {
                var stockQty = stockGroups.TryGetValue(key, out var sg) ? sg.Qty : 0;
                var soldQty = soldGroups.TryGetValue(key, out var sog) ? sog.Qty : 0;
                var label = stockGroups.TryGetValue(key, out var sgl) && sgl.Label != null
                    ? sgl.Label
                    : (soldGroups.TryGetValue(key, out var sogl) ? sogl.Label : null);

                var stockPercent = totalStockQty > 0 ? Math.Round(stockQty / totalStockQty * 100, 1, MidpointRounding.AwayFromZero) : 0;
                var soldPercent = totalSoldQty > 0 ? Math.Round(soldQty / totalSoldQty * 100, 1, MidpointRounding.AwayFromZero) : 0;

                decimal? index = null;
                string status;

                if (totalStockQty <= 0 || stockPercent <= 0)
                {
                    status = "nosale";
                }
                else if (soldQty == 0)
                {
                    status = "nosale";
                }
                else
                {
                    index = Math.Round(soldPercent / stockPercent, 2, MidpointRounding.AwayFromZero);
                    status = index < 0.7m ? "over" : (index > 1.3m ? "under" : "balanced");
                }

                groups.Add(new ProductionBalance.GroupData
                {
                    Key = key,
                    Label = label,
                    StockQty = stockQty,
                    StockPercent = stockPercent,
                    SoldQty = soldQty,
                    SoldPercent = soldPercent,
                    Index = index,
                    Status = status
                });
            }

            groups = groups.OrderByDescending(x => x.StockQty).ToList();

            return new ProductionBalance.Response
            {
                Groups = groups,
                TotalStockQty = totalStockQty,
                TotalSoldQty = totalSoldQty,
                SalesStart = request.SalesStart,
                SalesEnd = request.SalesEnd
            };
        }

        public async Task<DesignAlerts.Response> DesignAlerts(DesignAlerts.Request request)
        {
            var stockLines = await BuildResolvedStockLinesAsync(request.LocationCodes);
            var filteredStock = ApplyTypeFilters(stockLines, request.ProductTypes, request.Golds, request.GoldSizes);

            var soldLines = await BuildResolvedSoldLinesAsync(
                request.SalesStart, request.SalesEnd,
                request.LocationCodes, request.ProductTypes, request.Golds, request.GoldSizes);

            var stockByDesign = filteredStock.GroupBy(x => x.Design).ToDictionary(g => g.Key, g => g.ToList());
            var soldByDesign = soldLines.GroupBy(x => x.Design).ToDictionary(g => g.Key, g => g.ToList());

            var allDesignKeys = stockByDesign.Keys.Union(soldByDesign.Keys).ToList();

            var mode = (request.Mode ?? string.Empty).Trim().ToLowerInvariant();
            var isOverMode = mode == "over";

            var today = ThaiToday();
            var items = new List<DesignAlerts.ItemData>();

            foreach (var key in allDesignKeys)
            {
                var stockList = stockByDesign.TryGetValue(key, out var sl) ? sl : new List<ResolvedStockLine>();
                var soldList = soldByDesign.TryGetValue(key, out var sd) ? sd : new List<ResolvedSoldLine>();

                var stockQty = stockList.Sum(x => x.Qty);
                var soldQty = soldList.Sum(x => x.Qty);

                var matches = isOverMode
                    ? (soldQty == 0 && stockQty >= 10)
                    : (soldQty > 0 && stockQty <= 2);

                if (!matches)
                {
                    continue;
                }

                // sku ตัวแทน: สต็อกมากสุดก่อน ถ้าไม่มีสต็อกเลยใช้ตัวที่ขายล่าสุด
                var repStock = stockList.Count > 0 ? stockList.OrderByDescending(x => x.Qty).First() : null;
                var repSold = repStock == null && soldList.Count > 0 ? soldList.OrderByDescending(x => x.InvoiceDate).First() : null;

                var oldestProductionDate = stockList
                    .Where(x => x.ProductionDate.HasValue)
                    .Select(x => x.ProductionDate!.Value)
                    .OrderBy(x => x)
                    .Cast<DateTime?>()
                    .FirstOrDefault();

                var ageDays = oldestProductionDate.HasValue ? (int)(today - oldestProductionDate.Value.Date).TotalDays : 0;

                var skuCount = stockList.Select(x => x.SkuCode)
                    .Concat(soldList.Select(x => x.SkuCode))
                    .Distinct()
                    .Count();

                items.Add(new DesignAlerts.ItemData
                {
                    Design = key,
                    ProductNumber = repStock?.ProductNumber ?? repSold?.ProductNumber,
                    ProductNameTh = repStock?.ProductNameTh ?? repSold?.ProductNameTh,
                    ProductTypeName = repStock?.ProductTypeName ?? repSold?.ProductTypeName,
                    ProductionType = repStock?.ProductionType ?? repSold?.ProductionType,
                    ProductionTypeSize = repStock?.ProductionTypeSize ?? repSold?.ProductionTypeSize,
                    ImageBlobPath = StockImagePath.Build(
                        repStock?.ImagePath ?? repSold?.ImagePath,
                        repStock?.ImageName ?? repSold?.ImageName),
                    SoldQty = soldQty,
                    StockQty = stockQty,
                    OldestProductionDate = oldestProductionDate,
                    AgeDays = ageDays,
                    SkuCount = skuCount
                });
            }

            var ordered = isOverMode
                ? items.OrderByDescending(x => x.StockQty).ToList()
                : items.OrderByDescending(x => x.SoldQty).ThenBy(x => x.StockQty).ToList();

            var take = request.Take <= 0 ? 50 : Math.Min(request.Take, 200);
            var skip = Math.Max(request.Skip, 0);

            var page = ordered.Skip(skip).Take(take).ToList();

            return new DesignAlerts.Response
            {
                Data = page,
                Total = ordered.Count
            };
        }
    }
}
