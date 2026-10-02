using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.WorkerMonthly
{
    public class Response
    {
        public List<SeriesItem> Series { get; set; } = new List<SeriesItem>();
    }

    public class SeriesItem
    {
        public DateTime BucketEnd { get; set; }
        public int Jobs { get; set; }
        public int Plans { get; set; }
        public decimal Wages { get; set; }

        // null ถ้าไม่มีแถวคืนแล้ว (GOLD) ใน bucket นี้
        public decimal? GoldDiffPercent { get; set; }
    }
}
