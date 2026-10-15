using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.Production.Insight;
using Jewelry.Service.Production.Shared;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
        private readonly IProductionPlanWipHelper _wipHelper;

        private const int StatusDone = 100;
        private const string InStockStatus = "IN_STOCK";

        public ExecutiveReportService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor, IProductionPlanWipHelper wipHelper)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _wipHelper = wipHelper;
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

            // ล็อตเงินเก็บจำนวน >1 บน piece เดียว (ทอง 1 ชิ้น = 1) — ใช้ถ่วงน้ำหนักควบคู่กับนับแถว
            public decimal Qty { get; set; }
            public string? ProductionType { get; set; }
        }

        // metal ของสต็อก: tbt_stock_piece → tbt_sku.production_type ผ่าน MetalClassifier (ว่าง = GOLD)
        private async Task<List<InStockRow>> GetInStockRowsAsync(string metalFilter)
        {
            var rows = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Where(x => x.Status == InStockStatus)
                .Select(x => new InStockRow
                {
                    ProductCost = x.ProductCost,
                    ProductionDate = x.ProductionDate,
                    ReceiptType = x.ReceiptType,
                    Qty = x.Qty,
                    ProductionType = x.SkuCodeNavigation.ProductionType
                })
                .ToListAsync();

            return metalFilter == MetalClassifier.All
                ? rows
                : rows.Where(x => MetalClassifier.Matches(metalFilter, x.ProductionType)).ToList();
        }

        // แผนเปิดที่กรองตามโลหะ (plan.Type = tbm_gold.name_en) — ALL คืนชุดเดิม
        private async Task<List<OpenPlanRow>> GetOpenPlansAsync(string metalFilter)
        {
            var plans = await _wipHelper.GetOpenPlansAsync();
            return metalFilter == MetalClassifier.All ? plans : plans.Where(p => MetalClassifier.Matches(metalFilter, p.Type)).ToList();
        }

        private class InvoiceMoneyRow
        {
            public string Running { get; set; } = null!;
            public DateTime? DueDate { get; set; }
            public decimal Deposit { get; set; }
            public decimal GrandTotal { get; set; }
            public decimal Rate { get; set; }
            public decimal Paid { get; set; }
            public decimal Share { get; set; } = 1m;
        }

        // เงินระดับ header (GrandTotalRounded, Deposit, paid) ถูกจัดสรรตามสัดส่วนโลหะของบรรทัดขาย — ALL = ไม่คูณ (share=1)
        // บิลที่ share=0 ในโลหะที่ขอ (ไม่มีบรรทัดโลหะนั้นเลย) ถูกตัดออก — สถานะบิล (unpaid/overdue) ตัดสินจากค่าเต็มก่อนคูณ share
        private async Task<List<InvoiceMoneyRow>> GetInvoiceMoneyRowsAsync(string metalFilter)
        {
            var raw = await InScopeInvoices()
                .Select(x => new InvoiceMoneyRow
                {
                    Running = x.Running,
                    DueDate = x.DueDate,
                    Deposit = x.Deposit,
                    GrandTotal = x.GrandTotalRounded ?? 0,
                    Rate = x.CurrencyRate > 0 ? x.CurrencyRate : 1,
                    Paid = _jewelryContext.TbtSaleInvoicePaymentItem
                        .Where(p => p.InvoiceRunning == x.Running && p.IsDelete == false)
                        .Sum(p => (decimal?)p.Amount) ?? 0m
                })
                .ToListAsync();

            if (metalFilter == MetalClassifier.All) return raw;

            var shares = await new SaleMetalResolver(_jewelryContext).ByInvoiceAsync(raw.Select(x => x.Running).ToList());
            foreach (var row in raw)
            {
                row.Share = shares.TryGetValue(row.Running, out var sh) ? sh.ShareOf(metalFilter) : 0m;
            }

            return raw.Where(x => x.Share > 0).ToList();
        }

        public async Task<Summary.Response> Summary(Summary.Request request)
        {
            var now = DateTime.UtcNow;
            var metalFilter = MetalClassifier.Normalize(request.Metal);

            // ---- Production ----
            var openPlans = await GetOpenPlansAsync(metalFilter);
            int meltedOpenCount;
            if (metalFilter == MetalClassifier.All)
            {
                meltedOpenCount = await _wipHelper.GetMeltedOpenCountAsync();
            }
            else
            {
                var meltedTypes = await _jewelryContext.TbtProductionPlan
                    .AsNoTracking()
                    .Where(p => p.IsActive == true && p.Status == 500)
                    .Select(p => p.Type)
                    .ToListAsync();
                meltedOpenCount = meltedTypes.Count(t => MetalClassifier.Matches(metalFilter, t));
            }
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();

            var moved30dCutoff = now.AddDays(-30);
            var stale180dCutoff = now.AddDays(-180);

            var production = new Summary.ProductionData
            {
                OpenCount = openPlans.Count,
                Moved30dCount = openPlans.Count(p => ProductionPlanDepartments.LastMoveOf(p, lastMoveDates) > moved30dCutoff),
                Stale180dCount = openPlans.Count(p => ProductionPlanDepartments.LastMoveOf(p, lastMoveDates) < stale180dCutoff),
                MeltedOpenCount = meltedOpenCount
            };

            // ---- Receivables ----
            var invoiceRows = await GetInvoiceMoneyRowsAsync(metalFilter);

            // ยอดเงินคูณ Share (ALL = 1) — การนับบิล/สถานะ unpaid ใช้ค่าเต็มของบิล (ไม่คูณ)
            var invoiceTotalThb = invoiceRows.Sum(x => x.GrandTotal * x.Rate * x.Share);
            // unpaid = มียอดจริง (grand_total>0) และยังไม่มีเงินเข้าเลย ทั้ง payment_item และมัดจำ
            // (grand_total=0 หรือ deposit>0 แต่ยังไม่มี payment_item ไม่ถือว่า unpaid)
            var unpaidRows = invoiceRows.Where(x => x.GrandTotal > 0 && x.Paid == 0 && x.Deposit <= 0).ToList();
            var paidOrPartialThb = invoiceRows.Where(x => !(x.GrandTotal > 0 && x.Paid == 0 && x.Deposit <= 0)).Sum(x => x.GrandTotal * x.Rate * x.Share);
            var unpaidThb = unpaidRows.Sum(x => x.GrandTotal * x.Rate * x.Share);
            var overdueRows = unpaidRows.Where(x => x.DueDate.HasValue && x.DueDate.Value < now).ToList();
            var overdueThb = overdueRows.Sum(x => x.GrandTotal * x.Rate * x.Share);

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

            var soRowsRaw = await InScopeSalesOrders()
                .Select(x => new
                {
                    x.SoNumber,
                    x.DeliveryDate,
                    GrandTotal = x.GrandTotalRounded ?? 0,
                    Rate = x.CurrencyRate > 0 ? x.CurrencyRate : 1
                })
                .ToListAsync();

            Dictionary<string, SaleMetalShare>? soShares = metalFilter == MetalClassifier.All
                ? null
                : await new SaleMetalResolver(_jewelryContext).BySalesOrderAsync(soRowsRaw.Select(x => x.SoNumber).ToList());

            var soRows = soRowsRaw
                .Select(x => new
                {
                    x.SoNumber,
                    x.DeliveryDate,
                    x.GrandTotal,
                    x.Rate,
                    Share = soShares == null ? 1m : (soShares.TryGetValue(x.SoNumber, out var sh) ? sh.ShareOf(metalFilter) : 0m)
                })
                .Where(x => x.Share > 0)
                .ToList();

            var noInvoiceRows = soRows.Where(x => !invoicedSoNumbers.Contains(x.SoNumber)).ToList();

            var salesOrders = new Summary.SalesOrdersData
            {
                OpenCount = soRows.Count,
                NoInvoiceCount = noInvoiceRows.Count,
                NoInvoiceThb = noInvoiceRows.Sum(x => x.GrandTotal * x.Rate * x.Share),
                OverdueNoInvoiceCount = noInvoiceRows.Count(x => x.DeliveryDate.HasValue && x.DeliveryDate.Value < now),
                NoDeliveryDateCount = noInvoiceRows.Count(x => !x.DeliveryDate.HasValue)
            };

            // ---- Stock ----
            var inStockRows = await GetInStockRowsAsync(metalFilter);
            var agedCutoff1y = now.AddDays(-365);

            var stock = new Summary.StockData
            {
                InStockCount = inStockRows.Count,
                NoCostCount = inStockRows.Count(x => (x.ProductCost ?? 0) == 0),
                CostThb = inStockRows.Sum(x => x.ProductCost ?? 0),
                // production_date ว่างไม่มีจริงใน prod ปัจจุบัน แต่ถ้ามีให้นับเป็นของเก่า (เหมือน StockHealth bucket gt5y)
                AgedOver1yCount = inStockRows.Count(x => !x.ProductionDate.HasValue || x.ProductionDate.Value < agedCutoff1y),
                InStockQty = inStockRows.Sum(x => x.Qty),
                CostThbQty = inStockRows.Sum(x => (x.ProductCost ?? 0) * x.Qty)
            };

            // ---- Gold Loss (เดือนปัจจุบัน + ย้อนหลัง 2 เดือน, เวลาไทย) ----
            var nowThai = now.AddHours(7);
            var currentMonthStartThai = new DateTime(nowThai.Year, nowThai.Month, 1);
            var windowStartThai = currentMonthStartThai.AddMonths(-2);
            var windowStartUtc = DateTime.SpecifyKind(windowStartThai.AddHours(-7), DateTimeKind.Utc);

            var slipRows = await _jewelryContext.TbtGoldLossTangSlip
                .AsNoTracking()
                .Where(x => x.IsActive && x.RequestDateEnd != null && x.RequestDateEnd >= windowStartUtc)
                .Select(x => new { x.Id, x.RequestDateEnd, x.IssuedTotal, x.RawLoss, x.AllowedLoss, x.PricePerGram })
                .ToListAsync();

            // โลหะของใบ tang จัดจาก "item" ของใบ (item.Gold = "SV" → เงิน) — ใบปนโลหะใช้ฝั่งน้ำหนักมากสุด (เหมือน Gold topic)
            // ไม่มี item เลย fallback ดู price_per_gram (< 500 = เงิน) ผ่าน ProductionGoldLossEvaluator.ClassifySlipMetal
            var slipIds = slipRows.Select(x => x.Id).ToList();
            var slipItems = slipIds.Count == 0
                ? new List<ProductionGoldLossEvaluator.TangSlipItemMetalRow>().Select(i => (SlipId: 0L, Item: i)).ToList()
                : (await _jewelryContext.TbtGoldLossTangSlipItem
                    .AsNoTracking()
                    .Where(i => i.IsActive && slipIds.Contains(i.SlipId))
                    .Select(i => new { i.SlipId, i.Gold, i.GoldWeightSend, i.GoldWeightCheck })
                    .ToListAsync())
                    .Select(i => (SlipId: i.SlipId, Item: new ProductionGoldLossEvaluator.TangSlipItemMetalRow { Gold = i.Gold, WeightSend = i.GoldWeightSend, WeightCheck = i.GoldWeightCheck }))
                    .ToList();
            var itemsBySlip = slipItems.GroupBy(i => i.SlipId).ToDictionary(g => g.Key, g => g.Select(x => x.Item).ToList());

            var slipsWithMetal = slipRows
                .Select(x => new
                {
                    x.RequestDateEnd,
                    x.IssuedTotal,
                    x.RawLoss,
                    x.AllowedLoss,
                    Metal = ProductionGoldLossEvaluator.ClassifySlipMetal(
                        itemsBySlip.TryGetValue(x.Id, out var its) ? its : new List<ProductionGoldLossEvaluator.TangSlipItemMetalRow>(),
                        x.PricePerGram).Metal
                })
                .ToList();

            // diff_loss ของตาราง = allowed_loss - raw_loss (บวก = ต่ำกว่าเกณฑ์ ไม่ใช่เกินเกณฑ์) — ห้ามใช้ตรง ๆ
            // over = max(raw - allowed, 0) ต่อใบ แล้วค่อย sum ต่อเดือน
            List<Summary.GoldLossMonthData> BuildGoldLoss(string slipMetal)
            {
                var slipByMonth = slipsWithMetal
                    .Where(x => x.Metal == slipMetal)
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

                var result = new List<Summary.GoldLossMonthData>();
                for (var i = 2; i >= 0; i--)
                {
                    var monthKey = currentMonthStartThai.AddMonths(-i).ToString("yyyy-MM");

                    if (slipByMonth.TryGetValue(monthKey, out var g))
                    {
                        result.Add(new Summary.GoldLossMonthData
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
                        result.Add(new Summary.GoldLossMonthData { Month = monthKey });
                    }
                }

                return result;
            }

            // ALL: goldLoss = ใบทอง + silverLoss = ใบเงิน — GOLD/SILVER: goldLoss = ใบของโลหะที่ขอตัวเดียว (goldLossMetal บอกว่าเป็นอะไร)
            var goldLossMetal = metalFilter == MetalClassifier.Silver ? MetalClassifier.Silver : MetalClassifier.Gold;
            var goldLoss = BuildGoldLoss(goldLossMetal);
            var silverLoss = metalFilter == MetalClassifier.All ? BuildGoldLoss(MetalClassifier.Silver) : new List<Summary.GoldLossMonthData>();

            return new Summary.Response
            {
                AsOf = now,
                Production = production,
                Receivables = receivables,
                SalesOrders = salesOrders,
                Stock = stock,
                Metal = metalFilter,
                GoldLossMetal = goldLossMetal,
                GoldLoss = goldLoss,
                SilverLoss = silverLoss
            };
        }

        public async Task<ProductionWip.Response> ProductionWip(ProductionWip.Request request)
        {
            var now = DateTime.UtcNow;
            var metalFilter = MetalClassifier.Normalize(request.Metal);

            var openPlans = await GetOpenPlansAsync(metalFilter);
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();

            var moved30dCutoff = now.AddDays(-30);
            var stale180dCutoff = now.AddDays(-180);

            var departments = new List<ProductionWip.DepartmentData>();

            foreach (var department in ProductionPlanDepartments.Departments)
            {
                var deptPlans = openPlans.Where(p => department.StatusIds.Contains(p.Status)).ToList();

                var moved30d = deptPlans.Count(p => ProductionPlanDepartments.LastMoveOf(p, lastMoveDates) > moved30dCutoff);
                var stale180d = deptPlans.Count(p => ProductionPlanDepartments.LastMoveOf(p, lastMoveDates) < stale180dCutoff);
                var moved30to180d = deptPlans.Count(p =>
                {
                    var lastMove = ProductionPlanDepartments.LastMoveOf(p, lastMoveDates);
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

            var backdateClosedIds = await BackdateClosedPlans.LoadAsync(_jewelryContext);

            var completedRows = await _jewelryContext.TbtProductionPlan
                .AsNoTracking()
                .Where(p => p.IsActive == true && p.Status == StatusDone
                    && p.CompletedDate != null && p.CompletedDate.Value >= windowStartUtc)
                .Select(p => new { p.Id, CompletedDate = p.CompletedDate!.Value, p.Type })
                .ToListAsync();

            var completedDates = completedRows
                .Where(p => !backdateClosedIds.ContainsKey(p.Id))
                .Where(p => MetalClassifier.Matches(metalFilter, p.Type))
                .Select(p => p.CompletedDate)
                .ToList();

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
            var metalFilter = MetalClassifier.Normalize(request.Metal);

            var openPlans = await GetOpenPlansAsync(metalFilter);
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();
            var statusNames = await _wipHelper.GetStatusNamesAsync();

            var items = openPlans.Select(p =>
            {
                var lastMove = ProductionPlanDepartments.LastMoveOf(p, lastMoveDates);
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
                    DepartmentKey = ProductionPlanDepartments.DepartmentKeyOf(p.Status),
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

            var dataSource = ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);

            // enrich เฉพาะหน้าปัจจุบันหลัง paging (paging skill: post-processing pattern) — ไม่ query ทั้งตาราง
            var pageItems = dataSource.Data?.Cast<StalePlans.Item>().ToList() ?? new List<StalePlans.Item>();
            if (pageItems.Count > 0)
            {
                var plansById = openPlans.ToDictionary(p => p.Id);
                var pagePlans = pageItems.Select(i => plansById[i.PlanId]).ToList();
                var lastActionInfo = await _wipHelper.GetLastActionInfoAsync(pagePlans);

                foreach (var item in pageItems)
                {
                    if (!lastActionInfo.TryGetValue(item.PlanId, out var info)) continue;
                    item.LastUpdateBy = info.LastUpdateBy;
                    item.LastAction = info.LastAction;
                    item.LastActionRemark = info.LastActionRemark;
                    item.LastActionDate = info.LastActionDate;
                    item.Workers = info.Workers;
                    item.WorkerItems = info.WorkerItems.Select(w => new StalePlans.WorkerItem { Code = w.Code, Name = w.Name, IsQueue = w.IsQueue }).ToList();
                }

                dataSource.Data = pageItems;
            }

            return dataSource;
        }

        public async Task<DataSourceResult> Receivables(Receivables.Request request)
        {
            var now = DateTime.UtcNow;
            var metalFilter = MetalClassifier.Normalize(request.Metal);
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
            Dictionary<string, SaleMetalShare>? invoiceShares = metalFilter == MetalClassifier.All
                ? null
                : await new SaleMetalResolver(_jewelryContext).ByInvoiceAsync(rows.Select(r => r.Running).ToList());

            var mapped = rows.Select(x =>
            {
                // เงินระดับ header ถูกจัดสรรตามสัดส่วนโลหะของบรรทัดขาย (ALL = 1) — สถานะบิล (unpaid/overdue) ตัดสินจากค่าเต็ม
                var share = invoiceShares == null ? 1m : (invoiceShares.TryGetValue(x.Running, out var sh) ? sh.ShareOf(metalFilter) : 0m);
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
                    GrandTotal = x.GrandTotal * share,
                    GrandTotalThb = x.GrandTotal * x.Rate * share,
                    PaidAmount = x.Paid * share,
                    Outstanding = outstanding * share,
                    MetalShare = share,
                    PaymentState = outstanding <= 0 ? "paid" : (x.Paid > 0 || x.Deposit > 0 ? "partial" : "unpaid"),
                    IsOverdue = isOverdue,
                    DaysOverdue = isOverdue ? (int)(now - x.DueDate!.Value).TotalDays : 0,
                    // ลูกค้าไม่มี sale_person ผูกไว้เสมอ — fallback ไปคนสร้างเอกสาร (business rule: เจ้าของลูกค้า derive จาก create_by)
                    SalePerson = !string.IsNullOrEmpty(x.SalePerson) ? x.SalePerson : x.CreateBy,
                    SaleChannelCode = x.SaleChannelCode
                };

                return (Item: item, IsUnpaidStrict: isUnpaidStrict);
            }).Where(x => x.Item.MetalShare > 0).ToList();

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
            var metalFilter = MetalClassifier.Normalize(request.Metal);

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

            var noInvoiceSoRows = soRows.Where(x => !invoicedSoNumbers.Contains(x.SoNumber)).ToList();
            Dictionary<string, SaleMetalShare>? soShares = metalFilter == MetalClassifier.All
                ? null
                : await new SaleMetalResolver(_jewelryContext).BySalesOrderAsync(noInvoiceSoRows.Select(r => r.SoNumber).ToList());

            var items = noInvoiceSoRows
                .Select(r => (Row: r, Share: soShares == null ? 1m : (soShares.TryGetValue(r.SoNumber, out var sh) ? sh.ShareOf(metalFilter) : 0m)))
                .Where(t => t.Share > 0)
                .Select(t =>
                {
                    var x = t.Row;
                    var share = t.Share;
                    return new SalesOrdersWithoutInvoice.Item
                {
                    Running = x.Running,
                    SoNumber = x.SoNumber,
                    CustomerCode = x.CustomerCode,
                    CustomerName = x.CustomerName,
                    SoDate = x.SoDate,
                    DeliveryDate = x.DeliveryDate,
                    CurrencyUnit = x.CurrencyUnit,
                    GrandTotal = x.GrandTotal * share,
                    GrandTotalThb = x.GrandTotal * x.Rate * share,
                    MetalShare = share,
                    IsOverdue = x.DeliveryDate.HasValue && x.DeliveryDate.Value < now,
                    // ลูกค้าไม่มี sale_person ผูกไว้เสมอ — fallback ไปคนสร้างเอกสาร (business rule: เจ้าของลูกค้า derive จาก create_by)
                    SalePerson = !string.IsNullOrEmpty(x.SalePerson) ? x.SalePerson : x.CreateBy,
                    SaleChannelCode = x.SaleChannelCode
                };
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
            var metalFilter = MetalClassifier.Normalize(request.Metal);
            var rows = await GetInStockRowsAsync(metalFilter);

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
                    CostThb = list.Sum(x => x.ProductCost ?? 0),
                    Qty = list.Sum(x => x.Qty),
                    CostThbQty = list.Sum(x => (x.ProductCost ?? 0) * x.Qty)
                };
            }).ToList();

            var receiptTypes = rows
                .GroupBy(x => x.ReceiptType)
                .Select(g => new StockHealth.ReceiptTypeData
                {
                    ReceiptType = g.Key,
                    Count = g.Count(),
                    NoCostCount = g.Count(x => (x.ProductCost ?? 0) == 0),
                    Qty = g.Sum(x => x.Qty)
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
