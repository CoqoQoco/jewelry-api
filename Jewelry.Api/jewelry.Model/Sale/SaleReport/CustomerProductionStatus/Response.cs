using System;

namespace jewelry.Model.Sale.SaleReport.CustomerProductionStatus
{
    public class Response
    {
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public int TotalPlans { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public DateTime? NearestRequestDate { get; set; }
        public string LatestStatusName { get; set; } = null!;
    }
}
