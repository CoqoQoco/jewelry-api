using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.Invoice
{
    public interface IInvoiceService
    {
        Task<string> Create(jewelry.Model.Sale.Invoice.Create.Request request);
        Task<jewelry.Model.Sale.Invoice.Get.Response> Get(jewelry.Model.Sale.Invoice.Get.Request request);
        IQueryable<jewelry.Model.Sale.Invoice.List.Response> List(jewelry.Model.Sale.Invoice.List.Request request);
        Task<string> Delete(jewelry.Model.Sale.Invoice.Delete.Request request);
        Task<string> GenerateInvoiceNumber();

        // Invoice Version methods
        Task<jewelry.Model.Sale.InvoiceVersion.Upsert.Response> UpsertVersion(jewelry.Model.Sale.InvoiceVersion.Upsert.Request request);
        Task<jewelry.Model.Sale.InvoiceVersion.Get.Response> GetVersion(jewelry.Model.Sale.InvoiceVersion.Get.Request request);
        IQueryable<jewelry.Model.Sale.InvoiceVersion.List.Response> ListVersions(jewelry.Model.Sale.InvoiceVersion.List.Request request);
    }
}