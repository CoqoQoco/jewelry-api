using jewelry.Model.Report.Executive.Flow;
using Jewelry.Service.Production.Insight;
using Jewelry.Service.Production.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WorkerEval = Jewelry.Service.Report.Executive.ExecutiveFlowWorkerEvaluator;

namespace Jewelry.Service.Report.Executive
{
    public partial class ExecutiveFlowService
    {
        private static readonly string[] StockGoldOrder = { "Yellow", "White", "Pink", "Silver", ExecutiveFlowNormalizer.Unknown };
        private static readonly string[] SizeOrder = { "9K", "10K", "14K", "18K", "Silver", ExecutiveFlowNormalizer.Unknown };

        private static readonly (int Status, string Name, bool IsTerminal)[] MoldStages =
        {
            (10, "Design", false),
            (20, "Resin", false),
            (30, "CastingSilver", false),
            (40, "Casting", false),
            (50, "Cutting", false),
            (60, "Store", true),
            (500, "Melted", true)
        };

        public async Task<FlowZoneResponse> FlowZone(FlowZoneRequest request)
        {
            var zone = request?.Zone?.Trim().ToLowerInvariant();
            switch (zone)
            {
                case "mold":
                case "production":
                case "receive":
                case "stock":
                case "material":
                case "sales":
                    return await CachedAsync("Zone:" + zone, () => BuildZoneAsync(zone));
                default:
                    throw new ArgumentException("zone ต้องเป็น mold | production | receive | stock | material | sales");
            }
        }

        private async Task<FlowZoneResponse> BuildZoneAsync(string zone)
        {
            var now = DateTime.UtcNow;
            var response = new FlowZoneResponse { Zone = zone, AsOf = new DateTimeOffset(now, TimeSpan.Zero) };

            switch (zone)
            {
                case "mold": response.Mold = await BuildMoldZoneAsync(now); break;
                case "production": response.Production = await BuildProductionZoneAsync(now); break;
                case "receive": response.Receive = await BuildReceiveZoneAsync(now); break;
                case "stock": response.Stock = await BuildStockZoneAsync(now); break;
                case "material": response.Material = await BuildMaterialZoneAsync(now); break;
                case "sales": response.Sales = await BuildSalesZoneAsync(now); break;
            }

            return response;
        }

        // ---------------------------------------------------------------- mold

        private async Task<FlowMoldZone> BuildMoldZoneAsync(DateTime now)
        {
            var plans = await LoadMoldPlansAsync();
            var zone = new FlowMoldZone();

            foreach (var (status, name, isTerminal) in MoldStages)
            {
                var items = plans.Where(p => p.Status == status).ToList();
                var ages = items.Select(p => Math.Max(0d, (now - (p.UpdateDate ?? p.CreateDate)).TotalDays)).ToList();

                zone.Stages.Add(new FlowMoldStage
                {
                    Status = status,
                    Name = name,
                    IsTerminal = isTerminal,
                    Open = items.Count,
                    Stale180 = ages.Count(a => a >= 180),
                    Within14d = ages.Count(a => a <= 14),
                    MedianDays = ExecutiveFlowNormalizer.Median(ages),
                    MaxDays = ages.Count > 0 ? Math.Round(ages.Max(), 1) : (double?)null,
                    NewProcess = items.Count(p => p.IsNewProcess)
                });
            }

            var molds = await _ctx.TbtProductMold
                .AsNoTracking()
                .Where(m => m.IsActive)
                .Select(m => new { m.Category, m.CreateDate })
                .ToListAsync();

            var cutoff = now.AddDays(-90);
            var categories = molds
                .GroupBy(m => ExecutiveFlowNormalizer.LabelOrUnknown(m.Category))
                .Select(g => new FlowMoldCategory { Category = g.Key, Molds = g.Count(), New90d = g.Count(m => m.CreateDate >= cutoff) })
                .OrderByDescending(c => c.Molds)
                .ThenBy(c => c.Category, StringComparer.Ordinal)
                .ToList();

            zone.Catalog.ActiveMolds = molds.Count;
            zone.Catalog.Categories = categories.Take(8).ToList();
            if (categories.Count > 8)
            {
                var rest = categories.Skip(8).ToList();
                zone.Catalog.Categories.Add(new FlowMoldCategory { Category = "Other", Molds = rest.Sum(c => c.Molds), New90d = rest.Sum(c => c.New90d) });
            }

            return zone;
        }

