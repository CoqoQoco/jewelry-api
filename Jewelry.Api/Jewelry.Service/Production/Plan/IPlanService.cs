using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Plan
{
    public interface IPlanService
    {
        IQueryable<jewelry.Model.Production.Plan.StatusDetailList.Response> StatusDetailList(jewelry.Model.Production.Plan.StatusDetailList.RequestSearch request);
        IQueryable<jewelry.Model.Production.Plan.TransferList.Response> TransferList(jewelry.Model.Production.Plan.TransferList.RequestSearch request);
        Task<jewelry.Model.Production.Plan.Transfer.Response> Transfer(jewelry.Model.Production.Plan.Transfer.Request request);

        Task<jewelry.Model.Production.Plan.DailyPlan.Response> GetDailyReport(jewelry.Model.Production.Plan.DailyPlan.Criteria request);
    }
}
