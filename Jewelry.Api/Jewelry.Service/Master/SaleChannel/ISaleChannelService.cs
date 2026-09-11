using jewelry.Model.Master.SaleChannel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Master.SaleChannel
{
    public interface ISaleChannelService
    {
        Task<List<SaleChannelResponse>> List(SaleChannelListRequest req);
        Task<List<SaleChannelResponse>> Active();
        Task<SaleChannelResponse?> Current();
        Task<SaleChannelResponse> Get(string code);
        Task<string> Create(CreateSaleChannelRequest req);
        Task<string> Update(UpdateSaleChannelRequest req);
        Task Delete(string code);
    }
}
