using jewelry.Model.ProductionPlan.ProductionPlanStatus.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Production.Plan.Transfer
{
    public class Request
    {
        public int FormerStatus { get; set; }
        public int TargetStatus { get; set; }
        public List<RequestItem> Plans { get; set; } = new List<RequestItem>();
    }

    public class RequestItem
    {
        public int Id { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }
    }
}