        // ---------------------------------------------------------------- production

        private static int IndexOrMax(string[] order, string label)
        {
            var idx = Array.IndexOf(order, label);
            return idx < 0 ? int.MaxValue : idx;
        }

        private static List<FlowDimItem> BuildDim(IEnumerable<PlanAgeRow> plans, Func<PlanRow, string> label, string[]? order)
        {
            var groups = plans
                .GroupBy(p => label(p.Plan))
                .Select(g => new FlowDimItem { Label = g.Key, Plans = g.Count(), Pcs = g.Sum(p => p.Plan.Qty) });

            return order != null
                ? groups.OrderBy(i => IndexOrMax(order, i.Label)).ThenBy(i => i.Label, StringComparer.Ordinal).ToList()
                : groups.OrderByDescending(i => i.Pcs).ThenBy(i => i.Label, StringComparer.Ordinal).ToList();
        }

        private static List<FlowGoldLossDimItem> BuildLossDim(IEnumerable<(string Label, bool Returned, bool InWin, int Rows, decimal Send, decimal Check)> rows)
        {
            return rows
                .GroupBy(r => r.Label)
                .Select(g => new FlowGoldLossDimItem
                {
                    Label = g.Key,
                    Rows90 = g.Where(r => r.Returned && r.InWin).Sum(r => r.Rows),
                    SendG90 = ExecutiveFlowNormalizer.Round2(g.Where(r => r.Returned && r.InWin).Sum(r => r.Send)),
                    CheckG90 = ExecutiveFlowNormalizer.Round2(g.Where(r => r.Returned && r.InWin).Sum(r => r.Check)),
                    PendingG = ExecutiveFlowNormalizer.Round2(g.Where(r => !r.Returned).Sum(r => r.Send))
                })
                .OrderByDescending(i => i.SendG90)
                .ThenByDescending(i => i.PendingG)
                .ThenBy(i => i.Label, StringComparer.Ordinal)
                .ToList();
        }

        private static FlowLossBlock BuildLossBlock(IEnumerable<StageCell> cells)
        {
            var list = cells.ToList();
            return new FlowLossBlock
            {
                Rows = list.Sum(c => c.Rows),
                SendG = ExecutiveFlowNormalizer.Round2(list.Sum(c => c.Send)),
                CheckG = ExecutiveFlowNormalizer.Round2(list.Sum(c => c.Check))
            };
        }

        private static FlowDeptGoldLoss BuildDeptGoldLoss(string dept, StageData stage, IReadOnlyDictionary<string, string> names)
        {
            var cells = stage.Cells.Where(c => c.Dept == dept).ToList();
            var pending = cells.Where(c => !c.Returned).ToList();
            var queue = pending.Where(c => ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(c.Worker, names)).ToList();
            var dimCells = stage.DimCells.Where(c => c.Dept == dept).ToList();

            return new FlowDeptGoldLoss
            {
                Win90 = BuildLossBlock(cells.Where(c => c.Returned && c.InWin)),
                AllTime = BuildLossBlock(cells.Where(c => c.Returned)),
                Pending = new FlowPendingBlock
                {
                    Rows = pending.Sum(c => c.Rows),
                    G = ExecutiveFlowNormalizer.Round2(pending.Sum(c => c.Send)),
                    QueueRows = queue.Sum(c => c.Rows),
                    QueueG = ExecutiveFlowNormalizer.Round2(queue.Sum(c => c.Send)),
                    WorkerRows = pending.Sum(c => c.Rows) - queue.Sum(c => c.Rows),
                    WorkerG = ExecutiveFlowNormalizer.Round2(pending.Sum(c => c.Send) - queue.Sum(c => c.Send))
                },
                NotWeighed = ProductionGoldStageEvaluator.NotWeighedDepts.Contains(dept),
                IncludesScrap = ProductionGoldStageEvaluator.ScrapDepts.Contains(dept),
                Dims = new FlowGoldLossDims
                {
                    Gold = BuildLossDim(cells.Select(c => (ExecutiveFlowNormalizer.LabelOrUnknown(c.Gold), c.Returned, c.InWin, c.Rows, c.Send, c.Check))),
                    Size = BuildLossDim(dimCells.Select(c => (c.Size, c.Returned, c.InWin, c.Rows, c.Send, c.Check))),
                    Ptype = BuildLossDim(dimCells.Select(c => (c.PType, c.Returned, c.InWin, c.Rows, c.Send, c.Check)))
                }
            };
        }

