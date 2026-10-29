using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Stock;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Tests.Stock;

public class StockBalanceApplierTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 3, 0, 0, DateTimeKind.Utc);

    private static JewelryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JewelryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JewelryContext(options);
    }

    private static StockUnitSyncInfo Info() => new() { By = "u", Now = Now };

    private static StockMovementTemplate Template(string type = "TEST", string? refType = "SO", string? refNo = "SO1") => new()
    {
        MovementType = type,
        RefDocType = refType,
        RefDocNo = refNo,
        Date = Now,
        By = "u"
    };

    // ล็อต S1: units ตามคลังที่ส่งมา + balance ของ SKU1 ตรงกับ units (onHand/reserved รายคลัง)
    private static async Task<TbtStockPiece> SeedAsync(JewelryContext context, string primary, params (string Location, string Status)[] specs)
    {
        var active = specs.Where(s => s.Status != "SOLD").ToList();
        var piece = new TbtStockPiece
        {
            StockNumber = "S1",
            ProductCode = "P1",
            SkuCode = "SKU1",
            LocationCode = primary,
            Qty = active.Count,
            QtyReserved = active.Count(s => s.Status == "RESERVED"),
            CreateBy = "t"
        };
        StockPieceQtyHelper.RecalcStatus(piece);
        context.TbtStockPiece.Add(piece);

        for (var i = 0; i < specs.Length; i++)
        {
            context.TbtStockUnit.Add(new TbtStockUnit
            {
                StockNumber = "S1",
                ProductCode = "P1",
                UnitNo = i + 1,
                LocationCode = specs[i].Location,
                Status = specs[i].Status,
                CreateBy = "t"
            });
        }

        foreach (var group in active.GroupBy(s => s.Location))
        {
            context.TbtStockBalance.Add(new TbtStockBalance
            {
                SkuCode = "SKU1",
                LocationCode = group.Key,
                QtyOnHand = group.Count(),
                QtyReserved = group.Count(s => s.Status == "RESERVED"),
                CreateBy = "t"
            });
        }

        await context.SaveChangesAsync();
        return piece;
    }

    private static async Task<TbtStockBalance?> BalanceAsync(JewelryContext context, string location) =>
        await context.TbtStockBalance.FirstOrDefaultAsync(b => b.SkuCode == "SKU1" && b.LocationCode == location);

    [Fact]
    public async Task Apply_Reserve_UpdatesBalancePerLocation_AndWritesOneMovementPerLocationWithUnits()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L1", "IN_STOCK"), ("L2", "IN_STOCK"));
        var u1 = await context.TbtStockUnit.SingleAsync(u => u.UnitNo == 1);
        var u3 = await context.TbtStockUnit.SingleAsync(u => u.UnitNo == 3);

        var result = await StockUnitOps.ReserveAsync(context, piece, 2, new[] { u1.Id, u3.Id }, null,
            new ReservationKey(ReservationKey.So, "SO1", 5), Info());
        await StockBalanceApplier.ApplyAsync(context, result, Template("RESERVE"));
        await context.SaveChangesAsync();

        var l1 = await BalanceAsync(context, "L1");
        var l2 = await BalanceAsync(context, "L2");
        Assert.Equal(2, l1!.QtyOnHand);
        Assert.Equal(1, l1.QtyReserved);
        Assert.Equal(1, l2!.QtyOnHand);
        Assert.Equal(1, l2.QtyReserved);

        var movements = await context.TbtStockMovement.OrderBy(m => m.FromLocation).ToListAsync();
        Assert.Equal(2, movements.Count);
        Assert.Equal(new[] { "L1", "L2" }, movements.Select(m => m.FromLocation));
        Assert.All(movements, m =>
        {
            Assert.Equal("RESERVE", m.MovementType);
            Assert.Equal("SKU1", m.SkuCode);
            Assert.Equal("S1", m.StockNumber);
            Assert.Equal("P1", m.ProductCode);
            Assert.Equal(1, m.Qty);
            Assert.Equal("SO", m.RefDocType);
            Assert.Equal("SO1", m.RefDocNo);
        });
        Assert.Equal(2, await context.TbtStockMovementUnit.CountAsync());
    }

    [Fact]
    public async Task Apply_SellThenRevive_RestoresBalancesExactly()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L2", "IN_STOCK"), ("L2", "IN_STOCK"));
        var u3 = await context.TbtStockUnit.SingleAsync(u => u.UnitNo == 3);
        var key = new ReservationKey(ReservationKey.So, "SO1", 5);

        var reserve = await StockUnitOps.ReserveAsync(context, piece, 1, new[] { u3.Id }, null, key, Info());
        await StockBalanceApplier.ApplyAsync(context, reserve, Template("RESERVE"));
        var sale = await StockUnitOps.SellReservedAsync(context, piece, key, 1, "INV1", Now, Info());
        await StockBalanceApplier.ApplyAsync(context, sale, Template("SALE", "INVOICE", "INV1"));
        await context.SaveChangesAsync();

        var l2 = await BalanceAsync(context, "L2");
        Assert.Equal(1, l2!.QtyOnHand);
        Assert.Equal(0, l2.QtyReserved);
        Assert.Equal(1, (await BalanceAsync(context, "L1"))!.QtyOnHand);

        var revive = await StockUnitOps.ReviveAsync(context, piece, "INV1", key, 1, Info());
        await StockBalanceApplier.ApplyAsync(context, revive, Template("RETURN", "INVOICE_DELETE", "INV1"));
        await context.SaveChangesAsync();

        l2 = await BalanceAsync(context, "L2");
        Assert.Equal(2, l2!.QtyOnHand);
        Assert.Equal(1, l2.QtyReserved);
        var l1 = await BalanceAsync(context, "L1");
        Assert.Equal(1, l1!.QtyOnHand);
        Assert.Equal(0, l1.QtyReserved);
    }

    [Fact]
    public async Task Apply_Move_WritesSourceAndTargetBalances_AndOneMovementPerFromTo()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "RESERVED"), ("L1", "IN_STOCK"), ("L2", "IN_STOCK"));

        var move = await StockUnitOps.MoveAsync(context, piece, null, null, null, "L3", Info());
        await StockBalanceApplier.ApplyAsync(context, move, Template("Transfer", "MoveLocation", null));
        await context.SaveChangesAsync();

        var l1 = await BalanceAsync(context, "L1");
        var l2 = await BalanceAsync(context, "L2");
        var l3 = await BalanceAsync(context, "L3");
        Assert.Equal(0, l1!.QtyOnHand);
        Assert.Equal(0, l1.QtyReserved);
        Assert.Equal(0, l2!.QtyOnHand);
        Assert.Equal(3, l3!.QtyOnHand);
        Assert.Equal(1, l3.QtyReserved);

        var movements = await context.TbtStockMovement.OrderBy(m => m.FromLocation).ToListAsync();
        Assert.Equal(2, movements.Count);
        Assert.Equal(2, movements.Single(m => m.FromLocation == "L1").Qty);
        Assert.Equal(1, movements.Single(m => m.FromLocation == "L2").Qty);
        Assert.All(movements, m => Assert.Equal("L3", m.ToLocation));
        Assert.Equal(3, await context.TbtStockMovementUnit.CountAsync());
    }

    [Fact]
    public async Task Apply_AddUnits_CreatesBalanceWhenMissing()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"));

        var add = await StockUnitOps.AddUnitsAsync(context, piece, 2, "L2", Info());
        await StockBalanceApplier.ApplyAsync(context, add, Template("RECEIPT", "RECEIPT", "RC1"));
        await context.SaveChangesAsync();

        var l2 = await BalanceAsync(context, "L2");
        Assert.Equal(2, l2!.QtyOnHand);
        var movement = await context.TbtStockMovement.SingleAsync();
        Assert.Equal("L2", movement.ToLocation);
        Assert.Equal(2, movement.Qty);
    }

    [Fact]
    public async Task Apply_Release_WithoutBalanceRow_DoesNotCreateNegativeBalance()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"));
        var key = new ReservationKey(ReservationKey.So, "SO1", 5);
        await StockUnitOps.ReserveAsync(context, piece, 1, null, null, key, Info());
        context.TbtStockBalance.RemoveRange(await context.TbtStockBalance.ToListAsync());
        await context.SaveChangesAsync();

        var release = await StockUnitOps.ReleaseAsync(context, piece, key, 1, Info());
        await StockBalanceApplier.ApplyAsync(context, release, Template("UNRESERVE"));
        await context.SaveChangesAsync();

        Assert.Equal(0, await context.TbtStockBalance.CountAsync());
        Assert.Equal(1, await context.TbtStockMovement.CountAsync());
    }

    [Fact]
    public async Task Apply_TwoOpsInOneSave_ShareTheSameNewBalanceRow()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"));

        var first = await StockUnitOps.AddUnitsAsync(context, piece, 1, "L9", Info());
        await StockBalanceApplier.ApplyAsync(context, first, Template("RECEIPT"));
        var second = await StockUnitOps.AddUnitsAsync(context, piece, 2, "L9", Info());
        await StockBalanceApplier.ApplyAsync(context, second, Template("RECEIPT"));
        await context.SaveChangesAsync();

        var rows = await context.TbtStockBalance.Where(b => b.LocationCode == "L9").ToListAsync();
        Assert.Single(rows);
        Assert.Equal(3, rows[0].QtyOnHand);
    }

    [Fact]
    public async Task Apply_WriteBalanceFalse_WritesOnlyMovements()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"));

        var move = await StockUnitOps.MoveAsync(context, piece, null, null, null, "L2", Info());
        await StockBalanceApplier.ApplyAsync(context, move, Template("Transfer", "EditProduct", null), writeBalance: false);
        await context.SaveChangesAsync();

        Assert.Equal(1, (await BalanceAsync(context, "L1"))!.QtyOnHand);
        Assert.Null(await BalanceAsync(context, "L2"));
        Assert.Equal(1, await context.TbtStockMovement.CountAsync());
    }
}
