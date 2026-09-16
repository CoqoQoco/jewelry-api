using System;
using System.Collections.Generic;

namespace Jewelry.Model.Stock.Reconciliation
{
    public class PieceLedgerReport
    {
        public int IssueCount { get; set; }
        public List<PieceLedgerIssue> Items { get; set; } = new();

        public DateTime CheckedAt { get; set; }
    }

    public class PieceLedgerIssue
    {
        public string StockNumber { get; set; } = null!;
        public string ProductCode { get; set; } = null!;
        public string? Status { get; set; }
        public decimal Qty { get; set; }
        public decimal QtyReserved { get; set; }
        public decimal LedgerQty { get; set; }
        public decimal ConfirmedUninvoicedQty { get; set; }
        public List<string> Issues { get; set; } = new();
    }
}
