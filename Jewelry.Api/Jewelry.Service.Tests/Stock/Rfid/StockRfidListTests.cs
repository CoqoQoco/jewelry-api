using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Stock.Rfid;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Tests.Stock.Rfid;

public class StockRfidListTests
{
    private static JewelryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JewelryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new JewelryContext(options);

        context.TbtSku.AddRange(
            new TbtSku { SkuCode = "SKU1", ProductNameTh = "แหวนทอง", ProductNameEn = "Gold Ring", ProductNumber = "R-100", CreateBy = "t" },
            new TbtSku { SkuCode = "SKU2", ProductNameTh = "ต่างหู", ProductNameEn = "Earring", ProductNumber = "E-200", CreateBy = "t" });

        var now = DateTime.UtcNow;
        context.TbtStockPiece.AddRange(
            Piece("DK-001", "P1", "SKU1", "RC1", now.AddDays(-2)),
            Piece("DK-002", "P1", "SKU1", "RC1", now.AddDays(-2)),
            Piece("DK-003", "P1", "SKU2", "RC1", now.AddDays(-2)),
            Piece("DK-004", "P1", "SKU2", "RC2", now.AddDays(-1)),
            Piece("DK-005", "P1", "SKU1", "RC3", now.AddDays(-400)),
            Piece("DK-006", "P1", "SKU1", null, null),
            Piece("DK-007", "P1", "SKU1", "", null),
            Piece("DK-008", "P1", "SKU1", "RC2", now.AddDays(-1), status: "SOLD", location: "L2"));
        context.SaveChanges();

