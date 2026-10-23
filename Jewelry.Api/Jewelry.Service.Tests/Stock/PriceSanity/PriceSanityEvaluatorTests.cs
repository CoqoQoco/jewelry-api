using Jewelry.Service.Stock.PriceSanity;

namespace Jewelry.Service.Tests.Stock.PriceSanity;

public class PriceSanityEvaluatorTests
{
    private static PriceSanitySnapshot Snapshot() => new()
    {
        GlobalStats = new LnStats { N = 1000, Mu = Math.Log(2.0), Sd = 0.69 },
        WoPieces = { ["6905003"] = new() { new WoPiece { StockNumber = "DK-18K-20G-1393", Sku = "B", WoNumber = 11, Price = 59200m } } }
    };

    private static PriceSanityContext Ctx() => new()
    {
        SkuCode = "A",
        ProductionType = "18K",
        GoldWeight = 3.32m,
        GemPrice = 16000m,
        Wo = "6905003",
        WoNumber = 10
    };

    [Fact]
    public void FlagsTenTimesTypo_AndSuggestsDivideByTen()
    {
        var snap = Snapshot();
        var ctx = Ctx();

        var reasons = PriceSanityEvaluator.Evaluate(snap, ctx, 611000m);

        Assert.Contains(reasons, r => r.Code == "COST_RATIO");
        Assert.Contains(reasons, r => r.Code == "SAME_WO");
        Assert.Equal(61100m, PriceSanityEvaluator.Suggest(snap, ctx, 611000m));
    }

    [Fact]
    public void NormalPrice_HasNoReasons()
    {
        Assert.Empty(PriceSanityEvaluator.Evaluate(Snapshot(), Ctx(), 61100m));
    }

    [Theory]
    [InlineData("18K", 0.75)]
    [InlineData("14K", 0.585)]
    [InlineData("9K", 0.375)]
    [InlineData("SILVER", 0.75)]
    public void Purity_ParsesKarat(string type, double expected) =>
        Assert.Equal(expected, PriceSanityEvaluator.Purity(type));
}
