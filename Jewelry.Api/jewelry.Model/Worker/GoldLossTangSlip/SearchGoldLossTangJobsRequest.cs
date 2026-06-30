using System;
using System.Collections.Generic;

namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class SearchGoldLossTangJobsRequest
    {
        public string WorkerCode { get; set; }
        public DateTimeOffset? RequestDateStart { get; set; }
        public DateTimeOffset? RequestDateEnd { get; set; }
        public string? Wo { get; set; }
        public List<string>? GoldSize { get; set; }
    }

    public class SearchGoldLossTangJobsResponse
    {
        public int ProductionPlanId { get; set; }
        public string? ItemNo { get; set; }
        public string? Wo { get; set; }
        public int? WoNumber { get; set; }
        public string? WoText { get; set; }
        public string? ProductNumber { get; set; }
        public string? ProductName { get; set; }
        public string? Gold { get; set; }
        public string? GoldSize { get; set; }
        public DateTime? JobDate { get; set; }
        public decimal? GoldQtySend { get; set; }
        public decimal? GoldWeightSend { get; set; }
        public decimal? GoldQtyCheck { get; set; }
        public decimal? GoldWeightCheck { get; set; }
        public long? GoldLossTangSlipId { get; set; }
        public string? GoldLossTangSlipDocumentNo { get; set; }
    }
}
