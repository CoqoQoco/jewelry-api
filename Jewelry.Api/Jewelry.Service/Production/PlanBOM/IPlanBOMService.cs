using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.PlanBOM
{
    public interface IPlanBOMService
    {
        Task<jewelry.Model.Production.PlanBOM.NewGet.Response> GetTransactionBOM(int productionPlanId);
        Task<jewelry.Model.Base.Response> SavePlanBom(jewelry.Model.Production.PlanBOM.Save.Request request);
        Task<List<jewelry.Model.Production.PlanBOM.NewGet.BOM>> GetPlanBom(int productionPlanId);
        IQueryable<jewelry.Model.Production.PlanBOM.List.Response> ListBom(jewelry.Model.Production.PlanBOM.List.Criteria request);
    }
}
