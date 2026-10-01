using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    public interface IProductionGoldLossTargetService
    {
        // ค่าล่าสุด (effective_from มากสุด) ต่อ (scope, worker_type, metal) — seed migration ใส่ไว้แล้วเสมอ
        // ทั้ง SLIP (50/80 × GOLD/SILVER) และ STAGE (60/80/90 × GOLD/SILVER)
        Task<Dictionary<(string Scope, int WorkerType, string Metal), GoldLossTargetRow>> GetCurrentTargetsAsync();

        Task<List<GoldLossTargetRow>> GetHistoryAsync(string scope, int workerType, string metal);

        Task SaveAsync(List<(string Scope, int WorkerType, string Metal, decimal TargetPercent)> items, string? remark, string createBy);
    }
}
