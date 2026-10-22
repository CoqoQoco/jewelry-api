using System;
using System.Collections.Generic;
using Finding = jewelry.Model.Production.Insight.Wip.Finding;
using ActionItem = jewelry.Model.Production.Insight.Wip.ActionItem;

namespace jewelry.Model.Sale.Insight.Overview
{
    public class Response
    {
        public DateTime AsOf { get; set; }
        public string Metal { get; set; } = "ALL";

        // 'critical' | 'warning' | 'ok' — severity ที่แย่ที่สุดใน problems+forecasts (info ไม่นับ)
        public string Status { get; set; } = "ok";

        public ThresholdValues Thresholds { get; set; } = new ThresholdValues();
        public Kpi Kpi { get; set; } = new Kpi();

        // 3 slice เดียวกับโดนัทเดิม: received (รับแล้ว/บางส่วน) | unpaidNotDue (ยังไม่ครบกำหนด/ไม่มีวันครบกำหนด) | overdue
        public List<PaymentState> PaymentStates { get; set; } = new List<PaymentState>();

        // บิลค้างชำระ (unpaid) แยกตามอายุเลยกำหนด: notDue | d1_30 | d31_60 | d61_90 | gt90
        public List<AgingBucket> Aging { get; set; } = new List<AgingBucket>();

        public List<TopCustomer> TopCustomers { get; set; } = new List<TopCustomer>();
        public Collection Collection { get; set; } = new Collection();
        public List<SeriesPoint> Series { get; set; } = new List<SeriesPoint>();
        public SalesHistory SalesHistory { get; set; } = new SalesHistory();

        // ใช้ shape เดียวกับ ProductionInsight (Production.Insight.Wip.Finding / ActionItem)
        public List<Finding> Problems { get; set; } = new List<Finding>();
        public List<Finding> Forecasts { get; set; } = new List<Finding>();
        public List<ActionItem> Actions { get; set; } = new List<ActionItem>();
    }

    // ค่า threshold ที่ใช้จริง (หลัง resolve override + clamp)
    public class ThresholdValues
    {
        public decimal OverdueWarnPercent { get; set; }
        public decimal OverdueCriticalPercent { get; set; }
        public int OldestOverdueDays { get; set; }
        public decimal NoDueDateWarnPercent { get; set; }
        public decimal ConcentrationPercent { get; set; }
        public int CollectionWindowDays { get; set; }
        public int BecomingOverdueDays { get; set; }
        public int SoDeliveryDays { get; set; }

        // สำรองไว้ (ยังไม่มีกฎ trend ที่ใช้) — คืนค่าเพื่อให้ UI ใช้ร่วมกับ salesHistory
        public int MinHistoryMonths { get; set; }
    }

    public class Kpi
    {
        public int TotalInvoiceCount { get; set; }
        public decimal TotalGrandThb { get; set; }

        // ค้างชำระ = บิล unpaid (strict: ยอด>0, ไม่มี payment_item, ไม่มีมัดจำ) เต็มจำนวน (ไม่หักบางส่วน) — เหมือน Summary.unpaid
        public int OutstandingCount { get; set; }
        public decimal OutstandingThb { get; set; }
        public int OverdueCount { get; set; }
        public decimal OverdueThb { get; set; }

        // % ของ outstandingThb
        public decimal OverduePercent { get; set; }

        // % ของ totalGrandThb ที่ไม่ใช่ unpaid (รับแล้ว/บางส่วน)
        public decimal PaidPercent { get; set; }

        // บิล unpaid ที่ไม่มี due date / % ของบิล unpaid
        public int NoDueDateCount { get; set; }
        public decimal NoDueDatePercent { get; set; }

        // ทุกบิลในขอบเขตที่ไม่มี due date (ตรงกับ Summary.noDueDateCount / filter 'noduedate')
        public int NoDueDateAllCount { get; set; }

        public int? OldestOverdueDays { get; set; }

        // ลูกค้ารายใหญ่สุดของยอดค้าง (% ของ outstandingThb) — null ถ้าไม่มียอดค้าง
        public decimal? TopCustomerPercent { get; set; }

        public int SoNotInvoicedCount { get; set; }
        public decimal SoNotInvoicedThb { get; set; }
        public int SoDeliveryOverdueCount { get; set; }
        public int SoNoDeliveryDateCount { get; set; }
    }

    public class PaymentState
    {
        // received | unpaidNotDue | overdue
        public string Key { get; set; } = null!;
        public int Count { get; set; }
        public decimal AmountThb { get; set; }
    }

    public class AgingBucket
    {
        // notDue | d1_30 | d31_60 | d61_90 | gt90
        public string Key { get; set; } = null!;
        public int Count { get; set; }
        public decimal AmountThb { get; set; }
    }

    public class TopCustomer
    {
        public string? CustomerCode { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal OutstandingThb { get; set; }
        public decimal OverdueThb { get; set; }
    }

    public class Collection
    {
        // บิลค้างที่ครบกำหนดภายใน collectionWindowDays วันข้างหน้า (due >= now)
        public decimal DueNextWindowThb { get; set; }
        public int DueNextWindowCount { get; set; }

        // บิลค้างที่จะเลยกำหนดภายใน becomingOverdueDays วัน
        public decimal BecomingOverdueThb { get; set; }
        public int BecomingOverdueCount { get; set; }

        // SO ที่ยังไม่ออกบิล และกำหนดส่งภายใน soDeliveryDays วัน
        public int SoDeliveryDueCount { get; set; }
        public decimal SoDeliveryDueThb { get; set; }
    }

    public class SeriesPoint
    {
        // วันแรกของเดือน (เวลาไทย) yyyy-MM-dd
        public string Month { get; set; } = null!;
        public int InvoiceCount { get; set; }
        public decimal GrandTotalThb { get; set; }
    }

    public class SalesHistory
    {
        // วันแรกของเดือนที่มีใบแจ้งหนี้ครั้งแรก (เวลาไทย) yyyy-MM-dd หรือ null
        public string? StartMonth { get; set; }

        // จำนวนเดือนเต็มนับจาก startMonth ถึงก่อนเดือนปัจจุบัน
        public int FullMonths { get; set; }
    }
}
