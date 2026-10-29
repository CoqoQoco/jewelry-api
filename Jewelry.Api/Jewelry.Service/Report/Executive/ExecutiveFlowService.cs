using jewelry.Model.Report.Executive.Flow;
using Jewelry.Data.Context;
using Jewelry.Service.Production.Insight;
using Jewelry.Service.Production.Shared;
using Jewelry.Service.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkerEval = Jewelry.Service.Report.Executive.ExecutiveFlowWorkerEvaluator;

namespace Jewelry.Service.Report.Executive
{
    // หน้า Flow (/executive tab Flow) — read-only ทั้งหมด
    // DbContext ไม่ thread-safe: ทุก query รันต่อกันทีละตัว (ห้าม Task.WhenAll)
    // ผลลัพธ์/ชุดข้อมูลหนักถูก cache 5 นาที (IMemoryCache) — AsOf = เวลาที่คำนวณจริง
    public partial class ExecutiveFlowService : IExecutiveFlowService
    {
        private readonly JewelryContext _ctx;
        private readonly IMemoryCache _cache;
        private readonly IProductionPlanWipHelper _wipHelper;
        private readonly IProductionWorkerLookupService _workerLookup;
        private readonly IProductionGoldLossTargetService _targetService;

        private const int StatusDone = 100;
        private const int StatusMelted = 500;
        private const string InStockStatus = "IN_STOCK";
        private const string Thb = "THB";
        private const int LossWindowDays = 90;
        private const string CachePrefix = "ExecutiveFlow:";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        private static readonly int[] StageStatuses = { 50, 60, 70, 80, 90 };
        private static readonly int[] WaitStatuses = { 49, 59, 69, 79, 89, 94 };

        // แผนก -> สถานะ "ทำงานกับช่าง" (work)
        private static readonly Dictionary<string, int> WorkStatusOfDept = new Dictionary<string, int>
        {
            [ProductionGoldStageEvaluator.DeptTrim] = 50,
            [ProductionGoldStageEvaluator.DeptRawPolish] = 60,
            [ProductionGoldStageEvaluator.DeptGemSort] = 70,
            [ProductionGoldStageEvaluator.DeptSetting] = 80,
            [ProductionGoldStageEvaluator.DeptPlating] = 90
        };

        // เป้า STAGE มีเฉพาะ rawPolish=60, setting=80, plating=90 (trim ไม่มีเป้าระดับแผนก — ห้ามสร้างเอง)
        private static readonly Dictionary<string, int> StageTargetWorkerType = new Dictionary<string, int>
        {
            [ProductionGoldStageEvaluator.DeptRawPolish] = 60,
            [ProductionGoldStageEvaluator.DeptSetting] = 80,
            [ProductionGoldStageEvaluator.DeptPlating] = 90
        };

        private static readonly string[] TargetDepts =
        {
            ProductionGoldStageEvaluator.DeptRawPolish, ProductionGoldStageEvaluator.DeptSetting, ProductionGoldStageEvaluator.DeptPlating
        };

        // gemSort ไม่ชั่งจริง จึงไม่นับใน loss ภาพรวมของ board
        private static readonly string[] BoardLossDepts =
        {
            ProductionGoldStageEvaluator.DeptTrim, ProductionGoldStageEvaluator.DeptRawPolish,
            ProductionGoldStageEvaluator.DeptSetting, ProductionGoldStageEvaluator.DeptPlating
        };

        private static readonly string[] Metals = { ProductionGoldLossEvaluator.MetalGold, ProductionGoldLossEvaluator.MetalSilver };

        public ExecutiveFlowService(
            JewelryContext ctx,
            IMemoryCache cache,
            IProductionPlanWipHelper wipHelper,
            IProductionWorkerLookupService workerLookup,
            IProductionGoldLossTargetService targetService)
        {
            _ctx = ctx;
            _cache = cache;
            _wipHelper = wipHelper;
            _workerLookup = workerLookup;
            _targetService = targetService;
        }

        // ---------------------------------------------------------------- datasets

        private class PlanRow
        {
            public int Id { get; set; }
            public int Qty { get; set; }
            public int Status { get; set; }
            public string? Type { get; set; }
            public string? TypeSize { get; set; }
            public string? ProductType { get; set; }
            public DateTime RequestDate { get; set; }
            public DateTime CreateDate { get; set; }
            public DateTime? UpdateDate { get; set; }
        }

        private class PlanAgeRow
        {
            public PlanRow Plan { get; set; } = null!;
            public double AgeDays { get; set; }
        }

        private class StageCell
        {
            public string Dept { get; set; } = null!;
            public string Gold { get; set; } = null!;
            public string? Worker { get; set; }
            public bool Returned { get; set; }
            public bool InWin { get; set; }
            public int Rows { get; set; }
            public decimal Send { get; set; }
            public decimal Check { get; set; }
        }

        private class StageDimCell
        {
            public string Dept { get; set; } = null!;
            public string Gold { get; set; } = null!;
            public string Size { get; set; } = null!;
            public string PType { get; set; } = null!;
            public bool Returned { get; set; }
            public bool InWin { get; set; }
            public int Rows { get; set; }
            public decimal Send { get; set; }
            public decimal Check { get; set; }
        }

        private class StageData
        {
            public List<StageCell> Cells { get; set; } = new List<StageCell>();
            public List<StageDimCell> DimCells { get; set; } = new List<StageDimCell>();
        }

