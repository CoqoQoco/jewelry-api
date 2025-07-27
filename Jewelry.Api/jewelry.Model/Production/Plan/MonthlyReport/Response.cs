using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.MonthlyReport
{
    public class Response
    {
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public List<PlanFinishByType> PlanFinishByType { get; set; } = new List<PlanFinishByType>();
        public List<PlanFinishByProductType> PlanFinishByProductType { get; set; } = new List<PlanFinishByProductType>();
        public List<PlanFinishByCustomerType> PlanFinishByCustomerType { get; set; } = new List<PlanFinishByCustomerType>();
    }

    public class PlanFinishByType
    {
        public string Type { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalQty { get; set; }
        public decimal Percentage { get; set; }
    }

    public class PlanFinishByProductType
    {
        public string ProductType { get; set; } = string.Empty;
        public string ProductTypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalQty { get; set; }
        public decimal Percentage { get; set; }
    }

    public class PlanFinishByCustomerType
    {
        public string CustomerType { get; set; } = string.Empty;
        public string CustomerTypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalQty { get; set; }
        public decimal Percentage { get; set; }
    }
}