using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.CapacityReport
{
    public class Response
    {
        public string Bucket { get; set; } = string.Empty;
        public string GroupBy { get; set; } = string.Empty;
        public List<BucketPoint> Buckets { get; set; } = new List<BucketPoint>();
        public List<GroupSeries> Series { get; set; } = new List<GroupSeries>();
        public CapacitySummary Summary { get; set; } = new CapacitySummary();
    }

    public class BucketPoint
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public int PlanCount { get; set; }
        public int PieceCount { get; set; }
        public bool IsPartial { get; set; }
    }

    public class GroupSeries
    {
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public List<GroupPoint> Points { get; set; } = new List<GroupPoint>();
    }

    public class GroupPoint
    {
        public string BucketKey { get; set; } = string.Empty;
        public int PlanCount { get; set; }
        public int PieceCount { get; set; }
    }

    public class CapacitySummary
    {
        public int TotalPlans { get; set; }
        public int TotalPieces { get; set; }
        public decimal AvgPlansPerBucket { get; set; }
        public decimal AvgPiecesPerBucket { get; set; }
        public string BestBucketKey { get; set; } = string.Empty;
        public string BestBucketLabel { get; set; } = string.Empty;
        public int BestBucketPlans { get; set; }
    }
}
