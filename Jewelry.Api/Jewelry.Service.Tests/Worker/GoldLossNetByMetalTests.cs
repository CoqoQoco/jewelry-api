using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Worker;

namespace Jewelry.Service.Tests.Worker;

public class GoldLossNetByMetalTests
{
    private static TbtWorkerGoldLossSlipItem Item(string? gold, string? size, decimal? w) =>
        new() { Gold = gold, GoldSize = size, WeightLossActual = w };

    private static TbtWorkerGoldLossSlipReturn Ret(string? gold, string size, decimal w, bool count = true) =>
        new() { Gold = gold, GoldSize = size, Weight = w, CountInCalc = count };

    private static (decimal? Gold, decimal? Silver) Compute(List<TbtWorkerGoldLossSlipItem> items, List<TbtWorkerGoldLossSlipReturn> returns) =>
        WorkerGoldLossSlipService.ComputeNetWeightByMetal(items.Select(i => (i.Gold, i.GoldSize, i.WeightLossActual)), returns);

    [Fact]
    public void MixedGoldAndSilver_SplitsAndMatchesTypeSummaries()
    {
        var items = new List<TbtWorkerGoldLossSlipItem>
        {
            Item("YG", "18K", 0.05m), Item("YG", "18K", 0.10m),
            Item("YG", "9K", 0.05m), Item("YG", "9K", 0.08m), Item("YG", "9K", 0.03m),
            Item("SV", "SV", -0.71m), Item("SV", "SV", -0.50m), Item("SV", "SV", -0.88m), Item("SV", "SV", 0.35m),
        };
        var returns = new List<TbtWorkerGoldLossSlipReturn> { Ret("YG", "18K", 0.44m) };

        var (gold, silver) = Compute(items, returns);

        Assert.Equal(0.75m, gold);
        Assert.Equal(-1.74m, silver);

        var summaries = WorkerGoldLossSlipService.BuildTypeSummaries(items, returns);
        Assert.Equal(silver, summaries.Where(s => s.GoldSize == "SILVER").Sum(s => s.NetWeight));
        Assert.Equal(gold, summaries.Where(s => s.GoldSize != "SILVER").Sum(s => s.NetWeight));
        Assert.Equal(gold + silver, items.Sum(i => i.WeightLossActual ?? 0) + returns.Sum(r => r.Weight));
    }

    [Fact]
    public void GoldOnly_SilverIsNull()
    {
        var (gold, silver) = Compute(new() { Item("YG", "18K", 0.2m) }, new());
        Assert.Equal(0.2m, gold);
        Assert.Null(silver);
    }

    [Fact]
    public void ReturnWithNullGoldAndSilverSize_CountsAsSilver()
    {
        var (gold, silver) = Compute(new() { Item("YG", "18K", 0.1m) }, new() { Ret(null, "Silver", 0.5m) });
        Assert.Equal(0.1m, gold);
        Assert.Equal(0.5m, silver);
    }

    [Fact]
    public void NullWeightItem_CountsAsRowWithZero_AndUncountedReturnIgnored()
    {
        var (gold, silver) = Compute(new() { Item("SV", "SV", null) }, new() { Ret("YG", "18K", 9m, count: false) });
        Assert.Null(gold);
        Assert.Equal(0m, silver);
    }
}
