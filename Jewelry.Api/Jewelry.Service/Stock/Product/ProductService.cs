using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NPOI.SS.Formula.Atp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Jewelry.Service.Stock.Product
{
    public class ProductService : IProductService
    {

        private readonly string _admin = "@ADMIN";

        private readonly JewelryContext _jewelryContext;

        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public ProductService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public IQueryable<jewelry.Model.Stock.Product.List.Response> List(jewelry.Model.Stock.Product.List.Search request)
        {
            var stock = (from item in _jewelryContext.TbtStockProduct
                         .Include(x => x.TbtStockProductMaterial)
                         where item.Status == "Available"
                         select item);

            if (request.ReceiptType != null && request.ReceiptType.Any())
            {
                var stockTypeArray = request.ReceiptType.Select(x => x).ToArray();
                stock = stock.Where(x => stockTypeArray.Contains(x.ProductionType));
            }
            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                stock = stock.Where(x => x.StockNumber.Contains(request.StockNumber));
            }
            if (!string.IsNullOrEmpty(request.Mold))
            {
                stock = stock.Where(x => x.Mold.Contains(request.Mold));
            }
            if (request.ProductType != null && request.ProductType.Any())
            {
                var productTypeArray = request.ProductType.Select(x => x).ToArray();
                stock = stock.Where(x => productTypeArray.Contains(x.ProductType));
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                stock = stock.Where(x => x.ProductNumber.Contains(request.ProductNumber));
            }
            if (!string.IsNullOrEmpty(request.ProductNameTh))
            {
                stock = stock.Where(x => x.ProductNameTh.Contains(request.ProductNameTh));
            }
            if (!string.IsNullOrEmpty(request.ProductNameEn))
            {
                stock = stock.Where(x => x.ProductNameEn.Contains(request.ProductNameEn));
            }
            if (!string.IsNullOrEmpty(request.Size))
            {
                stock = stock.Where(x => x.Size.Contains(request.Size));
            }

            var response = from item in stock
                           select new jewelry.Model.Stock.Product.List.Response()
                           {
                               StockNumber = item.StockNumber,
                               Status = item.Status,

                               ReceiptNumber = item.ReceiptNumber,
                               ReceiptDate = item.ReceiptDate,
                               ReceiptType = item.ProductionType,

                               Mold = item.Mold,

                               Qty = item.Qty,
                               ProductPrice = item.ProductPrice,

                               ProductNumber = item.ProductNumber,
                               ProductNameTh = item.ProductNameTh,
                               ProductNameEn = item.ProductNameEn,

                               ProductType = item.ProductType,
                               ProductTypeName = item.ProductTypeName,

                               ImageName = item.ImageName,
                               ImagePath = item.ImagePath,

                               Wo = item.Wo,
                               WoNumber = item.WoNumber,
                               WoText = $"{item.Wo}{item.WoNumber.ToString()}",

                               ProductionDate = item.CreateDate,
                               ProductionType = item.ProductionType,
                               ProductionTypeSize = item.ProductionTypeSize,

                               Size = item.Size,
                               Location = item.Location,
                               Remark = item.Remark,

                               CreateBy = item.CreateBy,
                               CreateDate = item.CreateDate,

                               Materials = item.TbtStockProductMaterial.Any() ?
                                            (from material in item.TbtStockProductMaterial
                                             select new jewelry.Model.Stock.Product.List.Material()
                                             {
                                                Type = material.Type,
                                                TypeName = material.TypeName,
                                                TypeCode = material.TypeCode,
                                                TypeBarcode = material.TypeBarcode,
                                                Qty = material.Qty,
                                                QtyUnit = material.QtyUnit,
                                                Weight = material.Weight,
                                                WeightUnit = material.WeightUnit,
                                                Size = material.Size,
                                                Price = material.Price
                                             }).ToList() 
                                             : new List<jewelry.Model.Stock.Product.List.Material>()
                           };

            return response;
        }
    }
}
