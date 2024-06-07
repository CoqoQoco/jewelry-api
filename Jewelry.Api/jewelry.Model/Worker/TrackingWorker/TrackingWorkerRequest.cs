using jewelry.Model.Worker.Report;
using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Worker.TrackingWorker
{
    public class TrackingWorkerRequest : DataSourceRequest
    {
        public TrackingWorker Search { get; set; }
    }

    public class TrackingWorker
    {
        public DateTimeOffset? CreateStart { get; set; }
        public DateTimeOffset? CreateEnd { get; set; }
        public string? WoText { get; set; }
        public string? Text { get; set; }
    }
}
