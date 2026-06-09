using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Receipt.Outsource
{
    public interface IReceiptOutsourceService
    {
        Task<jewelry.Model.Receipt.Outsource.Confirm.Response> Confirm(jewelry.Model.Receipt.Outsource.Confirm.Request request);
        IQueryable<jewelry.Model.Receipt.Outsource.History.List.Response> ListHistory(jewelry.Model.Receipt.Outsource.History.List.Search request);
    }
}
