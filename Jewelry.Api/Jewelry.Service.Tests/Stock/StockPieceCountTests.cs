using jewelry.Model.Report.Executive.Flow;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Common;
using Jewelry.Service.Report.Executive;
using Jewelry.Service.Stock;
using Jewelry.Service.Stock.Insight;
using Jewelry.Service.Stock.Reconciliation;
using Jewelry.Service.Stock.StockReport;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Jewelry.Service.Tests.Stock;

// TK202610290001: รายงานสต็อกนับชิ้น (tbt_stock_unit) ไม่ใช่แถวล็อต
public class StockPieceCountTests
{
    private class NoHttpContext : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private static JewelryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JewelryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JewelryContext(options);
    }

    private static DateTime Utc(string iso)
        => DateTime.SpecifyKind(DateTime.Parse(iso, null, System.Globalization.DateTimeStyles.AdjustToUniversal), DateTimeKind.Utc);

    private static void AddSku(JewelryContext c, string sku, string productionType = "Silver", string size = "Silver", string type = "RG")
    {
        c.TbtSku.Add(new TbtSku
        {
            SkuCode = sku, ProductNameTh = "n", ProductNameEn = "n", CreateBy = "t",
            ProductionType = productionType, ProductionTypeSize = size, ProductType = type, ProductTypeName = type, Mold = "M-" + sku
        });
    }

    private static TbtStockPiece AddLot(JewelryContext c, string stockNumber, decimal qty, decimal reserved = 0, string sku = "SKU1",
        string location = "L1", string receipt = "R1", DateTime? production = null, decimal? cost = 100m, string status = "IN_STOCK", string productCode = "P1")
    {
        var piece = new TbtStockPiece
        {
            StockNumber = stockNumber, ProductCode = productCode, SkuCode = sku, LocationCode = location,
            Qty = qty, QtyReserved = reserved, Status = status, ReceiptNumber = receipt, ReceiptType = "production",
            ProductionDate = production, ProductCost = cost, CreateBy = "t"
        };
        c.TbtStockPiece.Add(piece);
        return piece;
    }

    private static void AddUnits(JewelryContext c, string stockNumber, int count, string location = "L1", string status = "IN_STOCK",
        string? receipt = "R1", DateTime? receiptDate = null, int startNo = 1, string productCode = "P1")
    {
        for (var i = 0; i < count; i++)
        {
            c.TbtStockUnit.Add(new TbtStockUnit
            {
                StockNumber = stockNumber, ProductCode = productCode, UnitNo = startNo + i, LocationCode = location,
                Status = status, ReceiptNumber = receipt, ReceiptDate = receiptDate, CreateBy = "t"
            });
        }
    }

    private static void AddMovement(JewelryContext c, string type, string stockNumber, decimal qty, string? refDocType = null, string? refDocNo = null,
        DateTime? date = null, string sku = "SKU1", string productCode = "P1")
    {
        c.TbtStockMovement.Add(new TbtStockMovement
        {
            MovementType = type, SkuCode = sku, StockNumber = stockNumber, ProductCode = productCode, Qty = qty,
            RefDocType = refDocType, RefDocNo = refDocNo, MovementDate = date ?? DateTime.UtcNow, CreateBy = "t"
        });
    }

    // ---------------------------------------------------------------- StockUnitSnapshot

    [Fact]
    public async Task Snapshot_TopUpUnitsUseReceiptDate_OthersUseProductionDate()
    {
        using var c = CreateContext();
        AddSku(c, "SKU1");
        AddLot(c, "S1", qty: 5, production: Utc("2020-01-01T00:00:00Z"));
        AddUnits(c, "S1", 3, receipt: "R1", receiptDate: Utc("2025-05-05T00:00:00Z"));
        AddUnits(c, "S1", 2, receipt: "R2", receiptDate: Utc("2026-10-01T00:00:00Z"), startNo: 4);
        await c.SaveChangesAsync();

        var buckets = await StockUnitSnapshot.LoadAsync(c);

        Assert.Equal(5, buckets.Sum(b => b.Pcs));
        Assert.Equal(3, buckets.Single(b => b.AgeDate == Utc("2020-01-01T00:00:00Z")).Pcs);
        Assert.Equal(2, buckets.Single(b => b.AgeDate == Utc("2026-10-01T00:00:00Z")).Pcs);
    }

    [Fact]
    public async Task Snapshot_LegacySilverTopUpReceipt_FallsBackToProductionDate()
    {
        using var c = CreateContext();
        AddSku(c, "SKU1");
        AddLot(c, "S1", qty: 2, production: Utc("2019-03-03T00:00:00Z"));
        AddUnits(c, "S1", 1, receipt: "R1", receiptDate: Utc("2025-05-05T00:00:00Z"));
        AddUnits(c, "S1", 1, receipt: "BATCH-TOPUP", receiptDate: Utc("2026-09-01T00:00:00Z"), startNo: 2);
        AddMovement(c, "RECEIPT", "S1", 1, "TRANSFER_SILVER_TOPUP", "BATCH-TOPUP");
        await c.SaveChangesAsync();

        var buckets = await StockUnitSnapshot.LoadAsync(c);

        Assert.All(buckets, b => Assert.Equal(Utc("2019-03-03T00:00:00Z"), b.AgeDate));
        Assert.Equal(2, buckets.Sum(b => b.Pcs));
    }

    [Fact]
    public async Task Snapshot_NoProductionDate_UsesUnitReceiptDate()
    {
        using var c = CreateContext();
        AddSku(c, "SKU1");
        AddLot(c, "S1", qty: 1, production: null);
        AddUnits(c, "S1", 1, receiptDate: Utc("2024-02-02T00:00:00Z"));
        await c.SaveChangesAsync();

        var buckets = await StockUnitSnapshot.LoadAsync(c);

        Assert.Equal(Utc("2024-02-02T00:00:00Z"), buckets.Single().AgeDate);
    }

    [Fact]
    public async Task Snapshot_LotWithoutUnits_IsSynthesizedFromQtyAndReserved()
    {
        using var c = CreateContext();
        AddSku(c, "SKU1");
        AddLot(c, "S1", qty: 3, reserved: 1, location: "L7", production: Utc("2020-01-01T00:00:00Z"));
        await c.SaveChangesAsync();

        var buckets = await StockUnitSnapshot.LoadAsync(c);

        Assert.Equal(2, buckets.Single(b => b.Status == "IN_STOCK").Pcs);
        Assert.Equal(1, buckets.Single(b => b.Status == "RESERVED").Pcs);
        Assert.All(buckets, b =>
        {
            Assert.Equal("L7", b.Location);
            Assert.Equal(Utc("2020-01-01T00:00:00Z"), b.AgeDate);
        });
    }

    [Fact]
    public async Task Snapshot_ExcludesSoldLotsAndSoldUnits_AndSplitsByLocation()
    {
        using var c = CreateContext();
        AddSku(c, "SKU1");
        AddLot(c, "S1", qty: 3, reserved: 1, production: Utc("2020-01-01T00:00:00Z"));
        AddUnits(c, "S1", 1, location: "L1");
        AddUnits(c, "S1", 1, location: "L2", startNo: 2);
        AddUnits(c, "S1", 1, location: "L2", status: "RESERVED", startNo: 3);
        AddUnits(c, "S1", 1, location: "L9", status: "SOLD", startNo: 4);
        AddLot(c, "S2", qty: 0, status: "SOLD", production: Utc("2020-01-01T00:00:00Z"));
        AddUnits(c, "S2", 1, status: "SOLD");
        await c.SaveChangesAsync();

        var buckets = await StockUnitSnapshot.LoadAsync(c);

        Assert.DoesNotContain(buckets, b => b.Lot.StockNumber == "S2");
        Assert.Equal(3, buckets.Sum(b => b.Pcs));
        Assert.Equal(1, buckets.Single(b => b.Location == "L1").Pcs);
        Assert.Equal(2, buckets.Where(b => b.Location == "L2").Sum(b => b.Pcs));
        Assert.DoesNotContain(buckets, b => b.Location == "L9");
    }

    // ---------------------------------------------------------------- StockAge / StockLedger

    [Fact]
    public void StockAge_CountsThaiCalendarDays()
    {
        var thaiJune2Midnight = Utc("2026-06-01T17:00:00Z");

        Assert.Equal(10, StockAge.DaysOld(thaiJune2Midnight, Utc("2026-06-12T16:59:00Z")));
        Assert.Equal(11, StockAge.DaysOld(thaiJune2Midnight, Utc("2026-06-12T17:00:00Z")));
        Assert.Equal(0, StockAge.DaysOld(thaiJune2Midnight, Utc("2026-06-02T16:59:00Z")));
    }

    [Fact]
    public void StockAgeLabel_UsesThaiCalendarDays()
    {
        var now = Utc("2026-09-01T00:00:00Z");

        Assert.Equal("<=90d", ExecutiveFlowNormalizer.StockAgeLabel(Utc("2026-06-02T17:00:00Z"), now));
        Assert.Equal("91-365d", ExecutiveFlowNormalizer.StockAgeLabel(Utc("2026-06-01T17:00:00Z"), now));
        Assert.Equal("Unknown", ExecutiveFlowNormalizer.StockAgeLabel(null, now));
    }

    [Fact]
    public void AgeBucketKey_ThaiMidnightBelongsToThaiDate()
    {
        var now = Utc("2026-10-21T00:00:00Z");

        Assert.Equal("lt1y", StockInsightCalculator.AgeBucketKey(Utc("2025-10-20T17:00:00Z"), now));
        Assert.Equal("y1to2", StockInsightCalculator.AgeBucketKey(Utc("2025-10-19T17:00:00Z"), now));
    }

    [Theory]
    [InlineData("RECEIPT", 1)]
    [InlineData("return", 1)]
    [InlineData("TRANSFORM_IN", 1)]
    [InlineData("SALE", -1)]
    [InlineData("TRANSFORM_OUT", -1)]
    [InlineData("RESERVE", 0)]
    [InlineData(null, 0)]
    public void StockLedger_QtySign(string? type, int expected)
    {
        Assert.Equal(expected, StockLedger.QtySign(type));
        Assert.Equal(expected, StockInsightCalculator.QtySign(type));
    }

    [Fact]
    public void StockLedger_NewStockReceiptExcludesLegacyTransfers()
    {
        Assert.True(StockLedger.IsNewStockReceipt("RECEIPT", "PRODUCTION"));
        Assert.True(StockLedger.IsNewStockReceipt("TRANSFORM_IN", "CONVERT"));
        Assert.False(StockLedger.IsNewStockReceipt("RECEIPT", "TRANSFER_9K"));
        Assert.False(StockLedger.IsNewStockReceipt("RECEIPT", "TRANSFER_SILVER_TOPUP"));
        Assert.False(StockLedger.IsNewStockReceipt("SALE", null));
    }

    // ---------------------------------------------------------------- ledger (reconciliation)

    [Fact]
    public async Task Ledger_ConvertedThenToppedUp_HasNoIssue()
    {
        using var c = CreateContext();
        AddLot(c, "S1", qty: 4);
        AddMovement(c, "RECEIPT", "S1", 3);
        AddMovement(c, "TRANSFORM_OUT", "S1", 1);
        AddMovement(c, "RECEIPT", "S1", 2);
        await c.SaveChangesAsync();

        var report = await new StockReconciliationService(c).CheckPieceLedgerAsync(CancellationToken.None);

        Assert.Equal(0, report.IssueCount);
    }

    [Fact]
    public async Task Ledger_TransformInOnlyLot_HasNoIssue()
    {
        using var c = CreateContext();
        AddLot(c, "S1", qty: 1);
        AddMovement(c, "TRANSFORM_IN", "S1", 1);
        await c.SaveChangesAsync();

        var report = await new StockReconciliationService(c).CheckPieceLedgerAsync(CancellationToken.None);

        Assert.Equal(0, report.IssueCount);
    }

    [Fact]
    public async Task Ledger_OpenConvertReservation_CountsAsExpectedReserved()
    {
        using var c = CreateContext();
        AddLot(c, "S1", qty: 1, reserved: 1);
        AddMovement(c, "RECEIPT", "S1", 1);
        c.TbtStockConvertHeader.Add(new TbtStockConvertHeader { Running = "CV1", Status = 0, CreateBy = "t" });
        c.TbtStockConvertItem.Add(new TbtStockConvertItem { HeaderRunning = "CV1", Role = "SOURCE", StockNumber = "S1", ProductCode = "P1", Qty = 1, CreateBy = "t" });
        await c.SaveChangesAsync();

        var report = await new StockReconciliationService(c).CheckPieceLedgerAsync(CancellationToken.None);

        Assert.Equal(0, report.IssueCount);
    }

    [Fact]
    public async Task Ledger_FlagsLedgerAndReservedMismatches_IgnoresPlaceholderSoLines()
    {
        using var c = CreateContext();
        AddLot(c, "S1", qty: 1, reserved: 0);
        AddMovement(c, "RECEIPT", "S1", 2);
        AddLot(c, "S2", qty: 1, reserved: 1);
        AddMovement(c, "RECEIPT", "S2", 1);
        c.TbtSaleOrderProduct.Add(new TbtSaleOrderProduct { Running = "L1", SoNumber = "SO1", StockNumber = "S2", Qty = 1, IsPlaceholder = true, CreateBy = "t" });
        await c.SaveChangesAsync();

        var report = await new StockReconciliationService(c).CheckPieceLedgerAsync(CancellationToken.None);

        Assert.Equal(2, report.IssueCount);
        Assert.Contains("LEDGER_MISMATCH", report.Items.Single(i => i.StockNumber == "S1").Issues);
        Assert.Contains("RESERVED_MISMATCH", report.Items.Single(i => i.StockNumber == "S2").Issues);
    }

    // ---------------------------------------------------------------- StockReport

    private static StockReportService CreateReport(JewelryContext c) => new StockReportService(c, new NoHttpContext());

    [Fact]
    public async Task StockReport_Summary_ReservedQtyCountsPartiallyReservedPieces()
    {
        using var c = CreateContext();
        AddSku(c, "SKU1");
        AddLot(c, "S1", qty: 3, reserved: 1, production: Utc("2024-01-01T00:00:00Z"));
        AddUnits(c, "S1", 2);
        AddUnits(c, "S1", 1, status: "RESERVED", startNo: 3);
        await c.SaveChangesAsync();

        var res = await CreateReport(c).Summary(new jewelry.Model.Stock.StockReport.Summary.Request());

        Assert.Equal(3, res.PieceQty);
        Assert.Equal(1, res.ReservedQty);
    }

    [Fact]
    public async Task StockReport_Aging_SplitsLotByPerPieceAgeDate()
    {
        using var c = CreateContext();
        AddSku(c, "SKU1");
        AddLot(c, "S1", qty: 5, production: DateTime.UtcNow.AddYears(-6));
        AddUnits(c, "S1", 3, receipt: "R1", receiptDate: DateTime.UtcNow.AddYears(-1));
        AddUnits(c, "S1", 2, receipt: "R2", receiptDate: DateTime.UtcNow.AddDays(-10), startNo: 4);
        await c.SaveChangesAsync();
        var service = CreateReport(c);

        var aging = await service.Aging(new jewelry.Model.Stock.StockReport.Aging.Request { ThresholdYears = 2 });

        Assert.Equal(3, aging.AgedQty);
        Assert.Equal(5, aging.TotalPieceQty);
        Assert.Equal(2, aging.Buckets.Single(b => b.Key == "lt6m").PieceQty);
        Assert.Equal(3, aging.Buckets.Single(b => b.Key == "gt5y").PieceQty);

        var items = await service.AgingItems(new jewelry.Model.Stock.StockReport.AgingItems.Request { ThresholdYears = 2 });
        Assert.Single(items.Data);
        Assert.Equal(3, items.Data[0].Qty);
        Assert.NotNull(items.Data[0].AgeDate);
    }

    // ---------------------------------------------------------------- Insight overview

    [Fact]
    public async Task Insight_Overview_KpiCountsPiecesAndWeightsCost()
    {
        using var c = CreateContext();
        AddSku(c, "SKU1");
        AddLot(c, "S1", qty: 4, cost: 100m, production: DateTime.UtcNow.AddMonths(-3));
        AddUnits(c, "S1", 4);
        AddLot(c, "S2", qty: 2, cost: null, production: DateTime.UtcNow.AddYears(-6));
        AddUnits(c, "S2", 2);
        AddLot(c, "S3", qty: 1, reserved: 1, cost: 50m, production: DateTime.UtcNow.AddMonths(-1));
        AddUnits(c, "S3", 1, status: "RESERVED");
        await c.SaveChangesAsync();
        var cache = new InsightCache(new MemoryCache(Options.Create(new MemoryCacheOptions())));
        var service = new StockInsightService(c, new NoHttpContext(), cache);

        var res = await service.Overview(new jewelry.Model.Stock.Insight.Overview.Request { Months = 3 });

        Assert.Equal(6, res.Kpi.InStockCount);
        Assert.Equal(6, res.Kpi.InStockQty);
        Assert.Equal(2, res.Kpi.InStockLotCount);
        Assert.Equal(1, res.Kpi.ReservedCount);
        Assert.Equal(7, res.Kpi.OnHandQty);
        Assert.Equal(2, res.Kpi.NoCostCount);
        Assert.Equal(1, res.Kpi.NoCostLotCount);
        Assert.Equal(400m, res.Kpi.CostThb);
        Assert.Equal(400m, res.Kpi.CostThbQty);
        Assert.Equal(2, res.Kpi.AgedOver5yCount);
        Assert.Equal(6, res.AgeBuckets.Sum(b => b.Count));
        Assert.Equal(6, res.ReceiptTypes.Sum(r => r.Count));
        Assert.Equal(400m, res.ReceiptTypes.Sum(r => r.CostThbQty));
        Assert.Equal(6, res.Categories.Sum(r => r.Count));
    }

    // ---------------------------------------------------------------- Flow zones

    private static ExecutiveFlowService CreateFlow(JewelryContext c)
        => new ExecutiveFlowService(c, new MemoryCache(Options.Create(new MemoryCacheOptions())), null!, null!, null!);

    [Fact]
    public async Task Flow_StockZone_WeightsEverythingByPiecesAndKeepsLensesReconciled()
    {
        using var c = CreateContext();
        AddSku(c, "SKU1", productionType: "Silver", size: "Silver", type: "RG");
        AddSku(c, "SKU2", productionType: "Yellow Gold", size: "18K", type: "NK");
        c.TbmStockLocation.Add(new TbmStockLocation { Code = "L1", NameTh = "คลัง1", CreateBy = "t" });
        c.TbmStockLocation.Add(new TbmStockLocation { Code = "L2", NameTh = "คลัง2", CreateBy = "t" });
        AddLot(c, "S1", qty: 10, sku: "SKU1", cost: 10m, production: DateTime.UtcNow.AddDays(-30));
        AddUnits(c, "S1", 6, location: "L1");
        AddUnits(c, "S1", 4, location: "L2", startNo: 7);
        AddLot(c, "S2", qty: 3, reserved: 1, sku: "SKU2", cost: null, production: DateTime.UtcNow.AddDays(-500));
        AddUnits(c, "S2", 2);
        AddUnits(c, "S2", 1, status: "RESERVED", startNo: 3);
        AddMovement(c, "SALE", "S1", 5);
        AddMovement(c, "RETURN", "S1", 1);
        await c.SaveChangesAsync();

        var res = await CreateFlow(c).FlowZone(new FlowZoneRequest { Zone = "stock" });
        var stock = res.Stock!;

        Assert.Equal(12, stock.Totals.InStock);
        Assert.Equal(1, stock.Totals.Reserved);
        Assert.Equal(4, stock.Totals.Sold);
        Assert.Equal(10, stock.Totals.CostRecordedPcs);
        Assert.Equal(2, stock.Totals.CostMissingPcs);
        Assert.Equal(2, stock.Totals.DesignsInStock);
        Assert.Equal(8, stock.Locations.Single(l => l.Code == "L1").Pcs);
        Assert.Equal(4, stock.Locations.Single(l => l.Code == "L2").Pcs);
        Assert.Equal(10, stock.Lenses.Age.Single(a => a.Label == "<=90d").Pcs);
        Assert.Equal(2, stock.Lenses.Age.Single(a => a.Label == ">365d").Pcs);
        Assert.True(stock.LensesReconcile);
    }

    [Fact]
    public async Task Flow_ReceiveZone_NewStockSumsReceiptAndTransformInQtyExcludingLegacyTransfers()
    {
        using var c = CreateContext();
        AddSku(c, "SKU1");
        AddLot(c, "S1", qty: 1);
        AddMovement(c, "RECEIPT", "S1", 40, "PRODUCTION", "R1");
        AddMovement(c, "TRANSFORM_IN", "S1", 2, "CONVERT", "CV1");
        AddMovement(c, "RECEIPT", "S1", 14150, "TRANSFER_9K", "T1");
        AddMovement(c, "RECEIPT", "S1", 30, "TRANSFER_SILVER_TOPUP", "T2");
        AddMovement(c, "SALE", "S1", 5);
        await c.SaveChangesAsync();

        var res = await CreateFlow(c).FlowZone(new FlowZoneRequest { Zone = "receive" });

        Assert.Equal(42, res.Receive!.ReceivedThisMonthPieces);
        Assert.Equal(42, res.Receive.Months.Last().NewStockPieces);
    }
}