        private async Task<FlowProductionZone> BuildProductionZoneAsync(DateTime now)
        {
            var plans = await LoadOpenPlansWithAgeAsync(now);
            var stage = await LoadStageDataAsync(now);
            var holders = await LoadHoldersAsync();
            var names = await _workerLookup.GetWorkerNamesAsync();
            var series = await LoadSeriesAsync(now);
            var done = await LoadDoneSummaryAsync();
            var targetFor = await BuildTargetLookupAsync();
            var nameOf = NameResolver(names);
            var holderTuples = holders.Select(h => (h.PlanId, h.Worker)).ToList();

            var zone = new FlowProductionZone();
            var reconcile = true;

            foreach (var (key, statusIds) in ProductionPlanDepartments.Departments)
            {
                var deptPlans = plans.Where(p => statusIds.Contains(p.Plan.Status)).ToList();
                var openPcs = deptPlans.Sum(p => p.Plan.Qty);
                var waitPcs = deptPlans.Where(p => WaitStatuses.Contains(p.Plan.Status)).Sum(p => p.Plan.Qty);
                var ages = deptPlans.Select(p => Math.Max(0d, p.AgeDays)).ToList();

                var dept = new FlowProductionDept
                {
                    Key = key,
                    OpenPlans = deptPlans.Count,
                    OpenPcs = openPcs,
                    WaitPcs = waitPcs,
                    WorkPcs = openPcs - waitPcs,
                    OverduePcs = deptPlans.Where(p => p.Plan.RequestDate < now).Sum(p => p.Plan.Qty),
                    MedianDays = ExecutiveFlowNormalizer.Median(ages),
                    MaxDays = ages.Count > 0 ? Math.Round(ages.Max(), 1) : (double?)null
                };

                foreach (var p in deptPlans)
                {
                    var bucket = ExecutiveFlowNormalizer.AgeBucket(p.AgeDays);
                    dept.AgePcs[bucket] += p.Plan.Qty;
                    dept.AgePlans[bucket] += 1;
                }

                dept.Dims = new FlowDims
                {
                    Gold = BuildDim(deptPlans, p => ExecutiveFlowNormalizer.NormalizePlanGold(p.Type), ExecutiveFlowNormalizer.PlanGoldOrder),
                    Size = BuildDim(deptPlans, p => ExecutiveFlowNormalizer.NormalizeSize(p.TypeSize), SizeOrder),
                    Ptype = BuildDim(deptPlans, p => ExecutiveFlowNormalizer.LabelOrUnknown(p.ProductType), null)
                };

                reconcile &= new[] { dept.Dims.Gold, dept.Dims.Size, dept.Dims.Ptype }
                    .All(d => d.Sum(i => i.Pcs) == openPcs && d.Sum(i => i.Plans) == deptPlans.Count);
                reconcile &= dept.AgePcs.Sum() == openPcs && dept.AgePlans.Sum() == deptPlans.Count;

                if (WorkStatusOfDept.TryGetValue(key, out var workStatus))
                {
                    var workPlanIds = deptPlans.Where(p => p.Plan.Status == workStatus).Select(p => p.Plan.Id).ToList();
                    var classes = WorkerEval.Classify(workPlanIds, holderTuples, names);

                    dept.Workers = new FlowDeptWorkers
                    {
                        NamedPlans = classes.Values.Count(c => c.Class == WorkerEval.HolderClass.Named),
                        QueuePlans = classes.Values.Count(c => c.Class == WorkerEval.HolderClass.Queue),
                        NonePlans = classes.Values.Count(c => c.Class == WorkerEval.HolderClass.None),
                        TestPlans = classes.Values.Count(c => c.Class == WorkerEval.HolderClass.Test),
                        Names = classes.Values
                            .Where(c => c.Class == WorkerEval.HolderClass.Named)
                            .SelectMany(c => c.NamedWorkers)
                            .GroupBy(w => w)
                            .Select(g => new FlowWorkerPlans { Name = nameOf(g.Key), Plans = g.Count() })
                            .OrderByDescending(w => w.Plans)
                            .ThenBy(w => w.Name, StringComparer.Ordinal)
                            .Take(12)
                            .ToList()
                    };

                    dept.GoldLoss = BuildDeptGoldLoss(key, stage, names);
                }

                zone.Departments.Add(dept);
            }

            zone.Totals = new FlowProductionTotals
            {
                OpenPlans = plans.Count,
                OpenPcs = plans.Sum(p => p.Plan.Qty),
                StalePlans = plans.Count(p => ExecutiveFlowNormalizer.AgeBucket(p.AgeDays) == 3),
                StalePcs = plans.Where(p => ExecutiveFlowNormalizer.AgeBucket(p.AgeDays) == 3).Sum(p => p.Plan.Qty),
                OverduePcs = plans.Where(p => p.Plan.RequestDate < now).Sum(p => p.Plan.Qty)
            };

            // สถานะนอก 7 แผนก (ถ้ามี) จะทำให้ผลรวมแผนกไม่เท่ายอดรวม
            reconcile &= zone.Departments.Sum(d => d.OpenPcs) == zone.Totals.OpenPcs
                && zone.Departments.Sum(d => d.OpenPlans) == zone.Totals.OpenPlans;
            zone.LensesReconcile = reconcile;

            var currentKey = series.MonthKeys.Last();
            var meltedPcs = await _ctx.TbtProductionPlan
                .AsNoTracking()
                .Where(p => p.IsActive == true && p.Status == StatusMelted)
                .SumAsync(p => (int?)p.ProductQty) ?? 0;

            zone.Done = new FlowProductionDone
            {
                CreatedThisMonthPcs = series.Created[currentKey],
                CompletedThisMonthPcs = series.Completed[currentKey],
                MeltedPcs = meltedPcs,
                DonePcs = done.ReceivedPcs + done.NotReceivedPcs
            };

            var winLoss = ToLossCells(stage.Cells.Where(c => c.Returned && c.InWin));
            var evaluated = ExecutiveFlowLossEvaluator.Evaluate(
                winLoss, TargetDepts, Metals, targetFor,
                w => ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(w, names), nameOf);

            zone.LossVsTarget = evaluated
                .Where(r => r.Total.Rows > 0)
                .Select(r => new FlowLossVsTarget
                {
                    Dept = r.Dept,
                    Metal = r.Metal,
                    TargetPercent = r.TargetPercent,
                    IsSystemDefault = r.IsSystemDefault,
                    Rows = r.Rows.Select(w => new FlowLossRow
                    {
                        Name = w.Name,
                        Code = w.Code,
                        IsTotal = w.IsTotal,
                        Rows = w.Rows,
                        SendG = w.SendG,
                        LossPercent = w.LossPercent,
                        DeltaPp = w.DeltaPp,
                        ExcessG = w.ExcessG
                    }).ToList()
                })
                .ToList();

            zone.CastCost = await BuildCastCostAsync(now);
            return zone;
        }

