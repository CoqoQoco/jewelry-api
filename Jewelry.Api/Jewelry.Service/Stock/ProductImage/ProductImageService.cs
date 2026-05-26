using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Stock.ProductImage
{
    public class ProductImageService : BaseService, IProductImageService
    {
        private readonly JewelryContext _jewelryContext;

        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        private readonly IAzureBlobStorageService _azureBlobService;
        public ProductImageService(JewelryContext JewelryContext,
            IHostEnvironment HostingEnvironment,
            IRunningNumber runningNumberService,
            IAzureBlobStorageService azureBlobService,
            IHttpContextAccessor httpContextAccessor) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
            _azureBlobService = azureBlobService;
        }

        public async Task<string> Create(jewelry.Model.Stock.Product.Image.Create.Request request)
        {
            var name = request.Name.ToUpper();
            var namePath = $"{name}.jpg";

            using var stream = request.Image.OpenReadStream();
            var result = await _azureBlobService.UploadImageAsync(
                stream,
                "Stock/Product",
                namePath
            );

            if (!result.Success)
            {
                throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
            }

            return "success";
        }

        public IQueryable<jewelry.Model.Stock.Product.Image.List.Response> List(jewelry.Model.Stock.Product.Image.List.Search request)
        {
            return Enumerable.Empty<jewelry.Model.Stock.Product.Image.List.Response>().AsQueryable();
        }
    }
}
