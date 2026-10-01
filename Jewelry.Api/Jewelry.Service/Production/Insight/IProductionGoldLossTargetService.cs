using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    public interface IProductionGoldLossTargetService
    {
        // ค่าล่าสุด (effective_from มากสุด) ต่อ (worker_type, metal) — seed migration ใส่ไว้แล้วเสมอทั้ง 4 ชุด
        // (50|GOLD, 80|GOLD, 50|SILVER, 80|SILVER)
        Task<Dictionary<(int WorkerType, string Metal), GoldLossTargetRow>> GetCurrentTargetsAsync();

        Task<List<GoldLossTargetRow>> GetHistoryAsync(int workerType, string metal);

        Task SaveAsync(List<(int WorkerType, string Metal, decimal TargetPercent)> items, string? remark, string createBy);
    }
}
