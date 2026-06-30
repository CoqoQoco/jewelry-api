using System;

namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class ListGoldLossTangSlipRequest
    {
        public string? WorkerCode { get; set; }
        public string? DocumentNo { get; set; }
        public DateTimeOffset? RequestDateStart { get; set; }
        public DateTimeOffset? RequestDateEnd { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }
}
