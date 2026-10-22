using jewelry.Model.Material.Insight;
using jewelry.Model.Material.Insight.Overview;
using Jewelry.Service.Material.Insight;

namespace Jewelry.Service.Tests.Material.Insight;

public class MaterialInsightTests
{
    private static readonly PolicyValues Defaults = MaterialInsightThresholds.Resolve(null, null);
    private static readonly DateTime Now = new(2026, 10, 22, 5, 0, 0, DateTimeKind.Utc);
    private static readonly IReadOnlyDictionary<string, MaterialMinOverride> NoOverrides = new Dictionary<string, MaterialMinOverride>();

    private static MaterialItemInput Input(string code, string size, decimal onHand, decimal onProcess = 0, DateTime? firstSeen = null)
        => new()
        {
            ItemKey = $"{code}|{size}",
            Code = code,
            Name = code + "-name",
            SizeCode = size,
            SizeName = size + "-size",
            OnHand = onHand,
            OnProcess = onProcess,
            FirstSeenUtc = firstSeen ?? Now.AddDays(-400)
        };

    private static MaterialMovement Use(string key, decimal qty, DateTime date)
        => new(key, date, MaterialMovementKind.Consumed, qty, -1);

    private static MaterialMovement Ret(string key, decimal qty, DateTime date)
        => new(key, date, MaterialMovementKind.Returned, qty, 1);

    private static Item Build(MaterialItemInput input, IReadOnlyList<MaterialMovement> mv, PolicyValues? p = null,
        IReadOnlyDictionary<string, MaterialMinOverride>? ov = null)
        => MaterialInsightCalculator.BuildItems("GOLD", new[] { input }, mv, ov ?? NoOverrides, p ?? Defaults, Now).Single();

    private static Item MakeItem(string status, string label = "x", decimal avail = 10, decimal min = 20, decimal usage = 1,
        bool dead = false, bool risk = false, decimal? cover = null, int lead = 14, decimal onProcess = 0, int? age = null)
        => new()
        {
            ItemKey = label, Code = label, Label = label, Status = status, OnHand = avail, Available = avail, EffectiveMin = min,
            AvgDailyUsage = usage, IsDead = dead, StockoutRisk = risk, CoverDays = cover, LeadTimeDays = lead,
            OnProcess = onProcess, OnProcessAgeDays = age
        };

    // ---- thresholds ----

    [Fact]
    public void Resolve_NullUsesDefaults()
    {
        Assert.Equal(3, Defaults.WindowMonths);
        Assert.Equal(14, Defaults.LeadTimeDays);
        Assert.Equal(7, Defaults.SafetyDays);
        Assert.Equal(0m, Defaults.FloorQty);
        Assert.Equal(125m, Defaults.NearPercent);
        Assert.Equal(180, Defaults.DeadStockDays);
        Assert.Equal(3, Defaults.ForecastMonths);
        Assert.Equal(30, Defaults.OnProcessStaleDays);
        Assert.Equal(3, Defaults.MinHistoryMonths);
        Assert.Equal("default", Defaults.Source);
    }

    [Fact]
    public void Resolve_PrecedenceRequestOverDbOverDefault_AndSource()
    {
        var db = new PolicyOverrides { LeadTimeDays = 30, SafetyDays = 10, WindowMonths = 6, FloorQty = 5, NearPercent = 150, DeadStockDays = 90, ForecastMonths = 2, OnProcessStaleDays = 20, PriceChangePercent = 5, ConcentrationPercent = 40, MinHistoryMonths = 2 };
        var dbOnly = MaterialInsightThresholds.Resolve(db, null);
        Assert.Equal("db", dbOnly.Source);
        Assert.Equal(30, dbOnly.LeadTimeDays);

        var withRequest = MaterialInsightThresholds.Resolve(db, new PolicyOverrides { LeadTimeDays = 7 });
        Assert.Equal("request", withRequest.Source);
        Assert.Equal(7, withRequest.LeadTimeDays);
        Assert.Equal(10, withRequest.SafetyDays);

        Assert.Equal("db", MaterialInsightThresholds.Resolve(db, new PolicyOverrides()).Source);
    }

    [Fact]
    public void Resolve_Clamps()
    {
        var t = MaterialInsightThresholds.Resolve(null, new PolicyOverrides
        {
            WindowMonths = 0, LeadTimeDays = -5, SafetyDays = 9999, FloorQty = -1, NearPercent = 50,
            DeadStockDays = 0, ForecastMonths = 99, OnProcessStaleDays = 0, PriceChangePercent = 500,
            ConcentrationPercent = -3, MinHistoryMonths = 99
        });
        Assert.Equal(1, t.WindowMonths);
        Assert.Equal(0, t.LeadTimeDays);
        Assert.Equal(365, t.SafetyDays);
        Assert.Equal(0m, t.FloorQty);
        Assert.Equal(100m, t.NearPercent);
        Assert.Equal(1, t.DeadStockDays);
        Assert.Equal(36, t.ForecastMonths);
        Assert.Equal(1, t.OnProcessStaleDays);
        Assert.Equal(100m, t.PriceChangePercent);
        Assert.Equal(0m, t.ConcentrationPercent);
        Assert.Equal(12, t.MinHistoryMonths);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(12, 12)]
    [InlineData(99, 36)]
    public void ClampMonths(int input, int expected) => Assert.Equal(expected, MaterialInsightThresholds.ClampMonths(input));

