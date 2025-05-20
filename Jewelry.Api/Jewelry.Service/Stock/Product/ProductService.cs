using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using NetTopologySuite.Index.HPRtree;
using NPOI.SS.Formula.Atp;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Jewelry.Service.Stock.Product
{
    public class ProductService : BaseService, IProductService
    {

        private readonly string _admin = "@ADMIN";

        private readonly JewelryContext _jewelryContext;

        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public ProductService(JewelryContext JewelryContext, IHttpContextAccessor httpContextAccessor,
            IHostEnvironment HostingEnvironment,
            IRunningNumber runningNumberService) : base(JewelryContext, httpContextAccessor)
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
                stock = stock.Where(x => x.MoldDesign.Contains(request.Mold));
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

                               Mold = item.MoldDesign ?? item.Mold,

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
                                                 Region = material.Region,
                                                 Price = material.Price
                                             }).ToList()
                                             : new List<jewelry.Model.Stock.Product.List.Material>()
                           };

            return response;
        }
        public jewelry.Model.Stock.Product.Get.Response Get(jewelry.Model.Stock.Product.Get.Request request)
        {
            if (string.IsNullOrEmpty(request.StockNumber) && string.IsNullOrEmpty(request.ProductNumber))
            { 
                throw new HandleException("StockNumber or ProductNumber is Required");
            }

            var query = (from item in _jewelryContext.TbtStockProduct
                        .Include(x => x.TbtStockProductMaterial)
                         where item.Status == "Available"
                         select item);

            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                query = query.Where(x => x.StockNumber == request.StockNumber);
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.ProductNumber == request.ProductNumber);
            }

            if (!query.Any())
            { 
                throw new HandleException(ErrorMessage.NotFound);
            }

            var stock = query.FirstOrDefault();
            var response = new jewelry.Model.Stock.Product.Get.Response()
            {
                StockNumber = stock.StockNumber,
                ReceiptNumber = stock.ReceiptNumber,
                ReceiptType = stock.ProductionType,
                ReceiptDate = stock.ReceiptDate,
                ProductNumber = stock.ProductNumber,
                ProductNameTh = stock.ProductNameTh,
                ProductNameEn = stock.ProductNameEn,
                ProductType = stock.ProductType,
                ProductTypeName = stock.ProductTypeName,
                ProductPrice = stock.ProductPrice,
                Wo = stock.Wo,
                WoNumber = stock.WoNumber,
                WoText = $"{stock.Wo}{stock.WoNumber.ToString()}",
                ProductionDate = stock.CreateDate,
                ProductionTypeSize = stock.ProductionTypeSize,
                Mold = stock.MoldDesign ?? stock.Mold,
                ImageName = stock.ImageName,
                ImagePath = stock.ImagePath,
                Qty = stock.Qty,
                Location = stock.Location,
                Size = stock.Size,
                Remark = stock.Remark,
                CreateBy = stock.CreateBy,
                CreateDate = stock.CreateDate,
                UpdateBy = stock.UpdateBy,
                UpdateDate = stock.UpdateDate,
                Materials = (from material in stock.TbtStockProductMaterial
                             select new jewelry.Model.Stock.Product.Get.Material()
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
                                 Region = material.Region,
                                 Price = material.Price
                             }).ToList()
            };

            return response;
        }
        

        public async Task<string> Update(jewelry.Model.Stock.Product.Update.Request request)
        {
            CheckPermissionLevel("update_stock");

            var stock = (from item in _jewelryContext.TbtStockProduct
                         .Include(x => x.TbtStockProductMaterial)
                         where item.StockNumber == request.StockNumber
                         && item.ReceiptNumber == request.ReceiptNumber
                         select item).FirstOrDefault();

            if (stock == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                stock.ProductNameEn = request.ProductNameEn;
                stock.ProductNameTh = request.ProductNameTh;

                stock.ImagePath = request.ImagePath;
                stock.ImageName = request.ImageName;

                stock.Qty = request.Qty;
                stock.ProductPrice = request.ProductPrice;

                stock.MoldDesign = request.Mold;

                stock.Size = request.Size;
                stock.Location = request.Location;

                stock.UpdateDate = DateTime.UtcNow;
                stock.UpdateBy = CurrentUsername;


                if (stock.TbtStockProductMaterial.Any())
                {
                    _jewelryContext.TbtStockProductMaterial.RemoveRange(stock.TbtStockProductMaterial);
                }

                var newMats = new List<TbtStockProductMaterial>();
                if (request.Materials.Any())
                {
                    foreach (var item in request.Materials)
                    {
                        var newMat = new TbtStockProductMaterial
                        {
                            StockNumber = request.StockNumber,

                            Type = item.Type,
                            TypeName = item.TypeName,
                            TypeCode = item.TypeCode,
                            TypeBarcode = item.TypeBarcode,

                            Qty = item.Qty,
                            QtyUnit = item.QtyUnit,
                            Weight = item.Weight,
                            WeightUnit = item.WeightUnit,

                            Size = item.Size,
                            Region = item.Region,
                            Price = item.Price,

                            CreateBy = CurrentUsername,
                            CreateDate = DateTime.UtcNow
                        };
                        newMats.Add(newMat);
                    }
                }

                _jewelryContext.TbtStockProduct.Update(stock);
                if (newMats.Any())
                {
                    _jewelryContext.TbtStockProductMaterial.AddRange(newMats);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return "success";
        }

        public IQueryable<jewelry.Model.Stock.Product.ListName.Response> ListName(jewelry.Model.Stock.Product.ListName.Request request)
        {
            if (request.Mode == "TH")
            {
                var response = (
                    from item in _jewelryContext.TbtStockProduct
                    where item.ProductNameTh.Contains(request.Text)
                    select new jewelry.Model.Stock.Product.ListName.Response()
                    {
                        Text = item.ProductNameTh
                    }).Distinct();

                return response;
            }

            if (request.Mode == "EN")
            {
                var response = (
                    from item in _jewelryContext.TbtStockProduct
                    where item.ProductNameEn.Contains(request.Text)
                    select new jewelry.Model.Stock.Product.ListName.Response()
                    {
                        Text = item.ProductNameEn
                    }).Distinct();

                return response;
            }

            throw new HandleException("Mode is Required");
        }

    }
}