        private class CastRow
        {
            public string Gold { get; set; } = null!;
            public DateTime? CastDate { get; set; }
            public decimal CastWeight { get; set; }
            public decimal CastLoss { get; set; }
            public decimal MeltWeight { get; set; }
            public decimal MeltLoss { get; set; }
        }

        private static void FillCastBlock(IReadOnlyCollection<CastRow> rows, FlowCastBlock target)
        {
            target.Rows = rows.Count;
            target.CastWeightG = ExecutiveFlowNormalizer.Round2(rows.Sum(r => r.CastWeight));
            target.CastLossG = ExecutiveFlowNormalizer.Round2(rows.Sum(r => r.CastLoss));
            target.MeltWeightG = ExecutiveFlowNormalizer.Round2(rows.Sum(r => r.MeltWeight));
            target.MeltLossG = ExecutiveFlowNormalizer.Round2(rows.Sum(r => r.MeltLoss));
        }

        private async Task<FlowCastCost> BuildCastCostAsync(DateTime now)
        {
            var raw = await _ctx.TbtProductionPlanCostGold
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { c.Gold, c.CastDate, c.CastWeight, c.CastWeightLoss, c.MeltWeight, c.MeltWeightLoss })
                .ToListAsync();

            var rows = raw.Select(r => new CastRow
            {
                Gold = ExecutiveFlowNormalizer.LabelOrUnknown(r.Gold),
                CastDate = r.CastDate,
                CastWeight = r.CastWeight ?? 0m,
                CastLoss = r.CastWeightLoss ?? 0m,
                MeltWeight = r.MeltWeight ?? 0m,
                MeltLoss = r.MeltWeightLoss ?? 0m
            }).ToList();

