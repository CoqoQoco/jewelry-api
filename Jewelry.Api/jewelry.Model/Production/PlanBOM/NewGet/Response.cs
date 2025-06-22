using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Production.PlanBOM.NewGet
{
    public class Response
    {

        public int ProductionPlanId { get; set; }

        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public string WoText { get; set; }

        public List<BOM> BOMs { get; set; }
    }

    public class BOM
    {
        public int No { get; set; }
        public string Type { get; set; }

        public string OriginCode { get; set; }
        public string OriginName { get; set; }

        public string MatchCode { get; set; }
        public string MatchName { get; set; }

        public string DisplayName { get; set; }

        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }

        public decimal? Price { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
    }
}
