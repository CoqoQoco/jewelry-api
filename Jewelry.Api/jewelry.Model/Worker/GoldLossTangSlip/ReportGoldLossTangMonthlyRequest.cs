using System;

namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class ReportGoldLossTangMonthlyRequest
    {
        public string WorkerCode { get; set; }
        public DateTimeOffset? RequestDateStart { get; set; }
        public DateTimeOffset? RequestDateEnd { get; set; }
    }
}
