using System;
using System.Collections.Generic;

namespace jewelry.Model.Report.Executive.Flow
{
    // มี section เดียวที่ไม่ null ตรงกับ Zone ที่ขอ
    public class FlowZoneResponse
    {
        public string Zone { get; set; } = null!;
        public DateTimeOffset AsOf { get; set; }
        public FlowMoldZone? Mold { get; set; }
        public FlowProductionZone? Production { get; set; }
        public FlowReceiveZone? Receive { get; set; }
        public FlowStockZone? Stock { get; set; }
        public FlowMaterialZone? Material { get; set; }
        public FlowSalesZone? Sales { get; set; }
    }

    // ---------------- mold ----------------

    public class FlowMoldZone
    {
        public List<FlowMoldStage> Stages { get; set; } = new List<FlowMoldStage>();
        public FlowMoldCatalog Catalog { get; set; } = new FlowMoldCatalog();
    }

    public class FlowMoldStage
    {
        public int Status { get; set; }

        // Design | Resin | CastingSilver | Casting | Cutting | Store | Melted
        public string Name { get; set; } = null!;
        public bool IsTerminal { get; set; }
        public int Open { get; set; }

        // update_date (fallback create_date) เก่ากว่า 180 วัน
        public int Stale180 { get; set; }
        public int Within14d { get; set; }
        public double? MedianDays { get; set; }
        public double? MaxDays { get; set; }
        public int NewProcess { get; set; }
    }

    public class FlowMoldCatalog
    {
        public int ActiveMolds { get; set; }
        public List<FlowMoldCategory> Categories { get; set; } = new List<FlowMoldCategory>();
    }

    public class FlowMoldCategory
    {
        public string Category { get; set; } = null!;
        public int Molds { get; set; }
        public int New90d { get; set; }
    }

    // ---------------- production ----------------

    public class FlowProductionZone
    {
        public List<FlowProductionDept> Departments { get; set; } = new List<FlowProductionDept>();
        public FlowProductionTotals Totals { get; set; } = new FlowProductionTotals();
        public FlowProductionDone Done { get; set; } = new FlowProductionDone();
        public List<FlowLossVsTarget> LossVsTarget { get; set; } = new List<FlowLossVsTarget>();
        public FlowCastCost CastCost { get; set; } = new FlowCastCost();

        // ผลรวมของทุก dim ในแต่ละแผนก = openPcs/openPlans ของแผนกนั้น
        public bool LensesReconcile { get; set; }
    }

    public class FlowProductionTotals
    {
        public int OpenPlans { get; set; }
        public int OpenPcs { get; set; }
        public int StalePlans { get; set; }
        public int StalePcs { get; set; }
        public int OverduePcs { get; set; }
    }

    public class FlowProductionDone
    {
        public int CreatedThisMonthPcs { get; set; }
        public int CompletedThisMonthPcs { get; set; }
        public int MeltedPcs { get; set; }
        public int DonePcs { get; set; }
    }

    public class FlowProductionDept
    {
        // design | trim | rawPolish | gemSort | setting | plating | costCard
        public string Key { get; set; } = null!;
        public int OpenPlans { get; set; }
        public int OpenPcs { get; set; }
        public int WaitPcs { get; set; }
        public int WorkPcs { get; set; }

        // อายุนับจากการเคลื่อนไหวล่าสุด: [<=14, 15-60, 61-179, >=180] วัน
        public int[] AgePcs { get; set; } = new int[4];
        public int[] AgePlans { get; set; } = new int[4];
        public int OverduePcs { get; set; }
        public double? MedianDays { get; set; }
        public double? MaxDays { get; set; }

        // null สำหรับ design / costCard (ไม่มีสถานะ "ทำงานกับช่าง")
        public FlowDeptWorkers? Workers { get; set; }
        public FlowDims Dims { get; set; } = new FlowDims();

        // null สำหรับ design / costCard
        public FlowDeptGoldLoss? GoldLoss { get; set; }
    }

    public class FlowDeptWorkers
    {
        public int NamedPlans { get; set; }
        public int QueuePlans { get; set; }
        public int NonePlans { get; set; }
        public int TestPlans { get; set; }
        public List<FlowWorkerPlans> Names { get; set; } = new List<FlowWorkerPlans>();
    }

    public class FlowWorkerPlans
    {
        public string Name { get; set; } = null!;
        public int Plans { get; set; }
    }

    public class FlowDims
    {
        // YG | WG | PG | SV | Unknown
        public List<FlowDimItem> Gold { get; set; } = new List<FlowDimItem>();

        // 9K | 10K | 14K | 18K | Silver | Unknown
        public List<FlowDimItem> Size { get; set; } = new List<FlowDimItem>();

        // plan.product_type code
        public List<FlowDimItem> Ptype { get; set; } = new List<FlowDimItem>();
    }

