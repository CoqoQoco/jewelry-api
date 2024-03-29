using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Worker.Report
{
    public class ReportWorkerSummeryResponse
    {
        public decimal? TotalGoldQtySend { get; set; }
        public decimal? TotalGoldWeightSend { get; set; }
        public decimal? TotalGoldQtyCheck { get; set; }
        public decimal? TotalGoldWeightCheck { get; set; }
        public decimal? TotalWages { get; set; }
    }
}
