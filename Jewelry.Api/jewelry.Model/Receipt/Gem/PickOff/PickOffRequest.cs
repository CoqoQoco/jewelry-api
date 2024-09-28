using jewelry.Model.Receipt.Gem.Outbound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Gem.PickOff
{
    public class PickOffRequest
    {
        public int Type { get; set; }
        public string? OperatorBy { get; set; }
        public string? Remark { get; set; }
        public string? Pass { get; set; }

        public DateTimeOffset RequestDate { get; set; }
        public DateTimeOffset ReturnDate { get; set; }

        public IEnumerable<PickOffItem> Gems { get; set; } = new List<PickOffItem>();
    }

    public class PickOffItem
    {
        public string Code { get; set; }
        public decimal IssueQty { get; set; }
        public decimal IssueQtyWeight { get; set; }
        public string? Remark { get; set; }
    }
}
