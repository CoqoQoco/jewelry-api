using jewelry.Model.Stock.Product.PriceCheck;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.PriceSanity
{
    public interface IPriceSanityService
    {
        Task<Response> Check(Request request);
    }
}
