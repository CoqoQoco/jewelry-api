using jewelry.Model.ProductionPlan.ProductionPlanStatus;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.ProductionPlan;

namespace Jewelry.Service.Tests.ProductionPlan;

public class ProductionPlanGemMatcherTests
{
    private static TbtProductionPlanStatusDetailGem Kept(string itemNo, string outbound, string code) => new()
    {
        ItemNo = itemNo,
        OutboundRunning = outbound,
        GemCode = code,
        GemPrice = 1m
    };

    // Mirrors HandleGemDetails loop: returns (updated, added-new, skipped)
    private static (List<TbtProductionPlanStatusDetailGem> updated, int created) Run(
        List<TbtProductionPlanStatusDetailGem> kept, List<GemItem> gems)
    {
        var updated = new List<TbtProductionPlanStatusDetailGem>();
        var created = 0;
        foreach (var gem in gems)
        {
            var existing = ProductionPlanGemMatcher.FindOutboundGem(kept, gem);
            if (existing == null && !string.IsNullOrEmpty(gem.OutboundRunning)) continue;
            if (existing != null)
            {
                ProductionPlanGemMatcher.ApplyOutboundGemUpdate(existing, gem);
                updated.Add(existing);
            }
            else created++;
        }
        return (updated, created);
    }

    [Fact]
    public void OutboundRow_GetsLengthAndUnit()
    {
        var kept = new List<TbtProductionPlanStatusDetailGem> { Kept("G-1", "OUT-1", "C1") };
        var (updated, created) = Run(kept, new() { new GemItem { OutboundRunning = "OUT-1", ItemNo = "G-1", Code = "C1", Length = 18m, LengthUnit = "inch", Price = 5m } });
        Assert.Single(updated);
        Assert.Equal(0, created);
        Assert.Equal(18m, kept[0].GemLength);
        Assert.Equal("inch", kept[0].GemLengthUnit);
    }

    [Fact]
    public void NullLength_ClearsValue()
    {
        var kept = new List<TbtProductionPlanStatusDetailGem> { Kept("G-1", "OUT-1", "C1") };
        kept[0].GemLength = 18m;
        kept[0].GemLengthUnit = "inch";
        Run(kept, new() { new GemItem { OutboundRunning = "OUT-1", ItemNo = "G-1", Code = "C1", Length = null, LengthUnit = null } });
        Assert.Null(kept[0].GemLength);
        Assert.Null(kept[0].GemLengthUnit);
    }

    [Fact]
    public void FallbackToGemCode_NoDuplicateRow()
    {
        var kept = new List<TbtProductionPlanStatusDetailGem> { Kept("G-1", "OUT-1", "C1") };
        var (updated, created) = Run(kept, new() { new GemItem { OutboundRunning = "OUT-1", ItemNo = "OTHER", Code = "C1", Length = 40m, LengthUnit = "cm" } });
        Assert.Same(kept[0], Assert.Single(updated));
        Assert.Equal(0, created);
        Assert.Equal(40m, kept[0].GemLength);
    }

    [Fact]
    public void UnmatchedOutboundRunning_IsSkipped()
    {
        var kept = new List<TbtProductionPlanStatusDetailGem> { Kept("G-1", "OUT-1", "C1") };
        var (updated, created) = Run(kept, new() { new GemItem { OutboundRunning = "OUT-X", ItemNo = "G-1", Code = "C1", Length = 9m, Price = 99m } });
        Assert.Empty(updated);
        Assert.Equal(0, created);
        Assert.Equal(1m, kept[0].GemPrice);
        Assert.Null(kept[0].GemLength);
    }

    [Fact]
    public void Price_StillUpdated()
    {
        var kept = new List<TbtProductionPlanStatusDetailGem> { Kept("G-1", "OUT-1", "C1") };
        Run(kept, new() { new GemItem { OutboundRunning = "OUT-1", ItemNo = "G-1", Code = "C1", Price = 77m } });
        Assert.Equal(77m, kept[0].GemPrice);
    }

    [Fact]
    public void NoOutboundRunning_ReturnsNullSoCallerCreatesNew()
    {
        var kept = new List<TbtProductionPlanStatusDetailGem> { Kept("G-1", "OUT-1", "C1") };
        var (updated, created) = Run(kept, new() { new GemItem { Code = "C1", ItemNo = "G-1" } });
        Assert.Empty(updated);
        Assert.Equal(1, created);
    }
}
