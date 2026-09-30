using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    public interface IProductionStageStandardService
    {
        // ค่าล่าสุด (effective_from มากสุด) ต่อ dept_key
        Task<Dictionary<string, StageStandardRow>> GetCurrentStandardsAsync();

        Task<List<StageStandardRow>> GetHistoryAsync(string deptKey);

        Task SaveAsync(List<(string DeptKey, decimal StandardDays)> items, string? remark, string createBy);
    }
}