    // ---- usage rate / auto min / effective min ----

    [Fact]
    public void AutoMin_IsCeilOfDailyUsageTimesLeadPlusSafety()
    {
        // 90 g / 90 วัน = 1 g/วัน x (14+7) = 21
        Assert.Equal(21m, MaterialInsightCalculator.AutoMin(1m, 14, 7));
        // 0.5 x 21 = 10.5 -> 11
        Assert.Equal(11m, MaterialInsightCalculator.AutoMin(0.5m, 14, 7));
        Assert.Equal(0m, MaterialInsightCalculator.AutoMin(0m, 14, 7));
    }

    [Fact]
    public void AvgDailyUsage_DividesByWindowDays_AndGuardsZero()
    {
        Assert.Equal(2m, MaterialInsightCalculator.AvgDailyUsage(180m, 90m));
        Assert.Equal(0m, MaterialInsightCalculator.AvgDailyUsage(180m, 0m));
        Assert.Equal(0m, MaterialInsightCalculator.AvgDailyUsage(0m, 90m));
    }

    [Fact]
    public void HeavyUseItemGetsHigherMinimumThanLightUse()
    {
        var start = Now.AddDays(-80);
        var heavyKey = "AU|18K";
        var lightKey = "AU|9K";
        var mv = new List<MaterialMovement>
        {
            Use(heavyKey, 800, start), Use(lightKey, 8, start),
            Use(heavyKey, 800, Now.AddDays(-10)), Use(lightKey, 8, Now.AddDays(-10))
        };
        var items = MaterialInsightCalculator.BuildItems("GOLD",
            new[] { Input("AU", "18K", 5000), Input("AU", "9K", 5000) }, mv, NoOverrides, Defaults, Now);

        var heavy = items.Single(i => i.ItemKey == heavyKey);
        var light = items.Single(i => i.ItemKey == lightKey);
        Assert.True(heavy.AutoMin > light.AutoMin * 50);
        Assert.Equal("auto", heavy.MinSource);
        Assert.True(heavy.AvgDailyUsage > light.AvgDailyUsage);
    }

    [Fact]
    public void ReturnsReduceConsumption_AndNeverNegative()
    {
        var key = "AU|18K";
        var mv = new List<MaterialMovement> { Use(key, 100, Now.AddDays(-30)), Ret(key, 40, Now.AddDays(-20)) };
        Assert.Equal(60m, Build(Input("AU", "18K", 500), mv).ConsumedInWindow);

        var mv2 = new List<MaterialMovement> { Use(key, 10, Now.AddDays(-30)), Ret(key, 40, Now.AddDays(-20)) };
        Assert.Equal(0m, Build(Input("AU", "18K", 500), mv2).ConsumedInWindow);
    }

