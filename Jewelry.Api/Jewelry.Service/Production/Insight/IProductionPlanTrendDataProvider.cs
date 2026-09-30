using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    public interface IProductionPlanTrendDataProvider
    {
        Task<(List<ProductionPlanTrendEvaluator.PlanTrendRow> Plans, List<ProductionPlanFlowCalculator.HeaderRow> Headers)> GetTrendDataAsync();
    }
}
