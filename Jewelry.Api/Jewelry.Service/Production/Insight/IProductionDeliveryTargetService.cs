using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    public interface IProductionDeliveryTargetService
    {
        // ค่าล่าสุด (effective_from มากสุด) — seed migration ใส่ไว้แล้วเสมอ (80%) จึงไม่ควรเป็น null จริง
        Task<DeliveryTargetRow?> GetCurrentTargetAsync();

        Task<List<DeliveryTargetRow>> GetHistoryAsync();

        Task SaveAsync(decimal targetPercent, string? remark, string createBy);
    }
}
