using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Stock.Rfid;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Tests.Stock.Rfid;

public class StockRfidListUnitsTests
{
    private static JewelryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JewelryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new JewelryContext(options);

        context.TbtSku.Add(new TbtSku { SkuCode = "SKU1", ProductNameTh = "แหวนทอง", ProductNameEn = "Gold Ring", ProductNumber = "R-100", CreateBy = "t" });
        context.TbtStockPiece.AddRange(
            Piece("DK20H015", "P1", "OLD-15", 3),
            Piece("DK-20H-016", "P1", null, 1));
        context.TbtStockUnit.AddRange(
            Unit("DK20H015", 1, "IN_STOCK", "L1"),
            Unit("DK20H015", 2, "RESERVED", "L2"),
            Unit("DK20H015", 3, "SOLD", "L1"),
            Unit("DK-20H-016", 1, "IN_STOCK", "L1"));
        context.SaveChanges();
        return context;
    }

    private static TbtStockPiece Piece(string stock, string code, string? origin, decimal qty) => new()
    {
        StockNumber = stock, ProductCode = code, SkuCode = "SKU1", LocationCode = "L1", Status = "IN_STOCK",
        StockNumberOrigin = origin, ReceiptType = stock == "DK20H015" ? "production" : "buy", Wo = "6906025", WoNumber = stock == "DK20H015" ? 21 : 22, Qty = qty, CreateBy = "t"
    };

    private static TbtStockUnit Unit(string stock, int no, string status, string location) => new()
    {
        StockNumber = stock, ProductCode = "P1", UnitNo = no, LocationCode = location, Status = status, CreateBy = "t"
    };

    private static StockRfidService CreateService(JewelryContext context) => new(context, new StubHttpContextAccessor());

    [Fact]
    public async Task ListUnits_ExcludesSold_AndDefaultsToUnbound()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ListUnits(new jewelry.Model.Stock.Rfid.ListUnits.Request { Take = 50 });

        Assert.Equal(3, result.Total);
        Assert.DoesNotContain(result.Items, i => i.UnitStatus == "SOLD");
        Assert.Contains(result.Items, i => i.UnitStatus == "RESERVED");
        Assert.All(result.Items, i => Assert.Equal(3m, result.Items.First(x => x.StockNumber == "DK20H015").LotQty));
    }

    [Fact]
    public async Task ListUnits_LocationFilterAppliesToUnit()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ListUnits(new jewelry.Model.Stock.Rfid.ListUnits.Request { LocationCodes = new[] { "L2" } });

        var item = Assert.Single(result.Items);
        Assert.Equal(2, item.UnitNo);
    }

    [Fact]
    public async Task ListUnits_StockNumberIgnoresDashes_AndTagStatusFilters()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(new jewelry.Model.Stock.Rfid.Bind.Request { Epc = "E1", StockNumber = "DK-20H-016", ProductCode = "P1" });

        var unbound = await service.ListUnits(new jewelry.Model.Stock.Rfid.ListUnits.Request { StockNumber = "dk20h016" });
        var bound = await service.ListUnits(new jewelry.Model.Stock.Rfid.ListUnits.Request { StockNumber = "DK-20H-016", TagStatus = "bound" });

        Assert.Empty(unbound.Items);
        var item = Assert.Single(bound.Items);
        Assert.Equal("E1", item.Epc);
        Assert.NotNull(item.BoundDate);
    }

    [Fact]
    public async Task ResolveLabel_HandlesUrlPlainAndOriginNumbers()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var byUrl = await service.ResolveLabel(new jewelry.Model.Stock.Rfid.ResolveLabel.Request { Code = "https://app.duangkeaw.com/p/DK20H015-a1b2c3d4" });
        var byDashed = await service.ResolveLabel(new jewelry.Model.Stock.Rfid.ResolveLabel.Request { Code = "DK-20H-015" });
        var byOrigin = await service.ResolveLabel(new jewelry.Model.Stock.Rfid.ResolveLabel.Request { Code = "old15" });
        var byOldUrl = await service.ResolveLabel(new jewelry.Model.Stock.Rfid.ResolveLabel.Request { Code = "https://x/p/DK-20H-016-a1b2c3d4" });
        var none = await service.ResolveLabel(new jewelry.Model.Stock.Rfid.ResolveLabel.Request { Code = "NOPE" });

        Assert.Equal(2, byUrl.Items.Count);
        Assert.Equal(2, byDashed.Items.Count);
        Assert.Equal(2, byOrigin.Items.Count);
        Assert.Single(byOldUrl.Items);
        Assert.Empty(none.Items);
    }

    [Fact]
    public async Task ListUnits_ReceiptTypeUsesPieceReceiptType()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ListUnits(new jewelry.Model.Stock.Rfid.ListUnits.Request { ReceiptType = new[] { "production" } });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, i => Assert.Equal("DK20H015", i.StockNumber));
    }

    [Fact]
    public async Task ListUnits_UnitIdsFilter()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var all = await service.ListUnits(new jewelry.Model.Stock.Rfid.ListUnits.Request { TagStatus = "all" });
        var ids = all.Items.Take(2).Select(i => i.UnitId).ToArray();

        var result = await service.ListUnits(new jewelry.Model.Stock.Rfid.ListUnits.Request { TagStatus = "all", UnitIds = ids });

        Assert.Equal(ids.OrderBy(x => x), result.Items.Select(i => i.UnitId).OrderBy(x => x));
    }

    [Fact]
    public async Task ResolveLabel_ExactDashedMatch_AndNormalizedFallback()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var exact = await service.ResolveLabel(new jewelry.Model.Stock.Rfid.ResolveLabel.Request { Code = "DK-20H-016" });
        var fallback = await service.ResolveLabel(new jewelry.Model.Stock.Rfid.ResolveLabel.Request { Code = "dk20h016" });

        Assert.Single(exact.Items);
        Assert.Single(fallback.Items);
    }

    [Fact]
    public async Task ListUnits_WoExact_AndWoTextWithSlash()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var exact = await service.ListUnits(new jewelry.Model.Stock.Rfid.ListUnits.Request { Wo = "6906025", WoNumber = 22 });
        var slash = await service.ListUnits(new jewelry.Model.Stock.Rfid.ListUnits.Request { WoText = "6906025/21" });
        var contains = await service.ListUnits(new jewelry.Model.Stock.Rfid.ListUnits.Request { WoText = "6906025" });

        Assert.Single(exact.Items);
        Assert.Equal(2, slash.Items.Count);
        Assert.Equal(3, contains.Items.Count);
    }

    [Fact]
    public async Task ListPlans_CountsUnits_AndOnlyIncomplete()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(new jewelry.Model.Stock.Rfid.Bind.Request { Epc = "E9", StockNumber = "DK-20H-016", ProductCode = "P1" });

        var incomplete = await service.ListPlans(new jewelry.Model.Stock.Rfid.ListPlans.Request());
        var all = await service.ListPlans(new jewelry.Model.Stock.Rfid.ListPlans.Request { OnlyIncomplete = false });
        var one = await service.ListPlans(new jewelry.Model.Stock.Rfid.ListPlans.Request { Search = "6906025/21", OnlyIncomplete = false });

        var plan = Assert.Single(incomplete.Items);
        Assert.Equal("6906025/21", plan.WoText);
        Assert.Equal(2, plan.TotalUnits);
        Assert.Equal(2, plan.UnboundUnits);
        Assert.Equal("แหวนทอง", plan.ProductNameTh);
        Assert.Equal(2, all.Total);
        Assert.Equal(0, all.Items.Single(i => i.WoNumber == 22).UnboundUnits);
        Assert.Single(one.Items);
    }

    [Fact]
    public async Task ListPlans_RangeSelectsPlans_ButCountsWholePlan_NullDatesLast()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var now = DateTime.UtcNow;
        var u1 = context.TbtStockUnit.First(u => u.StockNumber == "DK20H015" && u.UnitNo == 1);
        u1.ReceiptDate = now;
        context.SaveChanges();

        var inRange = await service.ListPlans(new jewelry.Model.Stock.Rfid.ListPlans.Request
        {
            ReceiptDateFrom = new DateTimeOffset(now.AddDays(-1), TimeSpan.Zero),
            ReceiptDateTo = new DateTimeOffset(now.AddDays(1), TimeSpan.Zero)
        });
        var all = await service.ListPlans(new jewelry.Model.Stock.Rfid.ListPlans.Request());

        var plan = Assert.Single(inRange.Items);
        Assert.Equal(21, plan.WoNumber);
        Assert.Equal(2, plan.TotalUnits);
        Assert.Equal(2, plan.UnboundUnits);
        Assert.NotNull(plan.LastReceiptDate);
        Assert.Equal(new[] { 21, 22 }, all.Items.Select(i => i.WoNumber));
    }

    [Fact]
    public async Task ListUnits_ReturnsWo_AndPlanParseAcceptsWoPrefix()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ListUnits(new jewelry.Model.Stock.Rfid.ListUnits.Request { WoText = "WO 6906025 / 22" });

        var item = Assert.Single(result.Items);
        Assert.Equal("6906025", item.Wo);
        Assert.Equal(22, item.WoNumber);
        Assert.True(StockRfidService.TryParsePlan("wo6906025/21", out var wo, out var n));
        Assert.Equal(("6906025", 21), (wo, n));
    }
}
