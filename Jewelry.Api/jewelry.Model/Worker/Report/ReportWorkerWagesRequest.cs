using jewelry.Model.ProductionPlan.ProductionPlanReport;
using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Worker.Report
{
    public class ReportWorkerWagesRequest : DataSourceRequest
    {
        public ReportWorkerWages Search { get; set; }
    }

    public class ReportWorkerWages
    {
        public DateTimeOffset CreateStart { get; set; }
        public DateTimeOffset CreateEnd { get; set; }
        public string? WoText { get; set; }
        public string? Text { get; set; }
        public List<string>? Gold { get; set; }
        public List<string>? GoldSize { get; set; }
        public List<int>? Status { get; set; }
        public string? WorkerCode { get; set; }
        public string? ProductNumber { get; set; }
        public string? Mold { get; set; }
    }
}
