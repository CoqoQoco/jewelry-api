using System;
using System.Collections.Generic;

namespace Jewelry.Model.Stock.Reconciliation
{
    public class ReconciliationReport
    {
        public int LegacyStockCount { get; set; }
        public int NewPieceCount { get; set; }
        public int CountDrift => LegacyStockCount - NewPieceCount;

        public int BalanceOnHandMismatchCount { get; set; }
        public int BalanceReservedMismatchCount { get; set; }
        public int StatusMismatchCount { get; set; }
        public int LegacyQtySaleMismatchCount { get; set; }

        public List<BalanceMismatch> OnHandMismatches { get; set; } = new();
        public List<BalanceMismatch> ReservedMismatches { get; set; } = new();
        public List<StatusMismatch> StatusMismatches { get; set; } = new();
        public List<QtySaleMismatch> LegacyQtySaleMismatches { get; set; } = new();

        public DateTime CheckedAt { get; set; }
    }

    public class BalanceMismatch
    {
        public string SkuCode { get; set; } = null!;
        public string LocationCode { get; set; } = null!;
        public decimal BalanceQty { get; set; }
        public int PieceCount { get; set; }
    }

    public class StatusMismatch
    {
        public string StockNumber { get; set; } = null!;
        public string? PieceStatus { get; set; }
        public decimal LegacyQtySale { get; set; }
    }

    public class QtySaleMismatch
    {
        public string StockNumber { get; set; } = null!;
        public decimal LegacyQtySale { get; set; }
        public string? PieceStatus { get; set; }
    }
}
