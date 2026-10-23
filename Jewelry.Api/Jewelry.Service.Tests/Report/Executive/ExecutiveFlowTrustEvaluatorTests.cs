using Jewelry.Service.Report.Executive;
using static Jewelry.Service.Report.Executive.ExecutiveFlowTrustEvaluator;

namespace Jewelry.Service.Tests.Report.Executive;

public class ExecutiveFlowTrustEvaluatorTests
{
    private static List<string> Keys(Input i) => Evaluate(i).Select(x => x.Key).ToList();

    [Fact]
    public void Evaluate_EmptyInput_ReturnsNoRules()
    {
        Assert.Empty(Evaluate(new Input()));
    }

    [Fact]
    public void StockCostMissing_ExactlyFiftyPercent_DoesNotFire()
    {
        Assert.DoesNotContain("stock-cost-missing", Keys(new Input { InStockPieces = 100, InStockCostMissing = 50 }));
    }

    [Fact]
    public void StockCostMissing_JustOverFiftyPercent_FiresAsFail()
    {
        var items = Evaluate(new Input { InStockPieces = 1000, InStockCostMissing = 501 });

        var item = Assert.Single(items, x => x.Key == "stock-cost-missing");
        Assert.Equal("fail", item.Level);
        Assert.Contains("501 จาก 1,000", item.Evidence);
        Assert.Contains("50.1%", item.Evidence);
    }

    [Fact]
    public void StockCostMissing_NoStock_DoesNotFire()
    {
        Assert.DoesNotContain("stock-cost-missing", Keys(new Input { InStockPieces = 0, InStockCostMissing = 5 }));
    }

    [Fact]
    public void NegativeGold_AnyRow_FiresAndEvidenceLimitedToTwelve()
    {
        var rows = Enumerable.Range(1, 15).Select(n => new NegativeGoldRow { Code = "G" + n, SizeCode = "18K", Weight = -1.5m }).ToList();

        var item = Assert.Single(Evaluate(new Input { NegativeGoldRows = rows }));

        Assert.Equal("gold-stock-negative", item.Key);
        Assert.Equal("fail", item.Level);
        Assert.StartsWith("15 แถว:", item.Evidence);
        Assert.Contains("G12/18K -1.50 g", item.Evidence);
        Assert.DoesNotContain("G13/", item.Evidence);
    }

    [Fact]
    public void UsdRateOne_AnyInvoice_FiresAndEvidenceLimitedToFive()
    {
        var invoices = Enumerable.Range(1, 7).Select(n => new UsdRateOneInvoice { CustomerName = "C" + n, Date = "2026-01-0" + n, Items = 2 }).ToList();

        var item = Assert.Single(Evaluate(new Input { UsdRateOneInvoices = invoices }));

        Assert.Equal("usd-invoice-rate-1", item.Key);
        Assert.StartsWith("7 บิล:", item.Evidence);
        Assert.Contains("C5", item.Evidence);
        Assert.DoesNotContain("C6", item.Evidence);
    }

    [Theory]
    [InlineData(24, false)]
    [InlineData(25, true)]
    [InlineData(26, true)]
    public void OpenIdle_FiresAtOrAboveTwentyFivePercent(int idle, bool fires)
    {
        Assert.Equal(fires, Keys(new Input { OpenPcs = 100, IdlePcs = idle }).Contains("open-idle-180d"));
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    public void OpenTypeMissing_FiresAtOrAboveTenPercent(int missing, bool fires)
    {
        Assert.Equal(fires, Keys(new Input { OpenPcs = 100, TypeMissingPcs = missing }).Contains("open-type-missing"));
    }

    [Theory]
    [InlineData(90, false)]
    [InlineData(91, true)]
    public void OpenOverdue_FiresOnlyStrictlyAboveNinetyPercent(int overdue, bool fires)
    {
        Assert.Equal(fires, Keys(new Input { OpenPcs = 100, OverduePcs = overdue }).Contains("open-overdue"));
    }

    [Theory]
    [InlineData(49, false)]
    [InlineData(50, true)]
    public void MoldStale_FiresAtOrAboveFiftyPercent(int stale, bool fires)
    {
        Assert.Equal(fires, Keys(new Input { MoldInProgressPlans = 100, MoldInProgressStalePlans = stale }).Contains("mold-in-progress-stale"));
    }

    [Theory]
    [InlineData(24, false)]
    [InlineData(25, true)]
    public void StockOld_FiresAtOrAboveTwentyFivePercentAsInfo(int old, bool fires)
    {
        var hit = Evaluate(new Input { InStockPieces = 100, StockOldPieces = old }).SingleOrDefault(x => x.Key == "stock-old-365d");
        Assert.Equal(fires, hit != null);
        if (hit != null) Assert.Equal("info", hit.Level);
    }

    [Fact]
    public void ShareRules_ZeroDenominator_NeverFire()
    {
        var keys = Keys(new Input { OpenPcs = 0, IdlePcs = 5, TypeMissingPcs = 5, OverduePcs = 5, MoldInProgressPlans = 0, MoldInProgressStalePlans = 3, InStockPieces = 0, StockOldPieces = 4 });
        Assert.Empty(keys);
    }

    [Theory]
    [InlineData("open-test-worker", "TestWorkerPlans")]
    [InlineData("done-not-received", "NotReceivedPcs")]
    [InlineData("invoice-no-total", "InvoicesNoTotal")]
    [InlineData("invoice-no-sale-person", "InvoicesNoSalePerson")]
    [InlineData("invoice-no-channel", "InvoicesNoChannel")]
    public void CountRules_FireAtOneAndNotAtZero(string key, string property)
    {
        var one = new Input();
        typeof(Input).GetProperty(property)!.SetValue(one, 1);

        Assert.DoesNotContain(key, Keys(new Input()));
        var item = Assert.Single(Evaluate(one), x => x.Key == key);
        Assert.Equal("warn", item.Level);
    }

    [Fact]
    public void OldestYear_WhenSet_AddsInfoItemWithPlainYear()
    {
        var item = Assert.Single(Evaluate(new Input { OldestProductionYear = 2019 }));

        Assert.Equal("stock-oldest-year", item.Key);
        Assert.Equal("info", item.Level);
        Assert.Equal("2019", item.Evidence);
    }

    [Fact]
    public void Evaluate_OrdersFailThenWarnThenInfo()
    {
        var input = new Input
        {
            InStockPieces = 100, InStockCostMissing = 80, StockOldPieces = 30,
            TestWorkerPlans = 2, OldestProductionYear = 2020,
        };

        var levels = Evaluate(input).Select(x => x.Level).ToList();

        Assert.Equal(new[] { "fail", "warn", "info", "info" }, levels);
    }
}
