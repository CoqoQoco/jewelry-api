using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.BillingNote
{
    public interface IBillingNoteService
    {
        Task<List<jewelry.Model.Sale.BillingNote.AvailableInvoices.Response>> AvailableInvoices(jewelry.Model.Sale.BillingNote.AvailableInvoices.Request request);
        Task<List<jewelry.Model.Sale.BillingNote.AvailableCustomers.Response>> AvailableCustomers();
        Task<List<jewelry.Model.Sale.BillingNote.PreviewProducts.Response>> PreviewProducts(jewelry.Model.Sale.BillingNote.PreviewProducts.Request request);
        Task<string> Create(jewelry.Model.Sale.BillingNote.Create.Request request);
        Task<jewelry.Model.Sale.BillingNote.Get.Response> Get(jewelry.Model.Sale.BillingNote.Get.Request request);
        IQueryable<jewelry.Model.Sale.BillingNote.List.Response> List(jewelry.Model.Sale.BillingNote.List.Request request);
        Task<string> Delete(jewelry.Model.Sale.BillingNote.Delete.Request request);
    }
}
