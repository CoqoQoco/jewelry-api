using System;

namespace jewelry.Model.Stock.Gold.Balance
{
    public class Response
    {
        public long Id { get; set; }

        public string GoldCode { get; set; } = null!;
        public string? GoldNameTh { get; set; }
        public string? GoldNameEn { get; set; }

        public string GoldSizeCode { get; set; } = null!;
        public string? GoldSizeNameTh { get; set; }
        public string? GoldSizeNameEn { get; set; }
        public decimal? GoldPercent { get; set; }

        public decimal Weight { get; set; }
        public decimal WeightOnProcess { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = null!;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
    }
}
