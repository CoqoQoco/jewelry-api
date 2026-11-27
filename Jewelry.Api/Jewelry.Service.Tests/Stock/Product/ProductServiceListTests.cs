using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Stock.Product;
using Jewelry.Service.Tests.Stock.Rfid;
using Microsoft.EntityFrameworkCore;
using ListModel = jewelry.Model.Stock.Product.List;

namespace Jewelry.Service.Tests.Stock.Product;

public class ProductServiceListTests
{
    private static JewelryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JewelryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new JewelryContext(options);

        context.TbtSku.Add(new TbtSku { SkuCode = "SKU1", ProductNameTh = "แหวนทอง", ProductNameEn = "Gold Ring", ProductNumber = "R-100", CreateBy = "t" });
        context.TbtStockPiece.AddRange(
            Piece("DK20H015", "P1", "OLD-15", 3, "L1"),   // 3 units, 1 tag + 1 sold unit -> partial
            Piece("DK-20H-016", "P1", null, 1, "L2"),     // 1 unit tagged -> bound
            Piece("DK20H017", "P1", null, 2.7m, "L1"),    // lot without units -> total floor(2.7)=2, no tag -> unbound
            Piece("DK20H018", "P2", null, 1, "L1"));      // same stock number family, other product code, no tag
        context.TbtStockUnit.AddRange(
            Unit(1, "DK20H015", 1, "IN_STOCK"),
            Unit(2, "DK20H015", 2, "RESERVED"),
            Unit(3, "DK20H015", 3, "SOLD"),
            Unit(4, "DK-20H-016", 1, "IN_STOCK"),
            Unit(5, "DK20H018", 1, "IN_STOCK", "P2"));
        context.TbtStockPieceRfid.AddRange(
            Tag("E1", "DK20H015", 1, true),
            Tag("E3", "DK20H015", 3, true),
            Tag("E2", "DK-20H-016", 4, true),
            Tag("E4", "DK20H015", 2, false));
        context.SaveChanges();
        return context;
    }

    private static TbtStockPiece Piece(string stock, string code, string? origin, decimal qty, string location) => new()
    {
        StockNumber = stock, ProductCode = code, SkuCode = "SKU1", LocationCode = location, Status = "IN_STOCK",
        StockNumberOrigin = origin, ReceiptType = "production", Wo = "6906025", WoNumber = 21, Qty = qty, CreateBy = "t"
    };

    private static TbtStockUnit Unit(long id, string stock, int no, string status, string code = "P1") => new()
    {
        Id = id, StockNumber = stock, ProductCode = code, UnitNo = no, LocationCode = "L1", Status = status, CreateBy = "t"
    };

    private static TbtStockPieceRfid Tag(string epc, string stock, long unitId, bool active) => new()
    {
        Epc = epc, StockNumber = stock, ProductCode = "P1", UnitId = unitId, IsActive = active, CreateBy = "t"
    };

    private static ProductService CreateService(JewelryContext context) =>
        new(context, new StubHttpContextAccessor(), null!, null!, null!);

    private static List<string> Numbers(IQueryable<ListModel.Response> q) =>
        q.ToList().Select(x => x.StockNumber).OrderBy(x => x).ToList();

    [Fact]
    public void List_NoFilter_ReturnsAll()
    {
        using var context = CreateContext();
        Assert.Equal(4, CreateService(context).List(new ListModel.Search()).Count());
    }

    [Fact]
    public void List_StockNumberIgnoresDashes()
    {
        using var context = CreateContext();
        var result = Numbers(CreateService(context).List(new ListModel.Search { StockNumber = "dk20h016" }));
        Assert.Equal(new[] { "DK-20H-016" }, result);
    }

    [Fact]
    public void List_LocationCodes_FiltersByUnitLocation()
    {
        using var context = CreateContext();
        var result = Numbers(CreateService(context).List(new ListModel.Search { LocationCodes = new[] { "L1" } }));
        Assert.Equal(new[] { "DK-20H-016", "DK20H015", "DK20H018" }, result);
    }

    [Fact]
    public void List_StockNumberOrigin_Contains()
    {
        using var context = CreateContext();
        var result = Numbers(CreateService(context).List(new ListModel.Search { StockNumberOrigin = "old15" }));
        Assert.Equal(new[] { "DK20H015" }, result);
    }

    [Fact]
    public void List_Pieces_ExactPairNormalizesDashes()
    {
        using var context = CreateContext();
        var result = Numbers(CreateService(context).List(new ListModel.Search
        {
            Pieces = new() { new() { StockNumber = "DK20H016", ProductCode = "P1" }, new() { StockNumber = "DK20H018", ProductCode = "P1" } }
        }));
        Assert.Equal(new[] { "DK-20H-016" }, result);
    }

    [Fact]
    public void List_Pieces_RawStockNumberMatchesDirectly()
    {
        using var context = CreateContext();
        var result = Numbers(CreateService(context).List(new ListModel.Search
        {
            Pieces = new() { new() { StockNumber = "DK-20H-016", ProductCode = "P1" }, new() { StockNumber = "DK20H015", ProductCode = "P2" } }
        }));
        Assert.Equal(new[] { "DK-20H-016" }, result);
    }

    [Fact]
    public void List_Pieces_OverLimitThrows()
    {
        using var context = CreateContext();
        var pieces = Enumerable.Range(0, 501).Select(i => new ListModel.PieceKey { StockNumber = "S" + i, ProductCode = "P1" }).ToList();
        Assert.ThrowsAny<Exception>(() => CreateService(context).List(new ListModel.Search { Pieces = pieces }));
    }

    [Theory]
    [InlineData("bound", "DK-20H-016")]
    [InlineData("partial", "DK20H015")]
    [InlineData("unbound", "DK20H017,DK20H018")]
    public void List_TagStatus(string status, string expected)
    {
        using var context = CreateContext();
        var result = Numbers(CreateService(context).List(new ListModel.Search { TagStatus = status }));
        Assert.Equal(expected.Split(','), result);
    }

    [Fact]
    public async Task AttachRfidTagCount_FillsTotalAndBound_KeepsRfidTagCount()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var rows = service.List(new ListModel.Search()).ToList();

        await service.AttachRfidTagCount(rows);

        var partial = rows.Single(r => r.StockNumber == "DK20H015");
        Assert.Equal(2, partial.TagTotalUnits);
        Assert.Equal(1, partial.TagBoundUnits);
        Assert.Equal(2, partial.RfidTagCount);

        var noUnits = rows.Single(r => r.StockNumber == "DK20H017");
        Assert.Equal(2, noUnits.TagTotalUnits);
        Assert.Equal(0, noUnits.TagBoundUnits);

        var bound = rows.Single(r => r.StockNumber == "DK-20H-016");
        Assert.Equal(1, bound.TagTotalUnits);
        Assert.Equal(1, bound.TagBoundUnits);
    }

    [Fact]
    public async Task Get_ReturnsProductCode()
    {
        using var context = CreateContext();
        var result = await CreateService(context).Get(new jewelry.Model.Stock.Product.Get.Request { StockNumber = "DK20H018" });
        Assert.Equal("P2", result.ProductCode);
    }
}
