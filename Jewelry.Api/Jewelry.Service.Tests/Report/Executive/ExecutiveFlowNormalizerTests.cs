using Jewelry.Service.Report.Executive;

namespace Jewelry.Service.Tests.Report.Executive;

public class ExecutiveFlowNormalizerTests
{
    [Theory]
    [InlineData("WG", "White")]
    [InlineData("wg", "White")]
    [InlineData(" White Gold ", "White")]
    [InlineData("YG", "Yellow")]
    [InlineData("Yellow Gold", "Yellow")]
    [InlineData("Pink Gold", "Pink")]
    [InlineData("Silver", "Silver")]
    [InlineData("", "Unknown")]
    [InlineData("   ", "Unknown")]
    [InlineData(null, "Unknown")]
    [InlineData("Platinum", "Unknown")]
    public void NormalizeStockGold_MapsKnownAndFallsBackToUnknown(string? input, string expected)
    {
        Assert.Equal(expected, ExecutiveFlowNormalizer.NormalizeStockGold(input));
    }

    [Theory]
    [InlineData("YG", "YG")]
    [InlineData("Yellow Gold", "YG")]
    [InlineData("WG", "WG")]
    [InlineData("white gold", "WG")]
    [InlineData("PG", "PG")]
    [InlineData("Pink Gold", "PG")]
    [InlineData("SV", "SV")]
    [InlineData("Silver", "SV")]
    [InlineData("SIL925", "SV")]
    [InlineData(null, "Unknown")]
    [InlineData("", "Unknown")]
    [InlineData("XX", "Unknown")]
    public void NormalizePlanGold_MapsToShortCodes(string? input, string expected)
    {
        Assert.Equal(expected, ExecutiveFlowNormalizer.NormalizePlanGold(input));
    }

    [Theory]
    [InlineData("7", "18K")]
    [InlineData("18K", "18K")]
    [InlineData("18k", "18K")]
    [InlineData("9K", "9K")]
    [InlineData("14K", "14K")]
    [InlineData("10K", "10K")]
    [InlineData("Silver", "Silver")]
    [InlineData("24K", "Unknown")]
    [InlineData("8", "Unknown")]
    [InlineData("", "Unknown")]
    [InlineData(null, "Unknown")]
    public void NormalizeSize_MapsLegacySevenTo18K(string? input, string expected)
    {
        Assert.Equal(expected, ExecutiveFlowNormalizer.NormalizeSize(input));
    }

    [Theory]
    [InlineData(null, "Unknown")]
    [InlineData("  ", "Unknown")]
    [InlineData(" Ring ", "Ring")]
    public void LabelOrUnknown_TrimsAndFallsBack(string? input, string expected)
    {
        Assert.Equal(expected, ExecutiveFlowNormalizer.LabelOrUnknown(input));
    }

    [Fact]
    public void Percent_WholeZero_ReturnsNull()
    {
        Assert.Null(ExecutiveFlowNormalizer.Percent(5, 0));
    }

    [Fact]
    public void Percent_RoundsHalfAwayFromZero()
    {
        Assert.Equal(33.3m, ExecutiveFlowNormalizer.Percent(1, 3));
        Assert.Equal(0.1m, ExecutiveFlowNormalizer.Percent(1, 1000));        Assert.Equal(66.67m, ExecutiveFlowNormalizer.Percent(2, 3, 2));
    }

    [Fact]
    public void Round2_RoundsMidpointAwayFromZero()
    {
        Assert.Equal(1.01m, ExecutiveFlowNormalizer.Round2(1.005m));
        Assert.Equal(-1.01m, ExecutiveFlowNormalizer.Round2(-1.005m));
    }

    [Fact]
    public void Median_Empty_ReturnsNull()
    {
        Assert.Null(ExecutiveFlowNormalizer.Median(new List<double>()));
    }

    [Fact]
    public void Median_OddCount_ReturnsMiddleOfSorted()
    {
        Assert.Equal(3.0, ExecutiveFlowNormalizer.Median(new List<double> { 9, 1, 3 }));
    }

