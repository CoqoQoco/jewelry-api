using System;

namespace jewelry.Model.Stock.Gold.Transection
{
    public class Response
    {
        public long Id { get; set; }
        public string Running { get; set; } = null!;

        public string GoldCode { get; set; } = null!;
        public string? GoldNameTh { get; set; }
        public string GoldSizeCode { get; set; } = null!;
        public string? GoldSizeNameTh { get; set; }
        public decimal? GoldPercent { get; set; }

        public int Type { get; set; }
        public string TypeName { get; set; } = null!;

        public decimal Weight { get; set; }
        public decimal? PreviousRemainWeight { get; set; }
        public decimal? PointRemainWeight { get; set; }

        public string? RefDocType { get; set; }
        public string? RefDocNo { get; set; }
        public string? ProductionPlanWo { get; set; }
        public int? ProductionPlanWoNumber { get; set; }
        public string? RefRunning { get; set; }

        public DateTime? RequestDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string? Status { get; set; }
        public string? Remark { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = null!;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
    }
}
