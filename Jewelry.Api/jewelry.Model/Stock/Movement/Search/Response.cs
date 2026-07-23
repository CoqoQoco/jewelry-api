using System;

namespace jewelry.Model.Stock.Movement.Search
{
    public class Response
    {
        public DateTime MovementDate { get; set; }
        public string? StockNumber { get; set; }
        public string? StockNumberOrigin { get; set; }
        public string? ProductCode { get; set; }
        public string? FromLocation { get; set; }
        public string? FromLocationName { get; set; }
        public string? ToLocation { get; set; }
        public string? ToLocationName { get; set; }
        public string CreateBy { get; set; } = null!;
        public string? Remark { get; set; }
    }
}
