using System;

namespace jewelry.Model.Stock.Gold.Adjust
{
    public class Request
    {
        public string GoldCode { get; set; } = null!;
        public string GoldSizeCode { get; set; } = null!;

        // ต้องเป็น GoldStockTransactionType.AdjustIncrease (5) หรือ AdjustDecrease (6) เท่านั้น
        public int Type { get; set; }
        public decimal Weight { get; set; }
        public string Remark { get; set; } = null!;
        public DateTimeOffset? RequestDate { get; set; }
    }
}
