using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.SaleOrder
{
    public interface ISaleOrderService
    {
        Task<string> Upsert(jewelry.Model.Sale.SaleOrder.Create.Request request);
        Task<jewelry.Model.Sale.SaleOrder.Get.Response> Get(jewelry.Model.Sale.SaleOrder.Get.Request request);
        IQueryable<jewelry.Model.Sale.SaleOrder.List.Response> List(jewelry.Model.Sale.SaleOrder.List.Request request);
        Task<string> GenerateRunningNumber();
        Task<jewelry.Model.Sale.SaleOrder.ConfirmStock.Response> ConfirmStockItems(jewelry.Model.Sale.SaleOrder.ConfirmStock.Request request);
        Task<jewelry.Model.Sale.SaleOrder.UnconfirmStock.Response> UnconfirmStockItems(jewelry.Model.Sale.SaleOrder.UnconfirmStock.Request request);
    }
}