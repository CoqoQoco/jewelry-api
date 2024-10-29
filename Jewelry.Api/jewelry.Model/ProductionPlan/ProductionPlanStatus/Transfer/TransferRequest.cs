using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanStatus.Transfer
{
    public class TransferRequest
    {
        public int FormerStatus { get; set; }
        public int TargetStatus { get; set; }
        public string? TransferBy { get; set; }
        public IEnumerable<TransferRequestItem> Plans { get; set; } = new List<TransferRequestItem>();
    }

    public class TransferRequestItem
    { 
        public int Id { get; set; }

        public string Wo { get; set; }
        public int WoNumber { get; set; }
    }
}
