using System;
using System.Collections.Generic;
using Finding = jewelry.Model.Production.Insight.Wip.Finding;
using ActionItem = jewelry.Model.Production.Insight.Wip.ActionItem;

namespace jewelry.Model.Stock.Insight.Overview
{
    public class Response
    {
        public DateTime AsOf { get; set; }
        public string Metal { get; set; } = "ALL";

        // 'critical' | 'warning' | 'ok' — severity ที่แย่ที่สุดใน problems+forecasts
        public string Status { get; set; } = "ok";

        public ThresholdValues Thresholds { get; set; } = new ThresholdValues();
        public SalesHistory SalesHistory { get; set; } = new SalesHistory();
        public Kpi Kpi { get; set; } = new Kpi();
        public List<AgeBucket> AgeBuckets { get; set; } = new List<AgeBucket>();
        public List<ReceiptTypeRow> ReceiptTypes { get; set; } = new List<ReceiptTypeRow>();
        public List<CategoryRow> Categories { get; set; } = new List<CategoryRow>();
        public List<SeriesPoint> Series { get; set; } = new List<SeriesPoint>();

        // ใช้ shape เดียวกับ ProductionInsight (Production.Insight.Wip.Finding / ActionItem)
        public List<Finding> Problems { get; set; } = new List<Finding>();
        public List<Finding> Forecasts { get; set; } = new List<Finding>();
        public List<ActionItem> Actions { get; set; } = new List<ActionItem>();
    }

    // ค่า threshold ที่ใช้จริง (หลัง resolve override + clamp)
    public class ThresholdValues
    {
        public decimal NoCostWarnPercent { get; set; }
        public decimal NoCostCriticalPercent { get; set; }
        public decimal Aged5yWarnPercent { get; set; }
        public decimal Aged5yCriticalPercent { get; set; }
        public int NoOutflowMonths { get; set; }
        public decimal CoverMonthsLow { get; set; }
        public decimal CoverMonthsHigh { get; set; }
        public int CoverWindowMonths { get; set; }
        public int AgingProjectionMonths { get; set; }
        public decimal AgingRisePointsWarn { get; set; }
        public int MinHistoryMonths { get; set; }
    }

    // ประวัติขายจริง (SALE ที่ RefDocType = INVOICE) — ใช้ตัดสินว่าข้อมูลพอสำหรับ forecast หรือไม่
    public class SalesHistory
    {
        // วันแรกของเดือนที่มี SALE(INVOICE) ครั้งแรก (เวลาไทย) yyyy-MM-dd หรือ null ถ้ายังไม่มี
        public string? StartMonth { get; set; }

        // จำนวนเดือนเต็มนับจาก startMonth ถึงก่อนเดือนปัจจุบัน
        public int FullMonths { get; set; }
    }

    public class Kpi
    {
        // สต็อกพร้อมขาย = piece status IN_STOCK (เหมือน StockHealth)
        public int InStockCount { get; set; }
        public decimal InStockQty { get; set; }

        // ยอดคงเหลือรวมที่จองแล้วด้วย (qty > 0, IN_STOCK + RESERVED) — ฐานของ series.endStockQty
        public decimal OnHandQty { get; set; }

        public int NoCostCount { get; set; }
        public decimal NoCostPercent { get; set; }
        public decimal CostThb { get; set; }
        public decimal CostThbQty { get; set; }

        // อายุนับจาก production_date (production_date ว่าง = gt5y เหมือน StockHealth)
        public int AgedOver1yCount { get; set; }
        public decimal AgedOver1yPercent { get; set; }
        public int AgedOver5yCount { get; set; }
        public decimal AgedOver5yPercent { get; set; }
        public int NoProductionDateCount { get; set; }
    }

    public class AgeBucket
    {
        // lt1y | y1to2 | y2to5 | gt5y
        public string Key { get; set; } = null!;
        public int Count { get; set; }
        public int NoCostCount { get; set; }
        public decimal CostThb { get; set; }
        public decimal Qty { get; set; }
        public decimal CostThbQty { get; set; }
    }

    public class ReceiptTypeRow
    {
        public string? ReceiptType { get; set; }
        public int Count { get; set; }
        public int NoCostCount { get; set; }
        public decimal Qty { get; set; }
        public decimal CostThb { get; set; }
    }

    public class CategoryRow
    {
        // tbt_sku.product_type (รหัส, "" = ไม่ระบุ) / ชื่อไทย tbt_sku.product_type_name
        public string Category { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Qty { get; set; }
        public decimal CostThb { get; set; }
        public int NoCostCount { get; set; }
        public int AgedOver1yCount { get; set; }
    }

    public class SeriesPoint
    {
        // วันแรกของเดือน (เวลาไทย) รูปแบบ yyyy-MM-dd
        public string Month { get; set; } = null!;

        // รับของใหม่ = RECEIPT ที่ไม่ใช่การย้ายสต็อกระบบเก่า (RefDocType ขึ้นต้น TRANSFER_)
        public decimal InflowQty { get; set; }

        // ขายออกสุทธิ = max(saleQty - returnQty, 0) (ลบบิลแล้วออกใหม่ถูกหักกลบ)
        public decimal OutflowQty { get; set; }

        // SALE รวม (gross)
        public decimal SaleQty { get; set; }

        // RETURN ที่เกิดจากลบบิล (RefDocType INVOICE_DELETE)
        public decimal ReturnQty { get; set; }

        // RECEIPT ที่มาจากการย้ายสต็อกระบบเก่า (RefDocType TRANSFER_9K/14K/18K/SILVER) — แยกไว้ไม่รวมใน inflowQty
        public decimal LegacyImportQty { get; set; }

        // RETURN อื่นที่ไม่ใช่ลบบิล + TRANSFORM_IN - TRANSFORM_OUT (แปลงสินค้า) — ไม่รวมใน inflow/outflow แต่กระทบยอดคงเหลือ
        public decimal OtherNetQty { get; set; }

        // ยอดคงเหลือสิ้นเดือน (ย้อนคำนวณจากยอดปัจจุบัน) — เดือนล่าสุด = ยอดปัจจุบัน
        public decimal EndStockQty { get; set; }
    }
}
