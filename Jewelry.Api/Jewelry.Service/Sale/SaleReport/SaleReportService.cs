using Jewelry.Data.Context;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using jewelry.Model.Constant;
using jewelry.Model.Sale.SaleReport.PipelineSummary;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ByChannel = jewelry.Model.Sale.SaleReport.ByChannel;

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

        public async Task<Response> PipelineSummary(Request request)
        {
            var quotationQuery = _jewelryContext.TbtSaleQuotation.AsNoTracking().AsQueryable();
            var saleOrderQuery = _jewelryContext.TbtSaleOrder.AsNoTracking().AsQueryable();
            var invoiceQuery = _jewelryContext.TbtSaleInvoiceHeader.AsNoTracking().Where(x => x.IsDelete == false).AsQueryable();

            if (request.Start.HasValue)
            {
                var startUtc = request.Start.Value.StartOfDayUtc().UtcDateTime;
                quotationQuery = quotationQuery.Where(x => x.CreateDate >= startUtc);
                saleOrderQuery = saleOrderQuery.Where(x => x.SoDate.HasValue && x.SoDate >= startUtc);
                invoiceQuery = invoiceQuery.Where(x => x.CreateDate >= startUtc);
            }

            if (request.End.HasValue)
            {
                var endUtc = request.End.Value.EndOfDayUtc().UtcDateTime;
                quotationQuery = quotationQuery.Where(x => x.CreateDate <= endUtc);
                saleOrderQuery = saleOrderQuery.Where(x => x.SoDate.HasValue && x.SoDate <= endUtc);
                invoiceQuery = invoiceQuery.Where(x => x.CreateDate <= endUtc);
            }

            var quotationCount = await quotationQuery.CountAsync();
            var saleOrderCount = await saleOrderQuery.CountAsync();
            var invoiceCount = await invoiceQuery.CountAsync();

            var totalQuotationValue = await quotationQuery.SumAsync(x => (decimal?)x.GrandTotalRaw) ?? 0;

            var activeCustomers = await quotationQuery
                .Where(x => x.CustomerCode != null)
                .Select(x => x.CustomerCode)
                .Distinct()
                .CountAsync();

            var conversionRate = quotationCount > 0
                ? Math.Round((decimal)saleOrderCount / quotationCount * 100, 2)
                : 0;

            var monthlyGroups = await quotationQuery
                .Where(x => x.GrandTotalRaw != null)
                .GroupBy(x => new { x.CreateDate.Year, x.CreateDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count(),
                    Value = g.Sum(x => x.GrandTotalRaw ?? 0)
                })
                .ToListAsync();

            var monthlyQuotation = monthlyGroups
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .Select(x => new MonthlyQuotationData
                {
                    Ym = $"{x.Year:D4}-{x.Month:D2}",
                    Count = x.Count,
                    Value = x.Value
                })
                .ToList();

            var customerAgg = await quotationQuery
                .Where(x => x.CustomerCode != null)
                .GroupBy(x => x.CustomerCode)
                .Select(g => new
                {
                    CustomerCode = g.Key!,
                    Count = g.Count(),
                    Value = g.Sum(x => x.GrandTotalRaw ?? 0)
                })
                .OrderByDescending(x => x.Value)
                .Take(10)
                .ToListAsync();

            var topCodes = customerAgg.Select(x => x.CustomerCode).ToList();

            var customerNames = await _jewelryContext.TbmCustomer
                .AsNoTracking()
                .Where(c => topCodes.Contains(c.Code))
                .ToDictionaryAsync(c => c.Code, c => c.NameTh);

            var fallbackRows = await quotationQuery
                .Where(x => x.CustomerCode != null && topCodes.Contains(x.CustomerCode!))
                .Select(x => new { x.CustomerCode, x.CustomerName })
                .ToListAsync();

            var fallbackNames = fallbackRows
                .Where(x => !string.IsNullOrEmpty(x.CustomerName))
                .GroupBy(x => x.CustomerCode!)
                .ToDictionary(g => g.Key, g => g.First().CustomerName);

            var topCustomers = customerAgg.Select(x => new TopCustomerData
            {
                CustomerCode = x.CustomerCode,
                CustomerName = ResolveCustomerName(x.CustomerCode, customerNames, fallbackNames),
                Count = x.Count,
                Value = x.Value
            }).ToList();

            return new Response
            {
                Summary = new SummaryData
                {
                    TotalQuotationValue = totalQuotationValue,
                    QuotationCount = quotationCount,
                    ActiveCustomers = activeCustomers,
                    ConversionRate = conversionRate
                },
                Funnel = new FunnelData
                {
                    QuotationCount = quotationCount,
                    SaleOrderCount = saleOrderCount,
                    InvoiceCount = invoiceCount
                },
                MonthlyQuotation = monthlyQuotation,
                TopCustomers = topCustomers
            };
        }

        private static string ResolveCustomerName(string customerCode, Dictionary<string, string> customerNames, Dictionary<string, string?> fallbackNames)
        {
            if (customerNames.TryGetValue(customerCode, out var nameTh) && !string.IsNullOrEmpty(nameTh))
                return nameTh;

            if (fallbackNames.TryGetValue(customerCode, out var fallback) && !string.IsNullOrEmpty(fallback))
                return fallback!;

            return customerCode;
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
    }
}