        private class HolderRow
        {
            public int PlanId { get; set; }
            public string? Worker { get; set; }
        }

        // หนึ่งแถว = ชิ้นของล็อตที่คลัง/สถานะ/วันเริ่มนับอายุเดียวกัน (Pcs ชิ้น) — ทุกตัวเลขของโซนสต็อกถ่วงด้วย Pcs
        private class StockRow
        {
            public string Location { get; set; } = null!;
            public string Status { get; set; } = null!;
            public int Pcs { get; set; }
            public decimal? Cost { get; set; }
            public DateTime? AgeDate { get; set; }
            public string? Gold { get; set; }
            public string? Size { get; set; }
            public string? PType { get; set; }
            public string? Mold { get; set; }
        }

        private class InvoiceRow
        {
            public string Running { get; set; } = null!;
            public DateTime CreateDate { get; set; }
            public string CustomerCode { get; set; } = null!;
            public string CustomerName { get; set; } = null!;
            public string Currency { get; set; } = null!;
            public decimal Rate { get; set; }
            public decimal? Total { get; set; }
            public string? SalePerson { get; set; }
            public string? Channel { get; set; }
            public bool IsThb => string.Equals(Currency?.Trim(), Thb, StringComparison.OrdinalIgnoreCase);
        }

        private class MoldPlanRow
        {
            public int Status { get; set; }
            public DateTime CreateDate { get; set; }
            public DateTime? UpdateDate { get; set; }
            public bool IsNewProcess { get; set; }
        }

        private class DoneSummary
        {
            public int ReceivedPcs { get; set; }
            public int ReceivedJobs { get; set; }
            public int NotReceivedPcs { get; set; }
            public int NotReceivedJobs { get; set; }
        }

        private class SeriesData
        {
            public List<string> MonthKeys { get; set; } = new List<string>();
            public Dictionary<string, int> Created { get; set; } = new Dictionary<string, int>();
            public Dictionary<string, int> Completed { get; set; } = new Dictionary<string, int>();
            public Dictionary<string, int> NewStock { get; set; } = new Dictionary<string, int>();
        }

        private async Task<T> CachedAsync<T>(string key, Func<Task<T>> factory) where T : class
        {
            var fullKey = CachePrefix + key;
            if (_cache.TryGetValue<T>(fullKey, out var cached) && cached != null)
            {
                return cached;
            }

            var value = await factory();
            _cache.Set(fullKey, value, CacheTtl);
            return value;
        }