            var cutoff = now.AddDays(-LossWindowDays);
            var win = rows.Where(r => r.CastDate.HasValue && r.CastDate.Value >= cutoff).ToList();

            var cost = new FlowCastCost
            {
                Note = "ค่าที่บันทึกไว้ในใบต้นทุนหล่อ (tbt_production_plan_cost_gold) ไม่ได้คำนวณใหม่ — ช่วง 90 วันนับจาก cast_date"
            };
            FillCastBlock(win, cost.Win90);
            FillCastBlock(rows, cost.AllTime);

            cost.ByGold90 = win
                .GroupBy(r => r.Gold)
                .Select(g =>
                {
                    var block = new FlowCastGoldBlock { Gold = g.Key };
                    FillCastBlock(g.ToList(), block);
                    return block;
                })
                .OrderByDescending(b => b.CastWeightG)
                .ThenBy(b => b.Gold, StringComparer.Ordinal)
                .ToList();

            return cost;
        }

        // ---------------------------------------------------------------- receive

        private async Task<FlowReceiveZone> BuildReceiveZoneAsync(DateTime now)
        {
            var done = await LoadDoneSummaryAsync();
            var series = await LoadSeriesAsync(now);
            var monthStart = ThaiMonthStartsUtc(now, 1)[0];

            var completedQuery = _ctx.TbtProductionPlan
                .AsNoTracking()
                .Where(p => p.Status == StatusDone && p.CompletedDate != null && p.CompletedDate >= monthStart);
            var completedJobs = await completedQuery.CountAsync();
            var completedPcs = await completedQuery.SumAsync(p => (int?)p.ProductQty) ?? 0;

            // รับเข้าสต็อกเดือนนี้ (ชิ้น) = ค่าเดียวกับสต็อกใหม่ของเดือนปัจจุบันในกราฟรายเดือน
            var receivedThisMonth = series.NewStock.TryGetValue(MonthKey(monthStart), out var thisMonthNew) ? thisMonthNew : 0;

            return new FlowReceiveZone
            {
                NotReceivedPcs = done.NotReceivedPcs,
                NotReceivedJobs = done.NotReceivedJobs,
                ReceivedPcs = done.ReceivedPcs,
                ReceivedJobs = done.ReceivedJobs,
                CompletedThisMonthPcs = completedPcs,
                CompletedThisMonthJobs = completedJobs,
                ReceivedThisMonthPieces = receivedThisMonth,
                Months = series.MonthKeys.Select(k => new FlowReceiveMonth { Month = k, NewStockPieces = series.NewStock[k] }).ToList()
            };
        }

        // ---------------------------------------------------------------- stock

