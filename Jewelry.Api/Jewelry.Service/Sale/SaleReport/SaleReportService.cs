using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using jewelry.Model.Constant;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ByChannel = jewelry.Model.Sale.SaleReport.ByChannel;
using SalesSummary = jewelry.Model.Sale.SaleReport.SalesSummary;
using ProductGroupSales = jewelry.Model.Sale.SaleReport.ProductGroupSales;
using TopDesignSales = jewelry.Model.Sale.SaleReport.TopDesignSales;
using InvoiceCustomerSuggest = jewelry.Model.Sale.SaleReport.InvoiceCustomerSuggest;

namespace Jewelry.Service.Sale.SaleReport
{
    public class SaleReportService : BaseService, ISaleReportService
    {
        private readonly JewelryContext _jewelryContext;

        public SaleReportService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public async Task<List<jewelry.Model.Sale.SaleReport.CustomerProductionStatus.Response>> CustomerProductionStatus(
            jewelry.Model.Sale.SaleReport.CustomerProductionStatus.Request request)
        {
            var customerCodes = new List<string>();

            if (request.OnlyMyCustomers)
            {
                var quotationCustomers = await _jewelryContext.TbtSaleQuotation
                    .Where(x => x.CreateBy == CurrentUsername && x.CustomerCode != null)
                    .Select(x => x.CustomerCode!)
                    .Distinct()
                    .ToListAsync();

                var orderCustomers = await _jewelryContext.TbtSaleOrder
                    .Where(x => x.CreateBy == CurrentUsername)
                    .Select(x => x.CustomerCode)
                    .Distinct()
                    .ToListAsync();

                customerCodes = quotationCustomers
                    .Union(orderCustomers)
                    .Distinct()
                    .ToList();

                if (!customerCodes.Any())
                {
                    return new List<jewelry.Model.Sale.SaleReport.CustomerProductionStatus.Response>();
                }
            }

            var planQuery = _jewelryContext.TbtProductionPlan
                .AsNoTracking()
                .Where(x => x.IsActive == true);

            if (request.OnlyMyCustomers)
            {
                planQuery = planQuery.Where(x => customerCodes.Contains(x.CustomerNumber));
            }

            var successStatus = new List<int>
            {
                ProductionPlanStatus.Completed,
                ProductionPlanStatus.Melted,
                ProductionPlanStatus.WaitCVD,
                ProductionPlanStatus.CVD,
            };

            // Aggregation (Count/Min) + OrderBy/Skip/Take all translate to SQL —
            // only the paged rows come back from the database.
            var groups = await planQuery
                .GroupBy(x => x.CustomerNumber)
                .Select(g => new
                {
                    CustomerCode = g.Key,
                    TotalPlans = g.Count(),
                    Completed = g.Count(x => successStatus.Contains(x.Status)),
                    InProgress = g.Count(x => !successStatus.Contains(x.Status)),
                    NearestRequestDate = g.Min(x => x.RequestDate)
                })
                .OrderBy(x => x.NearestRequestDate)
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync();

            if (!groups.Any())
            {
                return new List<jewelry.Model.Sale.SaleReport.CustomerProductionStatus.Response>();
            }

            var codes = groups.Select(x => x.CustomerCode).ToList();

            // Greatest-n-per-group doesn't translate — but this query is scoped to
            // only the already-paged customer codes (at most request.Take), not the whole table.
            var latestCandidates = await _jewelryContext.TbtProductionPlan
                .AsNoTracking()
                .Where(x => x.IsActive == true && codes.Contains(x.CustomerNumber))
                .Select(x => new
                {
                    x.CustomerNumber,
                    x.Status,
                    EffectiveDate = x.UpdateDate ?? x.CreateDate
                })
                .ToListAsync();

            var latestStatusByCustomer = latestCandidates
                .GroupBy(x => x.CustomerNumber)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.EffectiveDate).First().Status);

            var customerNames = await _jewelryContext.TbmCustomer
                .AsNoTracking()
                .Where(c => codes.Contains(c.Code))
                .ToDictionaryAsync(c => c.Code, c => c.NameTh);

            var statusIds = latestStatusByCustomer.Values.Distinct().ToList();
            var statusNames = await _jewelryContext.TbmProductionPlanStatus
                .AsNoTracking()
                .Where(s => statusIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.NameTh);

