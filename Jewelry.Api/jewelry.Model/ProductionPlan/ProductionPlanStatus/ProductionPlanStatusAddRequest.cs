using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanStatus
{
    public class ProductionPlanStatusAddRequest
    {
        public string Wo { get; set; }
        public int WoNumber { get; set; }

        public int ProductionPlanId { get; set; }
        public int Status { get; set; }

        public string? SendName { get; set; }
        public DateTimeOffset? SendDate { get; set; }
        public string? CheckName { get; set; }
        public DateTimeOffset? CheckDate { get; set; }

       public List<GoldItem>? Golds { get; set; }

        public string? Remark1 { get; set; }
        public string? Remark2 { get; set; }
        //public string? Description { get; set; }
        public decimal? TotalWages { get; set; }
    }

    public class GoldItem
    {
        public DateTimeOffset? RequestDate { get; set; }
        public string? Gold { get; set; }
        public decimal? GoldWeightSend { get; set; }
        public decimal? GoldQTYSend { get; set; }
        public decimal? GoldWeightCheck { get; set; }
        public decimal? GoldQTYCheck { get; set; }
        public string? Worker { get; set; }
        public string? WorkerSub { get; set; }
        public decimal? Wages { get; set; }
        public decimal? TotalWages { get; set; }
        public string? Description { get; set; }
    }
}
