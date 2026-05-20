using System;

namespace jewelry.Model.Worker.GoldLossSlip
{
    public class ListGoldLossSlipRequest
    {
        public string? WorkerCode { get; set; }
        public DateTimeOffset? RequestDateStart { get; set; }
        public DateTimeOffset? RequestDateEnd { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }
}
