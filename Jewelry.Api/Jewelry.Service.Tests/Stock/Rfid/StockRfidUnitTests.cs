using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Stock.Rfid;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Tests.Stock.Rfid;

public class StockRfidUnitTests
{
    private static JewelryContext CreateContext(int qty = 3, bool withUnits = true)
    {
        var options = new DbContextOptionsBuilder<JewelryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new JewelryContext(options);

        context.TbtSku.Add(new TbtSku { SkuCode = "SKU1", ProductNameTh = "แหวนเงิน", ProductNameEn = "Silver Ring", ProductNumber = "R-1", CreateBy = "t" });
        context.TbmStockLocation.Add(new TbmStockLocation { Code = "L1", NameTh = "หน้าร้าน", CreateBy = "t" });
        context.TbtStockPiece.Add(new TbtStockPiece
        {
            StockNumber = "S1", ProductCode = "P1", SkuCode = "SKU1", LocationCode = "L1",
            Status = "IN_STOCK", Qty = qty, ReceiptNumber = "RC1", ReceiptDate = DateTime.UtcNow, CreateBy = "t"
        });

        if (withUnits)
        {
            for (var n = 1; n <= qty; n++)
            {
                context.TbtStockUnit.Add(new TbtStockUnit
                {
                    StockNumber = "S1", ProductCode = "P1", UnitNo = n, LocationCode = "L1",
                    Status = "IN_STOCK", ReceiptNumber = "RC1", ReceiptDate = DateTime.UtcNow, CreateBy = "t"
                });
            }
        }

        context.SaveChanges();
        return context;
    }

    private static StockRfidService CreateService(JewelryContext context)
        => new(context, new StubHttpContextAccessor());

    private static jewelry.Model.Stock.Rfid.Bind.Request Bind(string epc, long? unitId = null)
        => new() { Epc = epc, StockNumber = "S1", ProductCode = "P1", UnitId = unitId };

    [Fact]
    public async Task Bind_WithoutUnitId_PicksLowestFreeUnitNo_UntilLotIsFull()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var first = await service.Bind(Bind("E1"));
        var second = await service.Bind(Bind("E2"));
        var third = await service.Bind(Bind("E3"));

        Assert.Equal(new int?[] { 1, 2, 3 }, new[] { first.UnitNo, second.UnitNo, third.UnitNo });
        Assert.Equal(3, first.LotQty);
        Assert.Equal("IN_STOCK", first.UnitStatus);
        Assert.Equal("L1", first.UnitLocationCode);
        Assert.Equal("หน้าร้าน", first.UnitLocationName);

