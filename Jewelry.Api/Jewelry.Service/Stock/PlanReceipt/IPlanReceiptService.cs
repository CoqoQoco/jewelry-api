using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.PlanReceipt
{
    public interface IPlanReceiptService
    {
        IQueryable<jewelry.Model.Stock.Product.Plan.Receipt.List.Response> List(jewelry.Model.Stock.Product.Plan.Receipt.List.RequestSearch request);
    }
}