    [Fact]
    public void WindowStart_IsNotBeforeFirstConsumptionMonth()
    {
        var first = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
        var start = MaterialInsightCalculator.EffectiveWindowStartUtc(Now, 3, first);
        // ต้นเดือนไทย ก.ย. = 31 ส.ค. 17:00 UTC (ใหม่กว่า 22 ก.ค.)
        Assert.Equal(new DateTime(2026, 8, 31, 17, 0, 0, DateTimeKind.Utc), start);

        var longHistory = MaterialInsightCalculator.EffectiveWindowStartUtc(Now, 3, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal(Now.AddMonths(-3), longHistory);
        Assert.Equal(Now.AddMonths(-3), MaterialInsightCalculator.EffectiveWindowStartUtc(Now, 3, null));
    }

    [Fact]
    public void WindowDays_MinOneDay()
        => Assert.Equal(1m, MaterialInsightCalculator.WindowDays(Now, Now));

    [Fact]
    public void ConsumptionOutsideWindow_IsIgnoredForRate()
    {
        var key = "AU|18K";
        var mv = new List<MaterialMovement> { Use(key, 1000, Now.AddMonths(-5)) };
        var item = Build(Input("AU", "18K", 500), mv);
        Assert.Equal(0m, item.AvgDailyUsage);
        Assert.Equal(0m, item.AutoMin);
        Assert.NotNull(item.LastMovementDate);
    }

    [Theory]
    [InlineData(10, 0, -1, 10, "auto")]
    [InlineData(10, 25, -1, 25, "floor")]
    [InlineData(10, 10, -1, 10, "auto")]
    [InlineData(10, 25, 5, 5, "override")]
    [InlineData(10, 25, 0, 0, "override")]
    [InlineData(10, 0, 100, 100, "override")]
    public void EffectiveMin_Precedence(decimal auto, decimal floor, int ov, decimal expected, string source)
    {
        var (value, src) = MaterialInsightCalculator.EffectiveMin(auto, floor, ov >= 0 ? ov : null);
        Assert.Equal(expected, value);
        Assert.Equal(source, src);
    }

    [Fact]
    public void PerItemLeadTimeOverride_ChangesAutoMin()
    {
        var key = "AU|18K";
        var mv = new List<MaterialMovement> { Use(key, 900, Now.AddDays(-60)) };
        var baseItem = Build(Input("AU", "18K", 5000), mv);
        var ov = new Dictionary<string, MaterialMinOverride> { [key] = new MaterialMinOverride(0m, 60) };
        var withOverride = Build(Input("AU", "18K", 5000), mv, ov: ov);

        Assert.Equal(60, withOverride.LeadTimeDays);
        Assert.Equal("override", withOverride.MinSource);
        Assert.Equal(0m, withOverride.EffectiveMin);
        Assert.True(withOverride.AutoMin > baseItem.AutoMin);
    }

    // ---- status classification ----

    [Theory]
    [InlineData(0, 20, 1, "zero")]
    [InlineData(-3, 20, 1, "negative")]
    [InlineData(0, 0, 0, "ok")]
    [InlineData(0, 0, 0.5, "zero")]
    [InlineData(0, 10, 0, "zero")]
    [InlineData(19.99, 20, 1, "below")]
    [InlineData(20, 20, 1, "near")]
    [InlineData(24.99, 20, 1, "near")]
    [InlineData(25, 20, 1, "ok")]
    [InlineData(5, 0, 0, "ok")]
    public void Classify_Boundaries(double available, double min, double usage, string expected)
        => Assert.Equal(expected, MaterialInsightCalculator.Classify((decimal)available, (decimal)min, (decimal)usage, 125m, false));

    [Fact]
    public void Classify_DeadOnlyWhenOtherwiseOk()
    {
        Assert.Equal("dead", MaterialInsightCalculator.Classify(10, 0, 0, 125m, true));
        Assert.Equal("below", MaterialInsightCalculator.Classify(10, 20, 0, 125m, true));
    }

    [Theory]
    [InlineData(10, 181, true)]
    [InlineData(10, 180, false)]
    [InlineData(0, 400, false)]
    [InlineData(-1, 400, false)]
    [InlineData(10, null, false)]
    public void IsDead_Boundaries(double onHand, int? days, bool expected)
        => Assert.Equal(expected, MaterialInsightCalculator.IsDead((decimal)onHand, days, 180));

    [Fact]
    public void NeverConsumedItem_AgesFromFirstSeen()
    {
        var item = Build(Input("AU", "18K", 100, firstSeen: Now.AddDays(-200)), new List<MaterialMovement>());
        Assert.True(item.NeverConsumed);
        Assert.Null(item.LastMovementDate);
        Assert.Equal(200, item.DaysSinceMove);
        Assert.True(item.IsDead);
        Assert.Equal("dead", item.Status);
        Assert.Equal(0m, item.AutoMin);
    }

    [Fact]
    public void ZeroUsageLowStock_IsOkWithoutOverride_BelowWithOverride()
    {
        var key = "AU|18K";
        var input = Input("AU", "18K", 5, firstSeen: Now.AddDays(-10));
        Assert.Equal("ok", Build(input, new List<MaterialMovement>()).Status);

        var ov = new Dictionary<string, MaterialMinOverride> { [key] = new MaterialMinOverride(50m, null) };
        Assert.Equal("below", Build(input, new List<MaterialMovement>(), ov: ov).Status);
    }

    [Fact]
    public void CoverDays_AndStockoutRisk()
    {
        var key = "AU|18K";
        // 600 g ใน 60 วันของหน้าต่าง -> ~10 g/วัน
        var mv = new List<MaterialMovement> { Use(key, 600, Now.AddDays(-50)) };
        var item = Build(Input("AU", "18K", 50), mv);
        Assert.NotNull(item.CoverDays);
        Assert.True(item.CoverDays < 14);
        Assert.True(item.StockoutRisk);

        var safe = Build(Input("AU", "18K", 5000), mv);
        Assert.False(safe.StockoutRisk);
        Assert.Null(Build(Input("AU", "18K", 0), mv).CoverDays);
    }

    // ---- series ----

    [Fact]
    public void BuildSeries_ReconstructsEndStockBackwardsFromCurrent()
    {
        var months = MaterialInsightCalculator.MonthStarts(Now, 3); // ส.ค., ก.ย., ต.ค. 2026
        var key = "AU|18K";
        var mv = new List<MaterialMovement>
        {
            new(key, new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc), MaterialMovementKind.Inflow, 100, 1),
            Use(key, 30, new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc)),
            Ret(key, 5, new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc)),
            new(key, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc), MaterialMovementKind.Other, 4, -1)
        };

        var s = MaterialInsightCalculator.BuildSeries(months, mv, 71m);
        Assert.Equal(71m, s[2].EndStock);
        Assert.Equal(75m, s[1].EndStock);
        Assert.Equal(100m, s[0].EndStock);
        Assert.Equal(100m, s[0].Inflow);
        Assert.Equal(30m, s[1].Consumed);
        Assert.Equal(5m, s[1].Returned);
        Assert.Equal(-4m, s[2].Other);
    }

    [Fact]
    public void BuildSeries_IgnoresMovementsOutsideMonths()
    {
        var months = MaterialInsightCalculator.MonthStarts(Now, 2);
        var mv = new List<MaterialMovement> { Use("k", 50, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)) };
        var s = MaterialInsightCalculator.BuildSeries(months, mv, 10m);
        Assert.All(s, p => Assert.Equal(10m, p.EndStock));
    }

    // ---- KPI / groups ----

    [Fact]
    public void Kpi_CountsAndNegativeData()
    {
        var items = new List<Item>
        {
            MakeItem("zero", "a", avail: 0),
            MakeItem("below", "b", avail: 5),
            MakeItem("near", "c", avail: 21),
            MakeItem("dead", "d", avail: 8, dead: true),
            MakeItem("negative", "e", avail: -2, usage: 0)
        };
        var kpi = MaterialInsightCalculator.BuildKpi(items, false, Defaults);
        Assert.Equal(5, kpi.ItemCount);
        Assert.Equal(1, kpi.ZeroCount);
        Assert.Equal(1, kpi.BelowMinCount);
        Assert.Equal(1, kpi.NearMinCount);
        Assert.Equal(1, kpi.DeadCount);
        Assert.Equal(8m, kpi.DeadOnHand);
        Assert.Equal(1, kpi.NegativeCount);
        Assert.Equal(-2m, kpi.NegativeWeight);
        Assert.Equal(34m, kpi.TotalOnHand);
        Assert.Equal(34m, kpi.TotalAvailable);
        Assert.Equal(0, kpi.OnProcessStaleCount);
        Assert.False(kpi.OnProcessAgeSupported);
    }

    [Fact]
    public void Groups_SumByCode()
    {
        var a1 = MakeItem("ok", "AU", avail: 10); a1.Code = "AU"; a1.Name = "ทอง";
        var a2 = MakeItem("ok", "AU2", avail: 5); a2.Code = "AU"; a2.OnProcess = 3;
        var b = MakeItem("ok", "AG", avail: 100); b.Code = "AG";
        var g = MaterialInsightCalculator.BuildGroups(new[] { a1, a2, b });
        Assert.Equal(2, g.Count);
        Assert.Equal("AG", g[0].Code);
        var au = g.Single(x => x.Code == "AU");
        Assert.Equal(2, au.ItemCount);
        Assert.Equal(15m, au.OnHand);
        Assert.Equal(3m, au.OnProcess);
        Assert.Equal("ทอง", au.Name);
    }

    // ---- rules ----

    [Fact]
    public void ZeroStock_CriticalWithTopItemsOrderedByUsage()
    {
        var items = new List<Item>
        {
            MakeItem("zero", "light", usage: 1), MakeItem("zero", "heavy", usage: 9), MakeItem("zero", "mid", usage: 5),
            MakeItem("zero", "mid2", usage: 4), MakeItem("ok", "fine")
        };
        var f = MaterialInsightRuleEngine.EvaluateZeroStock(items)!;
        Assert.Equal("MAT_ZERO_STOCK", f.Code);
        Assert.Equal("critical", f.Severity);
        Assert.Equal("matItems", f.ReportRef);
        Assert.Equal(4, f.Params["count"]);
        Assert.Equal(new List<string> { "heavy", "mid", "mid2" }, (List<string>)f.Params["topItems"]);
        Assert.Null(MaterialInsightRuleEngine.EvaluateZeroStock(new[] { MakeItem("ok") }));
    }

    [Fact]
    public void BelowAndNear_WarningOrderedByRatio()
    {
        var items = new List<Item>
        {
            MakeItem("below", "b1", avail: 10, min: 20), MakeItem("below", "b2", avail: 2, min: 20),
            MakeItem("near", "n1", avail: 21, min: 20), MakeItem("near", "n2", avail: 20, min: 20)
        };
        var below = MaterialInsightRuleEngine.EvaluateBelowMin(items)!;
        Assert.Equal("MAT_BELOW_MIN", below.Code);
        Assert.Equal("warning", below.Severity);
        Assert.Equal(new List<string> { "b2", "b1" }, (List<string>)below.Params["topItems"]);

        var near = MaterialInsightRuleEngine.EvaluateNearMin(items)!;
        Assert.Equal("MAT_NEAR_MIN", near.Code);
        Assert.Equal(2, near.Params["count"]);
        Assert.Equal(new List<string> { "n2", "n1" }, (List<string>)near.Params["topItems"]);
    }

    [Fact]
    public void DeadStock_SumsOnHand()
    {
        var items = new List<Item> { MakeItem("dead", "a", avail: 10, dead: true), MakeItem("dead", "b", avail: 5, dead: true), MakeItem("ok", "c") };
        var f = MaterialInsightRuleEngine.EvaluateDeadStock(items, Defaults)!;
        Assert.Equal("MAT_DEAD_STOCK", f.Code);
        Assert.Equal(2, f.Params["count"]);
        Assert.Equal(15m, f.Params["totalOnHand"]);
        Assert.Equal(180, f.Params["days"]);
        Assert.Null(MaterialInsightRuleEngine.EvaluateDeadStock(new[] { MakeItem("ok") }, Defaults));
    }

    [Fact]
    public void OnProcessStale_SkippedWhenUnsupported_OrNotOldEnough()
    {
        var items = new List<Item> { MakeItem("ok", "a", onProcess: 12, age: 45), MakeItem("ok", "b", onProcess: 3, age: 30), MakeItem("ok", "c", onProcess: 2, age: null) };
        Assert.Null(MaterialInsightRuleEngine.EvaluateOnProcessStale(items, false, Defaults));

        var f = MaterialInsightRuleEngine.EvaluateOnProcessStale(items, true, Defaults)!;
        Assert.Equal("MAT_ON_PROCESS_STALE", f.Code);
        Assert.Equal("matOnProcess", f.ReportRef);
        Assert.Equal(1, f.Params["count"]);
        Assert.Equal(12m, f.Params["totalOnProcess"]);
        Assert.Equal(30, f.Params["days"]);
    }

    [Fact]
    public void Stockout_GatedByHistory_AndSeverityByHalfLead()
    {
        var items = new List<Item>
        {
            MakeItem("ok", "a", avail: 50, cover: 10, risk: true), MakeItem("ok", "b", avail: 50, cover: 12, risk: true), MakeItem("ok", "c", cover: 60)
        };
        Assert.Null(MaterialInsightRuleEngine.EvaluateStockout(items, 2, Defaults));

        var warn = MaterialInsightRuleEngine.EvaluateStockout(items, 3, Defaults)!;
        Assert.Equal("FC_MAT_STOCKOUT", warn.Code);
        Assert.Equal("warning", warn.Severity);
        Assert.Equal("matTrend", warn.ReportRef);
        Assert.Equal(2, warn.Params["count"]);
        Assert.Equal(14, warn.Params["leadDays"]);
        Assert.Equal(3, warn.Params["historyMonths"]);
        Assert.Equal(3, warn.Params["windowMonths"]);
        Assert.Equal(10m, warn.Params["minCoverDays"]);

        items.Add(MakeItem("ok", "d", avail: 50, cover: 6.9m, risk: true));
        Assert.Equal("critical", MaterialInsightRuleEngine.EvaluateStockout(items, 3, Defaults)!.Severity);
    }

    [Fact]
    public void Stockout_MinHistoryOverridable()
    {
        var t = MaterialInsightThresholds.Resolve(null, new PolicyOverrides { MinHistoryMonths = 1 });
        var items = new List<Item> { MakeItem("ok", "a", avail: 50, cover: 10, risk: true) };
        Assert.NotNull(MaterialInsightRuleEngine.EvaluateStockout(items, 1, t));
        Assert.Null(MaterialInsightRuleEngine.EvaluateStockout(items, 0, t));
    }

    // ---- status + actions ----

    [Fact]
    public void WorstSeverity_Levels()
    {
        Assert.Equal("ok", MaterialInsightRuleEngine.WorstSeverity(new List<jewelry.Model.Production.Insight.Wip.Finding>()));
        var w = new jewelry.Model.Production.Insight.Wip.Finding { Code = "x", Severity = "warning" };
        var c = new jewelry.Model.Production.Insight.Wip.Finding { Code = "y", Severity = "critical" };
        Assert.Equal("warning", MaterialInsightRuleEngine.WorstSeverity(new[] { w }));
        Assert.Equal("critical", MaterialInsightRuleEngine.WorstSeverity(new[] { w, c }));
    }

    [Fact]
    public void Actions_NoFindings_Empty()
        => Assert.Empty(MaterialInsightRuleEngine.BuildActions(new List<jewelry.Model.Production.Insight.Wip.Finding>(), new List<jewelry.Model.Production.Insight.Wip.Finding>()));

    [Fact]
    public void Actions_PriorityCriticalFirst_ThenDeclaredOrder_WithOwnerAndRelated()
    {
        var items = new List<Item>
        {
            MakeItem("near", "n", avail: 21, min: 20),
            MakeItem("dead", "d", avail: 7, dead: true),
            MakeItem("zero", "z", avail: 0)
        };
        var problems = new List<jewelry.Model.Production.Insight.Wip.Finding>
        {
            MaterialInsightRuleEngine.EvaluateNearMin(items)!,
            MaterialInsightRuleEngine.EvaluateDeadStock(items, Defaults)!,
            MaterialInsightRuleEngine.EvaluateZeroStock(items)!
        };

        var actions = MaterialInsightRuleEngine.BuildActions(problems, new List<jewelry.Model.Production.Insight.Wip.Finding>());
        Assert.Equal(new[] { "ACT_REORDER_MATERIAL", "ACT_CLEAR_DEAD_STOCK", "ACT_REVIEW_MIN" }, actions.Select(a => a.Code).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, actions.Select(a => a.Priority).ToArray());
        Assert.Equal("planner", actions[0].OwnerRole);
        Assert.Equal("productionManager", actions[1].OwnerRole);
        Assert.Contains("MAT_ZERO_STOCK", actions[0].RelatedCodes);
        Assert.Equal(1, actions[0].Params["zeroCount"]);
        Assert.Equal(0, actions[0].Params["belowCount"]);
    }

    [Fact]
    public void Actions_ReturnOnProcess_WhenStaleFinding()
    {
        var items = new List<Item> { MakeItem("ok", "a", onProcess: 12, age: 45) };
        var stale = MaterialInsightRuleEngine.EvaluateOnProcessStale(items, true, Defaults)!;
        var actions = MaterialInsightRuleEngine.BuildActions(new List<jewelry.Model.Production.Insight.Wip.Finding> { stale }, new List<jewelry.Model.Production.Insight.Wip.Finding>());
        Assert.Single(actions);
        Assert.Equal("ACT_RETURN_ON_PROCESS", actions[0].Code);
        Assert.Equal("deptHead", actions[0].OwnerRole);
        Assert.Equal(12m, actions[0].Params["totalOnProcess"]);
    }

    [Fact]
    public void Actions_ForecastOnlyStockout_TriggersReorder()
    {
        var items = new List<Item> { MakeItem("ok", "a", avail: 50, cover: 3, risk: true) };
        var fc = MaterialInsightRuleEngine.EvaluateStockout(items, 3, Defaults)!;
        var actions = MaterialInsightRuleEngine.BuildActions(new List<jewelry.Model.Production.Insight.Wip.Finding>(), new List<jewelry.Model.Production.Insight.Wip.Finding> { fc });
        Assert.Equal("ACT_REORDER_MATERIAL", actions.Single().Code);
        Assert.Equal(1, actions[0].Params["stockoutCount"]);
    }

    // ---- negative balance (ledger without opening balance) ----

    private static Item NegItem(string code, decimal balance, decimal consumed90d)
    {
        var key = code + "|9";
        var mv = new List<MaterialMovement> { Use(key, consumed90d, Now.AddDays(-40)) };
        var input = Input(code, "9", balance);
        return Build(input, mv);
    }

    [Fact]
    public void Negative_StatusFirst_CoverNullNoRiskNotDead_UsageStillComputed()
    {
        var item = NegItem("SV", -7176.12m, 7200m);
        Assert.Equal("negative", item.Status);
        Assert.Null(item.CoverDays);
        Assert.False(item.StockoutRisk);
        Assert.False(item.IsDead);
        Assert.True(item.AvgDailyUsage > 0);
        Assert.True(item.AutoMin > 0);
        Assert.Equal(item.AutoMin, item.EffectiveMin);
        Assert.Equal("auto", item.MinSource);
        Assert.Equal(7200m, item.ConsumedInWindow);
        Assert.True(item.AvgMonthlyUsage > 0);
    }

    [Fact]
    public void Negative_NeverConsumedStaleItem_NotDead()
    {
        var item = Build(Input("SV", "9", -5m, firstSeen: Now.AddDays(-500)), new List<MaterialMovement>());
        Assert.Equal("negative", item.Status);
        Assert.False(item.IsDead);
    }

    [Fact]
    public void Kpi_NegativeExcludedFromTotalsAndCounts_SignedNegativeWeight()
    {
        var items = new List<Item>
        {
            NegItem("SV", -100m, 100m), NegItem("YG", -50.5m, 50m),
            Build(Input("WG", "7", 30m, firstSeen: Now.AddDays(-5)), new List<MaterialMovement>())
        };
        var kpi = MaterialInsightCalculator.BuildKpi(items, false, Defaults, inboundRecorded: false);
        Assert.Equal(3, kpi.ItemCount);
        Assert.Equal(2, kpi.NegativeCount);
        Assert.Equal(-150.5m, kpi.NegativeWeight);
        Assert.Equal(30m, kpi.TotalOnHand);
        Assert.Equal(30m, kpi.TotalAvailable);
        Assert.Equal(0, kpi.ZeroCount);
        Assert.Equal(0, kpi.BelowMinCount);
        Assert.Equal(0, kpi.NearMinCount);
        Assert.Equal(0, kpi.DeadCount);
        Assert.Equal(0, kpi.StockoutRiskCount);
        Assert.False(kpi.InboundRecorded);
        Assert.True(MaterialInsightCalculator.BuildKpi(items, false, Defaults, true).InboundRecorded);
    }

    [Fact]
    public void Groups_KeepSignedSum_AndCountNegatives()
    {
        var a = MakeItem("negative", "a", avail: -10); a.Code = "SV";
        var b = MakeItem("ok", "b", avail: 4); b.Code = "SV";
        var g = MaterialInsightCalculator.BuildGroups(new[] { a, b }).Single();
        Assert.Equal(-6m, g.OnHand);
        Assert.Equal(1, g.NegativeCount);
    }

    [Fact]
    public void NegativeBalance_WarningWithTopItemsByAbsoluteBalance()
    {
        var items = new List<Item>
        {
            MakeItem("negative", "small", avail: -5), MakeItem("negative", "huge", avail: -7176.12m),
            MakeItem("negative", "mid", avail: -400), MakeItem("negative", "mid2", avail: -100), MakeItem("ok", "fine")
        };
        var f = MaterialInsightRuleEngine.EvaluateNegativeBalance(items, false)!;
        Assert.Equal("MAT_NEGATIVE_BALANCE", f.Code);
        Assert.Equal("warning", f.Severity);
        Assert.Equal("matItems", f.ReportRef);
        Assert.Equal(4, f.Params["count"]);
        Assert.Equal(7681.12m, f.Params["totalWeight"]);
        Assert.Equal(new List<string> { "huge", "mid", "mid2" }, (List<string>)f.Params["topItems"]);
        Assert.Equal(false, f.Params["ledgerHasInbound"]);
        Assert.Equal(true, MaterialInsightRuleEngine.EvaluateNegativeBalance(items, true)!.Params["ledgerHasInbound"]);
        Assert.Null(MaterialInsightRuleEngine.EvaluateNegativeBalance(new[] { MakeItem("ok") }, false));
    }

    [Fact]
    public void NegativeItems_DoNotFeedZeroBelowNearDeadStockout()
    {
        var items = new List<Item> { MakeItem("negative", "a", avail: -10, dead: false, risk: false, cover: null) };
        Assert.Null(MaterialInsightRuleEngine.EvaluateZeroStock(items));
        Assert.Null(MaterialInsightRuleEngine.EvaluateBelowMin(items));
        Assert.Null(MaterialInsightRuleEngine.EvaluateNearMin(items));
        Assert.Null(MaterialInsightRuleEngine.EvaluateDeadStock(items, Defaults));
        Assert.Null(MaterialInsightRuleEngine.EvaluateStockout(items, 12, Defaults));
    }

    [Fact]
    public void Actions_RecordOpeningBalance_FirstAmongWarnings_CriticalStillFirst()
    {
        var neg = MakeItem("negative", "n", avail: -3);
        var below = MakeItem("below", "b", avail: 5, min: 20);
        var negF = MaterialInsightRuleEngine.EvaluateNegativeBalance(new[] { neg }, false)!;
        var belowF = MaterialInsightRuleEngine.EvaluateBelowMin(new[] { below })!;

        var onlyWarn = MaterialInsightRuleEngine.BuildActions(new List<jewelry.Model.Production.Insight.Wip.Finding> { belowF, negF }, new List<jewelry.Model.Production.Insight.Wip.Finding>());
        Assert.Equal(new[] { "ACT_RECORD_OPENING_BALANCE", "ACT_REORDER_MATERIAL" }, onlyWarn.Select(a => a.Code).ToArray());
        Assert.Equal("deptHead", onlyWarn[0].OwnerRole);
        Assert.Contains("MAT_NEGATIVE_BALANCE", onlyWarn[0].RelatedCodes);
        Assert.Equal(1, onlyWarn[0].Params["count"]);

        var zeroF = MaterialInsightRuleEngine.EvaluateZeroStock(new[] { MakeItem("zero", "z", avail: 0) })!;
        var withCritical = MaterialInsightRuleEngine.BuildActions(new List<jewelry.Model.Production.Insight.Wip.Finding> { negF, zeroF }, new List<jewelry.Model.Production.Insight.Wip.Finding>());
        Assert.Equal(new[] { "ACT_REORDER_MATERIAL", "ACT_RECORD_OPENING_BALANCE" }, withCritical.Select(a => a.Code).ToArray());
    }

    [Fact]
    public void ProdScenario_FiveNegativeItemsNoInbound_OnlyNegativeBalanceProblem()
    {
        var items = new List<Item>
        {
            NegItem("SV", -7176.12m, 7200m), NegItem("WG", -95.88m, 90m), NegItem("YG3", -403.03m, 400m),
            NegItem("YG5", -166.57m, 160m), NegItem("YG7", -1633.74m, 1600m)
        };
        Assert.All(items, i => Assert.Equal("negative", i.Status));

        var kpi = MaterialInsightCalculator.BuildKpi(items, false, Defaults, false);
        Assert.Equal(5, kpi.NegativeCount);
        Assert.Equal(-9475.34m, kpi.NegativeWeight);
        Assert.Equal(0m, kpi.TotalOnHand);

        var problems = new List<jewelry.Model.Production.Insight.Wip.Finding?>
        {
            MaterialInsightRuleEngine.EvaluateNegativeBalance(items, false),
            MaterialInsightRuleEngine.EvaluateZeroStock(items),
            MaterialInsightRuleEngine.EvaluateBelowMin(items),
            MaterialInsightRuleEngine.EvaluateNearMin(items),
            MaterialInsightRuleEngine.EvaluateDeadStock(items, Defaults)
        }.Where(f => f != null).Select(f => f!).ToList();
        var forecasts = new List<jewelry.Model.Production.Insight.Wip.Finding?>
        {
            MaterialInsightRuleEngine.EvaluateStockout(items, 12, Defaults)
        }.Where(f => f != null).Select(f => f!).ToList();

        Assert.Equal("MAT_NEGATIVE_BALANCE", Assert.Single(problems).Code);
        Assert.Empty(forecasts);
        Assert.Equal("warning", MaterialInsightRuleEngine.WorstSeverity(problems.Concat(forecasts)));
        var actions = MaterialInsightRuleEngine.BuildActions(problems, forecasts);
        Assert.Equal("ACT_RECORD_OPENING_BALANCE", Assert.Single(actions).Code);
    }

    [Fact]
    public void MixedScenario_NegativeAndNormalItemsBehaveIndependently()
    {
        var neg = NegItem("SV", -100m, 100m);
        var zero = Build(Input("YG", "5", 0m), new List<MaterialMovement> { Use("YG|5", 90m, Now.AddDays(-30)) });
        var normal = Build(Input("WG", "7", 5000m, firstSeen: Now.AddDays(-3)), new List<MaterialMovement>());
        var items = new List<Item> { neg, zero, normal };

        Assert.Equal("negative", neg.Status);
        Assert.Equal("zero", zero.Status);
        Assert.Equal("ok", normal.Status);

        var kpi = MaterialInsightCalculator.BuildKpi(items, false, Defaults);
        Assert.Equal(1, kpi.NegativeCount);
        Assert.Equal(1, kpi.ZeroCount);
        Assert.Equal(5000m, kpi.TotalOnHand);

        Assert.Equal(1, MaterialInsightRuleEngine.EvaluateZeroStock(items)!.Params["count"]);
        Assert.Equal(1, MaterialInsightRuleEngine.EvaluateNegativeBalance(items, true)!.Params["count"]);
    }

    // ---- gold provider mapping ----

    [Theory]
    [InlineData(1, null, MaterialMovementKind.Inflow, 1)]
    [InlineData(2, null, MaterialMovementKind.Other, 1)]
    [InlineData(3, "completed", MaterialMovementKind.Returned, 1)]
    [InlineData(4, "completed", MaterialMovementKind.Consumed, -1)]
    [InlineData(4, "reversed", MaterialMovementKind.Other, -1)]
    [InlineData(3, "reversed", MaterialMovementKind.Other, 1)]
    [InlineData(5, null, MaterialMovementKind.Other, 1)]
    [InlineData(6, null, MaterialMovementKind.Other, -1)]
    [InlineData(7, "completed", MaterialMovementKind.Other, 1)]
    [InlineData(8, "completed", MaterialMovementKind.Other, -1)]
    public void GoldMapping_TypeAndStatus(int type, string? status, MaterialMovementKind kind, int sign)
    {
        var m = GoldMaterialInsightProvider.MapMovement("AU|18K", type, 10m, status, Now)!;
        Assert.Equal(kind, m.Kind);
        Assert.Equal(sign, m.Sign);
        Assert.Equal(10m, m.Qty);
    }

    [Fact]
    public void GoldMapping_UnknownType_Skipped()
        => Assert.Null(GoldMaterialInsightProvider.MapMovement("AU|18K", 99, 10m, null, Now));
}
