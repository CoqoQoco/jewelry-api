using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Production.PlanBOM.List
{
    public class Response
    {
        public string Name { get; set; }
        public string NameDescription { get; set; }
        public string NameGroup { get; set; }

        public DateTime? Date { get; set; }

        public decimal? Qty { get; set; }
        public decimal? QtyPrice { get; set; }

        public decimal? QtyWeight { get; set; }
        public decimal? QtyWeightPrice { get; set; }

        // Additional useful fields for enhanced functionality
        public int ProductionPlanId { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public string WoText { get; set; }

        public DateTime? CompletedDate { get; set; }

        public string ProductNumber { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string Gold { get; set; }
        public string GoldSize { get; set; }
        public string Mold { get; set; }
        public string ProductType { get; set; }
    }
}
