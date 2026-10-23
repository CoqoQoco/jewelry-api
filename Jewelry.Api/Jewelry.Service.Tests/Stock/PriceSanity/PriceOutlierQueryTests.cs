using Reason = jewelry.Model.Stock.Product.PriceCheck.Reason;
using jewelry.Model.Stock.Product.PriceOutlierList;
using Jewelry.Service.Stock.PriceSanity;
using Kendo.DynamicLinqCore;

namespace Jewelry.Service.Tests.Stock.PriceSanity;

public class PriceOutlierQueryTests
{
    private static OutlierItem Item(string stock, decimal price, double score, string code = "COST_RATIO", string? origin = null, string type = "T") => new()
    {
        StockNumber = stock,
        StockNumberOrigin = origin,
        SkuCode = "SKU-" + stock,
        ProductTypeName = type,
        Price = price,
        Score = score,
        Reasons = new() { new Reason { Code = code } }
    };

    private static List<OutlierItem> Data() => new()
    {
        Item("DK-18K-1", 100m, 1.0),
        Item("DK-18K-2", 300m, 3.0, "SAME_WO", "AH37503"),
        Item("DK-18K-3", 200m, 2.0, "COST_RATIO", null, "Ring"),
    };

    [Fact]
    public void DefaultSort_IsScoreDesc_AndTotalIsPreTake()
    {
        var r = PriceOutlierQuery.FilterSortPage(Data(), new Request { Take = 2, Skip = 0 });
        var rows = ((IEnumerable<OutlierItem>)r.Data).ToList();
        Assert.Equal(3, r.Total);
        Assert.Equal(new[] { "DK-18K-2", "DK-18K-3" }, rows.Select(x => x.StockNumber));
    }

    [Fact]
    public void TextSearch_IgnoresDashesAndCase_AndMatchesOrigin()
    {
        var byStock = PriceOutlierQuery.FilterSortPage(Data(), new Request { Take = 10, Search = new Search { Text = "dk18k2" } });
        var byOrigin = PriceOutlierQuery.FilterSortPage(Data(), new Request { Take = 10, Search = new Search { Text = "ah375" } });
        Assert.Equal(1, byStock.Total);
        Assert.Equal(1, byOrigin.Total);
    }

    [Fact]
    public void ReasonAndTypeFilters_Apply()
    {
        var r = PriceOutlierQuery.FilterSortPage(Data(), new Request { Take = 10, Search = new Search { ReasonCode = "same_wo" } });
        Assert.Equal(1, r.Total);
        var t = PriceOutlierQuery.FilterSortPage(Data(), new Request { Take = 10, Search = new Search { ProductTypeName = "Ring" } });
        Assert.Equal(1, t.Total);
    }

    [Fact]
    public void SortByPriceAsc_AndSkip()
    {
        var r = PriceOutlierQuery.FilterSortPage(Data(), new Request
        {
            Take = 10,
            Skip = 1,
            Sort = new[] { new Sort { Field = "price", Dir = "asc" } }
        });
        var rows = ((IEnumerable<OutlierItem>)r.Data).ToList();
        Assert.Equal(new[] { 200m, 300m }, rows.Select(x => x.Price));
    }

    [Fact]
    public void Score_IsMaxAbsLogRatio_AndSymmetric()
    {
        var reasons = new List<Reason>
        {
            new() { Code = "A", Value = 20m, Reference = 2m },
            new() { Code = "B", Value = 1m, Reference = 100m }
        };
        Assert.Equal(Math.Round(Math.Log(100), 3), PriceSanityEvaluator.Score(reasons));
    }

    [Fact]
    public void SortByWo_NullsLast_ThenWoNumber()
    {
        var a = Item("A", 1m, 1); a.Wo = "B"; a.WoNumber = 2;
        var b = Item("B", 1m, 1); b.Wo = "B"; b.WoNumber = 1;
        var c = Item("C", 1m, 1); c.Wo = null;
        var d = Item("D", 1m, 1); d.Wo = "A"; d.WoNumber = 5;
        foreach (var dir in new[] { "asc", "desc" })
        {
            var r = PriceOutlierQuery.FilterSortPage(new List<OutlierItem> { a, b, c, d }, new Request
            {
                Take = 10,
                Sort = new[] { new Sort { Field = "wo", Dir = dir } }
            });
            var ids = ((IEnumerable<OutlierItem>)r.Data).Select(x => x.StockNumber).ToArray();
            Assert.Equal(dir == "asc" ? new[] { "D", "B", "A", "C" } : new[] { "A", "B", "D", "C" }, ids);
        }
    }

    private static OutlierItem Acked(string stock, bool isAcked, bool changed)
    {
        var i = Item(stock, 100m, 1.0);
        i.AckPrice = 100m;
        i.IsAcked = isAcked;
        i.PriceChangedSinceAck = changed;
        return i;
    }

    private static List<OutlierItem> ReviewData() => new()
    {
        Item("P", 100m, 1.0),
        Acked("A", true, false),
        Acked("C", false, true),
    };

    private static string[] Ids(DataSourceResult r) => ((IEnumerable<OutlierItem>)r.Data).Select(x => x.StockNumber).OrderBy(x => x).ToArray();

    [Theory]
    [InlineData(null, new[] { "C", "P" })]
    [InlineData("", new[] { "C", "P" })]
    [InlineData("PENDING", new[] { "C", "P" })]
    [InlineData("pending", new[] { "C", "P" })]
    [InlineData("ACKED", new[] { "A" })]
    [InlineData("ALL", new[] { "A", "C", "P" })]
    public void ReviewStatus_Filters(string? status, string[] expected)
    {
        var r = PriceOutlierQuery.FilterSortPage(ReviewData(), new Request { Take = 10, Search = new Search { ReviewStatus = status } });
        Assert.Equal(expected, Ids(r));
    }

    [Fact]
    public void NoSearch_DefaultsToPending()
    {
        var r = PriceOutlierQuery.FilterSortPage(ReviewData(), new Request { Take = 10 });
        Assert.Equal(new[] { "C", "P" }, Ids(r));
    }
}
