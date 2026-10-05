using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.MaterialWaitingPlans
{
    public class GemLineItem
    {
        public string? Gem { get; set; }
        public string? Shape { get; set; }
        public string? Size { get; set; }
        public string Metal { get; set; } = null!;
        public int Qty { get; set; }

        // null เฉพาะ unmatched
        public decimal? Available { get; set; }

        // 'ready' | 'short' | 'unmatched'
        public string Status { get; set; } = null!;

        // เฉพาะ unmatched: 'gem' | 'spec' — null ถ้า ready/short
        public string? UnmatchedReason { get; set; }
    }

    // ฐาน: jewelry.Model.Report.Executive.StalePlans.Item (PlanId/Wo/Mold/.../LastAction*/Workers/WorkerItems —
    // enrich ชุดเดียวกันทุกตาราง WIP ของโมดูลนี้)
    public class Item : jewelry.Model.Report.Executive.StalePlans.Item
    {
        public DateTime RequestDate { get; set; }

        // วันที่เข้าแผนกตัดพลอยครั้งแรก (header แรกที่ status 69 หรือ 70)
        public DateTime EnteredGemSortDate { get; set; }

        // ตอนนี้ - EnteredGemSortDate (วัน)
        public double WaitingDays { get; set; }

        public List<GemLineItem> Gems { get; set; } = new List<GemLineItem>();

        // worst ของ Gems — short > unmatched > ready
        public string GemStatus { get; set; } = null!;
    }
}
