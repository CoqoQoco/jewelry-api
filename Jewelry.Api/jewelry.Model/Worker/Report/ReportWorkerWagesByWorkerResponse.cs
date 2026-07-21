using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Worker.Report
{
    public class ReportWorkerWagesByWorkerResponse
    {
        public string WorkerCode { get; set; }
        public string WorkerName { get; set; }
        public int JobCount { get; set; }
        public decimal? TotalQty { get; set; }
        public decimal? TotalWages { get; set; }
    }
}