        private static List<FlowLabelPcs> BuildLens(IEnumerable<(string Label, int Pcs)> weighted, string[]? order)
        {
            var groups = weighted.GroupBy(l => l.Label).Select(g => new FlowLabelPcs { Label = g.Key, Pcs = g.Sum(x => x.Pcs) });
            return order != null
                ? groups.OrderBy(i => IndexOrMax(order, i.Label)).ThenBy(i => i.Label, StringComparer.Ordinal).ToList()
                : groups.OrderByDescending(i => i.Pcs).ThenBy(i => i.Label, StringComparer.Ordinal).ToList();
        }

        private async Task<FlowStockZone> BuildStockZoneAsync(DateTime now)
        {
            var allRows = await LoadStockRowsAsync();
            var rows = InStockOnly(allRows);
            var locations = await _ctx.TbmStockLocation
                .AsNoTracking()
                .Select(l => new { l.Code, l.NameTh, l.IsSalesPoint })
                .ToListAsync();
            var locationMap = locations.ToDictionary(l => l.Code, l => l);

            // ทุกตัวเลขเป็นชิ้น: พร้อมขาย = unit IN_STOCK · จอง = unit RESERVED · ขาย = Σ SALE - Σ RETURN (movement)
            var reserved = allRows.Where(r => r.Status != InStockStatus).Sum(r => r.Pcs);
            var soldMovements = await _ctx.TbtStockMovement
                .AsNoTracking()
                .Where(m => m.MovementType == "SALE" || m.MovementType == "RETURN")
                .GroupBy(m => m.MovementType)
                .Select(g => new { Type = g.Key, Qty = g.Sum(x => x.Qty) })
                .ToListAsync();
            var soldQty = soldMovements.Where(m => m.Type == "SALE").Sum(m => m.Qty)
                - soldMovements.Where(m => m.Type == "RETURN").Sum(m => m.Qty);
            var sold = (int)Math.Max(0m, Math.Round(soldQty, MidpointRounding.AwayFromZero));

            var zone = new FlowStockZone();
            zone.Totals = new FlowStockTotals
            {
                InStock = rows.Sum(r => r.Pcs),
                Reserved = reserved,
                Sold = sold,
                CostRecordedPcs = rows.Where(r => r.Cost > 0).Sum(r => r.Pcs),
                CostMissingPcs = rows.Where(r => !(r.Cost > 0)).Sum(r => r.Pcs),
                DesignsInStock = rows.Where(r => !string.IsNullOrWhiteSpace(r.Mold)).Select(r => r.Mold!.Trim()).Distinct().Count()
            };

            var byLocation = rows.GroupBy(r => r.Location).Select(g => (Code: g.Key, Pcs: g.Sum(x => x.Pcs))).ToList();
            zone.Locations = byLocation
                .Select(l =>
                {
                    locationMap.TryGetValue(l.Code, out var master);
                    return new FlowStockLocation
                    {
                        Code = l.Code,
                        Name = master?.NameTh ?? l.Code,
                        IsSalesPoint = master?.IsSalesPoint ?? false,
                        Pcs = l.Pcs
                    };
                })
                .OrderByDescending(l => l.Pcs)
                .ThenBy(l => l.Code, StringComparer.Ordinal)
                .ToList();

            var sloc = ExecutiveFlowNormalizer.TopWithOther(
                zone.Locations.Select(l => (l.Name, l.Pcs)), 3, n => $"Other {n}");

            var goldLabels = rows.Select(r => (Label: ExecutiveFlowNormalizer.NormalizeStockGold(r.Gold), r.Pcs)).ToList();
            var sizeLabels = rows.Select(r => (Label: ExecutiveFlowNormalizer.NormalizeSize(r.Size), r.Pcs)).ToList();
            var typeCounts = rows.GroupBy(r => ExecutiveFlowNormalizer.LabelOrUnknown(r.PType)).Select(g => (g.Key, g.Sum(x => x.Pcs)));
            var types = ExecutiveFlowNormalizer.TopWithOther(typeCounts, 5, _ => "Other");
            var ageLabels = rows.Select(r => (Label: ExecutiveFlowNormalizer.StockAgeLabel(r.AgeDate, now), r.Pcs)).ToList();

            zone.Lenses = new FlowStockLenses
            {
                Sloc = sloc.Select(s => new FlowLabelPcs { Label = s.Label, Pcs = s.Pcs }).ToList(),
                Gold = BuildLens(goldLabels, StockGoldOrder),
                Size = BuildLens(sizeLabels, SizeOrder),
                Type = types.Select(t => new FlowLabelPcs { Label = t.Label, Pcs = t.Pcs }).ToList(),
                Age = BuildLens(ageLabels, ExecutiveFlowNormalizer.StockAgeLabels)
            };

            zone.Matrix = rows
                .Select(r => (Gold: ExecutiveFlowNormalizer.NormalizeStockGold(r.Gold), Size: ExecutiveFlowNormalizer.NormalizeSize(r.Size), r.Pcs))
                .GroupBy(x => (x.Gold, x.Size))
                .Select(g => new FlowStockMatrixCell { Gold = g.Key.Gold, Size = g.Key.Size, Pcs = g.Sum(x => x.Pcs) })
                .OrderBy(c => IndexOrMax(StockGoldOrder, c.Gold))
                .ThenBy(c => IndexOrMax(SizeOrder, c.Size))
                .ToList();

            zone.LensesReconcile = ExecutiveFlowNormalizer.Reconciles(
                zone.Totals.InStock,
                zone.Locations.Select(l => l.Pcs),
                zone.Lenses.Sloc.Select(l => l.Pcs),
                zone.Lenses.Gold.Select(l => l.Pcs),
                zone.Lenses.Size.Select(l => l.Pcs),
                zone.Lenses.Type.Select(l => l.Pcs),
                zone.Lenses.Age.Select(l => l.Pcs),
                zone.Matrix.Select(c => c.Pcs));

            return zone;
        }

