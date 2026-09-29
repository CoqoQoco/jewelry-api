using System;

namespace Jewelry.Service.Production.Shared
{
    // ใช้ร่วมกันระหว่าง ExecutiveReportService (Report/Executive) และ ProductionInsightService (Production/Insight)
    public class OpenPlanRow
    {
        public int Id { get; set; }
        public string Wo { get; set; } = null!;
        public int WoNumber { get; set; }
        public string WoText { get; set; } = null!;
        public string Mold { get; set; } = null!;
        public string ProductNumber { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int ProductQty { get; set; }
        public int Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = null!;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
    }
}
