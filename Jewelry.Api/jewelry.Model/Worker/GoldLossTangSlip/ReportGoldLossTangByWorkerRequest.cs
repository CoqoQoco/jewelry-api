using Kendo.DynamicLinqCore;
using System;

namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class ReportGoldLossTangByWorkerRequest : DataSourceRequest
    {
        public ReportGoldLossTangByWorkerSearch Search { get; set; }
    }

    public class ReportGoldLossTangByWorkerSearch
    {
        public DateTimeOffset? RequestDateStart { get; set; }
        public DateTimeOffset? RequestDateEnd { get; set; }
        public string? WorkerCode { get; set; }
    }
}
