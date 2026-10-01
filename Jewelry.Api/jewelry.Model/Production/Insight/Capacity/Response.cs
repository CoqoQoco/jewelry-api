using System;
using System.Collections.Generic;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace jewelry.Model.Production.Insight.Capacity
{
    public class Response
    {
        public DateTime AsOf { get; set; }

        // 'critical' | 'warning' | 'ok'
        public string Status { get; set; } = "ok";

        public List<Wip.Finding> Problems { get; set; } = new List<Wip.Finding>();
        public List<Wip.Finding> Forecasts { get; set; } = new List<Wip.Finding>();
        public List<Wip.ActionItem> Actions { get; set; } = new List<Wip.ActionItem>();

        public KpiData Kpi { get; set; } = new KpiData();
        public List<SeriesItem> Series { get; set; } = new List<SeriesItem>();
        public List<DepartmentItem> Departments { get; set; } = new List<DepartmentItem>();
        public CostCardToDoneData CostCardToDone { get; set; } = new CostCardToDoneData();
    }

    public class KpiData
    {
        public decimal InflowPerMonth { get; set; }
        public decimal InflowPiecesPerMonth { get; set; }

        // first entry เข้าสถานะบัตรต้นทุน (95) — "ผลิตเสร็จ"
        public decimal OutputPerMonth { get; set; }

        // done date (transfer แรกเข้า 100 หรือ fallback completed_date) — "เสร็จสมบูรณ์" (รวม paperwork)
        public decimal CompletedPerMonth { get; set; }

        // inflowPerMonth - outputPerMonth (อัตราการโตของ WIP สายการผลิต — ไม่รวม lag หลังบัตรต้นทุน)
        public decimal NetPerMonth { get; set; }

        // snapshot ตอนนี้ (ไม่ใช่ค่าเฉลี่ยในช่วง) — แผนเปิดที่ไม่ stale (นิยามเดียวกับ WIP_STALE)
        public int ActiveWip { get; set; }

        // activeWip ÷ outputPerMonth — null ถ้า outputPerMonth = 0
        public decimal? BacklogMonths { get; set; }

        public int StaleWip { get; set; }

        // top 2 แผนกคิวยาวสุด (queueDays) เรียงมากไปน้อย
        public List<string> BottleneckDepts { get; set; } = new List<string>();

        // จำนวนเดือนในช่วงที่ inflow > output
        public int OverloadMonths { get; set; }

        public int MonthsInRange { get; set; }
    }

    public class SeriesItem
    {
        public DateTime BucketEnd { get; set; }
        public int Inflow { get; set; }
        public int InflowPieces { get; set; }
        public int Output { get; set; }
        public int Completed { get; set; }
        public int Melted { get; set; }

        // null ถ้า bucket อยู่ในอนาคต
        public int? ActiveWipEnd { get; set; }
    }

    public class DepartmentItem
    {
        public string Key { get; set; } = null!;
        public decimal ExitsPerMonth { get; set; }

        // จำนวน exit (visit จบ) ต่อเดือนของแผนกนี้ — bucket เดียวกับ series ระดับบน — 0 ถ้าไม่มี exit เดือนนั้น
        public List<ExitsSeriesPoint> ExitsSeries { get; set; } = new List<ExitsSeriesPoint>();

        // median ของจำนวนช่าง distinct ต่อ bucket ในช่วง — null ถ้าไม่มี bucket เลย (ไม่ควรเกิด)
        public double? WorkersMedian { get; set; }
        public List<WorkersSeriesPoint> WorkersSeries { get; set; } = new List<WorkersSeriesPoint>();

        // exitsPerMonth ÷ workersMedian — null ถ้า workersMedian เป็น null หรือ 0 (เช่นบัตรต้นทุนที่ไม่มีช่างผูกไว้)
        public decimal? PlansPerWorker { get; set; }

        public int WaitingNow { get; set; }
        public int WorkingNow { get; set; }
        public int ActiveWip { get; set; }

        // activeWip ÷ exitedPerDay (ปัจจุบัน) — null ถ้าไม่มี exit เลยในช่วง (คำนวณคิวไม่ได้)
        public double? QueueDays { get; set; }

        public bool IsBottleneck { get; set; }
    }

    public class WorkersSeriesPoint
    {
        public DateTime BucketEnd { get; set; }
        public int Workers { get; set; }
    }

    public class ExitsSeriesPoint
    {
        public DateTime BucketEnd { get; set; }
        public int Exits { get; set; }
    }

    public class CostCardToDoneData
    {
        public double? MedianDays { get; set; }
        public double? P90Days { get; set; }

        // เข้า 95 แล้ว ยังไม่มี done date สถานะปัจจุบันไม่ใช่ 100/500/84/85 — ทั้งหมด (รวม stale)
        public int PendingNow { get; set; }

        // ย่อยจาก PendingNow: ไม่ stale (last move < 180 วัน นิยามเดียวกับ kpi.activeWip) — ของที่ควรเร่งรัดจริง
        public int PendingActive { get; set; }

        // ย่อยจาก PendingNow: stale — ค้างนาน (เช่นตั้งแต่ปี 2024) ไม่ใช่ปัญหา "ช้า" ธรรมดา
        public int PendingStale { get; set; }

        // ใน pendingNow ที่ค้างมาเกิน 30 วันแล้ว
        public int PendingOver30d { get; set; }

        public List<CostCardSeriesPoint> Series { get; set; } = new List<CostCardSeriesPoint>();
    }

    public class CostCardSeriesPoint
    {
        public DateTime BucketEnd { get; set; }
        public int Count { get; set; }
        public double? MedianDays { get; set; }
        public double? P90Days { get; set; }
    }
}