            return groups.Select(x => new jewelry.Model.Sale.SaleReport.CustomerProductionStatus.Response
            {
                CustomerCode = x.CustomerCode,
                CustomerName = customerNames.TryGetValue(x.CustomerCode, out var name) ? name : x.CustomerCode,
                TotalPlans = x.TotalPlans,
                InProgress = x.InProgress,
                Completed = x.Completed,
                NearestRequestDate = x.NearestRequestDate,
                LatestStatusName = latestStatusByCustomer.TryGetValue(x.CustomerCode, out var latestStatus)
                    && statusNames.TryGetValue(latestStatus, out var statusName)
                        ? statusName
                        : string.Empty
            }).ToList();
        }

        public async Task<ByChannel.Response> ByChannel(ByChannel.Request request)
        {
            // Thailand has no DST, so a fixed +7h offset always converts a UTC instant
            // to the correct Thai calendar date/time — this lets us compute exact UTC
            // bounds for the requested Thai date range and filter with plain >=/< at the DB level.
            var fromThaiDate = request.DateFrom.UtcDateTime.AddHours(7).Date;
            var toThaiDate = request.DateTo.UtcDateTime.AddHours(7).Date;

            var fromUtcBound = fromThaiDate.AddHours(-7);
            var toUtcBoundExclusive = toThaiDate.AddDays(1).AddHours(-7);

            var invoiceQuery = _jewelryContext.TbtSaleInvoiceHeader
                .AsNoTracking()
                .Where(x => x.IsDelete == false
                    && x.CreateDate >= fromUtcBound
                    && x.CreateDate < toUtcBoundExclusive);

            // Empty filter = "ทุกจุดขาย": deliberately skip the SaleChannelCode predicate entirely
            // (rather than comparing against null) so old invoices with no SaleChannelCode
            // stay included in the "all channels" view instead of silently dropping out.
            if (!string.IsNullOrEmpty(request.SaleChannelCode))
            {
                invoiceQuery = invoiceQuery.Where(x => x.SaleChannelCode == request.SaleChannelCode);
            }

            // Grouping by Thai-local calendar day doesn't translate reliably to SQL in this
            // project (known EF Core translation gotcha) — pull the filtered rows into memory
            // first (report-scale volume, at most a few hundred rows/month) and do every
            // grouping/aggregation (day, seller, currency, payment) in C# from here on.
            var invoiceRows = await invoiceQuery
                .Select(x => new
                {
                    x.Running,
                    x.CreateDate,
                    x.GrandTotalRounded,
                    x.CurrencyUnit,
                    x.SalePersonUsername,
                    x.CreateBy,
                    x.Payment
                })
                .ToListAsync();

            var runningNumbers = invoiceRows.Select(x => x.Running).Distinct().ToList();

            var productRows = await _jewelryContext.TbtSaleOrderProduct
                .AsNoTracking()
                .Where(x => x.Invoice != null && runningNumbers.Contains(x.Invoice))
                .Select(x => new { x.Invoice, x.StockNumber })
                .ToListAsync();

            var pieceCount = productRows.Count;

            var summary = new ByChannel.SummaryData
            {
                InvoiceCount = invoiceRows.Count,
                PieceCount = pieceCount,
                Amounts = AggregateCurrency(invoiceRows.Select(x => (x.CurrencyUnit, x.GrandTotalRounded)))
            };

            var byDay = invoiceRows
                .GroupBy(x => x.CreateDate.AddHours(7).Date)
                .OrderBy(g => g.Key)
                .Select(g => new ByChannel.ByDayData
                {
                    Date = g.Key,
                    InvoiceCount = g.Count(),
                    Amounts = AggregateCurrency(g.Select(x => (x.CurrencyUnit, x.GrandTotalRounded)))
                })
                .ToList();

            var bySeller = invoiceRows
                .GroupBy(x => !string.IsNullOrEmpty(x.SalePersonUsername) ? x.SalePersonUsername! : x.CreateBy)
                .OrderByDescending(g => g.Count())
                .Select(g => new ByChannel.BySellerData
                {
                    SellerUsername = g.Key,
                    InvoiceCount = g.Count(),
                    Amounts = AggregateCurrency(g.Select(x => (x.CurrencyUnit, x.GrandTotalRounded)))
                })
                .ToList();

            var topProducts = productRows
                .GroupBy(x => x.StockNumber)
                .Select(g => new ByChannel.TopProductData
                {
                    ProductCode = g.Key,
                    PieceCount = g.Count()
                })
                .OrderByDescending(x => x.PieceCount)
                .Take(10)
                .ToList();

            var paymentMix = invoiceRows
                .GroupBy(x => x.Payment)
                .OrderByDescending(g => g.Count())
                .Select(g => new ByChannel.PaymentMixData
                {
                    Payment = g.Key,
                    InvoiceCount = g.Count()
                })
                .ToList();

            return new ByChannel.Response
            {
                Summary = summary,
                ByDay = byDay,
                BySeller = bySeller,
                TopProducts = topProducts,
                PaymentMix = paymentMix
            };
        }

        private static List<ByChannel.CurrencyTotal> AggregateCurrency(IEnumerable<(string CurrencyUnit, decimal? GrandTotalRounded)> rows)
        {
            return rows
                .GroupBy(x => x.CurrencyUnit)
                .Select(g => new ByChannel.CurrencyTotal
                {
                    CurrencyUnit = g.Key,
                    Amount = g.Sum(x => x.GrandTotalRounded ?? 0)
                })
                .OrderBy(x => x.CurrencyUnit)
                .ToList();
        }

        private const string NoSaleChannelCode = "__NONE__";
        private const string UnknownGroupKey = "__UNKNOWN__";
        private const int TopDesignDefaultTake = 10;
        private const int TopDesignMaxTake = 50;

        // Shared date + sale-channel filter for the invoice-based Sales Overview endpoints.
        // Thailand has no DST, so a fixed +7h offset always converts a UTC instant to the
        // correct Thai calendar date — same technique as ByChannel above.
        private IQueryable<TbtSaleInvoiceHeader> FilterInvoicesByDateAndChannel(
            DateTimeOffset? start, DateTimeOffset? end, List<string>? saleChannelCodes)
        {
            var query = _jewelryContext.TbtSaleInvoiceHeader.AsNoTracking().Where(x => x.IsDelete == false);

            if (start.HasValue)
            {
                var fromThaiDate = start.Value.UtcDateTime.AddHours(7).Date;
                var fromUtcBound = fromThaiDate.AddHours(-7);
                query = query.Where(x => x.CreateDate >= fromUtcBound);
            }

            if (end.HasValue)
            {
                var toThaiDate = end.Value.UtcDateTime.AddHours(7).Date;
                var toUtcBoundExclusive = toThaiDate.AddDays(1).AddHours(-7);
                query = query.Where(x => x.CreateDate < toUtcBoundExclusive);
            }

            if (saleChannelCodes != null && saleChannelCodes.Count > 0)
            {
                var includeNone = saleChannelCodes.Contains(NoSaleChannelCode);
                var realCodes = saleChannelCodes.Where(c => c != NoSaleChannelCode).ToList();

                if (includeNone && realCodes.Count > 0)
                {
                    query = query.Where(x => string.IsNullOrEmpty(x.SaleChannelCode) || realCodes.Contains(x.SaleChannelCode!));
                }
                else if (includeNone)
                {
                    query = query.Where(x => string.IsNullOrEmpty(x.SaleChannelCode));
                }
                else
                {
                    query = query.Where(x => x.SaleChannelCode != null && realCodes.Contains(x.SaleChannelCode));
                }
            }

            return query;
        }

        public async Task<SalesSummary.Response> SalesSummary(SalesSummary.Request request)
        {
            var query = FilterInvoicesByDateAndChannel(request.Start, request.End, request.SaleChannelCodes);

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                query = query.Where(x => x.CustomerCode == request.CustomerCode);
            }

            var invoiceRows = await query
                .Select(x => new
                {
                    x.Running,
                    x.GrandTotalRounded,
                    x.Deposit,
                    x.CurrencyRate
                })
                .ToListAsync();

            var runningNumbers = invoiceRows.Select(x => x.Running).ToList();

            var paymentRows = await _jewelryContext.TbtSaleInvoicePaymentItem
                .AsNoTracking()
                .Where(x => x.IsDelete == false && runningNumbers.Contains(x.InvoiceRunning))
                .Select(x => new { x.InvoiceRunning, x.Amount })
                .ToListAsync();

            var paidByInvoice = paymentRows
                .GroupBy(x => x.InvoiceRunning)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var pieceCount = await _jewelryContext.TbtSaleOrderProduct
                .AsNoTracking()
                .Where(x => x.Invoice != null && runningNumbers.Contains(x.Invoice))
                .SumAsync(x => x.Qty);

            var uncomputableInvoiceCount = 0;
            var paidInvoiceCount = 0;
            var outstandingInvoiceCount = 0;
            var overpaidInvoiceCount = 0;
            decimal totalAmountThb = 0;
            decimal paidAmountThb = 0;
            decimal overpaidAmountThb = 0;

            foreach (var invoice in invoiceRows)
            {
                if (invoice.GrandTotalRounded == null)
                {
                    uncomputableInvoiceCount++;
                    continue;
                }

                var rate = invoice.CurrencyRate <= 0 ? 1 : invoice.CurrencyRate;
                var grand = invoice.GrandTotalRounded.Value;
                var paid = paidByInvoice.TryGetValue(invoice.Running, out var paidAmount) ? paidAmount : 0;
                var settled = invoice.Deposit + paid;

                // จำกัดยอดจ่ายไม่ให้เกินยอดบิล (ป้องกันบิลจ่ายเกินดันยอด paid สูงเกินจริง
                // ขณะที่ outstanding ถูก floor ที่ 0 อยู่แล้ว ทำให้ total != paid + outstanding)
                // outstanding ต่อบิลไม่ต้องเก็บแยก เพราะยอดรวมท้ายสุดคำนวณจาก roundedTotal - roundedPaid
                var settledCapped = Math.Min(settled, grand);
                var overpaid = Math.Max(settled - grand, 0);

                if (grand - settled <= 0.005m)
                {
                    paidInvoiceCount++;
                }
                else
                {
                    outstandingInvoiceCount++;
                }

                if (overpaid > 0.005m)
                {
                    overpaidInvoiceCount++;
                }

                totalAmountThb += grand * rate;
                paidAmountThb += settledCapped * rate;
                overpaidAmountThb += overpaid * rate;
            }

            var roundedTotal = Math.Round(totalAmountThb, 2, MidpointRounding.AwayFromZero);
            var roundedPaid = Math.Round(paidAmountThb, 2, MidpointRounding.AwayFromZero);
            var roundedOverpaid = Math.Round(overpaidAmountThb, 2, MidpointRounding.AwayFromZero);

            return new SalesSummary.Response
            {
                InvoiceCount = invoiceRows.Count,
                UncomputableInvoiceCount = uncomputableInvoiceCount,
                TotalAmountThb = roundedTotal,
                PaidAmountThb = roundedPaid,
                OutstandingAmountThb = roundedTotal - roundedPaid,
                OverpaidAmountThb = roundedOverpaid,
                PaidInvoiceCount = paidInvoiceCount,
                OutstandingInvoiceCount = outstandingInvoiceCount,
                OverpaidInvoiceCount = overpaidInvoiceCount,
                PieceCount = pieceCount
            };
        }

        // Composite grouping key + resolved SKU attributes shared by ProductGroupSales and TopDesignSales.
        private class ResolvedSaleLine
        {
            public string StockNumber { get; set; } = null!;
            public string? StockNumberOrigin { get; set; }
            public string SkuCode { get; set; } = string.Empty;
            public string? ProductNumber { get; set; }
            public string? ProductType { get; set; }
            public string? ProductTypeName { get; set; }
            public string? Mold { get; set; }
            public string? MoldDesign { get; set; }
            public string? Size { get; set; }
            public string? ProductionType { get; set; }
            public string? ProductionTypeSize { get; set; }
            public string? ImagePath { get; set; }
            public string? ImageName { get; set; }
            public DateTime InvoiceDate { get; set; }
            public decimal AmountThb { get; set; }
            public decimal Pieces { get; set; }
        }

        // Shared filtering pipeline for the Sales Overview product-level endpoints: applies the
        // date/channel/customer invoice filter, resolves each sold line to its SKU, computes
        // FilterOptions from the resolved lines BEFORE the ProductTypes/Golds/GoldSizes filters
        // (so the filter dropdowns always show every option available in the date range), then
        // applies those filters. Both ProductGroupSales and TopDesignSales build on this.
        private async Task<(List<ResolvedSaleLine> Lines, ProductGroupSales.FilterOptionsData FilterOptions)> BuildFilteredSaleLines(
            DateTimeOffset? start, DateTimeOffset? end, List<string>? saleChannelCodes, string? customerCode,
            List<string>? productTypes, List<string>? golds, List<string>? goldSizes)
        {
            var query = FilterInvoicesByDateAndChannel(start, end, saleChannelCodes);

            if (!string.IsNullOrEmpty(customerCode))
            {
                query = query.Where(x => x.CustomerCode == customerCode);
            }

            var invoiceRows = await query.Select(x => new { x.Running, x.CreateDate }).ToListAsync();
            var invoiceDateByRunning = invoiceRows.ToDictionary(x => x.Running, x => x.CreateDate);
            var runningNumbers = invoiceRows.Select(x => x.Running).ToList();

            var lineRows = await _jewelryContext.TbtSaleOrderProduct
                .AsNoTracking()
                .Where(x => x.Invoice != null && runningNumbers.Contains(x.Invoice))
                .Select(x => new { x.Invoice, x.StockNumber, x.NetPrice, x.Qty })
                .ToListAsync();

            var stockNumbers = lineRows.Select(x => x.StockNumber).Distinct().ToList();

            // tbt_stock_piece PK is composite (stock_number, product_code) — never ToDictionary
            // by StockNumber directly; group by StockNumber then take first.
            // ใช้ tbt_stock_piece.stock_number_origin เป็นเลขที่ผลิตเก่า — tbt_sale_order_product.Stocknumberorigin
            // ยืนยันจาก prod แล้วว่าเก็บรหัสสินค้า (เช่น EF793DI7YL10) ไม่ใช่เลขที่ผลิตเก่า
            var pieceRows = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Where(x => stockNumbers.Contains(x.StockNumber))
                .Select(x => new { x.StockNumber, x.SkuCode, x.StockNumberOrigin })
                .ToListAsync();

            var pieceByStockNumber = pieceRows
                .GroupBy(x => x.StockNumber)
                .ToDictionary(g => g.Key, g => g.First());

            var skuCodeByStockNumber = pieceByStockNumber
                .ToDictionary(kv => kv.Key, kv => kv.Value.SkuCode);

            var skuCodes = skuCodeByStockNumber.Values.Distinct().ToList();

            var skuRows = await _jewelryContext.TbtSku
                .AsNoTracking()
                .Where(x => skuCodes.Contains(x.SkuCode))
                .Select(x => new
                {
                    x.SkuCode,
                    x.ProductNumber,
                    x.ProductType,
                    x.ProductTypeName,
                    x.Mold,
                    x.MoldDesign,
                    x.Size,
                    x.ProductionType,
                    x.ProductionTypeSize,
                    x.ImagePath,
                    x.ImageName
                })
                .ToListAsync();

            var skuBySkuCode = skuRows
                .GroupBy(x => x.SkuCode)
                .ToDictionary(g => g.Key, g => g.First());

            var resolvedLines = lineRows.Select(line =>
            {
                var resolved = new ResolvedSaleLine
                {
                    StockNumber = line.StockNumber,
                    StockNumberOrigin = pieceByStockNumber.TryGetValue(line.StockNumber, out var piece) ? piece.StockNumberOrigin : null,
                    AmountThb = (line.NetPrice ?? 0) * line.Qty,
                    Pieces = line.Qty,
                    InvoiceDate = line.Invoice != null && invoiceDateByRunning.TryGetValue(line.Invoice, out var invoiceDate)
                        ? invoiceDate
                        : default
                };

                if (skuCodeByStockNumber.TryGetValue(line.StockNumber, out var skuCode)
                    && skuBySkuCode.TryGetValue(skuCode, out var sku))
                {
                    resolved.SkuCode = sku.SkuCode;
                    resolved.ProductNumber = sku.ProductNumber;
                    resolved.ProductType = sku.ProductType;
                    resolved.ProductTypeName = sku.ProductTypeName;
                    resolved.Mold = sku.Mold;
                    resolved.MoldDesign = sku.MoldDesign;
                    resolved.Size = sku.Size;
                    resolved.ProductionType = sku.ProductionType;
                    resolved.ProductionTypeSize = sku.ProductionTypeSize;
                    resolved.ImagePath = sku.ImagePath;
                    resolved.ImageName = sku.ImageName;
                }

                return resolved;
            }).ToList();

            var filterOptions = new ProductGroupSales.FilterOptionsData
            {
                ProductTypes = resolvedLines
                    .Where(x => !string.IsNullOrEmpty(x.ProductType) && !string.IsNullOrEmpty(x.ProductTypeName))
                    .GroupBy(x => x.ProductType!)
                    .Select(g => new ProductGroupSales.ProductTypeOption { Code = g.Key, Name = g.First().ProductTypeName! })
                    .OrderBy(x => x.Name)
                    .ToList(),
                Golds = resolvedLines
                    .Where(x => !string.IsNullOrEmpty(x.ProductionType))
                    .Select(x => x.ProductionType!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                GoldSizes = resolvedLines
                    .Where(x => !string.IsNullOrEmpty(x.ProductionTypeSize))
                    .Select(x => x.ProductionTypeSize!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
            };

            var finalLines = resolvedLines.AsEnumerable();

            if (productTypes != null && productTypes.Count > 0)
            {
                finalLines = finalLines.Where(x => x.ProductType != null && productTypes.Contains(x.ProductType));
            }

            if (golds != null && golds.Count > 0)
            {
                finalLines = finalLines.Where(x => x.ProductionType != null && golds.Contains(x.ProductionType));
            }

            if (goldSizes != null && goldSizes.Count > 0)
            {
                finalLines = finalLines.Where(x => x.ProductionTypeSize != null && goldSizes.Contains(x.ProductionTypeSize));
            }

            return (finalLines.ToList(), filterOptions);
        }

        public async Task<ProductGroupSales.Response> ProductGroupSales(ProductGroupSales.Request request)
        {
            var (lines, filterOptions) = await BuildFilteredSaleLines(
                request.Start, request.End, request.SaleChannelCodes, request.CustomerCode,
                request.ProductTypes, request.Golds, request.GoldSizes);

            var groupBy = (request.GroupBy ?? string.Empty).Trim().ToLowerInvariant();

            List<ProductGroupSales.GroupData> groups;

            switch (groupBy)
            {
                case "gold":
                    groups = lines
                        .GroupBy(x => string.IsNullOrEmpty(x.ProductionType) ? UnknownGroupKey : x.ProductionType!)
                        .Select(g => new ProductGroupSales.GroupData
                        {
                            Key = g.Key,
                            Label = g.Key == UnknownGroupKey ? null : g.Key,
                            PieceCount = g.Sum(x => x.Pieces),
                            AmountThb = g.Sum(x => x.AmountThb)
                        })
                        .ToList();
                    break;

                case "goldsize":
                    groups = lines
                        .GroupBy(x => string.IsNullOrEmpty(x.ProductionTypeSize) ? UnknownGroupKey : x.ProductionTypeSize!)
                        .Select(g => new ProductGroupSales.GroupData
                        {
                            Key = g.Key,
                            Label = g.Key == UnknownGroupKey ? null : g.Key,
                            PieceCount = g.Sum(x => x.Pieces),
                            AmountThb = g.Sum(x => x.AmountThb)
                        })
                        .ToList();
                    break;

                case "combined":
                    groups = lines
                        .GroupBy(x => (string.IsNullOrEmpty(x.ProductType) && string.IsNullOrEmpty(x.ProductionType) && string.IsNullOrEmpty(x.ProductionTypeSize))
                            ? UnknownGroupKey
                            : $"{x.ProductType}|{x.ProductionType}|{x.ProductionTypeSize}")
                        .Select(g => new ProductGroupSales.GroupData
                        {
                            Key = g.Key,
                            Label = g.Key == UnknownGroupKey
                                ? null
                                : $"{g.First().ProductTypeName} · {g.First().ProductionType} · {g.First().ProductionTypeSize}",
                            PieceCount = g.Sum(x => x.Pieces),
                            AmountThb = g.Sum(x => x.AmountThb)
                        })
                        .ToList();
                    break;

                default:
                    groups = lines
                        .GroupBy(x => string.IsNullOrEmpty(x.ProductType) ? UnknownGroupKey : x.ProductType!)
                        .Select(g => new ProductGroupSales.GroupData
                        {
                            Key = g.Key,
                            Label = g.Key == UnknownGroupKey ? null : g.First().ProductTypeName,
                            PieceCount = g.Sum(x => x.Pieces),
                            AmountThb = g.Sum(x => x.AmountThb)
                        })
                        .ToList();
                    break;
            }

            foreach (var group in groups)
            {
                group.AmountThb = Math.Round(group.AmountThb, 2, MidpointRounding.AwayFromZero);
            }

            groups = groups
                .OrderByDescending(x => x.PieceCount)
                .ThenByDescending(x => x.AmountThb)
                .ToList();

            return new ProductGroupSales.Response
            {
                Groups = groups,
                TotalPieceCount = lines.Sum(x => x.Pieces),
                TotalAmountThb = Math.Round(lines.Sum(x => x.AmountThb), 2, MidpointRounding.AwayFromZero),
                FilterOptions = filterOptions
            };
        }

        public async Task<TopDesignSales.Response> TopDesignSales(TopDesignSales.Request request)
        {
            var (lines, _) = await BuildFilteredSaleLines(
                request.Start, request.End, request.SaleChannelCodes, request.CustomerCode,
                request.ProductTypes, request.Golds, request.GoldSizes);

            var designGroups = lines
                .GroupBy(x => ToDesignKey(EffectiveMold(x), x.SkuCode))
                .Select(BuildDesignData)
                .ToList();

            var sortBy = (request.SortBy ?? string.Empty).Trim().ToLowerInvariant();

            var sortedDesigns = sortBy == "amount"
                ? designGroups.OrderByDescending(x => x.AmountThb).ThenByDescending(x => x.PieceCount).ThenBy(x => x.DesignKey).ToList()
                : designGroups.OrderByDescending(x => x.PieceCount).ThenByDescending(x => x.AmountThb).ThenBy(x => x.DesignKey).ToList();

            var take = request.Take <= 0 ? TopDesignDefaultTake : Math.Min(request.Take, TopDesignMaxTake);

            return new TopDesignSales.Response
            {
                Designs = sortedDesigns.Take(take).ToList(),
                TotalDesignCount = designGroups.Count,
                TotalPieceCount = lines.Sum(x => x.Pieces),
                TotalAmountThb = Math.Round(lines.Sum(x => x.AmountThb), 2, MidpointRounding.AwayFromZero)
            };
        }

        // Mold ที่มีผล (ไม่ว่าง/ไม่เว้นวรรคล้วน) มาก่อนเสมอ ถ้าไม่มีค่อย fallback ไป MoldDesign —
        // ใช้จุดเดียวกันทุกที่ที่ต้องอ่าน "รหัสแม่พิมพ์ของแถวนี้" กันไม่ให้แต่ละจุดตีความไม่ตรงกัน
        private static string? EffectiveMold(ResolvedSaleLine line) =>
            !string.IsNullOrWhiteSpace(line.Mold) ? line.Mold!.Trim() : (string.IsNullOrWhiteSpace(line.MoldDesign) ? null : line.MoldDesign!.Trim());

        // รหัสแม่พิมพ์ต่อท้ายบอกสีโลหะ: W=ทองขาว, PG=ทองพิงค์โกลด์, -SIL=เงิน, ไม่มีต่อท้าย=ทองเหลือง/เบสสีเหลือง
        // ยืนยันจากข้อมูลจริงบน prod — ลูกค้ายืนยันว่า "520009E กับ 520009EW = แบบเดียวกัน คนละ item"
        // ส่วนต่อท้ายอื่น เช่น -E/-P/-A (คนละ product type) ยังคงไว้ ไม่ตัดออก
        private static string ToDesignKey(string? effectiveMold, string skuCode)
        {
            if (string.IsNullOrEmpty(effectiveMold))
            {
                return skuCode;
            }

            var raw = effectiveMold.ToUpperInvariant();

            var key = Regex.Replace(raw, "-SIL$", "");
            key = Regex.Replace(key, "(W|PG)(-[A-Z]+)?$", "$2");

            return string.IsNullOrEmpty(key) ? raw : key;
        }

        private static TopDesignSales.DesignData BuildDesignData(IGrouping<string, ResolvedSaleLine> group)
        {
            var designKey = group.Key;

            var productTypeName = group
                .Where(x => !string.IsNullOrEmpty(x.ProductTypeName))
                .GroupBy(x => x.ProductTypeName!)
                .OrderByDescending(g => g.Sum(x => x.Pieces))
                .Select(g => g.Key)
                .FirstOrDefault();

            var moldCodes = group
                .Select(x => EffectiveMold(x)?.ToUpperInvariant())
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x!)
                .Distinct()
                .OrderBy(x => x == designKey ? 0 : 1)
                .ThenBy(x => x)
                .ToList();

            var golds = group
                .Where(x => !string.IsNullOrEmpty(x.ProductionType))
                .Select(x => x.ProductionType!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var items = group
                .GroupBy(x => x.StockNumber)
                .Select(g =>
                {
                    var first = g.First();
                    return new TopDesignSales.ItemData
                    {
                        StockNumber = g.Key,
                        StockNumberOrigin = first.StockNumberOrigin,
                        SkuCode = first.SkuCode,
                        ProductNumber = first.ProductNumber,
                        Mold = EffectiveMold(first)?.ToUpperInvariant(),
                        ProductionType = first.ProductionType,
                        ProductionTypeSize = first.ProductionTypeSize,
                        Size = first.Size,
                        ImageBlobPath = StockImagePath.Build(first.ImagePath, first.ImageName),
                        PieceCount = g.Sum(x => x.Pieces),
                        AmountThb = Math.Round(g.Sum(x => x.AmountThb), 2, MidpointRounding.AwayFromZero)
                    };
                })
                .OrderByDescending(x => x.PieceCount)
                .ThenByDescending(x => x.AmountThb)
                .ThenBy(x => x.StockNumber)
                .ToList();

            return new TopDesignSales.DesignData
            {
                DesignKey = designKey,
                ProductTypeName = productTypeName,
                ImageBlobPath = ResolveDesignImage(group, designKey),
                PieceCount = Math.Round(group.Sum(x => x.Pieces), 2, MidpointRounding.AwayFromZero),
                AmountThb = Math.Round(group.Sum(x => x.AmountThb), 2, MidpointRounding.AwayFromZero),
                MoldCodes = moldCodes,
                Golds = golds,
                Items = items
            };
        }

        // เลือกรูปหน้าปกของแบบ: (1) ชื่อไฟล์ตรงกับ DesignKey เป๊ะ, (2) ชื่อไฟล์ตรงกับรหัสแม่พิมพ์ของแถวนั้น
        // (รูปแคตตาล็อกเก่าที่ตั้งชื่อไฟล์ตามแม่พิมพ์), (3) fallback ไปรูปของรายการที่ขายล่าสุด
        private static string? ResolveDesignImage(IEnumerable<ResolvedSaleLine> group, string designKey)
        {
            var candidates = new List<(ResolvedSaleLine Line, string BlobPath)>();

            foreach (var line in group)
            {
                var blobPath = StockImagePath.Build(line.ImagePath, line.ImageName);
                if (blobPath != null)
                {
                    candidates.Add((line, blobPath));
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var byDesignKey = candidates.FirstOrDefault(x =>
                string.Equals(Path.GetFileNameWithoutExtension(x.BlobPath), designKey, StringComparison.OrdinalIgnoreCase));
            if (byDesignKey.Line != null)
            {
                return byDesignKey.BlobPath;
            }

            var byOwnMold = candidates.FirstOrDefault(x =>
            {
                var ownMold = EffectiveMold(x.Line);
                return !string.IsNullOrEmpty(ownMold)
                    && string.Equals(Path.GetFileNameWithoutExtension(x.BlobPath), ownMold, StringComparison.OrdinalIgnoreCase);
            });
            if (byOwnMold.Line != null)
            {
                return byOwnMold.BlobPath;
            }

            return candidates.OrderByDescending(x => x.Line.InvoiceDate).First().BlobPath;
        }

        public async Task<List<InvoiceCustomerSuggest.Response>> InvoiceCustomerSuggest(InvoiceCustomerSuggest.Request request)
        {
            var search = request.Search;

            var query = FilterInvoicesByDateAndChannel(search?.Start, search?.End, search?.SaleChannelCodes)
                .Where(x => !string.IsNullOrEmpty(x.CustomerCode));

            if (search != null && !string.IsNullOrEmpty(search.Text))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(search.Text)}%";
                query = query.Where(x => EF.Functions.ILike(x.CustomerCode, pattern) || EF.Functions.ILike(x.CustomerName, pattern));
            }

            var invoiceRows = await query
                .Select(x => new { x.CustomerCode, x.CustomerName, x.CreateDate })
                .ToListAsync();

            var grouped = invoiceRows
                .GroupBy(x => x.CustomerCode)
                .Select(g =>
                {
                    var latestNamed = g
                        .Where(x => !string.IsNullOrEmpty(x.CustomerName))
                        .OrderByDescending(x => x.CreateDate)
                        .FirstOrDefault();

                    return new
                    {
                        CustomerCode = g.Key,
                        CustomerName = latestNamed?.CustomerName,
                        InvoiceCount = g.Count()
                    };
                })
                .ToList();

            var codes = grouped.Select(x => x.CustomerCode).ToList();

            var customerNames = await _jewelryContext.TbmCustomer
                .AsNoTracking()
                .Where(c => codes.Contains(c.Code))
                .ToDictionaryAsync(c => c.Code, c => c.NameTh);

            var take = request.Take > 0 ? request.Take : 20;

            return grouped
                .Select(x => new InvoiceCustomerSuggest.Response
                {
                    CustomerCode = x.CustomerCode,
                    CustomerName = !string.IsNullOrEmpty(x.CustomerName)
                        ? x.CustomerName!
                        : (customerNames.TryGetValue(x.CustomerCode, out var nameTh) && !string.IsNullOrEmpty(nameTh) ? nameTh : x.CustomerCode),
                    InvoiceCount = x.InvoiceCount
                })
                .OrderByDescending(x => x.InvoiceCount)
                .ThenBy(x => x.CustomerName)
                .Take(take)
                .ToList();
        }
    }
}
