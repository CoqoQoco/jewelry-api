using System;
using System.Collections.Generic;

namespace jewelry.Model.Report.Executive.Summary
{
    public class Response
    {
        public DateTime AsOf { get; set; }
        public ProductionData Production { get; set; } = new ProductionData();
        public ReceivablesData Receivables { get; set; } = new ReceivablesData();
        public SalesOrdersData SalesOrders { get; set; } = new SalesOrdersData();
        public StockData Stock { get; set; } = new StockData();

        // metal ที่ขอ: 'ALL' | 'GOLD' | 'SILVER'
        public string Metal { get; set; } = "ALL";

        // โลหะของ goldLoss: ALL/GOLD -> 'GOLD', SILVER -> 'SILVER' (ตัวเดียวที่ขอ)
        public string GoldLossMetal { get; set; } = "GOLD";

        public List<GoldLossMonthData> GoldLoss { get; set; } = new List<GoldLossMonthData>();

        // มีค่าเฉพาะ metal=ALL (ใบ tang ที่จัดเป็นเงิน) — GOLD/SILVER = ว่าง
        public List<GoldLossMonthData> SilverLoss { get; set; } = new List<GoldLossMonthData>();
    }

    public class ProductionData
    {
        public int OpenCount { get; set; }
        public int Moved30dCount { get; set; }
        public int Stale180dCount { get; set; }
        public int MeltedOpenCount { get; set; }
    }

    public class ReceivablesData
    {
        public int InvoiceCount { get; set; }
        public decimal InvoiceTotalThb { get; set; }
        public decimal PaidOrPartialThb { get; set; }
        public int UnpaidCount { get; set; }
        public decimal UnpaidThb { get; set; }
        public int OverdueCount { get; set; }
        public decimal OverdueThb { get; set; }
        public decimal UnpaidNotOverdueThb { get; set; }
        public int NoDueDateCount { get; set; }
    }

    public class SalesOrdersData
    {
        public int OpenCount { get; set; }
        public int NoInvoiceCount { get; set; }
        public decimal NoInvoiceThb { get; set; }
        public int OverdueNoInvoiceCount { get; set; }
        public int NoDeliveryDateCount { get; set; }
    }

    public class StockData
    {
        // TK202610290001: นับเป็นชิ้น (tbt_stock_unit) ไม่ใช่ล็อต — InStockCount = ชิ้น IN_STOCK (พร้อมขาย ไม่รวมจอง)
        public int InStockCount { get; set; }
        // จำนวนล็อตที่มีชิ้น IN_STOCK / ล็อตที่ไม่มีต้นทุน (ใช้เป็นจำนวนรายการที่ต้องแก้ใน DataQuality)
        public int InStockLotCount { get; set; }
        public int NoCostLotCount { get; set; }
        // ชิ้นที่จองแล้ว (RESERVED)
        public int ReservedCount { get; set; }
        public int NoCostCount { get; set; }
        // มูลค่า = Σ ต้นทุนต่อชิ้น x จำนวนชิ้น
        public decimal CostThb { get; set; }
        public int AgedOver1yCount { get; set; }

        // เท่ากับ InStockCount / CostThb (คงไว้เพื่อ compat)
        public decimal InStockQty { get; set; }
        public decimal CostThbQty { get; set; }
    }

    public class GoldLossMonthData
    {
        public string Month { get; set; } = null!;
        public int SlipCount { get; set; }
        public decimal IssuedGram { get; set; }
        public decimal RawLossGram { get; set; }
        public decimal AllowedGram { get; set; }
        public decimal OverAllowedGram { get; set; }
        public int OverSlipCount { get; set; }
        public decimal LossPercent { get; set; }
        public decimal AllowedPercent { get; set; }
        public decimal OverAllowedPercent { get; set; }
    }
}
