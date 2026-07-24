using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using jewelry.Model.Sale.SaleReport.PipelineSummary;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.SaleReport
{
    public class SaleReportService : ISaleReportService
    {
        private readonly JewelryContext _jewelryContext;

        public SaleReportService(JewelryContext jewelryContext)
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
    }
}
