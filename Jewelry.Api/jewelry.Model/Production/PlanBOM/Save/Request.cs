using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Production.PlanBOM.Save
{
    public class Request
    {
        public int Id { get; set; }
        public List<BOM> BOMs { get; set; } = new List<BOM>();
    }

    public class BOM
    {
        public int No { get; set; }
        public string Type { get; set; }

        public string OriginCode { get; set; }
        public string OriginName { get; set; }

        public string? MatchCode { get; set; }
        public string? MatchName { get; set; }

        public string DisplayName { get; set; }

        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }

        public decimal? Price { get; set; }
    }
}
