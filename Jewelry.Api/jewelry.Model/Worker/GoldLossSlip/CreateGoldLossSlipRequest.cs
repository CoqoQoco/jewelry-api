using System;
using System.Collections.Generic;

namespace jewelry.Model.Worker.GoldLossSlip
{
    public class CreateGoldLossSlipRequest
    {
        public string WorkerCode { get; set; }
        public string WorkerName { get; set; }
        public DateTimeOffset RequestDateStart { get; set; }
        public DateTimeOffset RequestDateEnd { get; set; }
        public string? Remark { get; set; }
        public List<CreateGoldLossSlipItem> Items { get; set; }
        public List<GoldReturnItem> GoldReturnItems { get; set; } = new List<GoldReturnItem>();
    }

    public class GoldReturnItem
    {
        public string GoldSize { get; set; }
        public decimal Weight { get; set; }
        public decimal PricePerGram { get; set; }
    }

    public class CreateGoldLossSlipItem
    {
        public int ProductionPlanId { get; set; }
        public string ItemNo { get; set; }
        public string Wo { get; set; }
        public int? WoNumber { get; set; }
        public string ProductNumber { get; set; }
        public string ProductName { get; set; }
        public string Gold { get; set; }
        public string GoldSize { get; set; }
        public DateTimeOffset? JobDate { get; set; }
        public decimal? GoldQtySend { get; set; }
        public decimal? GoldWeightSend { get; set; }
        public decimal? GoldQtyCheck { get; set; }
        public decimal? GoldWeightCheck { get; set; }
        public decimal? LossPercent { get; set; }
        public decimal? WeightLossAllowed { get; set; }
        public decimal? WeightLossActual { get; set; }
        public decimal? GoldLossPrice { get; set; }
        public decimal? MoneyDiff { get; set; }
    }
}
