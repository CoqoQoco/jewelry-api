using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Worker.TrackingWorker
{
    public class TrackingWorkerResponse
    {
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public string WoText { get; set; }
        public string Mold { get; set; }

        public string ProductNumber { get; set; }
        public string ProductName { get; set; }

        public string? WorkerCode { get; set; }
        public string? WorkerName { get; set; }

        public int Status { get; set; }
        public string StatusName { get; set; }
        public string? StatusDescription { get; set; }

        public string? Gold { get; set; }

        public decimal? GoldQtySend { get; set; }
        public decimal? GoldWeightSend { get; set; }
        public decimal? GoldQtyCheck { get; set; }
        public decimal? GoldWeightCheck { get; set; }
        public string? Description { get; set; }

        public decimal? Wages { get; set; }
        public decimal? TotalWages { get; set; }
        public int WagesStatus { get; set; }

        public DateTime? JobDate { get; set; }
    }
}
