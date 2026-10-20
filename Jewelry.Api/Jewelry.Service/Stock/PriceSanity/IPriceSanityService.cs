using jewelry.Model.Stock.Product.PriceCheck;
using Kendo.DynamicLinqCore;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.PriceSanity
{
    public interface IPriceSanityService
    {
        Task<Response> Check(Request request);
        Task<jewelry.Model.Stock.Product.PriceReviewAck.Response> Ack(jewelry.Model.Stock.Product.PriceReviewAck.AckRequest request);
        Task<jewelry.Model.Stock.Product.PriceReviewAck.Response> Unack(jewelry.Model.Stock.Product.PriceReviewAck.UnackRequest request);
        Task<DataSourceResult> OutlierList(jewelry.Model.Stock.Product.PriceOutlierList.Request request);
    }
}
