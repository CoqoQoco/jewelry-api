using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Production.Draft.Create
{
    public class Request
    {
        public string ReceiptNumber { get; set; }

        public IEnumerable<jewelry.Model.Receipt.Production.PlanGet.Material>? BreakDown { get; set; }
        public List<jewelry.Model.Receipt.Production.PlanGet.ReceiptStock> Stocks { get; set; }
    }
}