    public class FlowDeptGoldLoss
    {
        // เฉพาะแถวที่ "คืนแล้ว" (gold_weight_check มีค่า)
        public FlowLossBlock Win90 { get; set; } = new FlowLossBlock();
        public FlowLossBlock AllTime { get; set; } = new FlowLossBlock();
        public FlowPendingBlock Pending { get; set; } = new FlowPendingBlock();

        // gemSort: ระบบไม่ชั่งจริง (check = send) ห้ามอ่านเป็น loss
        public bool NotWeighed { get; set; }

        // trim: ส่วนต่างปนเศษทองที่คืนเข้าหลอม
        public bool IncludesScrap { get; set; }
        public FlowGoldLossDims Dims { get; set; } = new FlowGoldLossDims();
    }

    public class FlowLossBlock
    {
        public int Rows { get; set; }
        public decimal SendG { get; set; }
        public decimal CheckG { get; set; }
    }

    public class FlowPendingBlock
    {
        public int Rows { get; set; }
        public decimal G { get; set; }
        public int QueueRows { get; set; }
        public decimal QueueG { get; set; }
        public int WorkerRows { get; set; }
        public decimal WorkerG { get; set; }
    }

    public class FlowGoldLossDims
    {
        // label = detail.gold code
        public List<FlowGoldLossDimItem> Gold { get; set; } = new List<FlowGoldLossDimItem>();
        public List<FlowGoldLossDimItem> Size { get; set; } = new List<FlowGoldLossDimItem>();
        public List<FlowGoldLossDimItem> Ptype { get; set; } = new List<FlowGoldLossDimItem>();
    }

    public class FlowGoldLossDimItem
    {
        public string Label { get; set; } = null!;
        public int Rows90 { get; set; }
        public decimal SendG90 { get; set; }
        public decimal CheckG90 { get; set; }
        public decimal PendingG { get; set; }
    }

    public class FlowLossVsTarget
    {
        // rawPolish | setting | plating
        public string Dept { get; set; } = null!;

        // GOLD | SILVER
        public string Metal { get; set; } = null!;
        public decimal? TargetPercent { get; set; }
        public bool IsSystemDefault { get; set; }
        public List<FlowLossRow> Rows { get; set; } = new List<FlowLossRow>();
    }

    public class FlowLossRow
    {
        // แถวแรก = ยอดรวมแผนก (IsTotal = true) ตามด้วยช่างที่ส่ง >= 250 g
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public bool IsTotal { get; set; }
        public int Rows { get; set; }
        public decimal SendG { get; set; }
        public decimal? LossPercent { get; set; }
        public decimal? DeltaPp { get; set; }
        public decimal ExcessG { get; set; }
    }

    public class FlowCastCost
    {
        public FlowCastBlock Win90 { get; set; } = new FlowCastBlock();
        public List<FlowCastGoldBlock> ByGold90 { get; set; } = new List<FlowCastGoldBlock>();
        public FlowCastBlock AllTime { get; set; } = new FlowCastBlock();
        public string Note { get; set; } = null!;
    }

    public class FlowCastBlock
    {
        public int Rows { get; set; }
        public decimal CastWeightG { get; set; }
        public decimal CastLossG { get; set; }
        public decimal MeltWeightG { get; set; }
        public decimal MeltLossG { get; set; }
    }

    public class FlowCastGoldBlock : FlowCastBlock
    {
        public string Gold { get; set; } = null!;
    }

    // ---------------- receive ----------------

    public class FlowReceiveZone
    {
        public int NotReceivedPcs { get; set; }
        public int NotReceivedJobs { get; set; }
        public int ReceivedPcs { get; set; }
        public int ReceivedJobs { get; set; }
        public int CompletedThisMonthPcs { get; set; }
        public int CompletedThisMonthJobs { get; set; }
        public int ReceivedThisMonthPieces { get; set; }
        public List<FlowReceiveMonth> Months { get; set; } = new List<FlowReceiveMonth>();
    }

    public class FlowReceiveMonth
    {
        public string Month { get; set; } = null!;
        public int NewStockPieces { get; set; }
    }

    // ---------------- stock ----------------

    public class FlowStockZone
    {
        public FlowStockTotals Totals { get; set; } = new FlowStockTotals();
        public List<FlowStockLocation> Locations { get; set; } = new List<FlowStockLocation>();
        public FlowStockLenses Lenses { get; set; } = new FlowStockLenses();
        public List<FlowStockMatrixCell> Matrix { get; set; } = new List<FlowStockMatrixCell>();

        // ผลรวมทุก lens + matrix = totals.inStock
        public bool LensesReconcile { get; set; }
    }

