using Kendo.DynamicLinqCore;
using System;

namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class ListGoldLossTangSlipRequest : DataSourceRequest
    {
        public ListGoldLossTangSlipSearch Search { get; set; }
    }

    public class ListGoldLossTangSlipSearch
    {
        public string? WorkerCode { get; set; }
        public string? DocumentNo { get; set; }
        public DateTimeOffset? RequestDateStart { get; set; }
        public DateTimeOffset? RequestDateEnd { get; set; }
    }
}
