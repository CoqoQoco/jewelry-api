using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Receipt.Lot
{
    public interface IReceiptLotService
    {
        Task<List<jewelry.Model.Receipt.Lot.Search.Response>> Search(jewelry.Model.Receipt.Lot.Search.Request request);
    }
}
