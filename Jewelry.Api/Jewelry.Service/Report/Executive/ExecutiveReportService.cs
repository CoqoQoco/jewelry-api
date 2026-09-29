using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Summary = jewelry.Model.Report.Executive.Summary;
using ProductionWip = jewelry.Model.Report.Executive.ProductionWip;
using StalePlans = jewelry.Model.Report.Executive.StalePlans;
using Receivables = jewelry.Model.Report.Executive.Receivables;
using SalesOrdersWithoutInvoice = jewelry.Model.Report.Executive.SalesOrdersWithoutInvoice;
using StockHealth = jewelry.Model.Report.Executive.StockHealth;

namespace Jewelry.Service.Report.Executive
{
    public class ExecutiveReportService : BaseService, IExecutiveReportService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IMemoryCache _cache;

        private const int StatusDone = 100;
        private const int StatusMelted = 500;
        private const string InStockStatus = "IN_STOCK";
        private const string LastMoveDatesCacheKey = "ExecutiveReport:LastMoveDates";
        private static readonly TimeSpan LastMoveDatesCacheTtl = TimeSpan.FromMinutes(5);

        // แผนก -> status ids ของ tbt_production_plan (ยกเว้น 100=เสร็จ, 500=หลอม)
        private static readonly (string Key, int[] StatusIds)[] Departments = new[]
        {
            ("design", new[] { 10 }),
            ("trim", new[] { 49, 50 }),
            ("rawPolish", new[] { 59, 60 }),
            ("gemSort", new[] { 69, 70 }),
            ("setting", new[] { 79, 80 }),
            ("plating", new[] { 89, 90 }),
            ("costCard", new[] { 94, 95 }),
        };