        // เดือนปฏิทินเวลาไทย (เริ่มวันที่ 1 00:00 ไทย) เก่าสุดก่อน — คืนเป็น instant UTC
        private static List<DateTime> ThaiMonthStartsUtc(DateTime nowUtc, int count)
        {
            var thaiNow = nowUtc.AddHours(7);
            var currentStartThai = new DateTime(thaiNow.Year, thaiNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return Enumerable.Range(0, count)
                .Select(i => currentStartThai.AddMonths(-(count - 1) + i).AddHours(-7))
                .ToList();
        }

        private static string MonthKey(DateTime monthStartUtc) => ExecutiveFlowNormalizer.ThaiMonthKey(monthStartUtc);

        private async Task<List<PlanRow>> LoadOpenPlansAsync()
        {
            return await CachedAsync("OpenPlans", async () => await _ctx.TbtProductionPlan
                .AsNoTracking()
                .Where(p => p.IsActive == true && p.Status != StatusDone && p.Status != StatusMelted)
                .Select(p => new PlanRow
                {
                    Id = p.Id,
                    Qty = p.ProductQty,
                    Status = p.Status,
                    Type = p.Type,
                    TypeSize = p.TypeSize,
                    ProductType = p.ProductType,
                    RequestDate = p.RequestDate,
                    CreateDate = p.CreateDate,
                    UpdateDate = p.UpdateDate
                })
                .ToListAsync());
        }

        private async Task<List<PlanAgeRow>> LoadOpenPlansWithAgeAsync(DateTime now)
        {
            var plans = await LoadOpenPlansAsync();
            var lastMoves = await _wipHelper.GetLastMoveDatesAsync();
            return plans.Select(p =>
            {
                var lastMove = lastMoves.TryGetValue(p.Id, out var d) ? d : (p.UpdateDate ?? p.CreateDate);
                return new PlanAgeRow { Plan = p, AgeDays = (now - lastMove).TotalDays };
            }).ToList();
        }

        // 2 scan (ทั้งประวัติ): แยกตามช่าง และแยกตามมิติแผน — SQL GroupBy บน anonymous ที่ project แล้ว (ไม่มี First())
        // rule แถว = เดียวกับ ProductionInsightService.GetGoldStageRowsAsync แต่รวมเป็น cell ใน SQL ทั้งหมด
        private async Task<StageData> LoadStageDataAsync(DateTime now)
        {
            return await CachedAsync("StageData", async () =>
            {
                var winStart = now.AddDays(-LossWindowDays);

                var byWorker = await (
                    from d in _ctx.TbtProductionPlanStatusDetail
                    join h in _ctx.TbtProductionPlanStatusHeader on d.HeaderId equals h.Id
                    where h.IsActive && d.IsActive && d.GoldWeightSend > 0
                        && d.Gold != null && d.Gold != ""
                        && StageStatuses.Contains(h.Status)
                    select new
                    {
                        h.Status,
                        Gold = d.Gold!,
                        Worker = (d.Worker != null && d.Worker != "") ? d.Worker
                            : ((d.WorkerSub != null && d.WorkerSub != "") ? d.WorkerSub : h.WorkerCode),
                        Returned = d.GoldWeightCheck != null,
                        InWin = h.CreateDate >= winStart,
                        Send = d.GoldWeightSend ?? 0m,
                        Check = d.GoldWeightCheck ?? 0m
                    })
                    .AsNoTracking()
                    .GroupBy(x => new { x.Status, x.Gold, x.Worker, x.Returned, x.InWin })
                    .Select(g => new
                    {
                        g.Key.Status,
                        g.Key.Gold,
                        g.Key.Worker,
                        g.Key.Returned,
                        g.Key.InWin,
                        Rows = g.Count(),
                        Send = g.Sum(x => x.Send),
                        Check = g.Sum(x => x.Check)
                    })
                    .ToListAsync();

                var byDim = await (
                    from d in _ctx.TbtProductionPlanStatusDetail
                    join h in _ctx.TbtProductionPlanStatusHeader on d.HeaderId equals h.Id
                    where h.IsActive && d.IsActive && d.GoldWeightSend > 0
                        && d.Gold != null && d.Gold != ""
                        && StageStatuses.Contains(h.Status)
                    select new
                    {
                        h.Status,
                        Gold = d.Gold!,
                        Size = h.ProductionPlan.TypeSize,
                        PType = h.ProductionPlan.ProductType,
                        Returned = d.GoldWeightCheck != null,
                        InWin = h.CreateDate >= winStart,
                        Send = d.GoldWeightSend ?? 0m,
                        Check = d.GoldWeightCheck ?? 0m
                    })
                    .AsNoTracking()
                    .GroupBy(x => new { x.Status, x.Gold, x.Size, x.PType, x.Returned, x.InWin })
                    .Select(g => new
                    {
                        g.Key.Status,
                        g.Key.Gold,
                        g.Key.Size,
                        g.Key.PType,
                        g.Key.Returned,
                        g.Key.InWin,
                        Rows = g.Count(),
                        Send = g.Sum(x => x.Send),
                        Check = g.Sum(x => x.Check)
                    })
                    .ToListAsync();

                return new StageData
                {
                    Cells = byWorker
                        .Select(r => new StageCell
                        {
                            Dept = ProductionPlanDepartments.DepartmentKeyOf(r.Status) ?? string.Empty,
                            Gold = r.Gold.Trim(),
                            Worker = string.IsNullOrWhiteSpace(r.Worker) ? null : r.Worker.Trim(),
                            Returned = r.Returned,
                            InWin = r.InWin,
                            Rows = r.Rows,
                            Send = r.Send,
                            Check = r.Check
                        })
                        .ToList(),
                    DimCells = byDim
                        .Select(r => new StageDimCell
                        {
                            Dept = ProductionPlanDepartments.DepartmentKeyOf(r.Status) ?? string.Empty,
                            Gold = r.Gold.Trim(),
                            Size = ExecutiveFlowNormalizer.NormalizeSize(r.Size),
                            PType = ExecutiveFlowNormalizer.LabelOrUnknown(r.PType),
                            Returned = r.Returned,
                            InWin = r.InWin,
                            Rows = r.Rows,
                            Send = r.Send,
                            Check = r.Check
                        })
                        .ToList()
                };
            });
        }

        private async Task<List<HolderRow>> LoadHoldersAsync()
        {
            return await CachedAsync("Holders", async () =>
            {
                var workStatuses = WorkStatusOfDept.Values.ToArray();
                var rows = await (
                    from d in _ctx.TbtProductionPlanStatusDetail
                    join h in _ctx.TbtProductionPlanStatusHeader on d.HeaderId equals h.Id
                    join p in _ctx.TbtProductionPlan on h.ProductionPlanId equals p.Id
                    where p.IsActive == true && workStatuses.Contains(p.Status) && h.Status == p.Status
                        && h.IsActive && d.IsActive
                    select new
                    {
                        PlanId = p.Id,
                        Worker = (d.Worker != null && d.Worker != "") ? d.Worker
                            : ((d.WorkerSub != null && d.WorkerSub != "") ? d.WorkerSub : h.WorkerCode)
                    })
                    .AsNoTracking()
                    .Distinct()
                    .ToListAsync();

                return rows.Select(r => new HolderRow { PlanId = r.PlanId, Worker = r.Worker }).ToList();
            });
        }

        // ชิ้นที่ยังมีของทั้งหมด (IN_STOCK + RESERVED) จาก StockUnitSnapshot — ผู้ใช้กรอง IN_STOCK ผ่าน InStockOnly
        private async Task<List<StockRow>> LoadStockRowsAsync()
        {
            return await CachedAsync("StockRows", async () =>
            {
                var buckets = await StockUnitSnapshot.LoadAsync(_ctx);
                return buckets.Select(b => new StockRow
                {
                    Location = b.Location,
                    Status = b.Status,
                    Pcs = b.Pcs,
                    Cost = b.Lot.ProductCost,
                    AgeDate = b.AgeDate,
                    Gold = b.Lot.ProductionType,
                    Size = b.Lot.ProductionTypeSize,
                    PType = b.Lot.ProductType,
                    Mold = b.Lot.Mold
                }).ToList();
            });
        }

        private static List<StockRow> InStockOnly(IEnumerable<StockRow> rows) => rows.Where(r => r.Status == InStockStatus).ToList();

        // scope เดียวกับ ExecutiveReportService.InScopeInvoices: ไม่ถูกลบ และไม่ใช่ใบแจ้งหนี้วัตถุดิบ (MATERIAL)
        private async Task<List<InvoiceRow>> LoadInvoicesAsync()
        {
            return await CachedAsync("Invoices", async () => await _ctx.TbtSaleInvoiceHeader
                .AsNoTracking()
                .Where(x => x.IsDelete == false && x.InvoiceType != "MATERIAL")
                .Select(x => new InvoiceRow
                {
                    Running = x.Running,
                    CreateDate = x.CreateDate,
                    CustomerCode = x.CustomerCode,
                    CustomerName = x.CustomerName,
                    Currency = x.CurrencyUnit,
                    Rate = x.CurrencyRate,
                    Total = x.GrandTotalRounded,
                    SalePerson = x.SalePerson,
                    Channel = x.SaleChannelCode
                })
                .ToListAsync());
        }

        private async Task<List<MoldPlanRow>> LoadMoldPlansAsync()
        {
            return await CachedAsync("MoldPlans", async () => await _ctx.TbtProductMoldPlan
                .AsNoTracking()
                .Where(m => m.IsActive == true)
                .Select(m => new MoldPlanRow
                {
                    Status = m.Status,
                    CreateDate = m.CreateDate,
                    UpdateDate = m.UpdateDate,
                    IsNewProcess = m.IsNewProcess
                })
                .ToListAsync());
        }

        // งานเสร็จ (status 100) แยกตามรับเข้าสต็อกแล้ว/ยัง — ตามนิยาม: ไม่กรอง IsActive
        private async Task<DoneSummary> LoadDoneSummaryAsync()
        {
            return await CachedAsync("DoneSummary", async () =>
            {
                var groups = await _ctx.TbtProductionPlan
                    .AsNoTracking()
                    .Where(p => p.Status == StatusDone)
                    .GroupBy(p => p.IsReceipt)
                    .Select(g => new { Received = g.Key, Jobs = g.Count(), Pcs = g.Sum(x => x.ProductQty) })
                    .ToListAsync();

                var received = groups.FirstOrDefault(g => g.Received);
                var notReceived = groups.FirstOrDefault(g => !g.Received);
                return new DoneSummary
                {
                    ReceivedPcs = received?.Pcs ?? 0,
                    ReceivedJobs = received?.Jobs ?? 0,
                    NotReceivedPcs = notReceived?.Pcs ?? 0,
                    NotReceivedJobs = notReceived?.Jobs ?? 0
                };
            });
        }

        // 12 เดือนไทย: plan สร้าง/เสร็จ + ชิ้นสต็อกใหม่ (ดึงเฉพาะคอลัมน์ที่ใช้ในช่วง แล้ว bucket ในหน่วยความจำเพื่อเลี่ยงปัญหา timezone ใน SQL)
        private async Task<SeriesData> LoadSeriesAsync(DateTime now)
        {
            return await CachedAsync("Series", async () =>
            {
                var starts = ThaiMonthStartsUtc(now, 12);
                var windowStart = starts[0];
                var keys = starts.Select(MonthKey).ToList();

                var created = await _ctx.TbtProductionPlan
                    .AsNoTracking()
                    .Where(p => p.IsActive == true && p.CreateDate >= windowStart)
                    .Select(p => new { p.CreateDate, p.ProductQty })
                    .ToListAsync();

                var completed = await _ctx.TbtProductionPlan
                    .AsNoTracking()
                    .Where(p => p.Status == StatusDone && p.CompletedDate != null && p.CompletedDate >= windowStart)
                    .Select(p => new { Date = p.CompletedDate!.Value, p.ProductQty })
                    .ToListAsync();

                // สต็อกใหม่ = Σ qty ของ movement RECEIPT + TRANSFORM_IN ไม่รวมการย้ายสต็อกระบบเก่า (RefDocType ขึ้นต้น TRANSFER_)
                var receipts = (await _ctx.TbtStockMovement
                    .AsNoTracking()
                    .Where(m => m.MovementDate >= windowStart && (m.MovementType == "RECEIPT" || m.MovementType == "TRANSFORM_IN"))
                    .Select(m => new { m.MovementDate, m.MovementType, m.RefDocType, m.Qty })
                    .ToListAsync())
                    .Where(m => StockLedger.IsNewStockReceipt(m.MovementType, m.RefDocType))
                    .ToList();

                var data = new SeriesData { MonthKeys = keys };
                foreach (var k in keys)
                {
                    data.Created[k] = 0;
                    data.Completed[k] = 0;
                    data.NewStock[k] = 0;
                }

                foreach (var r in created)
                {
                    var k = ExecutiveFlowNormalizer.ThaiMonthKey(r.CreateDate);
                    if (data.Created.ContainsKey(k)) data.Created[k] += r.ProductQty;
                }

                foreach (var r in completed)
                {
                    var k = ExecutiveFlowNormalizer.ThaiMonthKey(r.Date);
                    if (data.Completed.ContainsKey(k)) data.Completed[k] += r.ProductQty;
                }

                foreach (var r in receipts)
                {
                    var k = ExecutiveFlowNormalizer.ThaiMonthKey(r.MovementDate);
                    if (data.NewStock.ContainsKey(k)) data.NewStock[k] += (int)Math.Round(r.Qty, MidpointRounding.AwayFromZero);
                }

                return data;
            });
        }

        // ---------------------------------------------------------------- gold-loss helpers

        private static List<ExecutiveFlowLossEvaluator.LossCell> ToLossCells(IEnumerable<StageCell> cells)
        {
            return cells
                .GroupBy(c => (c.Dept, Metal: ProductionGoldLossEvaluator.ClassifyMetal(c.Gold), Worker: c.Worker ?? string.Empty))
                .Select(g => new ExecutiveFlowLossEvaluator.LossCell
                {
                    Dept = g.Key.Dept,
                    Metal = g.Key.Metal,
                    Worker = string.IsNullOrEmpty(g.Key.Worker) ? null : g.Key.Worker,
                    Rows = g.Sum(c => c.Rows),
                    Send = g.Sum(c => c.Send),
                    Check = g.Sum(c => c.Check)
                })
                .ToList();
        }

        private async Task<Func<string, string, (decimal Target, bool IsSystemDefault)?>> BuildTargetLookupAsync()
        {
            var targets = await _targetService.GetCurrentTargetsAsync();
            return (dept, metal) =>
            {
                if (!StageTargetWorkerType.TryGetValue(dept, out var workerType)) return null;
                return targets.TryGetValue(("STAGE", workerType, metal), out var t)
                    ? (t.TargetPercent, string.Equals(t.CreateBy, "system", StringComparison.OrdinalIgnoreCase))
                    : ((decimal, bool)?)null;
            };
        }

        private static Func<string, string> NameResolver(IReadOnlyDictionary<string, string> names)
        {
            return code => names.TryGetValue(code, out var n) && !string.IsNullOrEmpty(n) ? n : code;
        }

        // ---------------------------------------------------------------- FlowBoard

        public async Task<FlowBoardResponse> FlowBoard(FlowBoardRequest request)
        {
            return await CachedAsync("Board", BuildBoardAsync);
        }

        private async Task<FlowBoardResponse> BuildBoardAsync()
        {
            var now = DateTime.UtcNow;
            var names = await _workerLookup.GetWorkerNamesAsync();
            var invoices = await LoadInvoicesAsync();
            var stage = await LoadStageDataAsync(now);
            var stockRows = await LoadStockRowsAsync();
            var plans = await LoadOpenPlansWithAgeAsync(now);
            var doneSummary = await LoadDoneSummaryAsync();
            var moldPlans = await LoadMoldPlansAsync();

            var response = new FlowBoardResponse
            {
                AsOf = new DateTimeOffset(now, TimeSpan.Zero),
                Flow = await BuildBoardFlowAsync(now, invoices),
                GoldLoss = await BuildBoardGoldLossAsync(stage, names),
                GoldNotReturned = BuildGoldNotReturned(stage, names),
                Sales = BuildBoardSales(now, invoices),
                Mix = await BuildMixAsync(plans, stockRows)
            };

            response.Trust = await BuildTrustAsync(now, names, plans, stockRows, invoices, moldPlans, doneSummary);
            return response;
        }

        private async Task<FlowSeriesData> BuildBoardFlowAsync(DateTime now, List<InvoiceRow> invoices)
        {
            var series = await LoadSeriesAsync(now);
            var thbWithTotal = invoices.Where(i => i.IsThb && i.Total.HasValue).ToList();

            var result = new FlowSeriesData();
            foreach (var key in series.MonthKeys)
            {
                var monthInvoices = invoices.Where(i => ExecutiveFlowNormalizer.ThaiMonthKey(i.CreateDate) == key).ToList();
                var monthThb = thbWithTotal.Where(i => ExecutiveFlowNormalizer.ThaiMonthKey(i.CreateDate) == key).ToList();

                result.Months.Add(new FlowMonthData
                {
                    Month = key,
                    CreatedPcs = series.Created[key],
                    CompletedPcs = series.Completed[key],
                    NewStockPieces = series.NewStock[key],
                    InvoiceCount = monthInvoices.Count,
                    InvoiceThb = monthThb.Count > 0 ? monthThb.Sum(i => i.Total!.Value) : (decimal?)null
                });
            }

            result.CreatedPcs12m = result.Months.Sum(m => m.CreatedPcs);
            result.CompletedPcs12m = result.Months.Sum(m => m.CompletedPcs);
            result.BacklogPcs = result.CreatedPcs12m - result.CompletedPcs12m;
            return result;
        }

        private async Task<FlowGoldLossData> BuildBoardGoldLossAsync(StageData stage, IReadOnlyDictionary<string, string> names)
        {
            var win = stage.Cells.Where(c => c.Returned && c.InWin && BoardLossDepts.Contains(c.Dept)).ToList();
            var lossCells = ToLossCells(win);

            decimal Send(string? metal) => lossCells.Where(c => metal == null || c.Metal == metal).Sum(c => c.Send);
            decimal Check(string? metal) => lossCells.Where(c => metal == null || c.Metal == metal).Sum(c => c.Check);

            var targetFor = await BuildTargetLookupAsync();
            var evaluated = ExecutiveFlowLossEvaluator.Evaluate(
                lossCells, TargetDepts, Metals, targetFor,
                w => ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(w, names), NameResolver(names));

            var send = Send(null);
            var check = Check(null);
            return new FlowGoldLossData
            {
                WindowDays = LossWindowDays,
                SendG = ExecutiveFlowNormalizer.Round2(send),
                ReceivedG = ExecutiveFlowNormalizer.Round2(check),
                LossG = ExecutiveFlowNormalizer.Round2(send - check),
                LossPercent = ExecutiveFlowNormalizer.Percent(send - check, send, 2),
                GoldLossPercent = ExecutiveFlowNormalizer.Percent(Send(ProductionGoldLossEvaluator.MetalGold) - Check(ProductionGoldLossEvaluator.MetalGold), Send(ProductionGoldLossEvaluator.MetalGold), 2),
                SilverLossPercent = ExecutiveFlowNormalizer.Percent(Send(ProductionGoldLossEvaluator.MetalSilver) - Check(ProductionGoldLossEvaluator.MetalSilver), Send(ProductionGoldLossEvaluator.MetalSilver), 2),
                ExcessOverTargetG = ExecutiveFlowLossEvaluator.TotalExcessG(evaluated),
                WorkersOverTargetCount = ExecutiveFlowLossEvaluator.CountWorkersOverTarget(evaluated)
            };
        }

        private static FlowGoldNotReturnedData BuildGoldNotReturned(StageData stage, IReadOnlyDictionary<string, string> names)
        {
            var pending = stage.Cells.Where(c => !c.Returned).ToList();
            var total = pending.Sum(c => c.Send);
            var queue = pending.Where(c => ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(c.Worker, names)).Sum(c => c.Send);

            return new FlowGoldNotReturnedData
            {
                GoldG = ExecutiveFlowNormalizer.Round2(pending.Where(c => ProductionGoldLossEvaluator.ClassifyMetal(c.Gold) == ProductionGoldLossEvaluator.MetalGold).Sum(c => c.Send)),
                SilverG = ExecutiveFlowNormalizer.Round2(pending.Where(c => ProductionGoldLossEvaluator.ClassifyMetal(c.Gold) == ProductionGoldLossEvaluator.MetalSilver).Sum(c => c.Send)),
                QueueSharePercent = ExecutiveFlowNormalizer.Percent(queue, total)
            };
        }

        private const string BucketCode = "WALKIN";
        private const string BucketName = "WALKIN (รหัสรวมลูกค้าหน้าร้าน)";

        private static bool IsBucketCode(string? code) => string.Equals(code?.Trim(), BucketCode, StringComparison.OrdinalIgnoreCase);

        private static List<FlowCustomerShare> BuildCustomerShares(List<InvoiceRow> thbWithTotal, int top)
        {
            var total = thbWithTotal.Sum(i => i.Total!.Value);
            return thbWithTotal
                .GroupBy(i => i.CustomerCode)
                .Select(g => new
                {
                    Code = g.Key,
                    Name = IsBucketCode(g.Key) ? BucketName : g.OrderByDescending(i => i.CreateDate).First().CustomerName,
                    Count = g.Count(),
                    Thb = g.Sum(i => i.Total!.Value)
                })
                .OrderByDescending(x => x.Thb)
                .Take(top)
                .Select(x => new FlowCustomerShare
                {
                    Code = x.Code,
                    Name = x.Name,
                    InvoiceCount = x.Count,
                    Thb = x.Thb,
                    SharePercent = ExecutiveFlowNormalizer.Percent(x.Thb, total),
                    IsBucket = IsBucketCode(x.Code)
                })
                .ToList();
        }

        private static FlowBoardSalesData BuildBoardSales(DateTime now, List<InvoiceRow> invoices)
        {
            var thbWithTotal = invoices.Where(i => i.IsThb && i.Total.HasValue).ToList();
            var starts = ThaiMonthStartsUtc(now, 2);
            var previousKey = MonthKey(starts[0]);
            var currentKey = MonthKey(starts[1]);

            var current = thbWithTotal.Where(i => ExecutiveFlowNormalizer.ThaiMonthKey(i.CreateDate) == currentKey).Sum(i => i.Total!.Value);
            var previous = thbWithTotal.Where(i => ExecutiveFlowNormalizer.ThaiMonthKey(i.CreateDate) == previousKey).Sum(i => i.Total!.Value);
            var total = thbWithTotal.Sum(i => i.Total!.Value);
            var topCustomers = BuildCustomerShares(thbWithTotal, 5);
            var top2Named = thbWithTotal.Where(i => !IsBucketCode(i.CustomerCode)).GroupBy(i => i.CustomerCode)
                .Select(g => g.Sum(i => i.Total!.Value)).OrderByDescending(v => v).Take(2).Sum();

            return new FlowBoardSalesData
            {
                ThbTotalWithAmount = total,
                FirstDateWithTotal = thbWithTotal.Count > 0
                    ? thbWithTotal.Min(i => i.CreateDate).AddHours(7).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    : null,
                CurrentMonthThb = current,
                PreviousMonthThb = previous,
                CurrentVsPreviousPercent = ExecutiveFlowNormalizer.Percent(current, previous),
                Top2CustomerSharePercent = ExecutiveFlowNormalizer.Percent(top2Named, total),
                TopCustomers = topCustomers,
                UsdInvoiceCount = invoices.Count(i => !i.IsThb),
                UsdAmountsHeld = true,
                PaymentHeld = true
            };
        }

        // ---------------------------------------------------------------- mix

        private static readonly string[] MixSizeOrder = { "18K", "9K", "14K", "10K", "Silver", ExecutiveFlowNormalizer.Unknown };

        private static FlowMixPopulation BuildMixPopulation(IEnumerable<(string Gold, string PType, string Size, decimal Pcs)> items)
        {
            var list = items.ToList();
            var total = (int)Math.Round(list.Sum(i => i.Pcs), MidpointRounding.AwayFromZero);
            var population = new FlowMixPopulation { TotalPcs = total };

            foreach (var gold in ExecutiveFlowNormalizer.PlanGoldOrder)
            {
                var pcs = (int)Math.Round(list.Where(i => i.Gold == gold).Sum(i => i.Pcs), MidpointRounding.AwayFromZero);
                population.Gold.Add(new FlowShareItem { Label = gold, Pcs = pcs, Percent = ExecutiveFlowNormalizer.Percent(pcs, total) });
            }

            foreach (var size in MixSizeOrder)
            {
                var pcs = (int)Math.Round(list.Where(i => i.Size == size).Sum(i => i.Pcs), MidpointRounding.AwayFromZero);
                population.Size.Add(new FlowShareItem { Label = size, Pcs = pcs, Percent = ExecutiveFlowNormalizer.Percent(pcs, total) });
            }

            var types = list
                .GroupBy(i => i.PType)
                .Select(g => (Label: g.Key, Pcs: (int)Math.Round(g.Sum(i => i.Pcs), MidpointRounding.AwayFromZero)));

            foreach (var t in ExecutiveFlowNormalizer.TopWithOther(types, 8, _ => "Other"))
            {
                population.ProductType.Add(new FlowShareItem { Label = t.Label, Pcs = t.Pcs, Percent = ExecutiveFlowNormalizer.Percent(t.Pcs, total) });
            }

            return population;
        }

        private async Task<FlowMixData> BuildMixAsync(List<PlanAgeRow> plans, List<StockRow> stockRows)
        {
            var wip = BuildMixPopulation(plans.Select(p => (
                ExecutiveFlowNormalizer.NormalizePlanGold(p.Plan.Type),
                ExecutiveFlowNormalizer.LabelOrUnknown(p.Plan.ProductType),
                ExecutiveFlowNormalizer.NormalizeSize(p.Plan.TypeSize),
                (decimal)p.Plan.Qty)));

            var stock = BuildMixPopulation(InStockOnly(stockRows).Select(s => (
                ExecutiveFlowNormalizer.NormalizePlanGold(s.Gold),
                ExecutiveFlowNormalizer.LabelOrUnknown(s.PType),
                ExecutiveFlowNormalizer.NormalizeSize(s.Size),
                (decimal)s.Pcs)));

            return new FlowMixData
            {
                Wip = wip,
                Stock = stock,
                Sold = BuildMixPopulation(await LoadSoldMixItemsAsync())
            };
        }

        // ขายแล้ว = Σ qty ของ tbt_sale_order_product ที่ออกใบแจ้งหนี้แล้ว (Invoice != null) ต่อ stock_number -> (piece ใดก็ได้ 1 แถวต่อ stock_number) -> sku
        // ไม่ใช้ ToDictionary บน piece ดิบ (PK คือ stock_number + product_code) — GroupBy ก่อนแล้วเลือก 1 แถวในหน่วยความจำ
        private async Task<List<(string Gold, string PType, string Size, decimal Pcs)>> LoadSoldMixItemsAsync()
        {
            var sold = await _ctx.TbtSaleOrderProduct
                .AsNoTracking()
                .Where(p => p.Invoice != null)
                .GroupBy(p => p.StockNumber)
                .Select(g => new { StockNumber = g.Key, Qty = g.Sum(x => x.Qty) })
                .ToListAsync();

            var numbers = sold.Select(s => s.StockNumber).Where(s => !string.IsNullOrEmpty(s)).ToList();

            var pieces = numbers.Count == 0
                ? new List<(string StockNumber, string SkuCode)>()
                : (await _ctx.TbtStockPiece
                    .AsNoTracking()
                    .Where(p => numbers.Contains(p.StockNumber))
                    .Select(p => new { p.StockNumber, p.SkuCode })
                    .ToListAsync())
                    .Select(p => (p.StockNumber, p.SkuCode))
                    .ToList();

            var skuByStock = pieces
                .GroupBy(p => p.StockNumber)
                .ToDictionary(g => g.Key, g => g.First().SkuCode);

            var skuCodes = skuByStock.Values.Distinct().ToList();
            var skus = skuCodes.Count == 0
                ? new Dictionary<string, (string? Gold, string? PType, string? Size)>()
                : (await _ctx.TbtSku
                    .AsNoTracking()
                    .Where(s => skuCodes.Contains(s.SkuCode))
                    .Select(s => new { s.SkuCode, s.ProductionType, s.ProductType, s.ProductionTypeSize })
                    .ToListAsync())
                    .ToDictionary(s => s.SkuCode, s => (Gold: s.ProductionType, PType: s.ProductType, Size: s.ProductionTypeSize));

            return sold.Select(s =>
            {
                (string? Gold, string? PType, string? Size) sku = (null, null, null);
                if (!string.IsNullOrEmpty(s.StockNumber)
                    && skuByStock.TryGetValue(s.StockNumber, out var skuCode)
                    && skus.TryGetValue(skuCode, out var found))
                {
                    sku = found;
                }

                return (ExecutiveFlowNormalizer.NormalizePlanGold(sku.Gold), ExecutiveFlowNormalizer.LabelOrUnknown(sku.PType), ExecutiveFlowNormalizer.NormalizeSize(sku.Size), s.Qty);
            }).ToList();
        }

        // ---------------------------------------------------------------- trust

        private async Task<List<FlowTrustItem>> BuildTrustAsync(
            DateTime now,
            IReadOnlyDictionary<string, string> names,
            List<PlanAgeRow> plans,
            List<StockRow> stockRows,
            List<InvoiceRow> invoices,
            List<MoldPlanRow> moldPlans,
            DoneSummary done)
        {
            var input = new ExecutiveFlowTrustEvaluator.Input();

            input.NegativeGoldRows = await _ctx.TbtStockGold
                .AsNoTracking()
                .Where(g => g.Weight < 0)
                .OrderBy(g => g.GoldCode).ThenBy(g => g.GoldSizeCode)
                .Select(g => new ExecutiveFlowTrustEvaluator.NegativeGoldRow { Code = g.GoldCode, SizeCode = g.GoldSizeCode, Weight = g.Weight })
                .ToListAsync();

            var inStockRows = InStockOnly(stockRows);
            input.InStockPieces = inStockRows.Sum(s => s.Pcs);
            input.InStockCostMissing = inStockRows.Where(s => !(s.Cost > 0)).Sum(s => s.Pcs);
            input.StockOldPieces = inStockRows.Where(s => s.AgeDate.HasValue && StockAge.DaysOld(s.AgeDate.Value, now) > 365).Sum(s => s.Pcs);
            var datedRows = inStockRows.Where(s => s.AgeDate.HasValue).ToList();
            input.OldestProductionYear = datedRows.Count > 0 ? datedRows.Min(s => s.AgeDate!.Value).AddHours(7).Year : (int?)null;

            var usdRateOne = invoices.Where(i => !i.IsThb && i.Rate == 1m).ToList();
            if (usdRateOne.Count > 0)
            {
                var runnings = usdRateOne.Select(i => i.Running).ToList();
                var itemCounts = await _ctx.TbtSaleOrderProduct
                    .AsNoTracking()
                    .Where(p => p.Invoice != null && runnings.Contains(p.Invoice))
                    .GroupBy(p => p.Invoice!)
                    .Select(g => new { Invoice = g.Key, Items = g.Count() })
                    .ToListAsync();

                input.UsdRateOneInvoices = usdRateOne
                    .Select(i => new ExecutiveFlowTrustEvaluator.UsdRateOneInvoice
                    {
                        CustomerName = i.CustomerName,
                        Date = i.CreateDate.AddHours(7).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                        Items = itemCounts.FirstOrDefault(c => c.Invoice == i.Running)?.Items ?? 0
                    })
                    .ToList();
            }

            input.OpenPcs = plans.Sum(p => p.Plan.Qty);
            input.OpenPlans = plans.Count;
            input.IdlePcs = plans.Where(p => ExecutiveFlowNormalizer.AgeBucket(p.AgeDays) == 3).Sum(p => p.Plan.Qty);
            input.IdlePlans = plans.Count(p => ExecutiveFlowNormalizer.AgeBucket(p.AgeDays) == 3);
            input.TypeMissingPcs = plans.Where(p => string.IsNullOrWhiteSpace(p.Plan.Type)).Sum(p => p.Plan.Qty);
            input.OverduePcs = plans.Where(p => p.Plan.RequestDate < now).Sum(p => p.Plan.Qty);

            var holders = await LoadHoldersAsync();
            var workPlanIds = plans.Where(p => WorkStatusOfDept.ContainsValue(p.Plan.Status)).Select(p => p.Plan.Id).ToList();
            var classes = WorkerEval.Classify(workPlanIds, holders.Select(h => (h.PlanId, h.Worker)), names);
            input.TestWorkerPlans = classes.Values.Count(c => c.Class == WorkerEval.HolderClass.Test);

            var moldInProgress = moldPlans.Where(m => m.Status >= 10 && m.Status <= 50).ToList();
            input.MoldInProgressPlans = moldInProgress.Count;
            input.MoldInProgressStalePlans = moldInProgress.Count(m => (now - (m.UpdateDate ?? m.CreateDate)).TotalDays >= 180);

            input.NotReceivedPcs = done.NotReceivedPcs;
            input.NotReceivedJobs = done.NotReceivedJobs;

            input.InvoicesTotal = invoices.Count;
            input.InvoicesNoTotal = invoices.Count(i => !i.Total.HasValue);
            input.InvoicesNoSalePerson = invoices.Count(i => string.IsNullOrWhiteSpace(i.SalePerson));
            input.InvoicesNoChannel = invoices.Count(i => string.IsNullOrWhiteSpace(i.Channel));

            return ExecutiveFlowTrustEvaluator.Evaluate(input);
        }
    }
}