    // TK202610290001: ทุกตัวเลขโซนสต็อกเป็นชิ้น (tbt_stock_unit) ไม่ใช่ล็อต
    public class FlowStockTotals
    {
        // ชิ้นพร้อมขาย (IN_STOCK) · InStock + Reserved = ของในมือทั้งหมด
        public int InStock { get; set; }
        // ชิ้นที่จองแล้ว (RESERVED)
        public int Reserved { get; set; }
        // ขายแล้ว = Σ SALE - Σ RETURN ของ movement (ชิ้น)
        public int Sold { get; set; }
        public int CostRecordedPcs { get; set; }
        public int CostMissingPcs { get; set; }
        public int DesignsInStock { get; set; }
    }

    public class FlowStockLocation
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsSalesPoint { get; set; }
        public int Pcs { get; set; }
    }

    public class FlowStockLenses
    {
        // top 3 + "Other N" (N = จำนวนคลังที่รวม)
        public List<FlowLabelPcs> Sloc { get; set; } = new List<FlowLabelPcs>();

        // Yellow | White | Pink | Silver | Unknown
        public List<FlowLabelPcs> Gold { get; set; } = new List<FlowLabelPcs>();

        // 9K | 10K | 14K | 18K | Silver | Unknown
        public List<FlowLabelPcs> Size { get; set; } = new List<FlowLabelPcs>();

        // top 5 product_type + "Other"
        public List<FlowLabelPcs> Type { get; set; } = new List<FlowLabelPcs>();

        // <=90d | 91-365d | >365d | Unknown (production_date ว่าง)
        public List<FlowLabelPcs> Age { get; set; } = new List<FlowLabelPcs>();
    }

    public class FlowStockMatrixCell
    {
        public string Gold { get; set; } = null!;
        public string Size { get; set; } = null!;
        public int Pcs { get; set; }
    }

    // ---------------- material ----------------

    public class FlowMaterialZone
    {
        public List<FlowGoldStockRow> GoldRows { get; set; } = new List<FlowGoldStockRow>();
        public List<FlowGoldTxRow> GoldTx30d { get; set; } = new List<FlowGoldTxRow>();
        public List<FlowGemGroup> GemGroups { get; set; } = new List<FlowGemGroup>();
        public FlowMaterialTotals Totals { get; set; } = new FlowMaterialTotals();
    }

    public class FlowGoldStockRow
    {
        public string Code { get; set; } = null!;
        public string SizeCode { get; set; } = null!;
        public string? SizeName { get; set; }
        public decimal Weight { get; set; }
        public decimal WeightOnProcess { get; set; }
    }

    public class FlowGoldTxRow
    {
        public string GoldCode { get; set; } = null!;

        // ความหมายของ type ยังไม่ยืนยัน คืนค่าดิบ
        public int Type { get; set; }
        public int Rows { get; set; }
        public decimal Weight { get; set; }
    }

    public class FlowGemGroup
    {
        public string Group { get; set; } = null!;
        public int Rows { get; set; }
        public int RowsWithStock { get; set; }

        // หน่วย K และ Q ห้ามรวมกัน
        public decimal QtyK { get; set; }
        public decimal QtyQ { get; set; }
    }

    public class FlowMaterialTotals
    {
        public int GemRows { get; set; }
        public int GemRowsWithStock { get; set; }
    }

    // ---------------- sales ----------------

    public class FlowSalesZone
    {
        public List<FlowSalesMonth> Months { get; set; } = new List<FlowSalesMonth>();
        public List<FlowSalesChannel> Channels { get; set; } = new List<FlowSalesChannel>();
        public List<FlowCustomerShare> Customers { get; set; } = new List<FlowCustomerShare>();
        public List<FlowSalePerson> SalePersons { get; set; } = new List<FlowSalePerson>();
        public FlowSalesTotals Totals { get; set; } = new FlowSalesTotals();
        public bool UsdAmountsHeld { get; set; } = true;
        public bool PaymentHeld { get; set; } = true;
    }

    public class FlowSalesMonth
    {
        public string Month { get; set; } = null!;
        public int Quotes { get; set; }
        public int SoThbCount { get; set; }
        public decimal SoThbAmount { get; set; }
        public int SoUsdCount { get; set; }
        public int InvThbCount { get; set; }
        public decimal InvThbAmount { get; set; }
        public int InvUsdCount { get; set; }
    }

    public class FlowSalesChannel
    {
        // null/ว่าง = "(none)"
        public string Code { get; set; } = null!;
        public int ThbInvoices { get; set; }
        public decimal ThbAmount { get; set; }
        public int UsdInvoices { get; set; }
    }

    public class FlowSalePerson
    {
        public string Name { get; set; } = null!;
        public int Invoices { get; set; }
        public decimal Thb { get; set; }
    }

    public class FlowSalesTotals
    {
        public int Quotations { get; set; }
        public int SalesOrders { get; set; }
        public int Invoices { get; set; }
    }
}
