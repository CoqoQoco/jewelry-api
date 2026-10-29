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

        // ล็อตที่จำนวน unit (ไม่ SOLD / RESERVED) ไม่ตรงกับ qty / qty_reserved ของ piece
        public int UnitCountMismatchCount { get; set; }
        public List<UnitCountMismatch> UnitCountMismatches { get; set; } = new();

        public DateTime CheckedAt { get; set; }
    }

    public class UnitCountMismatch
    {
        public string StockNumber { get; set; } = null!;
        public string ProductCode { get; set; } = null!;
        public decimal PieceQty { get; set; }
        public decimal UnitQty { get; set; }
        public decimal PieceReserved { get; set; }
        public decimal UnitReserved { get; set; }
    }

    public class BalanceMismatch
    {
        public string SkuCode { get; set; } = null!;
        public string LocationCode { get; set; } = null!;
        public decimal BalanceQty { get; set; }
        public decimal PieceCount { get; set; }
    }
}
