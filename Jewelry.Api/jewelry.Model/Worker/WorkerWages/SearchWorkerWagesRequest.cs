using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Worker.WorkerWages
{
    public class SearchWorkerWagesRequest
    {
        public DateTimeOffset RequestDateStart { get; set; }
        public DateTimeOffset RequestDateEnd { get; set; }
        public string Code { get; set; }
    }
}
