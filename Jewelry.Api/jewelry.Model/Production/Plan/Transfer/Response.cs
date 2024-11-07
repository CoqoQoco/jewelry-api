using jewelry.Model.ProductionPlan.ProductionPlanStatus.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Production.Plan.Transfer
{
    public class Response
    {
        public string Message { get; set; }
        public string? ReceiptNumber { get; set; }
        public string TransferNumber { get; set; }
        public List<TransferResponseItem> Errors { get; set; } = new List<TransferResponseItem>();
    }

    public class ResponseItem
    {
        public int Id { get; set; }

        public string Wo { get; set; }
        public int WoNumber { get; set; }

        public string? Message { get; set; }
    }
}
