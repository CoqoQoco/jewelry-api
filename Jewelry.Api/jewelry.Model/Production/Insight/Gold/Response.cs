using System;
using System.Collections.Generic;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace jewelry.Model.Production.Insight.Gold
{
    public class Response
    {
        public DateTime AsOf { get; set; }

        // 'critical' | 'warning' | 'ok'
        public string Status { get; set; } = "ok";

        public List<Wip.Finding> Problems { get; set; } = new List<Wip.Finding>();
        public List<Wip.Finding> Forecasts { get; set; } = new List<Wip.Finding>();
        public List<Wip.ActionItem> Actions { get; set; } = new List<Wip.ActionItem>();

        public List<TargetItem> Targets { get; set; } = new List<TargetItem>();
        public List<KpiItem> Kpi { get; set; } = new List<KpiItem>();
        public List<SeriesItem> Series { get; set; } = new List<SeriesItem>();
        public List<WorkerItem> Workers { get; set; } = new List<WorkerItem>();
    }

    public class TargetItem
    {
        public int WorkerType { get; set; }
        public string Metal { get; set; } = "GOLD";
        public decimal TargetPercent { get; set; }

        // 'saved' | 'draft'
        public string Source { get; set; } = "saved";
        public DateTime? EffectiveFrom { get; set; }
    }

    public class KpiItem
    {
        public int WorkerType { get; set; }
        public string Metal { get; set; } = "GOLD";

        // tang = จำนวนใบ (slip), setting = จำนวนงาน (item)
        public int SlipCount { get; set; }
        public int WorkerCount { get; set; }
        public decimal ReceivedGram { get; set; }
        public decimal RawLossGram { get; set; }
        public decimal AllowedGram { get; set; }

        // = rawLossGram / receivedGram × 100 — null ถ้า receivedGram = 0
        public decimal? LossPercent { get; set; }

        // (A) = allowedGram / receivedGram × 100 — null ถ้า receivedGram = 0
        public decimal? AllowedPercent { get; set; }

        // (B)
        public decimal TargetPercent { get; set; }

        public decimal ExcessGram { get; set; }
        public decimal ExcessMoney { get; set; }

        // ยอดสุทธิ signed (+ = เครดิตรวม, - = หักรวม) — ห้ามใช้ sign นี้สรุป "เกิน/ไม่เกิน" ดู excessGram แทน
        public decimal NetMoney { get; set; }

        public int OverCount { get; set; }
        public int WorkersOverCount { get; set; }

        public int CoverageJobs { get; set; }
        public int CoverageTotalJobs { get; set; }
        public decimal? CoveragePercent { get; set; }
    }

    public class SeriesItem
    {
        public DateTime BucketEnd { get; set; }
        public int WorkerType { get; set; }
        public int SlipCount { get; set; }

        // null เมื่อ slipCount = 0 ใน bucket นี้
        public decimal? RawLossGram { get; set; }
        public decimal? AllowedGram { get; set; }
        public decimal? LossPercent { get; set; }
        public decimal? AllowedPercent { get; set; }
    }

    public class WorkerItem
    {
        public int WorkerType { get; set; }
        public string WorkerCode { get; set; } = null!;
        public string? WorkerName { get; set; }
        public int SlipCount { get; set; }
        public decimal ReceivedGram { get; set; }
        public decimal? LossPercent { get; set; }
        public decimal? AllowedPercent { get; set; }
        public decimal TargetPercent { get; set; }
        public decimal ExcessGram { get; set; }
        public decimal ExcessMoney { get; set; }
        public decimal NetMoney { get; set; }

        // จำนวน bucket ที่ worker คนนี้เกิน allowance ของตัวเอง / จำนวน bucket ที่มีข้อมูล (qualifying)
        public int OverBuckets { get; set; }
        public int QualifyingBuckets { get; set; }
    }
}
