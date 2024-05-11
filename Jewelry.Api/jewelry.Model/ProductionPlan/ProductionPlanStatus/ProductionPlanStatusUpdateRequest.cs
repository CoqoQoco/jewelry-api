using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanStatus
{
    public class ProductionPlanStatusUpdateRequest
    {
        public string Wo { get; set; }
        public int WoNumber { get; set; }

        public int ProductionPlanId { get; set; }
        public int HeaderId { get; set; }
        public int Status { get; set; }

        public string? SendName { get; set; }
        public DateTimeOffset? SendDate { get; set; }
        public string? CheckName { get; set; }
        public DateTimeOffset? CheckDate { get; set; }

        public List<GoldItem>? Golds { get; set; }
        public List<GemItem>? Gems { get; set; }

        public string? Remark1 { get; set; }
        public string? Remark2 { get; set; }
        //public string? Description { get; set; }
        public decimal? TotalWages { get; set; }
    }
}
