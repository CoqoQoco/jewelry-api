using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.GoldLossReconcileReport
{
    public class SearchResponse
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public List<GoldLossReconcileRow> Rows { get; set; } = new List<GoldLossReconcileRow>();
        public GoldLossReconcileSummary Summary { get; set; } = new GoldLossReconcileSummary();
    }

    public class GoldLossReconcileRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int StatusCode { get; set; }
        public string StatusName { get; set; } = string.Empty;

        // plan side — returned-only, same rule as GoldLossByStageReport
        public decimal PlanSumSend { get; set; }
        public decimal PlanSumCheck { get; set; }
        public decimal PlanRawLoss { get; set; }
        public decimal PlanLossPercent { get; set; }
        public int PlanRowsReturned { get; set; }
        public int PlanRowsPending { get; set; }

        // slip side — tbt_gold_loss_tang_slip (status 50) / tbt_worker_gold_loss_slip (status 80)
        public int SlipCount { get; set; }
        public decimal SlipIssued { get; set; }
        public decimal SlipReturned { get; set; }
        public decimal SlipRawLoss { get; set; }
        public decimal SlipAllowedLoss { get; set; }
        public decimal SlipDiffLoss { get; set; }
        public decimal SlipMoneyDiff { get; set; }
        public decimal SlipLossPercent { get; set; }

        // gap explainers
        public decimal ExtraIssuedWeight { get; set; }
        public decimal ExtraReturnedWeight { get; set; }
        public decimal ExtraReturnedNotCounted { get; set; }
        public decimal GapWeight { get; set; }
        public decimal GapExplainedByExtras { get; set; }
        public decimal GapUnexplained { get; set; }

        // slip-linkage coverage
        public int LinkedDetailRows { get; set; }
        public int UnlinkedDetailRows { get; set; }
        public decimal LinkCoveragePercent { get; set; }
    }

    public class GoldLossReconcileSummary
    {
        public decimal PlanSumSend { get; set; }
        public decimal PlanSumCheck { get; set; }
        public decimal PlanRawLoss { get; set; }
        public decimal PlanLossPercent { get; set; }
        public int PlanRowsReturned { get; set; }
        public int PlanRowsPending { get; set; }

        public int SlipCount { get; set; }
        public decimal SlipIssued { get; set; }
        public decimal SlipReturned { get; set; }
        public decimal SlipRawLoss { get; set; }
        public decimal SlipAllowedLoss { get; set; }
        public decimal SlipDiffLoss { get; set; }
        public decimal SlipMoneyDiff { get; set; }
        public decimal SlipLossPercent { get; set; }

        public decimal ExtraIssuedWeight { get; set; }
        public decimal ExtraReturnedWeight { get; set; }
        public decimal ExtraReturnedNotCounted { get; set; }
        public decimal GapWeight { get; set; }
        public decimal GapExplainedByExtras { get; set; }
        public decimal GapUnexplained { get; set; }

        public int LinkedDetailRows { get; set; }
        public int UnlinkedDetailRows { get; set; }
        public decimal LinkCoveragePercent { get; set; }
    }
}
