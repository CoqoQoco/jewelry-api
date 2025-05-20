using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.Product
{
    public interface IProductService
    {
        IQueryable<jewelry.Model.Stock.Product.List.Response> List(jewelry.Model.Stock.Product.List.Search request);
        jewelry.Model.Stock.Product.Get.Response Get(jewelry.Model.Stock.Product.Get.Request request);

        Task<string> Update(jewelry.Model.Stock.Product.Update.Request request);
        IQueryable<jewelry.Model.Stock.Product.ListName.Response> ListName(jewelry.Model.Stock.Product.ListName.Request request);
    }
}
