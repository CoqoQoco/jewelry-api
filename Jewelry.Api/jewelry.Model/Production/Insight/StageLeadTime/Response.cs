using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.StageLeadTime
{
    public class Response
    {
        public List<DepartmentLeadTimeData> Departments { get; set; } = new List<DepartmentLeadTimeData>();
        public CapacityData Capacity { get; set; } = new CapacityData();

        // min(receive_date) ทั้งตาราง — null ถ้ายังไม่มีข้อมูลเลย — UI ใช้บอก "เริ่มเก็บข้อมูลแยกรอ/ทำงานตั้งแต่..."
        public DateTime? SplitDataSince { get; set; }
    }

    public class DepartmentLeadTimeData
    {
        public string Key { get; set; } = null!;
        public decimal StandardDays { get; set; }

        // 'saved' | 'draft'
        public string StandardSource { get; set; } = null!;
        public DateTime? StandardEffectiveFrom { get; set; }

        public int ExitedCount { get; set; }
        public LeadTimeStat Median { get; set; } = new LeadTimeStat();
        public LeadTimeStat P90 { get; set; } = new LeadTimeStat();

        // (median.total - standard) / standard * 100 — null ถ้า standard <= 0
        public decimal? OverStandardPercent { get; set; }

        public int CurrentCount { get; set; }
        public int AbnormalCount { get; set; }

        // จำนวน exited visit ที่แยก wait/work ได้จริง (มี receive_date) — ใช้เช็คว่า median.wait/work น่าเชื่อถือแค่ไหน
        public int SplitSampleCount { get; set; }

        // แผนที่ plan.status ปัจจุบัน = สถานะ "รอ" ของแผนกนี้พอดี (snapshot ตรงๆ จาก tbt_production_plan.status
        // ไม่เกี่ยวกับ history) — independent จาก CurrentCount/AbnormalCount
        public int CurrentWaitingCount { get; set; }

        public List<LeadTimeSeriesPoint> Series { get; set; } = new List<LeadTimeSeriesPoint>();
    }

    public class LeadTimeStat
    {
        public double Total { get; set; }

        // null ถ้าไม่มี exited visit ที่แยก wait/work ได้เลยในช่วงนี้ (ดู SplitSampleCount)
        public double? Wait { get; set; }
        public double? Work { get; set; }
    }

    public class LeadTimeSeriesPoint
    {
        public DateTime BucketEnd { get; set; }
        public int Count { get; set; }

        // null เมื่อ count == 0 (ไม่มี visit จบในช่วงนี้เลย) — UI ควรเว้นช่องว่างในกราฟ ไม่ใช่วาดเป็น 0 จริง
        public double? MedianTotal { get; set; }

        // null เมื่อไม่มี split sample ใน bucket นี้เลย (แยกจาก count==0 ของ MedianTotal — อาจมี visit จบแต่แยก
        // wait/work ไม่ได้สักตัว)
        public double? MedianWait { get; set; }
        public double? MedianWork { get; set; }
        public double? P90Total { get; set; }
    }

    public class CapacityData
    {
        public CapacitySummary Current { get; set; } = new CapacitySummary();
        public CapacitySummary AtStandard { get; set; } = new CapacitySummary();
        public List<CapacityDepartmentData> Departments { get; set; } = new List<CapacityDepartmentData>();
    }

    public class CapacitySummary
    {
        public double TotalLeadDays { get; set; }
        public double MonthlyThroughput { get; set; }
        public string? BottleneckDept { get; set; }
    }

    public class CapacityDepartmentData
    {
        public string Key { get; set; } = null!;
        public int ExitedCount { get; set; }
        public double ExitedPerDay { get; set; }
        public double MedianTotal { get; set; }
        public double StandardDays { get; set; }
        public double AtStandardPerDay { get; set; }
        public bool IsBottleneckCurrent { get; set; }
        public bool IsBottleneckAtStandard { get; set; }
    }
}
