using System;
using System.Collections.Generic;

namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class CreateGoldLossTangSlipRequest
    {
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

    public class CreateGoldLossTangSlipItem
    {
        public int ProductionPlanId { get; set; }
        public string ItemNo { get; set; }
        public string? Wo { get; set; }
        public int? WoNumber { get; set; }
        public string? ProductNumber { get; set; }
        public string? ProductName { get; set; }
        public string? Gold { get; set; }
        public string? GoldSize { get; set; }
        public DateTimeOffset? JobDate { get; set; }
    }

    public class GoldLossTangExtraLine
    {
        public string Name { get; set; }
        public decimal Weight { get; set; }
    }
}
