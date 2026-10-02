using System;
using System.Collections.Generic;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace jewelry.Model.Production.Insight.Workers
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
        public List<SeriesTotalItem> SeriesTotal { get; set; } = new List<SeriesTotalItem>();
        public List<WorkerItem> Workers { get; set; } = new List<WorkerItem>();
        public List<ConcentrationItem> Concentration { get; set; } = new List<ConcentrationItem>();
    }

    public class KpiData
    {
        public decimal WagesPerMonth { get; set; }
        public List<DeptWageItem> WagesByDept { get; set; } = new List<DeptWageItem>();

        // wages รวมช่วง ÷ จำนวนแผนที่เข้าบัตรต้นทุน (95) ครั้งแรกในช่วง — null ถ้าไม่มีแผนเข้า 95 เลยในช่วง
        public decimal? WagePerOutputPlan { get; set; }

        // ช่างจริง distinct ทั้งหมด (ไม่รวม placeholder/TEST) ที่มีงานอย่างน้อย 1 ชิ้นในช่วง ไม่ว่าแผนกไหน
        public int ActiveWorkers { get; set; }
        public List<DeptCountItem> ActiveWorkersByDept { get; set; } = new List<DeptCountItem>();

        // ค่าแรงช่างนอกบ้าน+ร้าน (OUTSIDE+SHOP) ÷ ค่าแรงรวม — 0-100
        public decimal OutsideWageShare { get; set; }

        public int UnpaidPieceJobs { get; set; }

        // true เสมอ — บอก UI ว่าตัวเลขนี้ไม่รวมช่างเงินเดือน (เช่น gemSort ไม่มีใครถูกบันทึกค่าแรงเลย)
        public bool ExcludesSalaried { get; set; } = true;
    }

    public class DeptWageItem
    {
        public string DeptKey { get; set; } = null!;
        public decimal WagesPerMonth { get; set; }

        // สัดส่วนของค่าแรงรวม — 0-100
        public decimal Share { get; set; }
    }

    public class DeptCountItem
    {
        public string DeptKey { get; set; } = null!;
        public int Count { get; set; }
    }

    public class SeriesItem
    {
        public DateTime BucketEnd { get; set; }
        public string DeptKey { get; set; } = null!;
        public decimal Wages { get; set; }
    }

    public class SeriesTotalItem
    {
        public DateTime BucketEnd { get; set; }
        public decimal Wages { get; set; }

        // จำนวนแผนที่เข้าบัตรต้นทุน (95) ครั้งแรกใน bucket นี้
        public int Output { get; set; }

        // wages ÷ output — null ถ้า output = 0
        public decimal? WagePerPlan { get; set; }
    }

    public class WorkerItem
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string DeptKey { get; set; } = null!;

        // 'IN_HOUSE' | 'OUTSIDE' | 'SHOP' | null
        public string? EmploymentType { get; set; }

        public int Jobs { get; set; }
        public int Plans { get; set; }
        public decimal Wages { get; set; }

        // null ถ้า jobs = 0 (ไม่เกิดจริงเพราะแถวนี้มีจาก group ที่มี job อย่างน้อย 1)
        public decimal? WagePerJob { get; set; }

        // สัดส่วนจำนวนงานของช่างคนนี้ต่อจำนวนงานรวมของแผนกเดียวกัน — 0-100
        public decimal ShareOfDeptJobs { get; set; }

        // % loss จากฝั่งใบ gold loss (scope=SLIP) ของช่างคนนี้ — เฉพาะ trim(จากใบตั้งฉาก)/setting(จากใบฝัง) GOLD
        // เท่านั้น — null สำหรับ rawPolish/gemSort/plating (ไม่มีใบคู่กัน) หรือไม่มีข้อมูลในช่วง
        public decimal? GoldLossPercent { get; set; }

        // % diff จ่าย-รับรายแผนก (เฉพาะแถวคืนแล้ว, GOLD เท่านั้น) — null ถ้าไม่มีแถวคืนแล้วเลยในช่วง
        public decimal? GoldDiffPercent { get; set; }

        // true = wagePerJob > 3×median ของกลุ่มแผนก+ประเภทการจ้างเดียวกัน (ต้อง jobs >= 5)
        public bool IsRateOutlier { get; set; }

        // true = เป็น 1 ใน top 2 ของแผนกที่เข้าเกณฑ์ WRK_CONCENTRATION (>=3 ช่างจริง, top2Share >= 50)
        public bool IsConcentrated { get; set; }
    }

    public class ConcentrationItem
    {
        public string DeptKey { get; set; } = null!;

        // สัดส่วนงานของช่าง 2 อันดับแรกต่องานรวมของแผนก — 0-100 (เทียบเท่า "top2Share >= 0.5" ของ spec = >= 50 ที่นี่)
        public decimal Top2Share { get; set; }
        public List<ConcentrationWorkerItem> TopWorkers { get; set; } = new List<ConcentrationWorkerItem>();
    }

    public class ConcentrationWorkerItem
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;

        // สัดส่วนงานของช่างคนนี้ต่องานรวมของแผนก — 0-100
        public decimal Share { get; set; }
    }
}