        var ex = await Assert.ThrowsAsync<HandleException>(() => service.Bind(Bind("E4")));
        Assert.Contains("E1", ex.Message);
    }

    [Fact]
    public async Task Bind_WithUnitId_BindsThatUnit_AndRejectsTakenOrSold()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var unit2 = await context.TbtStockUnit.SingleAsync(u => u.UnitNo == 2);

        var result = await service.Bind(Bind("E2", unit2.Id));
        Assert.Equal(unit2.Id, result.UnitId);
        Assert.Equal(2, result.UnitNo);

        var taken = await Assert.ThrowsAsync<HandleException>(() => service.Bind(Bind("E9", unit2.Id)));
        Assert.Contains("E2", taken.Message);

        var unit3 = await context.TbtStockUnit.SingleAsync(u => u.UnitNo == 3);
        unit3.Status = "SOLD";
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<HandleException>(() => service.Bind(Bind("E8", unit3.Id)));
        await Assert.ThrowsAsync<HandleException>(() => service.Bind(Bind("E7", 99999)));

        var next = await service.Bind(Bind("E1"));
        Assert.Equal(1, next.UnitNo);
    }

    [Fact]
    public async Task Bind_SoldUnitsAreSkippedByAutoPick()
    {
        using var context = CreateContext(2);
        var service = CreateService(context);
        var unit1 = await context.TbtStockUnit.SingleAsync(u => u.UnitNo == 1);
        unit1.Status = "SOLD";
        await context.SaveChangesAsync();

        var result = await service.Bind(Bind("E1"));

        Assert.Equal(2, result.UnitNo);
        await Assert.ThrowsAsync<HandleException>(() => service.Bind(Bind("E2")));
    }

    [Fact]
    public async Task Bind_LotWithoutUnits_CreatesUnitsFromQty()
    {
        using var context = CreateContext(2, withUnits: false);
        var service = CreateService(context);

        var result = await service.Bind(Bind("E1"));

        Assert.Equal(1, result.UnitNo);
        Assert.Equal(2, await context.TbtStockUnit.CountAsync());
    }

    [Fact]
    public async Task Unbind_SetsManualReason_AndByStockWithManyTagsRequiresChoice()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(Bind("E1"));
        await service.Bind(Bind("E2"));

        await Assert.ThrowsAsync<HandleException>(() => service.Unbind(new jewelry.Model.Stock.Rfid.Unbind.Request { StockNumber = "S1", ProductCode = "P1" }));

        var byUnit = await service.Unbind(new jewelry.Model.Stock.Rfid.Unbind.Request
        {
            UnitId = (await context.TbtStockUnit.SingleAsync(u => u.UnitNo == 2)).Id
        });
        Assert.True(byUnit.Unbound);

        var byEpc = await service.Unbind(new jewelry.Model.Stock.Rfid.Unbind.Request { Epc = "E1" });
        Assert.True(byEpc.Unbound);

        Assert.All(await context.TbtStockPieceRfid.ToListAsync(), r =>
        {
            Assert.False(r.IsActive);
            Assert.Equal("MANUAL", r.UnbindReason);
        });
    }

    [Fact]
    public async Task GetByStock_ReturnsAllUnitsWithTags()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(Bind("E1"));

        var result = await service.GetByStock(new jewelry.Model.Stock.Rfid.GetByStock.Request { StockNumber = "S1", ProductCode = "P1" });

        Assert.True(result.Found);
        Assert.Equal("E1", result.Epc);
        Assert.Equal(3, result.LotQty);
        Assert.Equal(3, result.Units.Count);
        Assert.Equal(new[] { 1, 2, 3 }, result.Units.Select(u => u.UnitNo));
        Assert.Equal("E1", result.Units[0].Epc);
        Assert.Null(result.Units[1].Epc);
        Assert.Equal("หน้าร้าน", result.Units[0].LocationName);
    }

    [Fact]
    public async Task GetByStock_NoTags_FoundFalseButUnitsListed()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetByStock(new jewelry.Model.Stock.Rfid.GetByStock.Request { StockNumber = "S1", ProductCode = "P1" });

        Assert.False(result.Found);
        Assert.Equal(3, result.Units.Count);
    }

    [Fact]
    public async Task GetByEpc_ReturnsUnitFields()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(Bind("E1"));
        var bound = await service.Bind(Bind("E2"));

        var result = await service.GetByEpc(new jewelry.Model.Stock.Rfid.GetByEpc.Request { Epc = "e2" });

        Assert.True(result.Found);
        Assert.Equal(bound.UnitId, result.UnitId);
        Assert.Equal(2, result.UnitNo);
        Assert.Equal("IN_STOCK", result.UnitStatus);
        Assert.Equal("L1", result.UnitLocationCode);
        Assert.Equal("หน้าร้าน", result.UnitLocationName);
        Assert.Equal(3, result.LotQty);
        Assert.False(result.SoldMismatch);
        Assert.Equal("S1", result.StockNumber);
    }

    [Fact]
    public async Task GetByEpcs_ReturnsUnitFieldsPerTag_AndNotFound()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(Bind("E1"));
        await service.Bind(Bind("E2"));

        var result = await service.GetByEpcs(new jewelry.Model.Stock.Rfid.GetByEpcs.Request { Epcs = new() { "E1", "E2", "ZZ" } });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(new int?[] { 1, 2 }, result.Items.OrderBy(i => i.UnitNo).Select(i => i.UnitNo));
        Assert.Equal(new[] { "ZZ" }, result.NotFound);
    }

    [Fact]
    public async Task ListByReceipt_OneRowPerUnit_WithUnitNoAndTag()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(Bind("E1"));

        var result = await service.ListByReceipt(new jewelry.Model.Stock.Rfid.ListByReceipt.Request { ReceiptNumber = "RC1" });

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Bound);
        Assert.Equal(new int?[] { 1, 2, 3 }, result.Items.Select(i => i.UnitNo));
        Assert.Equal("E1", result.Items[0].Epc);
        Assert.Null(result.Items[1].Epc);
    }

    [Fact]
    public async Task ListUnbound_OneRowPerFreeUnit()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(Bind("E1"));

        var result = await service.ListUnbound(new jewelry.Model.Stock.Rfid.ListUnbound.Request { Take = 50 });

        Assert.Equal(2, result.Total);
        Assert.Equal(new int?[] { 2, 3 }, result.Items.Select(i => i.UnitNo));
        Assert.All(result.Items, i => Assert.NotNull(i.UnitId));
    }

    [Fact]
    public async Task ListReceipts_CountsUnits()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(Bind("E1"));

        var result = await service.ListReceipts(new jewelry.Model.Stock.Rfid.ListReceipts.Request { Take = 20 });

        var item = Assert.Single(result.Items);
        Assert.Equal(3, item.TotalPieces);
        Assert.Equal(1, item.BoundPieces);
        Assert.Equal(2, item.RemainingPieces);
    }

    private static async Task SellTaggedUnitInsteadOfFreeOne(JewelryContext context, StockRfidService service)
    {
        await service.Bind(Bind("E1"));
        await service.Bind(Bind("E2"));

        // ระบบตัดผิดชิ้น: ขาย unit 1 (มีแท็ก E1) แทนที่จะเป็น unit 3 (ไม่มีแท็ก)
        var unit1 = await context.TbtStockUnit.SingleAsync(u => u.UnitNo == 1);
        unit1.Status = "SOLD";
        unit1.SoldRef = "INV1";
        unit1.SoldDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        var tag = await context.TbtStockPieceRfid.SingleAsync(r => r.Epc == "E1");
        tag.IsActive = false;
        tag.UnbindReason = "SOLD";
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByEpc_SoldBinding_WithFreeUnitInLot_SwapsAndReactivates()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await SellTaggedUnitInsteadOfFreeOne(context, service);

        var result = await service.GetByEpc(new jewelry.Model.Stock.Rfid.GetByEpc.Request { Epc = "E1" });

        Assert.True(result.Found);
        Assert.False(result.SoldMismatch);
        Assert.Equal(1, result.UnitNo);
        Assert.Equal("IN_STOCK", result.UnitStatus);

        var units = await context.TbtStockUnit.OrderBy(u => u.UnitNo).ToListAsync();
        Assert.Equal("IN_STOCK", units[0].Status);
        Assert.Null(units[0].SoldRef);
        Assert.Equal("SOLD", units[2].Status);
        Assert.Equal("INV1", units[2].SoldRef);
        Assert.Equal(new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc), units[2].SoldDate);

        var tag = await context.TbtStockPieceRfid.SingleAsync(r => r.Epc == "E1");
        Assert.True(tag.IsActive);
        Assert.Null(tag.UnbindReason);
        Assert.Equal(units[0].Id, tag.UnitId);
    }

    [Fact]
    public async Task GetByEpcs_SoldBinding_Ambiguous_FlagsMismatchWithoutChangingData()
    {
        using var context = CreateContext(2);
        var service = CreateService(context);
        await SellTaggedUnitInsteadOfFreeOne(context, service);

        var result = await service.GetByEpcs(new jewelry.Model.Stock.Rfid.GetByEpcs.Request { Epcs = new() { "E1" } });

        var item = Assert.Single(result.Items);
        Assert.True(item.SoldMismatch);
        Assert.Equal("SOLD", item.UnitStatus);
        Assert.Empty(result.NotFound);

        var tag = await context.TbtStockPieceRfid.SingleAsync(r => r.Epc == "E1");
        Assert.False(tag.IsActive);
        Assert.Equal("SOLD", (await context.TbtStockUnit.SingleAsync(u => u.UnitNo == 1)).Status);
    }

    [Fact]
    public async Task GetByEpc_ManuallyUnboundTag_StaysNotFound()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        await service.Bind(Bind("E1"));
        await service.Unbind(new jewelry.Model.Stock.Rfid.Unbind.Request { Epc = "E1" });

        var result = await service.GetByEpc(new jewelry.Model.Stock.Rfid.GetByEpc.Request { Epc = "E1" });

        Assert.False(result.Found);
    }
}
