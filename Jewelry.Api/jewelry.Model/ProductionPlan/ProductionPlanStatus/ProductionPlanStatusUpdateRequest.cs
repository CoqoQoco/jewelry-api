using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanStatus
{
    public class ProductionPlanStatusUpdateRequest
    {
        public int ProductionPlanId { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public int Id { get; set; }

        public int Status { get; set; }
        public DateTimeOffset AssignDate { get; set; }
        public string AssignBy { get; set; }
        public DateTimeOffset? ReceiveDate { get; set; }
        public string? ReceiveBy { get; set; }
        public string? Remark { get; set; }
        public string? AssignTo { get; set; }
        public string? AssignDetail { get; set; }
        public string? ReceiveDetail { get; set; }
    }
}
