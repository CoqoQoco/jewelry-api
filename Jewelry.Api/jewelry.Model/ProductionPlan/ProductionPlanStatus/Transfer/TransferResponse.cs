using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanStatus.Transfer
{
    public class TransferResponse
    {
        public string Message { get; set; }
        public List<TransferResponseItem> Errors { get; set; } = new List<TransferResponseItem>();
    }

    public class TransferResponseItem
    {
        public int Id { get; set; }

        public string Wo { get; set; }
        public int WoNumber { get; set; }

        public string? Message { get; set; }
    }
}
