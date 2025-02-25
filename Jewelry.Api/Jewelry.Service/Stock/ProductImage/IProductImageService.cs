using Jewelry.Service.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.ProductImage
{
    public interface IProductImageService
    {
        Task<string> Create(jewelry.Model.Stock.Product.Image.Create.Request request);

        IQueryable<jewelry.Model.Stock.Product.Image.List.Response> List(jewelry.Model.Stock.Product.Image.List.Search request);
    }
}
