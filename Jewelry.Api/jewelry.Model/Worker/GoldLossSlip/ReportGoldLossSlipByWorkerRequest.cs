using Kendo.DynamicLinqCore;
using System;

namespace jewelry.Model.Worker.GoldLossSlip
{
    public class ReportGoldLossSlipByWorkerRequest : DataSourceRequest
    {
        public ReportGoldLossSlipByWorkerSearch Search { get; set; }
    }

    public class ReportGoldLossSlipByWorkerSearch
    {
        public DateTimeOffset? RequestDateStart { get; set; }
        public DateTimeOffset? RequestDateEnd { get; set; }
        public string? WorkerCode { get; set; }
        public bool GroupByMonth { get; set; } = false;
    }
}
