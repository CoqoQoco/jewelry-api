using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.WipTrend
{
    public class Response
    {
        public List<BucketInfo> Buckets { get; set; } = new List<BucketInfo>();
        public List<DepartmentTrendData> Departments { get; set; } = new List<DepartmentTrendData>();
        public TrendSummary Total { get; set; } = new TrendSummary();
    }

    public class BucketInfo
    {
        // bucket end instant (UTC)
        public DateTime End { get; set; }

        // 'dd/MM' (week) | 'yyyy-MM' (month) — เวลาไทย
        public string Label { get; set; } = null!;
    }

    public class DepartmentTrendData
    {
        public string Key { get; set; } = null!;
        public int StartWip { get; set; }
        public int EndWip { get; set; }
        public int Delta { get; set; }
        public decimal? DeltaPercent { get; set; }
        public int Inflow { get; set; }
        public int Outflow { get; set; }
        public int Net { get; set; }
        public List<SeriesPoint> Series { get; set; } = new List<SeriesPoint>();
    }

    public class TrendSummary
    {
        public int StartWip { get; set; }
        public int EndWip { get; set; }
        public int Delta { get; set; }
        public decimal? DeltaPercent { get; set; }
        public int Inflow { get; set; }
        public int Outflow { get; set; }
        public int Net { get; set; }
        public List<SeriesPoint> Series { get; set; } = new List<SeriesPoint>();
    }

    public class SeriesPoint
    {
        public DateTime BucketEnd { get; set; }
        public int Wip { get; set; }
        public int Inflow { get; set; }
        public int Outflow { get; set; }
    }
}