    [Fact]
    public void Median_EvenCount_ReturnsAverageOfMiddlePairRoundedToOneDecimal()
    {
        Assert.Equal(2.5, ExecutiveFlowNormalizer.Median(new List<double> { 4, 1, 2, 3 }));
        Assert.Equal(1.6, ExecutiveFlowNormalizer.Median(new List<double> { 1.55, 1.7 })); // 1.625 -> 1.6
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(14, 0)]
    [InlineData(14.99, 0)]
    [InlineData(15, 1)]
    [InlineData(60, 1)]
    [InlineData(60.99, 1)]
    [InlineData(61, 2)]
    [InlineData(179, 2)]
    [InlineData(179.99, 2)]
    [InlineData(180, 3)]
    [InlineData(1000, 3)]
    public void AgeBucket_UsesWholeDayBoundaries(double days, int expected)
    {
        Assert.Equal(expected, ExecutiveFlowNormalizer.AgeBucket(days));
    }

    [Fact]
    public void StockAgeLabel_NullDate_ReturnsUnknown()
    {
        Assert.Equal("Unknown", ExecutiveFlowNormalizer.StockAgeLabel(null, new DateTime(2026, 1, 1)));
    }

    [Theory]
    [InlineData(90, "<=90d")]
    [InlineData(91, "91-365d")]
    [InlineData(365, "91-365d")]
    [InlineData(366, ">365d")]
    public void StockAgeLabel_BoundariesAreInclusiveOnUpperEdge(int ageDays, string expected)
    {
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, ExecutiveFlowNormalizer.StockAgeLabel(now.AddDays(-ageDays), now));
    }

    [Theory]
    [InlineData("2026-09-30T16:59:59Z", "2026-09")]
    [InlineData("2026-09-30T17:00:00Z", "2026-10")]
    [InlineData("2026-12-31T17:00:00Z", "2027-01")]
    [InlineData("2026-03-01T00:00:00Z", "2026-03")]
    [InlineData("2026-02-28T17:00:00Z", "2026-03")]
    public void ThaiMonthKey_ShiftsToUtcPlus7(string utcIso, string expected)
    {
        var utc = DateTime.Parse(utcIso, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        Assert.Equal(expected, ExecutiveFlowNormalizer.ThaiMonthKey(utc));
    }

    [Fact]
    public void TopWithOther_FewerOrEqualToTop_ReturnsOrderedWithoutOther()
    {
        var items = new[] { ("B", 2), ("A", 5) };

        var result = ExecutiveFlowNormalizer.TopWithOther(items, 2, n => $"Other {n}");

        Assert.Equal(new List<(string, int)> { ("A", 5), ("B", 2) }, result);
    }

    [Fact]
    public void TopWithOther_MoreThanTop_AppendsSummedOtherRowWithGroupCount()
    {
        var items = new[] { ("A", 10), ("B", 7), ("C", 3), ("D", 2), ("E", 1) };

        var result = ExecutiveFlowNormalizer.TopWithOther(items, 2, n => $"Other ({n})");

        Assert.Equal(3, result.Count);
        Assert.Equal(("A", 10), result[0]);
        Assert.Equal(("B", 7), result[1]);
        Assert.Equal(("Other (3)", 6), result[2]);
    }

    [Fact]
    public void TopWithOther_TiesBreakByLabelOrdinal()
    {
        var items = new[] { ("b", 4), ("B", 4), ("a", 4) };

        var result = ExecutiveFlowNormalizer.TopWithOther(items, 3, n => "x");

        Assert.Equal(new[] { "B", "a", "b" }, result.Select(r => r.Label));
    }

    [Fact]
    public void Reconciles_AllLensesSumToTotal_ReturnsTrue()
    {
        Assert.True(ExecutiveFlowNormalizer.Reconciles(10, new[] { 4, 6 }, new[] { 10 }));
    }

    [Fact]
    public void Reconciles_AnyLensMismatch_ReturnsFalse()
    {
        Assert.False(ExecutiveFlowNormalizer.Reconciles(10, new[] { 4, 6 }, new[] { 9 }));
    }

    [Fact]
    public void Reconciles_NoLenses_ReturnsTrue()
    {
        Assert.True(ExecutiveFlowNormalizer.Reconciles(10));
    }
}
