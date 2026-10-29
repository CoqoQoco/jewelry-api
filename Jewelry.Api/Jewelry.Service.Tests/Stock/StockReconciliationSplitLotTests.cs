using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Stock;
using Jewelry.Service.Stock.Reconciliation;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Tests.Stock;

public class StockReconciliationSplitLotTests
{
    private static JewelryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JewelryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JewelryContext(options);
    }

    private static async Task SeedSplitLotAsync(JewelryContext context, decimal balanceL1, decimal balanceL2, decimal reservedL2 = 0)
    {
        context.TbtStockPiece.Add(new TbtStockPiece
        {
            StockNumber = "S1", ProductCode = "P1", SkuCode = "SKU1", LocationCode = "L1",
            Qty = 3, QtyReserved = 1, Status = "IN_STOCK", CreateBy = "t"
        });

        var specs = new[] { ("L1", "IN_STOCK"), ("L1", "IN_STOCK"), ("L2", "RESERVED"), ("L9", "SOLD") };
        for (var i = 0; i < specs.Length; i++)
        {
            context.TbtStockUnit.Add(new TbtStockUnit
            {
                StockNumber = "S1", ProductCode = "P1", UnitNo = i + 1,
                LocationCode = specs[i].Item1, Status = specs[i].Item2, CreateBy = "t"
            });
        }

        context.TbtStockBalance.Add(new TbtStockBalance { SkuCode = "SKU1", LocationCode = "L1", QtyOnHand = balanceL1, QtyReserved = 0, CreateBy = "t" });
        context.TbtStockBalance.Add(new TbtStockBalance { SkuCode = "SKU1", LocationCode = "L2", QtyOnHand = balanceL2, QtyReserved = reservedL2, CreateBy = "t" });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CheckDrift_SplitLot_ExpectedComesFromUnitsPerLocation_NoMismatchWhenBalancesMatch()
    {
        using var context = CreateContext();
        await SeedSplitLotAsync(context, balanceL1: 2, balanceL2: 1, reservedL2: 1);
        var service = new StockReconciliationService(context);

        var report = await service.CheckDriftAsync(CancellationToken.None);

        Assert.Equal(0, report.BalanceOnHandMismatchCount);
        Assert.Equal(0, report.BalanceReservedMismatchCount);
        Assert.Equal(0, report.UnitCountMismatchCount);
    }

    [Fact]
    public async Task CheckDrift_SplitLot_FlagsBalanceThatStillFollowsPieceLocation()
    {
        using var context = CreateContext();
        await SeedSplitLotAsync(context, balanceL1: 3, balanceL2: 0, reservedL2: 0);
        var service = new StockReconciliationService(context);

        var report = await service.CheckDriftAsync(CancellationToken.None);

        Assert.Equal(2, report.BalanceOnHandMismatchCount);
        Assert.Contains(report.OnHandMismatches, m => m.LocationCode == "L1" && m.BalanceQty == 3 && m.PieceCount == 2);
        Assert.Contains(report.OnHandMismatches, m => m.LocationCode == "L2" && m.BalanceQty == 0 && m.PieceCount == 1);
        Assert.Equal(1, report.BalanceReservedMismatchCount);
    }

    [Fact]
    public async Task CheckDrift_FlagsPieceVsUnitCountMismatch()
    {
        using var context = CreateContext();
        await SeedSplitLotAsync(context, balanceL1: 2, balanceL2: 1, reservedL2: 1);
        var piece = await context.TbtStockPiece.SingleAsync();
        piece.Qty = 4;
        await context.SaveChangesAsync();
        var service = new StockReconciliationService(context);

        var report = await service.CheckDriftAsync(CancellationToken.None);

        Assert.Equal(1, report.UnitCountMismatchCount);
        var row = report.UnitCountMismatches.Single();
        Assert.Equal("S1", row.StockNumber);
        Assert.Equal(4, row.PieceQty);
        Assert.Equal(3, row.UnitQty);
    }
}