        // ---------------------------------------------------------------- material

        private async Task<FlowMaterialZone> BuildMaterialZoneAsync(DateTime now)
        {
            var zone = new FlowMaterialZone();

            zone.GoldRows = await (
                from g in _ctx.TbtStockGold
                join s in _ctx.TbmGoldSize on g.GoldSizeCode equals s.Code into sizes
                from s in sizes.DefaultIfEmpty()
                orderby g.GoldCode, g.GoldSizeCode
                select new FlowGoldStockRow
                {
                    Code = g.GoldCode,
                    SizeCode = g.GoldSizeCode,
                    SizeName = s != null ? s.NameTh : null,
                    Weight = g.Weight,
                    WeightOnProcess = g.WeightOnProcess
                })
                .AsNoTracking()
                .ToListAsync();

            var cutoff = now.AddDays(-30);
            zone.GoldTx30d = await _ctx.TbtStockGoldTransection
                .AsNoTracking()
                .Where(t => t.CreateDate >= cutoff)
                .GroupBy(t => new { t.GoldCode, t.Type })
                .Select(g => new FlowGoldTxRow { GoldCode = g.Key.GoldCode, Type = g.Key.Type, Rows = g.Count(), Weight = g.Sum(x => x.Weight) })
                .OrderBy(r => r.GoldCode).ThenBy(r => r.Type)
                .ToListAsync();

            // หน่วย K และ Q แยกกันเสมอ — ห้ามบวกรวม
            zone.GemGroups = await _ctx.TbtStockGem
                .AsNoTracking()
                .GroupBy(g => g.GroupName)
                .Select(g => new FlowGemGroup
                {
                    Group = g.Key,
                    Rows = g.Count(),
                    RowsWithStock = g.Count(x => x.Quantity > 0),
                    QtyK = g.Sum(x => x.Unit != null && x.Unit.Trim().ToUpper() == "K" ? x.Quantity : 0m),
                    QtyQ = g.Sum(x => x.Unit != null && x.Unit.Trim().ToUpper() == "Q" ? x.Quantity : 0m)
                })
                .OrderByDescending(g => g.Rows)
                .Take(12)
                .ToListAsync();

            zone.Totals.GemRows = await _ctx.TbtStockGem.AsNoTracking().CountAsync();
            zone.Totals.GemRowsWithStock = await _ctx.TbtStockGem.AsNoTracking().CountAsync(g => g.Quantity > 0);
            return zone;
        }

