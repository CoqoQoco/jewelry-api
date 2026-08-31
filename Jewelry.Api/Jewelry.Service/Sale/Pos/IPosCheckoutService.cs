using System.Threading.Tasks;

namespace Jewelry.Service.Sale.Pos
{
    public interface IPosCheckoutService
    {
        Task<jewelry.Model.Sale.Pos.Checkout.Response> Checkout(jewelry.Model.Sale.Pos.Checkout.Request request);
    }
}
