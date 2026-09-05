using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.GoldLossByWorkerReport
{
    public class Response
    {
        public List<WorkerStageRow> Rows { get; set; } = new List<WorkerStageRow>();
        public List<MonthlyTopRow> MonthlyTop { get; set; } = new List<MonthlyTopRow>();
        public List<MonthlyRow> MonthlyRows { get; set; } = new List<MonthlyRow>();
        public SummaryRow Summary { get; set; } = new SummaryRow();
    }

    public class WorkerStageRow
    {
        public string WorkerCode { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int JobCount { get; set; }
        public decimal SumGoldWeightSend { get; set; }
        public decimal SumGoldWeightCheck { get; set; }
        public decimal RawLoss { get; set; }
        public decimal LossPercent { get; set; }
        public decimal StageAvgLossPercent { get; set; }
        public decimal DiffFromStageAvgPercent { get; set; }
        public int RankInStage { get; set; }
        public bool IsBelowMinJobs { get; set; }
    }

    public class MonthlyTopRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int StatusCode { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string WorkerCode { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public decimal LossPercent { get; set; }
        public int JobCount { get; set; }
    }

    public class MonthlyRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string WorkerCode { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public decimal LossPercent { get; set; }
        public int JobCount { get; set; }
        public decimal SumGoldWeightSend { get; set; }
        public decimal SumGoldWeightCheck { get; set; }
        public decimal RawLoss { get; set; }
    }

    public class SummaryRow
    {
        public DateTimeOffset? PeriodStart { get; set; }
        public DateTimeOffset? PeriodEnd { get; set; }
        public int WorkerCount { get; set; }
        public int JobCount { get; set; }
        public int RowsMissingWorkerCount { get; set; }
        public decimal RowsMissingWorkerPercent { get; set; }
        public int RowsNotReturnedCount { get; set; }
        public decimal RowsNotReturnedPercent { get; set; }
        public List<StageSummaryRow> StageSummaries { get; set; } = new List<StageSummaryRow>();
    }

    public class StageSummaryRow
    {
        public int StatusCode { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public decimal AvgLossPercent { get; set; }
        public int JobCount { get; set; }
        public int WorkerCount { get; set; }
    }
}