        // ---------------------------------------------------------------- sales

        private async Task<FlowSalesZone> BuildSalesZoneAsync(DateTime now)
        {
            var invoices = await LoadInvoicesAsync();
            var starts = ThaiMonthStartsUtc(now, 5);
            var windowStart = starts[0];
            var keys = starts.Select(MonthKey).ToList();

            var quotes = await _ctx.TbtSaleQuotation
                .AsNoTracking()
                .Where(q => q.CreateDate >= windowStart)
                .Select(q => q.CreateDate)
                .ToListAsync();

            var salesOrders = await _ctx.TbtSaleOrder
                .AsNoTracking()
                .Where(s => s.StatusName == "DK-SO" && s.CreateDate >= windowStart)
                .Select(s => new { s.CreateDate, s.CurrencyUnit, s.GrandTotalRounded })
                .ToListAsync();

            bool IsThb(string? c) => string.Equals(c?.Trim(), Thb, StringComparison.OrdinalIgnoreCase);

            var zone = new FlowSalesZone();
            foreach (var key in keys)
            {
                var so = salesOrders.Where(s => ExecutiveFlowNormalizer.ThaiMonthKey(s.CreateDate) == key).ToList();
                var inv = invoices.Where(i => ExecutiveFlowNormalizer.ThaiMonthKey(i.CreateDate) == key).ToList();

                zone.Months.Add(new FlowSalesMonth
                {
                    Month = key,
                    Quotes = quotes.Count(q => ExecutiveFlowNormalizer.ThaiMonthKey(q) == key),
                    SoThbCount = so.Count(s => IsThb(s.CurrencyUnit)),
                    SoThbAmount = so.Where(s => IsThb(s.CurrencyUnit)).Sum(s => s.GrandTotalRounded ?? 0m),
                    SoUsdCount = so.Count(s => !IsThb(s.CurrencyUnit)),
                    InvThbCount = inv.Count(i => i.IsThb),
                    InvThbAmount = inv.Where(i => i.IsThb).Sum(i => i.Total ?? 0m),
                    InvUsdCount = inv.Count(i => !i.IsThb)
                });
            }

            zone.Channels = invoices
                .GroupBy(i => string.IsNullOrWhiteSpace(i.Channel) ? "(none)" : i.Channel!.Trim())
                .Select(g => new FlowSalesChannel
                {
                    Code = g.Key,
                    ThbInvoices = g.Count(i => i.IsThb),
                    ThbAmount = g.Where(i => i.IsThb).Sum(i => i.Total ?? 0m),
                    UsdInvoices = g.Count(i => !i.IsThb)
                })
                .OrderByDescending(c => c.ThbAmount)
                .ThenBy(c => c.Code, StringComparer.Ordinal)
                .ToList();

            var thbWithTotal = invoices.Where(i => i.IsThb && i.Total.HasValue).ToList();
            zone.Customers = BuildCustomerShares(thbWithTotal, 8);

            zone.SalePersons = invoices
                .GroupBy(i => string.IsNullOrWhiteSpace(i.SalePerson) ? "(none)" : i.SalePerson!.Trim())
                .Select(g => new FlowSalePerson { Name = g.Key, Invoices = g.Count(), Thb = g.Where(i => i.IsThb).Sum(i => i.Total ?? 0m) })
                .OrderByDescending(p => p.Thb)
                .ThenByDescending(p => p.Invoices)
                .ThenBy(p => p.Name, StringComparer.Ordinal)
                .Take(8)
                .ToList();

            zone.Totals = new FlowSalesTotals
            {
                Quotations = await _ctx.TbtSaleQuotation.AsNoTracking().CountAsync(),
                SalesOrders = await _ctx.TbtSaleOrder.AsNoTracking().CountAsync(s => s.StatusName == "DK-SO"),
                Invoices = invoices.Count
            };

            return zone;
        }
    }
}
