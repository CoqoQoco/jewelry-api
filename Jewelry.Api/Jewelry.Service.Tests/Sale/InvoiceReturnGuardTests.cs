using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Sale.Invoice;
using Jewelry.Service.Stock;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Tests.Sale;

public class InvoiceReturnGuardTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 3, 0, 0, DateTimeKind.Utc);

    private static JewelryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JewelryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JewelryContext(options);
    }

    private static async Task<TbtStockPiece> SeedSoldPieceAsync(JewelryContext context, string soldRef)
    {
        var piece = new TbtStockPiece
        {
            StockNumber = "S1",
            ProductCode = "P1",
            SkuCode = "SKU1",
            LocationCode = "MAIN",
            Qty = 0,
            QtyReserved = 0,
            Status = "SOLD",
            ReceiptNumber = "RC0",
            ReceiptDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreateBy = "t"
        };
        context.TbtStockPiece.Add(piece);
        context.TbtStockUnit.Add(new TbtStockUnit
        {
            StockNumber = "S1",
            ProductCode = "P1",
            UnitNo = 1,
            LocationCode = "MAIN",
            Status = "SOLD",
            SoldRef = soldRef,
            SoldDate = Now,
            ReceiptNumber = "RC0",
            CreateBy = "t"
        });
        await context.SaveChangesAsync();
        return piece;
    }

    private static void AddMovement(JewelryContext context, string type, string refDocType, string invoice, decimal qty = 1)
    {
        context.TbtStockMovement.Add(new TbtStockMovement
        {
            MovementDate = Now,
            MovementType = type,
            SkuCode = "SKU1",
            StockNumber = "S1",
            ProductCode = "P1",
            Qty = qty,
            RefDocType = refDocType,
            RefDocNo = invoice,
            CreateDate = Now,
            CreateBy = "t"
        });
    }

    [Fact]
    public async Task CanReturn_WhenSoldByThisInvoiceAndNotReturned()
    {
        using var context = CreateContext();
        var piece = await SeedSoldPieceAsync(context, "INV1");
        AddMovement(context, "SALE", "INVOICE", "INV1");
        await context.SaveChangesAsync();

        Assert.True(await InvoiceReturnGuard.CanReturnAsync(context, "INV1", piece));
    }

    [Fact]
    public async Task CannotReturn_WhenAlreadyReturnedForSameInvoice()
    {
        using var context = CreateContext();
        var piece = await SeedSoldPieceAsync(context, "INV1");
        AddMovement(context, "SALE", "INVOICE", "INV1");
        AddMovement(context, "RETURN", "INVOICE_DELETE", "INV1");
        await context.SaveChangesAsync();

        Assert.False(await InvoiceReturnGuard.CanReturnAsync(context, "INV1", piece));
    }

    [Fact]
    public async Task SecondReturnAttempt_WritesNoSecondReturnMovement()
    {
        using var context = CreateContext();
        var piece = await SeedSoldPieceAsync(context, "INV1");
        AddMovement(context, "SALE", "INVOICE", "INV1");
        await context.SaveChangesAsync();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (!await InvoiceReturnGuard.CanReturnAsync(context, "INV1", piece)) continue;

            var info = new StockUnitSyncInfo { By = "u", Now = Now };
            var revive = await StockUnitOps.ReviveAsync(context, piece, "INV1", new ReservationKey("SO", "SO1", 1), 1, info);
            await StockBalanceApplier.ApplyAsync(context, revive, new StockMovementTemplate
            {
                MovementType = "RETURN",
                RefDocType = "INVOICE_DELETE",
                RefDocNo = "INV1",
                Date = Now,
                By = "u"
            }, writeBalance: false);
            await context.SaveChangesAsync();
        }

        Assert.Equal(1, await context.TbtStockMovement.CountAsync(m => m.MovementType == "RETURN"));
        Assert.Equal(1, piece.Qty);
        Assert.Equal(1, piece.QtyReserved);
    }

    [Fact]
    public async Task CannotReturn_WhenNoLedgerAndLotSoldByAnotherInvoice()
    {
        using var context = CreateContext();
        var piece = await SeedSoldPieceAsync(context, "INV2");

        Assert.False(await InvoiceReturnGuard.CanReturnAsync(context, "INV1", piece));
    }

    [Fact]
    public async Task CanReturn_WhenNoLedgerAndNoUnits_LegacyPreLedger()
    {
        using var context = CreateContext();
        var piece = new TbtStockPiece { StockNumber = "S1", ProductCode = "P1", SkuCode = "SKU1", LocationCode = "MAIN", Qty = 0, ReceiptNumber = "RC0", CreateBy = "t" };
        context.TbtStockPiece.Add(piece);
        await context.SaveChangesAsync();

        Assert.True(await InvoiceReturnGuard.CanReturnAsync(context, "INV1", piece));
    }
}
