using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Gem.Inbound
{
    public class InboundRequest
    {
        public int Type { get; set; }
        public string? JobNoOrPO { get; set; }
        public string? SubplierName { get; set; }
        public string? Remark { get; set; }
        public string? Pass { get; set; }
        public DateTimeOffset RequestDate { get; set; }
        public IEnumerable<InboundItem> Gems { get; set; } = new List<InboundItem>();
    }
    public class InboundItem
    {
        public string Code { get; set; }
        public decimal ReceiveQty { get; set; }
        public decimal SupplierCost { get; set; }
        public string? Remark { get; set; }
    }
}
