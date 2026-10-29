using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Stock;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Tests.Stock;

public class StockUnitOpsTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 3, 0, 0, DateTimeKind.Utc);

    private static StockUnitSyncInfo Info() => new() { By = "u", Now = Now };

    private static JewelryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JewelryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JewelryContext(options);
    }

    // spec: คลัง + สถานะของแต่ละชิ้น (unit_no เรียง 1..n ตามลำดับที่ส่ง) · primary = คลังหลักที่ตั้งให้ piece
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
            ReceiptNumber = "RC0",
            ReceiptDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
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
                ReceiptNumber = "RC0",
                CreateBy = "t"
            });
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

    private static Task<TbtStockUnit> UnitAsync(JewelryContext context, int unitNo) =>
        context.TbtStockUnit.SingleAsync(u => u.UnitNo == unitNo);

    private static ReservationKey SoKey(long lineId, string so = "SO1") => new(ReservationKey.So, so, lineId);

    private static decimal OnHandDelta(UnitOpResult r, string location) =>
        r.Deltas.Where(d => d.Location == location).Sum(d => d.OnHand);

    private static decimal ReservedDelta(UnitOpResult r, string location) =>
        r.Deltas.Where(d => d.Location == location).Sum(d => d.Reserved);

    #region reserve

    [Fact]
    public async Task Reserve_ByUnitIds_ReservesThoseUnitsAtTheirLocations()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L1", "IN_STOCK"), ("L2", "IN_STOCK"), ("L2", "IN_STOCK"));
        var u1 = await UnitAsync(context, 1);
        var u3 = await UnitAsync(context, 3);

        var result = await StockUnitOps.ReserveAsync(context, piece, 2, new[] { u1.Id, u3.Id }, null, SoKey(10), Info());
        await context.SaveChangesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, ReservedDelta(result, "L1"));
        Assert.Equal(1, ReservedDelta(result, "L2"));
        Assert.Equal(0, OnHandDelta(result, "L1"));
        Assert.Equal(2, piece.QtyReserved);
        Assert.Equal("IN_STOCK", piece.Status);

        var reserved = await context.TbtStockUnit.Where(u => u.Status == "RESERVED").OrderBy(u => u.UnitNo).ToListAsync();
        Assert.Equal(new[] { 1, 3 }, reserved.Select(u => u.UnitNo));
        Assert.All(reserved, u =>
        {
            Assert.Equal("SO", u.ReservedRefType);
            Assert.Equal("SO1", u.ReservedRef);
            Assert.Equal(10, u.ReservedLineId);
        });
        Assert.Equal(2, result.Lines.Count);
        Assert.All(result.Lines, l => Assert.Null(l.To));
    }

    [Fact]
    public async Task Reserve_ByUnitIds_Validation()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L1", "IN_STOCK"), ("L2", "SOLD"), ("L2", "IN_STOCK"));
        var u1 = await UnitAsync(context, 1);
        var u2 = await UnitAsync(context, 2);
        var sold = await UnitAsync(context, 3);
        var l2 = await UnitAsync(context, 4);

        await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.ReserveAsync(context, piece, 2, new[] { u1.Id, u1.Id }, null, SoKey(1), Info()));
        await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.ReserveAsync(context, piece, 1, new[] { u1.Id, u2.Id }, null, SoKey(1), Info()));
        await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.ReserveAsync(context, piece, 1, new[] { sold.Id }, null, SoKey(1), Info()));
        await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.ReserveAsync(context, piece, 1, new[] { 99999L }, null, SoKey(1), Info()));
        await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.ReserveAsync(context, piece, 2, new[] { u1.Id, l2.Id }, "L1", SoKey(1), Info()));

        Assert.Equal(0, piece.QtyReserved);
        Assert.Equal(0, await context.TbtStockUnit.CountAsync(u => u.Status == "RESERVED"));
    }

    [Fact]
    public async Task Reserve_ByUnitIds_AlreadyReservedByOtherLine_Throws_SameLine_IsIdempotent()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L1", "IN_STOCK"));
        var u1 = await UnitAsync(context, 1);

        await StockUnitOps.ReserveAsync(context, piece, 1, new[] { u1.Id }, null, SoKey(10), Info());
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.ReserveAsync(context, piece, 1, new[] { u1.Id }, null, SoKey(11), Info()));

        var again = await StockUnitOps.ReserveAsync(context, piece, 1, new[] { u1.Id }, null, SoKey(10), Info());
        Assert.Equal(0, again.Count);
        Assert.Equal(1, piece.QtyReserved);
    }

    [Fact]
    public async Task Reserve_ByLocation_StrictlyFromThatLocation()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L1", "IN_STOCK"), ("L1", "IN_STOCK"), ("L2", "IN_STOCK"));

        var ex = await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.ReserveAsync(context, piece, 2, null, "L2", SoKey(1), Info()));
        Assert.Contains("L2", ex.Message);
        Assert.Contains("1", ex.Message);

        var result = await StockUnitOps.ReserveAsync(context, piece, 1, null, "l2", SoKey(2), Info());
        await context.SaveChangesAsync();

        Assert.Equal("L2", (await UnitAsync(context, 4)).LocationCode);
        Assert.Equal("RESERVED", (await UnitAsync(context, 4)).Status);
        Assert.Equal(1, ReservedDelta(result, "L2"));
    }

    [Fact]
    public async Task Reserve_Fallback_PrimaryLocationFirst_ThenLocationWithMostAvailable()
    {
        using var context = CreateContext();
        // L1 (หลัก) มี 1 ชิ้นว่าง, L2 มี 2, L3 มี 3 → ขอ 3 ชิ้น = L1 ก่อน แล้ว L3 (มากสุด) 2 ชิ้น
        var piece = await SeedAsync(context, "L1",
            ("L1", "IN_STOCK"),
            ("L2", "IN_STOCK"), ("L2", "IN_STOCK"),
            ("L3", "IN_STOCK"), ("L3", "IN_STOCK"), ("L3", "IN_STOCK"));

        var result = await StockUnitOps.ReserveAsync(context, piece, 3, null, null, SoKey(1), Info());
        await context.SaveChangesAsync();

        Assert.Equal(1, ReservedDelta(result, "L1"));
        Assert.Equal(0, ReservedDelta(result, "L2"));
        Assert.Equal(2, ReservedDelta(result, "L3"));
    }

    [Fact]
    public async Task Reserve_Fallback_WithinLocation_UntaggedFirst_ThenHighestUnitNo()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L1", "IN_STOCK"), ("L1", "IN_STOCK"), ("L1", "IN_STOCK"));
        await TagAsync(context, 4, "E4");

        await StockUnitOps.ReserveAsync(context, piece, 2, null, null, SoKey(1), Info());
        await context.SaveChangesAsync();

        var reserved = await context.TbtStockUnit.Where(u => u.Status == "RESERVED").Select(u => u.UnitNo).OrderBy(n => n).ToListAsync();
        Assert.Equal(new[] { 2, 3 }, reserved);
    }

    [Fact]
    public async Task Reserve_NotEnough_ThrowsWithAvailableCount()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L2", "RESERVED"));

        var ex = await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.ReserveAsync(context, piece, 2, null, null, SoKey(1), Info()));
        Assert.Contains("พร้อมขาย 1", ex.Message);
    }

    [Fact]
    public async Task Reserve_FullyReserved_SetsPieceStatusReserved()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L1", "IN_STOCK"));

        await StockUnitOps.ReserveAsync(context, piece, 2, null, null, SoKey(1), Info());

        Assert.Equal("RESERVED", piece.Status);
        Assert.Equal(2, piece.QtyReserved);
    }

    #endregion

    #region release / sell / revive

    [Fact]
    public async Task Release_ByLine_FreesOnlyThatLinesUnits_AndRestoresDeltas()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L1", "IN_STOCK"), ("L2", "IN_STOCK"));
        var u1 = await UnitAsync(context, 1);
        var u3 = await UnitAsync(context, 3);
        await StockUnitOps.ReserveAsync(context, piece, 1, new[] { u1.Id }, null, SoKey(10), Info());
        await StockUnitOps.ReserveAsync(context, piece, 1, new[] { u3.Id }, null, SoKey(11), Info());
        await context.SaveChangesAsync();

        var result = await StockUnitOps.ReleaseAsync(context, piece, SoKey(11), 1, Info());
        await context.SaveChangesAsync();

        Assert.Equal(1, result.Count);
        Assert.Equal(-1, ReservedDelta(result, "L2"));
        Assert.Equal("L2", result.Lines.Single().To);
        var freed = await UnitAsync(context, 3);
        Assert.Equal("IN_STOCK", freed.Status);
        Assert.Null(freed.ReservedRefType);
        Assert.Null(freed.ReservedLineId);
        Assert.Equal("RESERVED", (await UnitAsync(context, 1)).Status);
        Assert.Equal(1, piece.QtyReserved);
    }

    [Fact]
    public async Task Release_LegacyLineWithoutLinkedUnits_FallsBackToUnlinkedReservedUnits_PrimaryFirst()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "RESERVED"), ("L2", "RESERVED"), ("L2", "IN_STOCK"));

        var result = await StockUnitOps.ReleaseAsync(context, piece, SoKey(77), 1, Info());
        await context.SaveChangesAsync();

        Assert.Equal(1, result.Count);
        Assert.Equal(-1, ReservedDelta(result, "L1"));
        Assert.Equal("IN_STOCK", (await UnitAsync(context, 1)).Status);
        Assert.Equal("RESERVED", (await UnitAsync(context, 2)).Status);
        Assert.Equal(1, piece.QtyReserved);
    }

    [Fact]
    public async Task SellReserved_SellsLinkedUnits_KeepsLineRefs_DeactivatesTags_PerLocationDeltas()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L1", "IN_STOCK"), ("L2", "IN_STOCK"));
        var u2 = await UnitAsync(context, 2);
        var u3 = await UnitAsync(context, 3);
        await StockUnitOps.ReserveAsync(context, piece, 2, new[] { u2.Id, u3.Id }, null, SoKey(10), Info());
        await context.SaveChangesAsync();
        await TagAsync(context, 3, "E3");
        var soldAt = new DateTime(2026, 10, 30, 5, 0, 0, DateTimeKind.Utc);

        var result = await StockUnitOps.SellReservedAsync(context, piece, SoKey(10), 2, "INV1", soldAt, Info());
        await context.SaveChangesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(-1, OnHandDelta(result, "L1"));
        Assert.Equal(-1, ReservedDelta(result, "L1"));
        Assert.Equal(-1, OnHandDelta(result, "L2"));
        Assert.Equal(-1, ReservedDelta(result, "L2"));
        Assert.Equal(1, piece.Qty);
        Assert.Equal(0, piece.QtyReserved);
        Assert.Equal("IN_STOCK", piece.Status);

        var sold = await context.TbtStockUnit.Where(u => u.Status == "SOLD").OrderBy(u => u.UnitNo).ToListAsync();
        Assert.Equal(new[] { 2, 3 }, sold.Select(u => u.UnitNo));
        Assert.All(sold, u =>
        {
            Assert.Equal("INV1", u.SoldRef);
            Assert.Equal(soldAt, u.SoldDate);
            Assert.Equal(10, u.ReservedLineId);
        });
        var e3 = await context.TbtStockPieceRfid.SingleAsync(r => r.Epc == "E3");
        Assert.False(e3.IsActive);
        Assert.Equal("SOLD", e3.UnbindReason);
    }

    [Fact]
    public async Task SellReserved_LegacyLine_UsesUnlinkedReservedUnits_AndThrowsWhenStillShort()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L2", "RESERVED"), ("L1", "RESERVED"), ("L1", "IN_STOCK"));

        var result = await StockUnitOps.SellReservedAsync(context, piece, SoKey(5), 1, "INV1", Now, Info());
        await context.SaveChangesAsync();

        Assert.Equal("L1", result.Lines.Single().From);
        Assert.Equal("SOLD", (await UnitAsync(context, 2)).Status);
        Assert.Equal("RESERVED", (await UnitAsync(context, 1)).Status);

        await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.SellReservedAsync(context, piece, SoKey(6), 2, "INV2", Now, Info()));
    }

    [Fact]
    public async Task SellReserved_ThenRevive_RestoresReservedWithOriginalLineAndLocation()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L2", "IN_STOCK"));
        var u2 = await UnitAsync(context, 2);
        await StockUnitOps.ReserveAsync(context, piece, 1, new[] { u2.Id }, null, SoKey(10), Info());
        await StockUnitOps.SellReservedAsync(context, piece, SoKey(10), 1, "INV1", Now, Info());
        await context.SaveChangesAsync();
        Assert.Equal(1, piece.Qty);

        var result = await StockUnitOps.ReviveAsync(context, piece, "INV1", SoKey(10), 1, Info());
        await context.SaveChangesAsync();

        Assert.Equal(1, OnHandDelta(result, "L2"));
        Assert.Equal(1, ReservedDelta(result, "L2"));
        Assert.Equal("L2", result.Lines.Single().To);
        var revived = await UnitAsync(context, 2);
        Assert.Equal("RESERVED", revived.Status);
        Assert.Equal("L2", revived.LocationCode);
        Assert.Equal(10, revived.ReservedLineId);
        Assert.Null(revived.SoldRef);
        Assert.Equal(2, piece.Qty);
        Assert.Equal(1, piece.QtyReserved);
    }

    [Fact]
    public async Task Revive_NoSoldUnits_CreatesReservedUnitAtPrimaryLocation()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"));

        var result = await StockUnitOps.ReviveAsync(context, piece, "INV-MISSING", SoKey(9), 1, Info());
        await context.SaveChangesAsync();

        Assert.Equal(1, result.Count);
        Assert.Equal(2, await context.TbtStockUnit.CountAsync());
        var created = await UnitAsync(context, 2);
        Assert.Equal("RESERVED", created.Status);
        Assert.Equal("L1", created.LocationCode);
        Assert.Equal(9, created.ReservedLineId);
        Assert.Equal(2, piece.Qty);
        Assert.Equal(1, piece.QtyReserved);
    }

    #endregion

    #region move

    [Fact]
    public async Task Move_WholeLot_MovesAllUnsoldUnits_SoldStay()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L2", "IN_STOCK"), ("L1", "SOLD"));

        var result = await StockUnitOps.MoveAsync(context, piece, null, null, null, "L3", Info());
        await context.SaveChangesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(-1, OnHandDelta(result, "L1"));
        Assert.Equal(-1, OnHandDelta(result, "L2"));
        Assert.Equal(2, OnHandDelta(result, "L3"));
        Assert.Equal(2, result.Lines.Count);
        Assert.All(result.Lines, l => Assert.Equal("L3", l.To));
        Assert.Equal("L3", piece.LocationCode);
        Assert.Equal("L1", (await UnitAsync(context, 3)).LocationCode);
    }

    [Fact]
    public async Task Move_ReservedUnit_MovesReservationWithIt()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L1", "IN_STOCK"));
        var u1 = await UnitAsync(context, 1);
        await StockUnitOps.ReserveAsync(context, piece, 1, new[] { u1.Id }, null, SoKey(10), Info());
        await context.SaveChangesAsync();

        var result = await StockUnitOps.MoveAsync(context, piece, new[] { u1.Id }, null, null, "L2", Info());
        await context.SaveChangesAsync();

        Assert.Equal(-1, OnHandDelta(result, "L1"));
        Assert.Equal(-1, ReservedDelta(result, "L1"));
        Assert.Equal(1, OnHandDelta(result, "L2"));
        Assert.Equal(1, ReservedDelta(result, "L2"));
        var moved = await UnitAsync(context, 1);
        Assert.Equal("L2", moved.LocationCode);
        Assert.Equal("RESERVED", moved.Status);
        Assert.Equal(10, moved.ReservedLineId);
        Assert.Equal(1, piece.QtyReserved);
        Assert.Equal("L1", piece.LocationCode);
    }

    [Fact]
    public async Task Move_ByQty_FromLocation_PrefersFreeUntaggedUnits()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "RESERVED"), ("L1", "IN_STOCK"), ("L1", "IN_STOCK"), ("L1", "IN_STOCK"));
        await TagAsync(context, 4, "E4");

        await StockUnitOps.MoveAsync(context, piece, null, "L1", 2, "L2", Info());
        await context.SaveChangesAsync();

        var moved = await context.TbtStockUnit.Where(u => u.LocationCode == "L2").Select(u => u.UnitNo).OrderBy(n => n).ToListAsync();
        Assert.Equal(new[] { 2, 3 }, moved);
        Assert.Equal("L1", piece.LocationCode);
    }

    [Fact]
    public async Task Move_Validation()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L2", "SOLD"));
        var sold = await UnitAsync(context, 2);

        await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.MoveAsync(context, piece, new[] { sold.Id }, null, null, "L3", Info()));
        await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.MoveAsync(context, piece, null, "L1", 5, "L3", Info()));
        await Assert.ThrowsAsync<HandleException>(() =>
            StockUnitOps.MoveAsync(context, piece, new[] { 424242L }, null, null, "L3", Info()));
    }

    [Fact]
    public async Task Move_UnitAlreadyAtTarget_IsSkipped()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"), ("L2", "IN_STOCK"));

        var result = await StockUnitOps.MoveAsync(context, piece, null, null, null, "L2", Info());

        Assert.Equal(1, result.Count);
        Assert.Equal(1, result.Lines.Single().Qty);
        Assert.Equal("L1", result.Lines.Single().From);
    }

    [Fact]
    public async Task Move_AllAtTarget_ReturnsEmptyResult()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L2", ("L2", "IN_STOCK"), ("L2", "IN_STOCK"));

        var result = await StockUnitOps.MoveAsync(context, piece, null, null, null, "L2", Info());

        Assert.Equal(0, result.Count);
        Assert.Empty(result.Deltas);
    }

    #endregion

    #region add units / primary location

    [Fact]
    public async Task AddUnits_AtDifferentLocation_CreatesUnitsThere_AndUpdatesPrimaryWhenMajority()
    {
        using var context = CreateContext();
        var piece = await SeedAsync(context, "L1", ("L1", "IN_STOCK"));

        var result = await StockUnitOps.AddUnitsAsync(context, piece, 3, "L2", new StockUnitSyncInfo
        {
            By = "u",
            Now = Now,
            ReceiptNumber = "RC9",
            ReceiptDate = Now
        });
        await context.SaveChangesAsync();

        Assert.Equal(3, OnHandDelta(result, "L2"));
        Assert.Equal(4, piece.Qty);
        var added = await context.TbtStockUnit.Where(u => u.LocationCode == "L2").OrderBy(u => u.UnitNo).ToListAsync();
        Assert.Equal(new[] { 2, 3, 4 }, added.Select(u => u.UnitNo));
        Assert.All(added, u => Assert.Equal("RC9", u.ReceiptNumber));
        Assert.Equal("L2", piece.LocationCode);
    }

    [Fact]
    public void RecalcPrimary_Tie_KeepsCurrent_ElseLowestCode()
    {
        var piece = new TbtStockPiece { LocationCode = "L2" };
        TbtStockUnit U(string loc, string status = "IN_STOCK") => new() { LocationCode = loc, Status = status };

        var tie = new[] { U("L1"), U("L2") };
        Assert.False(StockUnitSync.RecalcPrimaryLocation(piece, tie));
        Assert.Equal("L2", piece.LocationCode);

        piece.LocationCode = "L9";
        Assert.True(StockUnitSync.RecalcPrimaryLocation(piece, tie));
        Assert.Equal("L1", piece.LocationCode);

        var majority = new[] { U("L1"), U("L3"), U("L3"), U("L2", "SOLD"), U("L2", "SOLD"), U("L2", "SOLD") };
        Assert.True(StockUnitSync.RecalcPrimaryLocation(piece, majority));
        Assert.Equal("L3", piece.LocationCode);

        var noneLeft = new[] { U("L5", "SOLD") };
        Assert.False(StockUnitSync.RecalcPrimaryLocation(piece, noneLeft));
        Assert.Equal("L3", piece.LocationCode);
    }

    [Fact]
    public async Task Ops_SelfHeal_LotWithoutUnits_GetsUnitsAtPrimaryLocationFirst()
    {
        using var context = CreateContext();
        var piece = new TbtStockPiece
        {
            StockNumber = "S1", ProductCode = "P1", SkuCode = "SKU1", LocationCode = "L1",
            Qty = 2, QtyReserved = 0, ReceiptNumber = "RC0", CreateBy = "t"
        };
        StockPieceQtyHelper.RecalcStatus(piece);
        context.TbtStockPiece.Add(piece);
        await context.SaveChangesAsync();

        var result = await StockUnitOps.ReserveAsync(context, piece, 1, null, null, SoKey(1), Info());
        await context.SaveChangesAsync();

        Assert.Equal(1, result.Count);
        Assert.Equal(2, await context.TbtStockUnit.CountAsync());
        Assert.All(await context.TbtStockUnit.ToListAsync(), u => Assert.Equal("L1", u.LocationCode));
    }

    #endregion
}