        return context;
    }

    private static TbtStockPiece Piece(string stock, string code, string sku, string? receipt, DateTime? receiptDate,
        string status = "IN_STOCK", string location = "L1")
        => new()
        {
            StockNumber = stock,
            ProductCode = code,
            SkuCode = sku,
            LocationCode = location,
            Status = status,
            ReceiptNumber = receipt,
            ReceiptDate = receiptDate,
            CreateBy = "t"
        };

    private static StockRfidService CreateService(JewelryContext context)
        => new(context, new StubHttpContextAccessor());

    private static async Task<long> BindAsync(StockRfidService service, string epc, string stock)
        => (await service.Bind(new jewelry.Model.Stock.Rfid.Bind.Request { Epc = epc, StockNumber = stock, ProductCode = "P1" })).Id;

    [Fact]
    public async Task ListReceipts_CountsTotalAndBound_OrderedByDateDesc_IgnoresEmptyAndOld()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await BindAsync(service, "E1", "DK-001");
        await BindAsync(service, "E2", "DK-004");

        var result = await service.ListReceipts(new jewelry.Model.Stock.Rfid.ListReceipts.Request { Skip = 0, Take = 20 });

        Assert.Equal(2, result.Total);
        Assert.Equal(new[] { "RC2", "RC1" }, result.Items.Select(i => i.ReceiptNumber));
        var rc1 = result.Items.Single(i => i.ReceiptNumber == "RC1");
        Assert.Equal(3, rc1.TotalPieces);
        Assert.Equal(1, rc1.BoundPieces);
        Assert.Equal(2, rc1.RemainingPieces);
        var rc2 = result.Items.Single(i => i.ReceiptNumber == "RC2");
        Assert.Equal(2, rc2.TotalPieces);
        Assert.Equal(1, rc2.BoundPieces);
    }

    [Fact]
    public async Task ListReceipts_InactiveTagNotCounted()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await BindAsync(service, "E1", "DK-001");
        await service.Unbind(new jewelry.Model.Stock.Rfid.Unbind.Request { Epc = "E1" });

        var result = await service.ListReceipts(new jewelry.Model.Stock.Rfid.ListReceipts.Request { Take = 20 });

        Assert.Equal(0, result.Items.Single(i => i.ReceiptNumber == "RC1").BoundPieces);
    }

    [Fact]
    public async Task ListReceipts_OnlyIncomplete_ExcludesFullyBound()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await BindAsync(service, "E1", "DK-001");
        await BindAsync(service, "E2", "DK-002");
        await BindAsync(service, "E3", "DK-003");

        var all = await service.ListReceipts(new jewelry.Model.Stock.Rfid.ListReceipts.Request { Take = 20 });
        var incomplete = await service.ListReceipts(new jewelry.Model.Stock.Rfid.ListReceipts.Request { OnlyIncomplete = true, Take = 20 });

        Assert.Equal(2, all.Total);
        Assert.Equal(1, incomplete.Total);
        Assert.Equal("RC2", incomplete.Items.Single().ReceiptNumber);
    }

    [Fact]
    public async Task ListReceipts_SearchIsCaseInsensitive_AndIncludesOldReceipts()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ListReceipts(new jewelry.Model.Stock.Rfid.ListReceipts.Request { Search = "rc3", Take = 20 });

        Assert.Equal(1, result.Total);
        Assert.Equal("RC3", result.Items.Single().ReceiptNumber);
    }

    [Fact]
    public async Task ListReceipts_PagingAndTakeClamp()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var page = await service.ListReceipts(new jewelry.Model.Stock.Rfid.ListReceipts.Request { Skip = 1, Take = 1 });
        Assert.Equal(2, page.Total);
        Assert.Equal("RC1", page.Items.Single().ReceiptNumber);

        var defaulted = await service.ListReceipts(new jewelry.Model.Stock.Rfid.ListReceipts.Request { Take = 0 });
        Assert.Equal(2, defaulted.Items.Count);
    }

    [Fact]
    public async Task ListByReceipt_ReturnsShapeWithBoundAndUnbound()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await BindAsync(service, "E1", "DK-002");

        var result = await service.ListByReceipt(new jewelry.Model.Stock.Rfid.ListByReceipt.Request { ReceiptNumber = "RC1" });

        Assert.Equal("RC1", result.ReceiptNumber);
        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Bound);
        Assert.Equal(new[] { "DK-001", "DK-002", "DK-003" }, result.Items.Select(i => i.StockNumber));

        var bound = result.Items.Single(i => i.StockNumber == "DK-002");
        Assert.Equal("E1", bound.Epc);
        Assert.NotNull(bound.BoundDate);
        Assert.Equal("system", bound.BoundBy);
        Assert.Equal("แหวนทอง", bound.ProductName);
        Assert.Equal("R-100", bound.ProductNumber);
        Assert.Equal("L1", bound.LocationCode);

        var free = result.Items.First(i => i.StockNumber == "DK-001");
        Assert.Null(free.Epc);
        Assert.Null(free.Tid);
        Assert.Null(free.BoundDate);
        Assert.Null(free.BoundBy);
    }

    [Fact]
    public async Task ListByReceipt_BadReceipt_ReturnsEmpty()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ListByReceipt(new jewelry.Model.Stock.Rfid.ListByReceipt.Request { ReceiptNumber = "NOPE" });
        var blank = await service.ListByReceipt(new jewelry.Model.Stock.Rfid.ListByReceipt.Request { ReceiptNumber = " " });

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
        Assert.Empty(blank.Items);
    }

    [Fact]
    public async Task ListUnbound_ExcludesTaggedAndNotInStock()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await BindAsync(service, "E1", "DK-001");

        var result = await service.ListUnbound(new jewelry.Model.Stock.Rfid.ListUnbound.Request { Take = 100 });

        var numbers = result.Items.Select(i => i.StockNumber).ToList();
        Assert.DoesNotContain("DK-001", numbers);
        Assert.DoesNotContain("DK-008", numbers);
        Assert.Contains("DK-002", numbers);
        Assert.Equal(6, result.Total);
        Assert.Equal(numbers.OrderBy(n => n, StringComparer.Ordinal), numbers);
    }

    [Fact]
    public async Task ListUnbound_AfterUnbind_PieceReturnsToList()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await BindAsync(service, "E1", "DK-001");
        await service.Unbind(new jewelry.Model.Stock.Rfid.Unbind.Request { Epc = "E1" });

        var result = await service.ListUnbound(new jewelry.Model.Stock.Rfid.ListUnbound.Request { Search = "DK001" });

        Assert.Equal("DK-001", result.Items.Single().StockNumber);
    }

    [Theory]
    [InlineData("dk-002")]
    [InlineData("DK002")]
    [InlineData("dk002")]
    public async Task ListUnbound_StockNumberSearch_IgnoresDashesAndCase(string search)
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ListUnbound(new jewelry.Model.Stock.Rfid.ListUnbound.Request { Search = search });

        Assert.Equal("DK-002", result.Items.Single().StockNumber);
        Assert.Equal("RC1", result.Items.Single().ReceiptNumber);
    }

    [Fact]
    public async Task ListUnbound_SearchByProductNameAndNumber_AndLocationFilter()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var byName = await service.ListUnbound(new jewelry.Model.Stock.Rfid.ListUnbound.Request { Search = "ต่างหู" });
        Assert.Equal(new[] { "DK-003", "DK-004" }, byName.Items.Select(i => i.StockNumber));

        var byNumber = await service.ListUnbound(new jewelry.Model.Stock.Rfid.ListUnbound.Request { Search = "e-200" });
        Assert.Equal(2, byNumber.Total);

        var wrongLocation = await service.ListUnbound(new jewelry.Model.Stock.Rfid.ListUnbound.Request { LocationCode = "L2" });
        Assert.Equal(0, wrongLocation.Total);
    }

    [Fact]
    public async Task ListUnbound_Paging()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var page = await service.ListUnbound(new jewelry.Model.Stock.Rfid.ListUnbound.Request { Skip = 2, Take = 2 });

        Assert.Equal(7, page.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal("DK-003", page.Items[0].StockNumber);
    }

    [Fact]
    public async Task ListRecent_NoSinceId_ReturnsLatestTakeNewestFirst()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var ids = new List<long>();
        foreach (var (epc, stock) in new[] { ("E1", "DK-001"), ("E2", "DK-002"), ("E3", "DK-003") })
        {
            ids.Add(await BindAsync(service, epc, stock));
        }

        var result = await service.ListRecent(new jewelry.Model.Stock.Rfid.ListRecent.Request { Take = 2 });

        Assert.Equal(new[] { ids[2], ids[1] }, result.Items.Select(i => i.Id));
        Assert.Equal(ids[2], result.LastId);
        Assert.Equal("RC1", result.Items[0].ReceiptNumber);
        Assert.Equal("ต่างหู", result.Items[0].ProductName);
        Assert.True(result.Items[0].IsActive);
        Assert.Equal("system", result.Items[0].BoundBy);
    }

    [Fact]
    public async Task ListRecent_SinceId_ReturnsNewerAscending_AndTakeClamped()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var ids = new List<long>();
        foreach (var (epc, stock) in new[] { ("E1", "DK-001"), ("E2", "DK-002"), ("E3", "DK-003") })
        {
            ids.Add(await BindAsync(service, epc, stock));
        }

        var result = await service.ListRecent(new jewelry.Model.Stock.Rfid.ListRecent.Request { SinceId = ids[0], Take = 1000 });
        Assert.Equal(new[] { ids[1], ids[2] }, result.Items.Select(i => i.Id));
        Assert.Equal(ids[2], result.LastId);

        var limited = await service.ListRecent(new jewelry.Model.Stock.Rfid.ListRecent.Request { SinceId = ids[0], Take = 1 });
        Assert.Equal(new[] { ids[1] }, limited.Items.Select(i => i.Id));
        Assert.Equal(ids[1], limited.LastId);
    }

    [Fact]
    public async Task ListRecent_NothingNew_KeepsSinceId_AndEmptyDbGivesZero()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var empty = await service.ListRecent(new jewelry.Model.Stock.Rfid.ListRecent.Request());
        Assert.Empty(empty.Items);
        Assert.Equal(0, empty.LastId);

        var id = await BindAsync(service, "E1", "DK-001");
        var none = await service.ListRecent(new jewelry.Model.Stock.Rfid.ListRecent.Request { SinceId = id });
        Assert.Empty(none.Items);
        Assert.Equal(id, none.LastId);
    }

    [Fact]
    public async Task ListRecent_CancelledBind_ShowsInactiveWithUpdatedDate()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var id = await BindAsync(service, "E1", "DK-001");
        await service.Unbind(new jewelry.Model.Stock.Rfid.Unbind.Request { Epc = "E1" });

        var result = await service.ListRecent(new jewelry.Model.Stock.Rfid.ListRecent.Request());

        var item = Assert.Single(result.Items);
        Assert.Equal(id, item.Id);
        Assert.False(item.IsActive);
        Assert.NotNull(item.UpdatedDate);
    }
}
