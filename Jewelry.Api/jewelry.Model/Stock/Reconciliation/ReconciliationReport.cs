using System;
using System.Collections.Generic;

namespace Jewelry.Model.Stock.Reconciliation
{
    public class ReconciliationReport
    {
        public int NewPieceCount { get; set; }

        public int BalanceOnHandMismatchCount { get; set; }
        public int BalanceReservedMismatchCount { get; set; }

        public List<BalanceMismatch> OnHandMismatches { get; set; } = new();
        public List<BalanceMismatch> ReservedMismatches { get; set; } = new();

        public DateTime CheckedAt { get; set; }
    }

    public class BalanceMismatch
    {
        public string SkuCode { get; set; } = null!;
        public string LocationCode { get; set; } = null!;
        public decimal BalanceQty { get; set; }
        public int PieceCount { get; set; }
    }
}
