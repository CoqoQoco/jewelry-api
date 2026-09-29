using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Shared
{
    public interface IProductionPlanWipHelper
    {
        Task<List<OpenPlanRow>> GetOpenPlansAsync();
        Task<int> GetMeltedOpenCountAsync();
        Task<Dictionary<int, DateTime>> GetLastMoveDatesAsync();
        Task<Dictionary<int, string>> GetStatusNamesAsync();
        Task<Dictionary<int, PlanLastActionInfo>> GetLastActionInfoAsync(IReadOnlyList<OpenPlanRow> plans);
    }
}
