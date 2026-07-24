using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.CompletedDailySeries
{
    public class Response
    {
        public List<Row> Rows { get; set; } = new List<Row>();
        public int Total { get; set; }
        public int DaysElapsed { get; set; }
        public int DaysInPeriod { get; set; }
    }

    public class Row
    {
        public string Date { get; set; }
        public int Count { get; set; }
    }
}
