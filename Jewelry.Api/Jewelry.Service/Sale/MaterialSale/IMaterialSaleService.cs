using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.MaterialSale
{
    public interface IMaterialSaleService
    {
        Task<string> GenerateDocumentNumber();
        Task<jewelry.Model.Sale.MaterialSale.Create.Response> Create(jewelry.Model.Sale.MaterialSale.Create.Request request);
        Task<string> Update(jewelry.Model.Sale.MaterialSale.Update.Request request);
        Task<jewelry.Model.Sale.MaterialSale.Get.Response> Get(jewelry.Model.Sale.MaterialSale.Get.Request request);
        IQueryable<jewelry.Model.Sale.MaterialSale.List.Response> List(jewelry.Model.Sale.MaterialSale.List.Request request);
        Task<string> Confirm(jewelry.Model.Sale.MaterialSale.Confirm.Request request);
        Task<string> Cancel(jewelry.Model.Sale.MaterialSale.Cancel.Request request);
        Task<string> Delete(jewelry.Model.Sale.MaterialSale.Delete.Request request);
    }
}
