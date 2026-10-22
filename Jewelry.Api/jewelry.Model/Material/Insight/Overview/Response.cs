using System;
using System.Collections.Generic;
using Finding = jewelry.Model.Production.Insight.Wip.Finding;
using ActionItem = jewelry.Model.Production.Insight.Wip.ActionItem;

namespace jewelry.Model.Material.Insight.Overview
{
    public class Response
    {
        public DateTime AsOf { get; set; }
        public string Kind { get; set; } = "GOLD";

        // หน่วยของ qty ทุกตัว (ทอง = g)
        public string Unit { get; set; } = "g";

        // 'critical' | 'warning' | 'ok' — severity ที่แย่ที่สุดใน problems+forecasts
        public string Status { get; set; } = "ok";

        public PolicyValues Policy { get; set; } = new PolicyValues();
        public UsageHistory UsageHistory { get; set; } = new UsageHistory();
        public Kpi Kpi { get; set; } = new Kpi();
        public List<Item> Items { get; set; } = new List<Item>();
        public List<GroupRow> Groups { get; set; } = new List<GroupRow>();
        public List<SeriesPoint> Series { get; set; } = new List<SeriesPoint>();

        public List<Finding> Problems { get; set; } = new List<Finding>();
        public List<Finding> Forecasts { get; set; } = new List<Finding>();
        public List<ActionItem> Actions { get; set; } = new List<ActionItem>();
    }

    // ประวัติการใช้จริง (เบิกที่ไม่ถูกกลับรายการ) — ใช้ตัดสินว่าข้อมูลพอสำหรับ forecast หรือไม่
    public class UsageHistory
    {
        // วันแรกของเดือนที่มีการเบิกครั้งแรก (เวลาไทย) yyyy-MM-dd หรือ null ถ้ายังไม่เคยเบิก
        public string? StartMonth { get; set; }

        // จำนวนเดือนเต็มนับจาก startMonth ถึงก่อนเดือนปัจจุบัน
        public int FullMonths { get; set; }
    }

    public class Kpi
    {
        public int ItemCount { get; set; }
        // ผลรวมเฉพาะ item ที่ยอด >= 0 (ไม่รวม negative — ดู NegativeWeight)
        public decimal TotalOnHand { get; set; }
        public decimal TotalOnProcess { get; set; }
        // เหมือน TotalOnHand (ไม่รวม item ติดลบ)
        public decimal TotalAvailable { get; set; }

        // status แยกกัน (zero/below/near ไม่ซ้อนกัน)
        public int ZeroCount { get; set; }
        public int BelowMinCount { get; set; }
        public int NearMinCount { get; set; }

        // ของตาย — นับแยกจาก status (อาจซ้อนกับ below/near ได้)
        public int DeadCount { get; set; }
        public decimal DeadOnHand { get; set; }

        public int OnProcessStaleCount { get; set; }
        public decimal OnProcessStaleWeight { get; set; }

        // false = provider ไม่มีข้อมูลอายุของที่อยู่ในกระบวนการ (ทอง) -> ไม่ประเมิน MAT_ON_PROCESS_STALE
        public bool OnProcessAgeSupported { get; set; }

        public int StockoutRiskCount { get; set; }
        public int OverrideCount { get; set; }
        public int WithUsageCount { get; set; }

        // item ที่ status = negative (ยอดในคลังติดลบ = ledger ไม่เคยตั้งยอดยกมา/รับเข้า) — ไม่นับรวมใน zero/below/near/dead
        public int NegativeCount { get; set; }

        // ผลรวมยอดติดลบแบบมีเครื่องหมาย (<= 0); totalOnHand/totalAvailable "ไม่รวม" item ติดลบ
        public decimal NegativeWeight { get; set; }

        // มี movement รับเข้า/ยอดยกมา (type 1/2) ที่ยังมีผลอยู่ใน ledger หรือไม่ (false = คลังไม่เคยมียอดตั้งต้น)
        public bool InboundRecorded { get; set; }
    }

    public class Item
    {
        public string ItemKey { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? Name { get; set; }
        public string? SizeCode { get; set; }
        public string? SizeName { get; set; }
        public string Label { get; set; } = null!;

        // เฉพาะ kind GOLD (null สำหรับ kind อื่น) — ตาม contract
        public string? GoldCode { get; set; }
        public string? GoldName { get; set; }

        public decimal OnHand { get; set; }
        public decimal OnProcess { get; set; }
        public decimal Available { get; set; }

        public decimal ConsumedInWindow { get; set; }
        public decimal AvgDailyUsage { get; set; }
        public decimal AvgMonthlyUsage { get; set; }
        public decimal? CoverDays { get; set; }

        public int LeadTimeDays { get; set; }
        public int? OverrideLeadTimeDays { get; set; }
        public decimal AutoMin { get; set; }
        public decimal? OverrideMin { get; set; }
        public decimal EffectiveMin { get; set; }

        // 'auto' | 'override' | 'floor'
        public string MinSource { get; set; } = "auto";

        // 'negative' | 'zero' | 'below' | 'near' | 'ok' | 'dead'
        public string Status { get; set; } = "ok";
        public bool IsDead { get; set; }
        public bool StockoutRisk { get; set; }

        // วันที่เบิกล่าสุด (ไม่นับที่ถูกกลับรายการ); null = ไม่เคยเบิก
        public DateTime? LastMovementDate { get; set; }

        // วันนับจากเบิกล่าสุด (ถ้าไม่เคยเบิก นับจากวันที่ item เริ่มมีในคลัง)
        public int? DaysSinceMove { get; set; }
        public bool NeverConsumed { get; set; }

        public int? OnProcessAgeDays { get; set; }
    }

    public class GroupRow
    {
        public string Code { get; set; } = null!;
        public string? Name { get; set; }
        public int ItemCount { get; set; }
        // OnHand = ผลรวมมีเครื่องหมาย (รวมยอดติดลบ)
        public decimal OnHand { get; set; }
        public decimal OnProcess { get; set; }
        public int NegativeCount { get; set; }
    }

    public class SeriesPoint
    {
        // วันแรกของเดือน (เวลาไทย) yyyy-MM-dd
        public string Month { get; set; } = null!;

        // รับเข้าจากการซื้อ/รับใหม่ (type 1)
        public decimal Inflow { get; set; }

        // เบิกใช้ (ไม่รวมที่ถูกกลับรายการ)
        public decimal Consumed { get; set; }

        // คืนจากการผลิต (ไม่รวมที่ถูกกลับรายการ)
        public decimal Returned { get; set; }

        // สุทธิของรายการอื่น (ยอดยกมา/ปรับยอด/กลับรายการ) มีเครื่องหมาย
        public decimal Other { get; set; }

        // ยอดคงเหลือสิ้นเดือน (ย้อนจากยอดปัจจุบัน)
        public decimal EndStock { get; set; }
    }
}
