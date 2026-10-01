using System;
using System.Collections.Generic;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace jewelry.Model.Production.Insight.Delivery
{
    public class Response
    {
        public DateTime AsOf { get; set; }

        // 'critical' | 'warning' | 'ok' — severity ที่แย่ที่สุดใน problems+forecasts
        public string Status { get; set; } = "ok";

        public List<Wip.Finding> Problems { get; set; } = new List<Wip.Finding>();
        public List<Wip.Finding> Forecasts { get; set; } = new List<Wip.Finding>();
        public List<Wip.ActionItem> Actions { get; set; } = new List<Wip.ActionItem>();

        public decimal TargetPercent { get; set; }

        // 'saved' | 'draft'
        public string TargetSource { get; set; } = "saved";
        public DateTime? TargetEffectiveFrom { get; set; }

        public KpiData Kpi { get; set; } = new KpiData();
        public List<SeriesPoint> Series { get; set; } = new List<SeriesPoint>();
        public List<LateCustomerItem> LateCustomers { get; set; } = new List<LateCustomerItem>();
    }

    public class KpiData
    {
        public int CompletedCount { get; set; }
        public int OnTimeCount { get; set; }

        // null ถ้า completedCount = 0
        public decimal? OnTimePercent { get; set; }
        public double? LateMedianDays { get; set; }
        public double? PlannedLeadMedianDays { get; set; }
        public double? ActualLeadMedianDays { get; set; }

        // ceil ของ actualLeadMedianDays — 0 ถ้าไม่มีแผนเสร็จเลยในช่วง
        public int SuggestedLeadDays { get; set; }

        public int OpenCount { get; set; }
        public int OpenOverdueCount { get; set; }

        // openOverdueCount ที่ไม่ใช่ stale (นิยามเดียวกับ WIP_STALE)
        public int OpenOverdueActiveCount { get; set; }
        public int AtRiskCount { get; set; }
        public int StuckAfterCostCardCount { get; set; }
    }

    public class SeriesPoint
    {
        public DateTime BucketEnd { get; set; }
        public int CompletedCount { get; set; }
        public int OnTimeCount { get; set; }
        public decimal? OnTimePercent { get; set; }
        public double? PlannedLeadMedianDays { get; set; }
        public double? ActualLeadMedianDays { get; set; }
    }

    public class LateCustomerItem
    {
        public string CustomerCode { get; set; } = null!;
        public string? CustomerName { get; set; }
        public int CompletedCount { get; set; }
        public int LateCount { get; set; }
        public decimal LatePercent { get; set; }
        public double? LateMedianDays { get; set; }
    }
}
