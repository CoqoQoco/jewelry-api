using jewelry.Model.Material.Insight;
using jewelry.Model.Material.Insight.Overview;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Common;
using Jewelry.Service.Material.Insight;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Jewelry.Service.Tests.Material.Insight;

public class GemMaterialInsightTests
{
    private static readonly PolicyValues Defaults = MaterialInsightThresholds.Resolve(null, null);
    private static readonly DateTime Now = new(2026, 10, 22, 5, 0, 0, DateTimeKind.Utc);

    private class NoHttpContext : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private static JewelryContext NewContext()
        => new(new DbContextOptionsBuilder<JewelryContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static TbtStockGem Gem(int id, string code, decimal price, decimal priceQty, decimal qty, decimal qtyProc,
        decimal weight, decimal weightProc, string group = "RU")
        => new()
        {
            Id = id, Code = code, GroupName = group, Shape = "RD", Size = "3.0", Grade = "AA", CreateBy = "t",
            Price = price, PriceQty = priceQty, Quantity = qty, QuantityOnProcess = qtyProc,
            QuantityWeight = weight, QuantityWeightOnProcess = weightProc,
            CreateDate = DateTime.UtcNow.AddDays(-400)
        };

    private static TbtStockGemTransection Tx(long id, string code, int type, decimal qty, decimal weight, DateTime date, string status = "completed")
        => new()
        {
            Id = id, Running = "R" + id, Code = code, Type = type, Qty = qty, QtyWeight = weight,
            RequestDate = date, Stastus = status, CreateBy = "t", CreateDate = date
        };

    [Theory]
    [InlineData(0, 5, 9, "pcs", 5)]      // price_qty -> เม็ด
    [InlineData(7, 0, 0, "ct", 7)]       // price -> กะรัต
    [InlineData(0, 0, 2.5, "ct", 0)]     // ไม่มีราคา มีน้ำหนัก -> ct
    [InlineData(0, 0, 0, "pcs", 0)]      // ไม่มีราคา ไม่มีน้ำหนัก -> pcs
    public void DecideUnit_FollowsData(double price, double priceQty, double weight, string unit, double unitValue)
    {
        var (u, v) = GemMaterialInsightProvider.DecideUnit((decimal)price, (decimal)priceQty, (decimal)weight);
        Assert.Equal(unit, u);
        Assert.Equal((decimal)unitValue, v);
    }

    [Theory]
    [InlineData(1, MaterialMovementKind.Inflow, 1)]
    [InlineData(2, MaterialMovementKind.Inflow, 1)]
    [InlineData(3, MaterialMovementKind.Returned, 1)]
    [InlineData(4, MaterialMovementKind.Consumed, -1)]
    [InlineData(5, MaterialMovementKind.Other, -1)]
    [InlineData(6, MaterialMovementKind.Other, 1)]
    [InlineData(7, MaterialMovementKind.Consumed, 0)]
    [InlineData(8, MaterialMovementKind.Other, -1)]
    [InlineData(9, MaterialMovementKind.Other, 1)]
    public void MapMovement_TypeMapping(int type, MaterialMovementKind kind, int sign)
    {
        var m = GemMaterialInsightProvider.MapMovement("A", type, 3m, Now)!;
        Assert.Equal(kind, m.Kind);
        Assert.Equal(sign, m.Sign);
        Assert.Equal(3m, m.Qty);
    }

    [Fact]
    public void MapMovement_UnknownTypeIgnored()
        => Assert.Null(GemMaterialInsightProvider.MapMovement("A", 10, 1m, Now));

    [Fact]
    public async Task Load_DecidesUnitValueAndKeepsBucketsSeparate()
    {
        var ctx = NewContext();
        ctx.TbmGem.Add(new TbmGem { Id = 1, Code = "RU", NameEn = "Ruby", NameTh = "ทับทิม", CreateBy = "t" });
        ctx.TbtStockGem.AddRange(
            Gem(1, "BYQ", 0, 10, 20, 5, 99, 99),     // by count: 20 pcs, 5 on process, value 20*10
            Gem(2, "BYW", 100, 0, 99, 99, 4, 1.5m),  // by weight: 4 ct, 1.5 ct on process, value 4*100
            Gem(3, "NOP", 0, 0, 7, 0, 0, 0));        // ไม่มีราคา ไม่มีน้ำหนัก -> pcs 7
        var d1 = DateTime.UtcNow.AddDays(-45);
        var d2 = DateTime.UtcNow.AddDays(-10);
        ctx.TbtStockGemTransection.AddRange(
            Tx(1, "BYQ", 4, 3, 30, d2),
            Tx(2, "BYW", 4, 30, 2, d2),
            Tx(3, "BYW", 5, 9, 1.5m, d1, "process"),
            Tx(4, "BYW", 5, 9, 0.5m, d2, "process"),
            Tx(5, "BYQ", 5, 2, 8, d1, "completed"),
            Tx(6, "BYW", 8, 6, 2, d2));
        await ctx.SaveChangesAsync();

        var ds = await new GemMaterialInsightProvider(ctx).LoadAsync();

        var q = ds.Items.Single(i => i.ItemKey == "BYQ");
        Assert.Equal("pcs", q.Unit);
        Assert.Equal(20m, q.OnHand);       // quantity ไม่หัก on_process
        Assert.Equal(5m, q.OnProcess);
        Assert.Equal("RU", q.GroupCode);
        Assert.Equal("ทับทิม", q.GroupName);
        Assert.Null(q.OnProcessAgeDays);   // ไม่มีใบยืมที่ยังเปิด

        var w = ds.Items.Single(i => i.ItemKey == "BYW");
        Assert.Equal("ct", w.Unit);
        Assert.Equal(4m, w.OnHand);
        Assert.Equal(1.5m, w.OnProcess);
        Assert.InRange(w.OnProcessAgeDays!.Value, 44, 45); // เก่าสุดของใบยืมที่ยังเปิด

        Assert.Equal("pcs", ds.Items.Single(i => i.ItemKey == "NOP").Unit);
        Assert.True(ds.OnProcessAgeSupported);

        // movement วัดตามหน่วยของ item
        Assert.Equal(2m, ds.Movements.Single(m => m.ItemKey == "BYW" && m.Sign == -1 && m.Kind == MaterialMovementKind.Other && m.Qty == 2m).Qty);
        Assert.Equal(3m, ds.Movements.Single(m => m.ItemKey == "BYQ" && m.Kind == MaterialMovementKind.Consumed).Qty);
        Assert.Equal(2m, ds.Movements.Single(m => m.ItemKey == "BYW" && m.Kind == MaterialMovementKind.Consumed).Qty);

        var items = MaterialInsightCalculator.BuildItems("GEM", ds.Items, ds.Movements, new Dictionary<string, MaterialMinOverride>(), Defaults, DateTime.UtcNow);
        Assert.Equal(200m, items.Single(i => i.ItemKey == "BYQ").Value);
        Assert.Equal(400m, items.Single(i => i.ItemKey == "BYW").Value);
        Assert.Equal(0m, items.Single(i => i.ItemKey == "NOP").Value);
    }

    [Fact]
    public void ConsumptionIsType4Plus7_Not5()
    {
        var mv = new[]
        {
            GemMaterialInsightProvider.MapMovement("A", 4, 2m, Now.AddDays(-5))!,
            GemMaterialInsightProvider.MapMovement("A", 7, 3m, Now.AddDays(-4))!,
            GemMaterialInsightProvider.MapMovement("A", 5, 10m, Now.AddDays(-6))!,
            GemMaterialInsightProvider.MapMovement("A", 6, 5m, Now.AddDays(-3))!
        };
        var input = new MaterialItemInput { ItemKey = "A", Code = "A", Unit = "pcs", OnHand = 10, UnitValue = 1m, FirstSeenUtc = Now.AddDays(-400) };

        var item = MaterialInsightCalculator.BuildItems("GEM", new[] { input }, mv, new Dictionary<string, MaterialMinOverride>(), Defaults, Now).Single();

        Assert.Equal(5m, item.ConsumedInWindow);
    }

    [Fact]
    public void Kpi_SplitsTotalsByUnit_AndNeverAddsPiecesToCarats()
    {
        var inputs = new[]
        {
            new MaterialItemInput { ItemKey = "P", Code = "P", Unit = "pcs", UnitValue = 10m, OnHand = 20, OnProcess = 5, FirstSeenUtc = Now.AddDays(-10) },
            new MaterialItemInput { ItemKey = "C", Code = "C", Unit = "ct", UnitValue = 100m, OnHand = 4, OnProcess = 1, FirstSeenUtc = Now.AddDays(-10) }
        };
        var items = MaterialInsightCalculator.BuildItems("GEM", inputs, Array.Empty<MaterialMovement>(), new Dictionary<string, MaterialMinOverride>(), Defaults, Now);
        var kpi = MaterialInsightCalculator.BuildKpi(items, true, Defaults);

        Assert.True(kpi.MixedUnits);
        Assert.Equal(0m, kpi.TotalOnHand);
        Assert.Equal(2, kpi.ByUnit.Count);
        Assert.Equal(20m, kpi.ByUnit.Single(u => u.Unit == "pcs").TotalOnHand);
        Assert.Equal(4m, kpi.ByUnit.Single(u => u.Unit == "ct").TotalOnHand);
        Assert.Equal(200m, kpi.ByUnit.Single(u => u.Unit == "pcs").Value);
        Assert.Equal(600m, kpi.TotalValue);
        Assert.Equal(150m, kpi.TotalOnProcessValue);

        var groups = MaterialInsightCalculator.BuildGroups(items);
        Assert.Equal(2, groups.Count);
    }

    [Fact]
    public void Gold_OutputUnchanged()
    {
        var inputs = new[]
        {
            new MaterialItemInput { ItemKey = "G|1", Code = "G", Name = "gold", SizeCode = "1", OnHand = 10, OnProcess = 2, FirstSeenUtc = Now.AddDays(-10) },
            new MaterialItemInput { ItemKey = "G|2", Code = "G", Name = "gold", SizeCode = "2", OnHand = 5, FirstSeenUtc = Now.AddDays(-10) }
        };
        var items = MaterialInsightCalculator.BuildItems("GOLD", inputs, Array.Empty<MaterialMovement>(), new Dictionary<string, MaterialMinOverride>(), Defaults, Now);
        var kpi = MaterialInsightCalculator.BuildKpi(items, false, Defaults);

        Assert.All(items, i => { Assert.Equal("g", i.Unit); Assert.Null(i.Value); Assert.Equal("G", i.GoldCode); });
        Assert.False(kpi.MixedUnits);
        Assert.Equal(15m, kpi.TotalOnHand);
        Assert.Equal(2m, kpi.TotalOnProcess);
        Assert.Null(kpi.TotalValue);
        Assert.Single(kpi.ByUnit);

        var g = MaterialInsightCalculator.BuildGroups(items).Single();
        Assert.Equal("G", g.Code);
        Assert.Equal("gold", g.Name);
        Assert.Equal(15m, g.OnHand);
        Assert.Equal("g", g.Unit);
    }

    [Fact]
    public void Series_ByUnit_AndValue()
    {
        var months = MaterialInsightCalculator.MonthStarts(Now, 3);
        var inputs = new[]
        {
            new MaterialItemInput { ItemKey = "P", Code = "P", Unit = "pcs", UnitValue = 10m, OnHand = 20 },
            new MaterialItemInput { ItemKey = "C", Code = "C", Unit = "ct", UnitValue = 100m, OnHand = 4 }
        };
        var mv = new[]
        {
            new MaterialMovement("P", Now.AddDays(-1), MaterialMovementKind.Consumed, 3m, -1),
            new MaterialMovement("C", Now.AddDays(-1), MaterialMovementKind.Consumed, 1m, -1)
        };

        var byUnit = MaterialInsightCalculator.BuildSeriesByUnit(months, inputs, mv);
        Assert.Equal(new[] { "ct", "pcs" }, byUnit.Select(s => s.Unit).ToArray());
        Assert.Equal(3m, byUnit.Single(s => s.Unit == "pcs").Points.Last().Consumed);
        Assert.Equal(1m, byUnit.Single(s => s.Unit == "ct").Points.Last().Consumed);
        Assert.Equal(20m, byUnit.Single(s => s.Unit == "pcs").Points.Last().EndStock);

        var value = MaterialInsightCalculator.BuildValueSeries(months, inputs, mv)!;
        Assert.Equal(130m, value.Last().ConsumedValue);
        Assert.Equal(600m, value.Last().EndStockValue);
        Assert.Equal(730m, value[^2].EndStockValue);

        Assert.Null(MaterialInsightCalculator.BuildValueSeries(months, new[] { new MaterialItemInput { ItemKey = "G", Code = "G" } }, mv));
    }

    [Fact]
    public async Task Service_AcceptsGemKind_EndToEnd_AndStaleOnProcess()
    {
        var ctx = NewContext();
        ctx.TbtStockGem.AddRange(
            Gem(1, "BYQ", 0, 10, 20, 5, 0, 0),
            Gem(2, "BYW", 100, 0, 0, 0, 4, 1.5m));
        ctx.TbtStockGemTransection.Add(Tx(1, "BYW", 5, 0, 1.5m, DateTime.UtcNow.AddDays(-60), "process"));
        await ctx.SaveChangesAsync();

        var cache = new InsightCache(new MemoryCache(Options.Create(new MemoryCacheOptions())));
        var service = new MaterialInsightService(ctx, new NoHttpContext(), new IMaterialInsightProvider[] { new GemMaterialInsightProvider(ctx) }, cache);

        var res = await service.Overview(new Request { Kind = "gem", Months = 6 });

        Assert.Equal("GEM", res.Kind);
        Assert.Equal(new[] { "ct", "pcs" }, res.Units.ToArray());
        Assert.True(res.Kpi.MixedUnits);
        Assert.True(res.Kpi.OnProcessAgeSupported);
        Assert.Equal(1, res.Kpi.OnProcessStaleCount);
        Assert.Empty(res.Series);
        Assert.Equal(2, res.SeriesByUnit.Count);
        Assert.Equal(6, res.ValueSeries.Count);
        Assert.Contains(res.Problems, p => p.Code == "MAT_ON_PROCESS_STALE");
    }
}
