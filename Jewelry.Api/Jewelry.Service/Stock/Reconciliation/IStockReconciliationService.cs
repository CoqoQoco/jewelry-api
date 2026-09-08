using Jewelry.Model.Stock.Reconciliation;

namespace Jewelry.Service.Stock.Reconciliation
{
    public interface IStockReconciliationService
    {
        Task<ReconciliationReport> CheckDriftAsync(CancellationToken ct);
        Task<RebuildBalanceResult> RebuildBalanceFromPiecesAsync(CancellationToken ct);
    }
}
