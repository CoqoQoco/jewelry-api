using jewelry.Model.GoldPrice;
using System.Threading.Tasks;

namespace Jewelry.Service.GoldPrice
{
    public interface IGoldPriceService
    {
        Task<TodayGoldPriceResponse> Today(TodayGoldPriceRequest request);
        Task<HistoryGoldPriceResponse> History(HistoryGoldPriceRequest request);
    }
}
