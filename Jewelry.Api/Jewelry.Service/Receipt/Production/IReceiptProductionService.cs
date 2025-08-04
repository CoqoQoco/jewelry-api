using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Receipt.Production
{
    public interface IReceiptProductionService
    {
        IQueryable<jewelry.Model.Receipt.Production.PlanList.Response> ListPlan(jewelry.Model.Receipt.Production.PlanList.Search request);
        Task<jewelry.Model.Receipt.Production.PlanGet.Response> GetPlan(jewelry.Model.Receipt.Production.PlanGet.Request request);

        Task<jewelry.Model.Receipt.Production.Confirm.Response> Confirm(jewelry.Model.Receipt.Production.Confirm.Request request);
        IQueryable<jewelry.Model.Receipt.Production.History.List.Response> ListHistory(jewelry.Model.Receipt.Production.History.List.Search request);

        Task<string> Darft(jewelry.Model.Receipt.Production.Draft.Create.Request request);
        Task<string> ImportProduct();
        Task<string> ImportBraceletStock();
    }
}
