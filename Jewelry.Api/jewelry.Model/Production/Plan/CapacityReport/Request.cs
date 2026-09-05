using System;

namespace jewelry.Model.Production.Plan.CapacityReport
{
    public class Request
    {
        public Criteria Search { get; set; } = new Criteria();
    }

    public class Criteria
    {
        public string Bucket { get; set; } = "month";
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public string GroupBy { get; set; } = "none";

        public string[]? Gold { get; set; }
        public string[]? GoldSize { get; set; }
        public string[]? ProductType { get; set; }
        public string[]? CustomerType { get; set; }
        public string? CustomerCode { get; set; }
        public string? Mold { get; set; }
        public string? ProductNumber { get; set; }
        public string? Text { get; set; }
    }
}
