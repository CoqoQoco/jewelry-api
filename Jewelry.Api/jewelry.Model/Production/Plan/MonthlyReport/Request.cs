using System;

namespace jewelry.Model.Production.Plan.MonthlyReport
{
    public class Request
    {
        public Criteria Search { get; set; } = new Criteria();
    }

    public class Criteria
    {
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }
}