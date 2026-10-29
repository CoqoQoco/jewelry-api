using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Stock;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Tests.Stock;

public class StockUnitSyncTests
{
    private static JewelryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JewelryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JewelryContext(options);
    }

    private static async Task<TbtStockPiece> SeedLotAsync(JewelryContext context, int qty, int reserved = 0, string location = "L1", bool withUnits = true)
    {
        var piece = new TbtStockPiece
        {
            StockNumber = "S1",
            ProductCode = "P1",
            SkuCode = "SKU1",
            LocationCode = location,
            Qty = qty,
            QtyReserved = reserved,
            ReceiptNumber = "RC0",
            ReceiptDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreateBy = "t"
        };
        StockPieceQtyHelper.RecalcStatus(piece);
        context.TbtStockPiece.Add(piece);

        if (withUnits)
        {
            for (var n = 1; n <= qty; n++)
            {
                context.TbtStockUnit.Add(new TbtStockUnit
                {
                    StockNumber = "S1",
                    ProductCode = "P1",
                    UnitNo = n,
                    LocationCode = location,
                    Status = n <= reserved ? "RESERVED" : "IN_STOCK",
                    ReceiptNumber = "RC0",
                    CreateBy = "t"
                });
            }
        }

        await context.SaveChangesAsync();
        return piece;
    }

    private static async Task<TbtStockPieceRfid> TagAsync(JewelryContext context, int unitNo, string epc)
    {
        var unit = await context.TbtStockUnit.SingleAsync(u => u.UnitNo == unitNo);
        var tag = new TbtStockPieceRfid
        {
            Epc = epc,
            StockNumber = "S1",
            ProductCode = "P1",
            UnitId = unit.Id,
            IsActive = true,
            CreateDate = DateTime.UtcNow,
            CreateBy = "t"
        };
        context.TbtStockPieceRfid.Add(tag);
        await context.SaveChangesAsync();
        return tag;
    }

    private static async Task SyncAsync(JewelryContext context, TbtStockPiece piece, StockUnitSyncInfo? info = null)
    {
        await StockUnitSync.ReconcileAsync(context, piece, info ?? new StockUnitSyncInfo { By = "u" });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddUnitsForNewPieces_CreatesOneUnitPerQty_WithReserved()
    {
        using var context = CreateContext();
        var piece = new TbtStockPiece
        {
            StockNumber = "S1", ProductCode = "P1", SkuCode = "SKU1", LocationCode = "L1",
            Qty = 3, QtyReserved = 1, ReceiptNumber = "RC1", ReceiptDate = DateTime.UtcNow, CreateBy = "t"
        };
        context.TbtStockPiece.Add(piece);
        StockUnitSync.AddUnitsForNewPieces(context, new[] { piece }, "u");
        await context.SaveChangesAsync();

        var units = await context.TbtStockUnit.OrderBy(u => u.UnitNo).ToListAsync();
        Assert.Equal(new[] { 1, 2, 3 }, units.Select(u => u.UnitNo));
        Assert.Equal(new[] { "RESERVED", "IN_STOCK", "IN_STOCK" }, units.Select(u => u.Status));
        Assert.All(units, u => Assert.Equal("RC1", u.ReceiptNumber));
        Assert.All(units, u => Assert.Equal("L1", u.LocationCode));
    }

    [Fact]
    public async Task Reconcile_NoChange_DoesNothing()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 3, 1);

        await SyncAsync(context, piece);

        Assert.Equal(3, await context.TbtStockUnit.CountAsync());
        Assert.Equal(1, await context.TbtStockUnit.CountAsync(u => u.Status == "RESERVED"));
    }

    [Fact]
    public async Task Reconcile_Increase_CreatesUnitsWithNextNoAndCallerReceipt()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 2);
        var receiptDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        piece.Qty = 5;
        await SyncAsync(context, piece, new StockUnitSyncInfo { By = "u", ReceiptNumber = "RC9", ReceiptDate = receiptDate });

        var units = await context.TbtStockUnit.OrderBy(u => u.UnitNo).ToListAsync();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, units.Select(u => u.UnitNo));
        Assert.Equal(new[] { "RC0", "RC0", "RC9", "RC9", "RC9" }, units.Select(u => u.ReceiptNumber));
        Assert.Equal(receiptDate, units[4].ReceiptDate);
    }

    [Fact]
    public async Task Reconcile_TwoIncreasesInOneSave_DoNotCollideUnitNo()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 1);

        piece.Qty = 3;
        await StockUnitSync.ReconcileAsync(context, piece, new StockUnitSyncInfo { By = "u" });
        piece.Qty = 5;
        await StockUnitSync.ReconcileAsync(context, piece, new StockUnitSyncInfo { By = "u" });
        await context.SaveChangesAsync();

        var numbers = await context.TbtStockUnit.Select(u => u.UnitNo).OrderBy(n => n).ToListAsync();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, numbers);
    }

    [Fact]
    public async Task Reconcile_LotWithoutUnits_CreatesThemAndReserved()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 4, 2, withUnits: false);

        await SyncAsync(context, piece);

        var units = await context.TbtStockUnit.ToListAsync();
        Assert.Equal(4, units.Count);
        Assert.Equal(2, units.Count(u => u.Status == "RESERVED"));
        Assert.All(units, u => Assert.Equal("RC0", u.ReceiptNumber));
    }

    [Fact]
    public async Task Reconcile_Decrease_SellsUntaggedFirstThenHighestUnitNo_AndReleasesTag()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 4);
        var tag = await TagAsync(context, 4, "E4");
        await TagAsync(context, 3, "E3");
        var soldDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);

        piece.Qty = 2;
        await SyncAsync(context, piece, new StockUnitSyncInfo { By = "u", SoldRef = "INV1", SoldDate = soldDate });

        var sold = await context.TbtStockUnit.Where(u => u.Status == "SOLD").OrderBy(u => u.UnitNo).ToListAsync();
        Assert.Equal(new[] { 1, 2 }, sold.Select(u => u.UnitNo));
        Assert.All(sold, u => Assert.Equal("INV1", u.SoldRef));
        Assert.All(sold, u => Assert.Equal(soldDate, u.SoldDate));
        Assert.True((await context.TbtStockPieceRfid.SingleAsync(r => r.Epc == "E4")).IsActive);
        Assert.Equal(tag.Id, (await context.TbtStockPieceRfid.SingleAsync(r => r.Epc == "E4")).Id);
    }

    [Fact]
    public async Task Reconcile_Decrease_OnlyTaggedLeft_SellsTaggedAndDeactivatesBinding()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 2);
        await TagAsync(context, 1, "E1");
        await TagAsync(context, 2, "E2");

        piece.Qty = 1;
        await SyncAsync(context, piece, new StockUnitSyncInfo { By = "u", SoldRef = "INV1" });

        var soldUnit = await context.TbtStockUnit.SingleAsync(u => u.Status == "SOLD");
        Assert.Equal(2, soldUnit.UnitNo);
        var e2 = await context.TbtStockPieceRfid.SingleAsync(r => r.Epc == "E2");
        Assert.False(e2.IsActive);
        Assert.Equal("SOLD", e2.UnbindReason);
        Assert.NotNull(e2.UpdateDate);
        Assert.True((await context.TbtStockPieceRfid.SingleAsync(r => r.Epc == "E1")).IsActive);
    }

    [Fact]
    public async Task Reconcile_Sale_PrefersReservedUnitsWhenReservedDrops()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 3, 1);

        piece.Qty = 2;
        piece.QtyReserved = 0;
        await SyncAsync(context, piece, new StockUnitSyncInfo { By = "u", SoldRef = "INV1" });

        var sold = await context.TbtStockUnit.SingleAsync(u => u.Status == "SOLD");
        Assert.Equal(1, sold.UnitNo);
        Assert.Equal(0, await context.TbtStockUnit.CountAsync(u => u.Status == "RESERVED"));
        Assert.Equal(2, await context.TbtStockUnit.CountAsync(u => u.Status == "IN_STOCK"));
    }

    [Fact]
    public async Task Reconcile_ReserveAndUnreserve_PreferUntaggedUnits()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 3);
        await TagAsync(context, 3, "E3");

        piece.QtyReserved = 2;
        await SyncAsync(context, piece);

        var reserved = await context.TbtStockUnit.Where(u => u.Status == "RESERVED").Select(u => u.UnitNo).OrderBy(n => n).ToListAsync();
        Assert.Equal(new[] { 1, 2 }, reserved);

        piece.QtyReserved = 1;
        await SyncAsync(context, piece);

        var stillReserved = await context.TbtStockUnit.Where(u => u.Status == "RESERVED").Select(u => u.UnitNo).ToListAsync();
        Assert.Single(stillReserved);
        Assert.Equal(2, await context.TbtStockUnit.CountAsync(u => u.Status == "IN_STOCK"));
    }

    [Fact]
    public async Task Reconcile_PieceLocationChange_DoesNotForceUnitLocations()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 3);

        piece.LocationCode = "L2";
        await SyncAsync(context, piece);

        var units = await context.TbtStockUnit.ToListAsync();
        Assert.All(units, u => Assert.Equal("L1", u.LocationCode));
    }

    [Fact]
    public async Task Reconcile_Increase_NewUnitsUseInfoLocation_ElsePieceLocation()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 1);

        piece.Qty = 2;
        await SyncAsync(context, piece, new StockUnitSyncInfo { By = "u", LocationCode = "L9" });
        piece.Qty = 3;
        await SyncAsync(context, piece);

        var byNo = await context.TbtStockUnit.OrderBy(u => u.UnitNo).Select(u => u.LocationCode).ToListAsync();
        Assert.Equal(new[] { "L1", "L9", "L1" }, byNo);
    }

    [Fact]
    public async Task Reconcile_SplitLot_WithMatchingCounts_DoesNothing()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 3);
        var second = await context.TbtStockUnit.SingleAsync(u => u.UnitNo == 2);
        second.LocationCode = "L2";
        await context.SaveChangesAsync();

        await SyncAsync(context, piece);

        Assert.Equal("L2", (await context.TbtStockUnit.SingleAsync(u => u.UnitNo == 2)).LocationCode);
        Assert.Equal("L1", piece.LocationCode);
    }

    [Fact]
    public async Task Reconcile_Return_RevivesUnitsSoldByRef_BeforeCreatingNew()
    {
        using var context = CreateContext();
        var piece = await SeedLotAsync(context, 2);
        piece.Qty = 0;
        await SyncAsync(context, piece, new StockUnitSyncInfo { By = "u", SoldRef = "INV1" });
        Assert.Equal(2, await context.TbtStockUnit.CountAsync(u => u.Status == "SOLD"));

        piece.Qty = 1;
        piece.QtyReserved = 1;
        await SyncAsync(context, piece, new StockUnitSyncInfo { By = "u", ReviveSoldRef = "INV1" });

        Assert.Equal(2, await context.TbtStockUnit.CountAsync());
        var revived = await context.TbtStockUnit.SingleAsync(u => u.Status == "RESERVED");
        Assert.Equal(2, revived.UnitNo);
        Assert.Null(revived.SoldRef);
    }
}