        public ExecutiveReportService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _cache = cache;
        }

        private static string? DepartmentKeyOf(int status)
        {
            foreach (var department in Departments)
            {
                if (department.StatusIds.Contains(status))
                {
                    return department.Key;
                }
            }

            return null;
        }

        private class OpenPlanRow
        {
            public int Id { get; set; }
            public string Wo { get; set; } = null!;
            public int WoNumber { get; set; }
            public string WoText { get; set; } = null!;
            public string Mold { get; set; } = null!;
            public string ProductNumber { get; set; } = null!;
            public string ProductName { get; set; } = null!;
            public int ProductQty { get; set; }
            public int Status { get; set; }
            public DateTime CreateDate { get; set; }
            public DateTime? UpdateDate { get; set; }
        }

        // เฉพาะ plan ที่ active และยังไม่จบ/ไม่ถูกหลอม (status นอก {100,500})
        private async Task<List<OpenPlanRow>> GetOpenPlansAsync()
        {
            return await _jewelryContext.TbtProductionPlan
                .AsNoTracking()
                .Where(p => p.IsActive == true && p.Status != StatusDone && p.Status != StatusMelted)
                .Select(p => new OpenPlanRow
                {
                    Id = p.Id,
                    Wo = p.Wo,
                    WoNumber = p.WoNumber,
                    WoText = p.WoText,
                    Mold = p.Mold,
                    ProductNumber = p.ProductNumber,
                    ProductName = p.ProductName,
                    ProductQty = p.ProductQty,
                    Status = p.Status,
                    CreateDate = p.CreateDate,
                    UpdateDate = p.UpdateDate
                })
                .ToListAsync();
        }

        private async Task<int> GetMeltedOpenCountAsync()
        {
            return await _jewelryContext.TbtProductionPlan
                .AsNoTracking()
                .CountAsync(p => p.IsActive == true && p.Status == StatusMelted);
        }

        // Max(create_date) ของ tbt_production_plan_status_header ต่อ production_plan_id
        // GroupBy + Max เท่านั้น (ห้าม First() ในนี้ — เคยทำ CPU DB พุ่งมาแล้ว)
        // cache 5 นาที — หน้าเดียวยิง Summary/ProductionWip/StalePlans พร้อมกัน ไม่ต้อง scan ~52k rows ซ้ำ 3 รอบ
        private async Task<Dictionary<int, DateTime>> GetLastMoveDatesAsync()
        {
            if (_cache.TryGetValue<Dictionary<int, DateTime>>(LastMoveDatesCacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var rows = await _jewelryContext.TbtProductionPlanStatusHeader
                .AsNoTracking()
                .GroupBy(h => h.ProductionPlanId)
                .Select(g => new { PlanId = g.Key, LastDate = g.Max(x => x.CreateDate) })
                .ToListAsync();

            var result = rows.ToDictionary(x => x.PlanId, x => x.LastDate);
            _cache.Set(LastMoveDatesCacheKey, result, LastMoveDatesCacheTtl);
            return result;
        }

        private static DateTime LastMoveOf(OpenPlanRow plan, Dictionary<int, DateTime> lastMoveDates)
        {
            return lastMoveDates.TryGetValue(plan.Id, out var lastMove) ? lastMove : (plan.UpdateDate ?? plan.CreateDate);
        }

        private async Task<Dictionary<int, string>> GetStatusNamesAsync()
        {
            return await _jewelryContext.TbmProductionPlanStatus
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.NameTh);
        }

        private IQueryable<TbtSaleInvoiceHeader> InScopeInvoices()
        {
            return _jewelryContext.TbtSaleInvoiceHeader
                .AsNoTracking()
                .Where(x => x.IsDelete == false && x.InvoiceType != "MATERIAL");
        }

        private IQueryable<TbtSaleOrder> InScopeSalesOrders()
        {
            return _jewelryContext.TbtSaleOrder
                .AsNoTracking()
                .Where(x => x.StatusName != "Inactive");
        }

        private class InStockRow
        {
            public decimal? ProductCost { get; set; }
            public DateTime? ProductionDate { get; set; }
            public string? ReceiptType { get; set; }
        }

        private async Task<List<InStockRow>> GetInStockRowsAsync()
        {
            return await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Where(x => x.Status == InStockStatus)
                .Select(x => new InStockRow
                {
                    ProductCost = x.ProductCost,
                    ProductionDate = x.ProductionDate,
                    ReceiptType = x.ReceiptType
                })
                .ToListAsync();
        }

        public async Task<Summary.Response> Summary(Summary.Request request)
        {
            var now = DateTime.UtcNow;

            // ---- Production ----
            var openPlans = await GetOpenPlansAsync();
            var meltedOpenCount = await GetMeltedOpenCountAsync();
            var lastMoveDates = await GetLastMoveDatesAsync();

            var moved30dCutoff = now.AddDays(-30);
            var stale180dCutoff = now.AddDays(-180);

            var production = new Summary.ProductionData
            {
                OpenCount = openPlans.Count,
                Moved30dCount = openPlans.Count(p => LastMoveOf(p, lastMoveDates) > moved30dCutoff),
                Stale180dCount = openPlans.Count(p => LastMoveOf(p, lastMoveDates) < stale180dCutoff),
                MeltedOpenCount = meltedOpenCount
            };

            // ---- Receivables ----
            var invoiceRows = await InScopeInvoices()
                .Select(x => new
                {
                    x.DueDate,
                    x.Deposit,
                    GrandTotal = x.GrandTotalRounded ?? 0,
                    Rate = x.CurrencyRate > 0 ? x.CurrencyRate : 1,
                    Paid = _jewelryContext.TbtSaleInvoicePaymentItem
                        .Where(p => p.InvoiceRunning == x.Running && p.IsDelete == false)
                        .Sum(p => (decimal?)p.Amount) ?? 0m
                })
                .ToListAsync();

            var invoiceTotalThb = invoiceRows.Sum(x => x.GrandTotal * x.Rate);
            // unpaid = มียอดจริง (grand_total>0) และยังไม่มีเงินเข้าเลย ทั้ง payment_item และมัดจำ
            // (grand_total=0 หรือ deposit>0 แต่ยังไม่มี payment_item ไม่ถือว่า unpaid)
            var unpaidRows = invoiceRows.Where(x => x.GrandTotal > 0 && x.Paid == 0 && x.Deposit <= 0).ToList();
            var paidOrPartialThb = invoiceRows.Where(x => !(x.GrandTotal > 0 && x.Paid == 0 && x.Deposit <= 0)).Sum(x => x.GrandTotal * x.Rate);
            var unpaidThb = unpaidRows.Sum(x => x.GrandTotal * x.Rate);
            var overdueRows = unpaidRows.Where(x => x.DueDate.HasValue && x.DueDate.Value < now).ToList();
            var overdueThb = overdueRows.Sum(x => x.GrandTotal * x.Rate);

            var receivables = new Summary.ReceivablesData
            {
                InvoiceCount = invoiceRows.Count,
                InvoiceTotalThb = invoiceTotalThb,
                PaidOrPartialThb = paidOrPartialThb,
                UnpaidCount = unpaidRows.Count,
                UnpaidThb = unpaidThb,
                OverdueCount = overdueRows.Count,
                OverdueThb = overdueThb,
                UnpaidNotOverdueThb = unpaidThb - overdueThb,
                NoDueDateCount = invoiceRows.Count(x => !x.DueDate.HasValue)
            };

            // ---- Sales Orders (without invoice) ----
            var invoicedSoNumbers = new HashSet<string>(
                await InScopeInvoices().Select(x => x.SoRunning).Distinct().ToListAsync());

            var soRows = await InScopeSalesOrders()
                .Select(x => new
                {
                    x.SoNumber,
                    x.DeliveryDate,
                    GrandTotal = x.GrandTotalRounded ?? 0,
                    Rate = x.CurrencyRate > 0 ? x.CurrencyRate : 1
                })
                .ToListAsync();

            var noInvoiceRows = soRows.Where(x => !invoicedSoNumbers.Contains(x.SoNumber)).ToList();

            var salesOrders = new Summary.SalesOrdersData
            {
                OpenCount = soRows.Count,
                NoInvoiceCount = noInvoiceRows.Count,
                NoInvoiceThb = noInvoiceRows.Sum(x => x.GrandTotal * x.Rate),
                OverdueNoInvoiceCount = noInvoiceRows.Count(x => x.DeliveryDate.HasValue && x.DeliveryDate.Value < now),
                NoDeliveryDateCount = noInvoiceRows.Count(x => !x.DeliveryDate.HasValue)
            };

            // ---- Stock ----
            var inStockRows = await GetInStockRowsAsync();
            var agedCutoff1y = now.AddDays(-365);

            var stock = new Summary.StockData
            {
                InStockCount = inStockRows.Count,
                NoCostCount = inStockRows.Count(x => (x.ProductCost ?? 0) == 0),
                CostThb = inStockRows.Sum(x => x.ProductCost ?? 0),
                // production_date ว่างไม่มีจริงใน prod ปัจจุบัน แต่ถ้ามีให้นับเป็นของเก่า (เหมือน StockHealth bucket gt5y)
                AgedOver1yCount = inStockRows.Count(x => !x.ProductionDate.HasValue || x.ProductionDate.Value < agedCutoff1y)
            };

            // ---- Gold Loss (เดือนปัจจุบัน + ย้อนหลัง 2 เดือน, เวลาไทย) ----
            var nowThai = now.AddHours(7);
            var currentMonthStartThai = new DateTime(nowThai.Year, nowThai.Month, 1);
            var windowStartThai = currentMonthStartThai.AddMonths(-2);
            var windowStartUtc = DateTime.SpecifyKind(windowStartThai.AddHours(-7), DateTimeKind.Utc);

            var slipRows = await _jewelryContext.TbtGoldLossTangSlip
                .AsNoTracking()
                .Where(x => x.IsActive && x.RequestDateEnd != null && x.RequestDateEnd >= windowStartUtc)
                .Select(x => new { x.RequestDateEnd, x.IssuedTotal, x.RawLoss, x.AllowedLoss })
                .ToListAsync();

            // diff_loss ของตาราง = allowed_loss - raw_loss (บวก = ต่ำกว่าเกณฑ์ ไม่ใช่เกินเกณฑ์) — ห้ามใช้ตรง ๆ
            // over = max(raw - allowed, 0) ต่อใบ แล้วค่อย sum ต่อเดือน
            var slipByMonth = slipRows
                .GroupBy(x => x.RequestDateEnd!.Value.AddHours(7).ToString("yyyy-MM"))
                .ToDictionary(g => g.Key, g =>
                {
                    var list = g.Select(x => new
                    {
                        Issued = x.IssuedTotal ?? 0,
                        Raw = x.RawLoss ?? 0,
                        Allowed = x.AllowedLoss ?? 0
                    }).ToList();

                    return new
                    {
                        SlipCount = list.Count,
                        IssuedGram = list.Sum(x => x.Issued),
                        RawLossGram = list.Sum(x => x.Raw),
                        AllowedGram = list.Sum(x => x.Allowed),
                        OverAllowedGram = list.Sum(x => Math.Max(x.Raw - x.Allowed, 0)),
                        OverSlipCount = list.Count(x => x.Raw > x.Allowed)
                    };
                });

            var goldLoss = new List<Summary.GoldLossMonthData>();
            for (var i = 2; i >= 0; i--)
            {
                var monthKey = currentMonthStartThai.AddMonths(-i).ToString("yyyy-MM");

                if (slipByMonth.TryGetValue(monthKey, out var g))
                {
                    goldLoss.Add(new Summary.GoldLossMonthData
                    {
                        Month = monthKey,
                        SlipCount = g.SlipCount,
                        IssuedGram = g.IssuedGram,
                        RawLossGram = g.RawLossGram,
                        AllowedGram = g.AllowedGram,
                        OverAllowedGram = g.OverAllowedGram,
                        OverSlipCount = g.OverSlipCount,
                        LossPercent = g.IssuedGram > 0
                            ? Math.Round(g.RawLossGram / g.IssuedGram * 100, 2, MidpointRounding.AwayFromZero)
                            : 0,
                        AllowedPercent = g.IssuedGram > 0
                            ? Math.Round(g.AllowedGram / g.IssuedGram * 100, 2, MidpointRounding.AwayFromZero)
                            : 0,
                        OverAllowedPercent = g.IssuedGram > 0
                            ? Math.Round(g.OverAllowedGram / g.IssuedGram * 100, 2, MidpointRounding.AwayFromZero)
                            : 0
                    });
                }
                else
                {
                    goldLoss.Add(new Summary.GoldLossMonthData { Month = monthKey });
                }
            }

            return new Summary.Response
            {
                AsOf = now,
                Production = production,
                Receivables = receivables,
                SalesOrders = salesOrders,
                Stock = stock,
                GoldLoss = goldLoss
            };
        }

        public async Task<ProductionWip.Response> ProductionWip(ProductionWip.Request request)
        {
            var now = DateTime.UtcNow;

            var openPlans = await GetOpenPlansAsync();
            var lastMoveDates = await GetLastMoveDatesAsync();

            var moved30dCutoff = now.AddDays(-30);
            var stale180dCutoff = now.AddDays(-180);

            var departments = new List<ProductionWip.DepartmentData>();

            foreach (var department in Departments)
            {
                var deptPlans = openPlans.Where(p => department.StatusIds.Contains(p.Status)).ToList();

                var moved30d = deptPlans.Count(p => LastMoveOf(p, lastMoveDates) > moved30dCutoff);
                var stale180d = deptPlans.Count(p => LastMoveOf(p, lastMoveDates) < stale180dCutoff);
                var moved30to180d = deptPlans.Count(p =>
                {
                    var lastMove = LastMoveOf(p, lastMoveDates);
                    return lastMove <= moved30dCutoff && lastMove >= stale180dCutoff;
                });

                departments.Add(new ProductionWip.DepartmentData
                {
                    Key = department.Key,
                    StatusIds = department.StatusIds.ToList(),
                    Total = deptPlans.Count,
                    Moved30d = moved30d,
                    Moved30to180d = moved30to180d,
                    Stale180d = stale180d
                });
            }

            // ---- Monthly Completed (13 เดือนไทยล่าสุด รวมเดือนปัจจุบัน) ----
            var nowThai = now.AddHours(7);
            var currentMonthStartThai = new DateTime(nowThai.Year, nowThai.Month, 1);
            var windowStartThai = currentMonthStartThai.AddMonths(-12);
            var windowStartUtc = DateTime.SpecifyKind(windowStartThai.AddHours(-7), DateTimeKind.Utc);

            var completedDates = await _jewelryContext.TbtProductionPlan
                .AsNoTracking()
                .Where(p => p.IsActive == true && p.Status == StatusDone
                    && p.CompletedDate != null && p.CompletedDate.Value >= windowStartUtc)
                .Select(p => p.CompletedDate!.Value)
                .ToListAsync();

            var completedByMonth = completedDates
                .GroupBy(d => d.AddHours(7).ToString("yyyy-MM"))
                .ToDictionary(g => g.Key, g => g.Count());

            var monthlyCompleted = new List<ProductionWip.MonthlyCompletedData>();
            for (var i = 12; i >= 0; i--)
            {
                var monthKey = currentMonthStartThai.AddMonths(-i).ToString("yyyy-MM");
                monthlyCompleted.Add(new ProductionWip.MonthlyCompletedData
                {
                    Month = monthKey,
                    CompletedCount = completedByMonth.TryGetValue(monthKey, out var count) ? count : 0
                });
            }

            return new ProductionWip.Response
            {
                Departments = departments,
                MonthlyCompleted = monthlyCompleted
            };
        }

        public async Task<DataSourceResult> StalePlans(StalePlans.Request request)
        {
            var now = DateTime.UtcNow;

            var openPlans = await GetOpenPlansAsync();
            var lastMoveDates = await GetLastMoveDatesAsync();
            var statusNames = await GetStatusNamesAsync();

            var items = openPlans.Select(p =>
            {
                var lastMove = LastMoveOf(p, lastMoveDates);
                return new StalePlans.Item
                {
                    PlanId = p.Id,
                    Wo = p.Wo,
                    WoNumber = p.WoNumber,
                    WoText = p.WoText,
                    Mold = p.Mold,
                    ProductNumber = p.ProductNumber,
                    ProductName = p.ProductName,
                    ProductQty = p.ProductQty,
                    StatusId = p.Status,
                    StatusName = statusNames.TryGetValue(p.Status, out var name) ? name : null,
                    DepartmentKey = DepartmentKeyOf(p.Status),
                    CreateDate = p.CreateDate,
                    LastMoveDate = lastMove,
                    DaysSinceMove = (int)(now - lastMove).TotalDays
                };
            })
            .Where(x => x.DaysSinceMove >= request.MinDays)
            .ToList();

            if (request.DepartmentKeys != null && request.DepartmentKeys.Length > 0)
            {
                var departmentKeySet = new HashSet<string>(request.DepartmentKeys);
                items = items.Where(x => x.DepartmentKey != null && departmentKeySet.Contains(x.DepartmentKey)).ToList();
            }

            IEnumerable<StalePlans.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.DaysSinceMove);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        public async Task<DataSourceResult> Receivables(Receivables.Request request)
        {
            var now = DateTime.UtcNow;
            var filter = string.IsNullOrWhiteSpace(request.Filter) ? "unpaid" : request.Filter.Trim().ToLowerInvariant();

            // paid/outstanding: สูตรเดียวกับ InvoiceService.List เป๊ะ ๆ
            // paid = sum(payment_item.amount ที่ไม่ถูกลบ), outstanding = grand_total - deposit - paid
            var rows = await InScopeInvoices()
                .Select(x => new
                {
                    x.Running,
                    x.DkInvoiceNumber,
                    x.SoRunning,
                    x.CustomerCode,
                    x.CustomerName,
                    x.CreateDate,
                    x.CreateBy,
                    x.DueDate,
                    x.CurrencyUnit,
                    x.Deposit,
                    x.SalePerson,
                    x.SaleChannelCode,
                    GrandTotal = x.GrandTotalRounded ?? 0,
                    Rate = x.CurrencyRate > 0 ? x.CurrencyRate : 1,
                    Paid = _jewelryContext.TbtSaleInvoicePaymentItem
                        .Where(p => p.InvoiceRunning == x.Running && p.IsDelete == false)
                        .Sum(p => (decimal?)p.Amount) ?? 0m
                })
                .ToListAsync();

            // unpaid (strict) = มียอดจริง (grand_total>0) และยังไม่มีเงินเข้าเลย ทั้ง payment_item และมัดจำ — ใช้เกณฑ์เดียวกับ Summary
            // เก็บคู่กับ Item ไว้ใช้กรอง filter 'unpaid'/'overdue' เท่านั้น ไม่ขึ้น contract (paymentState ยังใช้สูตร paid/partial/unpaid เดิม)
            var mapped = rows.Select(x =>
            {
                var outstanding = x.GrandTotal - x.Deposit - x.Paid;
                var isOverdue = x.DueDate.HasValue && x.DueDate.Value < now;
                var isUnpaidStrict = x.GrandTotal > 0 && x.Paid == 0 && x.Deposit <= 0;

                var item = new Receivables.Item
                {
                    Running = x.Running,
                    DkInvoiceNumber = x.DkInvoiceNumber,
                    SoRunning = x.SoRunning,
                    CustomerCode = x.CustomerCode,
                    CustomerName = x.CustomerName,
                    CreateDate = x.CreateDate,
                    DueDate = x.DueDate,
                    CurrencyUnit = x.CurrencyUnit,
                    GrandTotal = x.GrandTotal,
                    GrandTotalThb = x.GrandTotal * x.Rate,
                    PaidAmount = x.Paid,
                    Outstanding = outstanding,
                    PaymentState = outstanding <= 0 ? "paid" : (x.Paid > 0 || x.Deposit > 0 ? "partial" : "unpaid"),
                    IsOverdue = isOverdue,
                    DaysOverdue = isOverdue ? (int)(now - x.DueDate!.Value).TotalDays : 0,
                    // ลูกค้าไม่มี sale_person ผูกไว้เสมอ — fallback ไปคนสร้างเอกสาร (business rule: เจ้าของลูกค้า derive จาก create_by)
                    SalePerson = !string.IsNullOrEmpty(x.SalePerson) ? x.SalePerson : x.CreateBy,
                    SaleChannelCode = x.SaleChannelCode
                };

                return (Item: item, IsUnpaidStrict: isUnpaidStrict);
            }).ToList();

            List<Receivables.Item> filtered;
            switch (filter)
            {
                case "overdue":
                    filtered = mapped.Where(x => x.IsUnpaidStrict && x.Item.IsOverdue).Select(x => x.Item).ToList();
                    break;
                case "noduedate":
                    filtered = mapped.Where(x => x.Item.DueDate == null).Select(x => x.Item).ToList();
                    break;
                case "all":
                    filtered = mapped.Select(x => x.Item).ToList();
                    break;
                case "unpaid":
                default:
                    filtered = mapped.Where(x => x.IsUnpaidStrict).Select(x => x.Item).ToList();
                    break;
            }

            IEnumerable<Receivables.Item> ordered = filtered;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = filtered
                    .OrderByDescending(x => x.IsOverdue)
                    .ThenBy(x => x.DueDate == null)
                    .ThenBy(x => x.DueDate);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        public async Task<DataSourceResult> SalesOrdersWithoutInvoice(SalesOrdersWithoutInvoice.Request request)
        {
            var now = DateTime.UtcNow;

            var invoicedSoNumbers = new HashSet<string>(
                await InScopeInvoices().Select(x => x.SoRunning).Distinct().ToListAsync());

            var soRows = await InScopeSalesOrders()
                .Select(x => new
                {
                    x.Running,
                    x.SoNumber,
                    x.CustomerCode,
                    x.CustomerName,
                    x.SoDate,
                    x.DeliveryDate,
                    x.CurrencyUnit,
                    x.SalePerson,
                    x.CreateBy,
                    x.SaleChannelCode,
                    GrandTotal = x.GrandTotalRounded ?? 0,
                    Rate = x.CurrencyRate > 0 ? x.CurrencyRate : 1
                })
                .ToListAsync();

            var items = soRows
                .Where(x => !invoicedSoNumbers.Contains(x.SoNumber))
                .Select(x => new SalesOrdersWithoutInvoice.Item
                {
                    Running = x.Running,
                    SoNumber = x.SoNumber,
                    CustomerCode = x.CustomerCode,
                    CustomerName = x.CustomerName,
                    SoDate = x.SoDate,
                    DeliveryDate = x.DeliveryDate,
                    CurrencyUnit = x.CurrencyUnit,
                    GrandTotal = x.GrandTotal,
                    GrandTotalThb = x.GrandTotal * x.Rate,
                    IsOverdue = x.DeliveryDate.HasValue && x.DeliveryDate.Value < now,
                    // ลูกค้าไม่มี sale_person ผูกไว้เสมอ — fallback ไปคนสร้างเอกสาร (business rule: เจ้าของลูกค้า derive จาก create_by)
                    SalePerson = !string.IsNullOrEmpty(x.SalePerson) ? x.SalePerson : x.CreateBy,
                    SaleChannelCode = x.SaleChannelCode
                })
                .ToList();

            IEnumerable<SalesOrdersWithoutInvoice.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items
                    .OrderBy(x => x.DeliveryDate == null)
                    .ThenBy(x => x.DeliveryDate);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        public async Task<StockHealth.Response> StockHealth(StockHealth.Request request)
        {
            var rows = await GetInStockRowsAsync();

            var now = DateTime.UtcNow;
            var oneYearAgo = now.AddYears(-1);
            var twoYearsAgo = now.AddYears(-2);
            var fiveYearsAgo = now.AddYears(-5);

            // production_date ว่างไม่มีจริงใน prod ปัจจุบัน (28 ก.ย. 2026) แต่ถ้ามีให้จัดกลุ่มเป็น gt5y (เก่าสุด)
            string BucketOf(DateTime? productionDate)
            {
                if (!productionDate.HasValue) return "gt5y";
                var date = productionDate.Value;
                if (date >= oneYearAgo) return "lt1y";
                if (date >= twoYearsAgo) return "y1to2";
                if (date >= fiveYearsAgo) return "y2to5";
                return "gt5y";
            }

            var bucketOrder = new[] { "lt1y", "y1to2", "y2to5", "gt5y" };

            var ageBuckets = bucketOrder.Select(key =>
            {
                var list = rows.Where(x => BucketOf(x.ProductionDate) == key).ToList();
                return new StockHealth.AgeBucketData
                {
                    Key = key,
                    Count = list.Count,
                    NoCostCount = list.Count(x => (x.ProductCost ?? 0) == 0),
                    CostThb = list.Sum(x => x.ProductCost ?? 0)
                };
            }).ToList();

            var receiptTypes = rows
                .GroupBy(x => x.ReceiptType)
                .Select(g => new StockHealth.ReceiptTypeData
                {
                    ReceiptType = g.Key,
                    Count = g.Count(),
                    NoCostCount = g.Count(x => (x.ProductCost ?? 0) == 0)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            return new StockHealth.Response
            {
                AgeBuckets = ageBuckets,
                ReceiptTypes = receiptTypes
            };
        }
    }
}
