using jewelry.Model.Receipt.Gem.Outbound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Gem.Return
{
    public class PickReturnRequest
    {
        public string ReferenceRunning { get; set; }
        public int Type { get; set; }
        public string? Remark { get; set; }
        public string? Pass { get; set; }
        public DateTimeOffset RequestDate { get; set; }
        public IEnumerable<ReturnItem> GemsReturn { get; set; } = new List<ReturnItem>();
    }

    public class ReturnItem
    {
        public string Code { get; set; }

        public decimal PickOffQty { get; set; }
        public decimal PickOffQtyWeight { get; set; }

        public decimal ReturnQty { get; set; }
        public decimal ReturnQtyWeight { get; set; }

        public IEnumerable<OutboundItem> GemsOutbound { get; set; } = new List<OutboundItem>();
    }

    public class OutboundItem
    {
        public string WO { get; set; }
        public int WONumber { get; set; }
        public string Mold { get; set; }

        public decimal IssueQty { get; set; }
        public decimal IssueQtyWeight { get; set; }

        public string? Remark { get; set; }
    }
}
