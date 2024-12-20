﻿using jewelry.Model.Receipt.Gem.Inbound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Gem.Outbound
{
    public class OutboundRequest
    {
        public int Type { get; set; }
        public string? OperatorBy { get; set; }
        public string? Remark { get; set; }
        public string? Pass { get; set; }
        public DateTimeOffset RequestDate { get; set; }
        public IEnumerable<OutboundItem> Gems { get; set; } = new List<OutboundItem>();
    }

    public class OutboundItem
    {
        public string Code { get; set; }
        public decimal IssueQty { get; set; }
        public decimal IssueQtyWeight { get; set; }
        public string? Remark { get; set; }

        public string? WO { get; set; }
        public int? WONumber { get; set; }
        public string? WOText { get; set; }
        public string? Mold { get; set; }
    }
}
