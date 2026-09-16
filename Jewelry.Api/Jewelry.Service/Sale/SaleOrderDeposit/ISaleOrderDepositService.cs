using System.Threading.Tasks;

namespace Jewelry.Service.Sale.SaleOrderDeposit
{
    public interface ISaleOrderDepositService
    {
        Task<string> Create(jewelry.Model.Sale.SaleOrderDeposit.Create.Request request);
        Task<jewelry.Model.Sale.SaleOrderDeposit.List.Response> List(jewelry.Model.Sale.SaleOrderDeposit.List.Request request);
        Task<string> Delete(jewelry.Model.Sale.SaleOrderDeposit.Delete.Request request);
    }
}
