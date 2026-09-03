using System;

namespace jewelry.Model.Stock.Gold.Inbound
{
    public class Request
    {
        public string GoldCode { get; set; } = null!;
        public string GoldSizeCode { get; set; } = null!;
        public decimal Weight { get; set; }
        public DateTimeOffset? RequestDate { get; set; }
        public string? Remark { get; set; }
    }
}
