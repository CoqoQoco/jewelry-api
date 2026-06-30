using System;
using System.Collections.Generic;

namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class UpdateGoldLossTangSlipRequest
    {
        public long Id { get; set; }
        public string WorkerCode { get; set; }
        public string WorkerName { get; set; }
        public DateTimeOffset RequestDateStart { get; set; }
        public DateTimeOffset RequestDateEnd { get; set; }
        public decimal LossPercent { get; set; }
        public decimal PricePerGram { get; set; }
        public string? Remark { get; set; }
        public List<CreateGoldLossTangSlipItem> Items { get; set; } = new List<CreateGoldLossTangSlipItem>();
        public List<GoldLossTangExtraLine> IssuedLines { get; set; } = new List<GoldLossTangExtraLine>();
        public List<GoldLossTangExtraLine> ReturnedLines { get; set; } = new List<GoldLossTangExtraLine>();
    }
}
