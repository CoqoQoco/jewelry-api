using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.Quotation
{
    public interface IQuotationService
    {
        Task<string> Upsert(jewelry.Model.Sale.Quotation.Create.Request request);
        Task<jewelry.Model.Sale.Quotation.Get.Response> Get(jewelry.Model.Sale.Quotation.Get.Request request);
        IQueryable<jewelry.Model.Sale.Quotation.List.Response> List(jewelry.Model.Sale.Quotation.List.Request request);
    }
}
