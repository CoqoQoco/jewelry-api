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
        public ProductImageService(JewelryContext JewelryContext, 
            IHostEnvironment HostingEnvironment, 
            IRunningNumber runningNumberService, 
            IHttpContextAccessor httpContextAccessor) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public async Task<string> Create(jewelry.Model.Stock.Product.Image.Create.Request request)
        {
            var year = DateTime.UtcNow.Year;
            var name = request.Name.ToUpper();
            var namePath = $"{name}-{year}.png";

            var image = (from item in _jewelryContext.TbtStockProductImage
                         where item.Name == name 
                         && item.Year == year
                         && item.IsActive == true
                         select item).SingleOrDefault();

            if (image != null)
            {
                throw new HandleException($"{ErrorMessage.AlreadyExist} : {name}");
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)) 
            {
                var newImage = new TbtStockProductImage()
                {
                    Name = name,
                    Year = year,

                    NamePath = namePath,
                    Remark = request.Description ?? string.Empty,

                    IsActive = true,

                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername,
                };

                try
                {

                    string imagePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/Stock/Product");
                    if (!Directory.Exists(imagePath))
                    {
                        Directory.CreateDirectory(imagePath);
                    }

                    string imagePathWithFileName = Path.Combine(imagePath, $"{namePath}");

                    //https://www.thecodebuzz.com/how-to-save-iformfile-to-disk/
                    using (Stream fileStream = new FileStream(imagePathWithFileName, FileMode.Create, FileAccess.Write))
                    {
                        request.Image.CopyTo(fileStream);
                        fileStream.Close();
                    }
                }
                catch (Exception ex)
                {
                    //_logger.LogError($"ไม่สามารถบันทึกรูปภาพได้: {ex.Message}", ex);
                    throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                }

                _jewelryContext.TbtStockProductImage.Add(newImage);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

                return "success";
        }

        public IQueryable<jewelry.Model.Stock.Product.Image.List.Response> List(jewelry.Model.Stock.Product.Image.List.Search request)
        {
            var query = (from item in _jewelryContext.TbtStockProductImage
                         where item.IsActive == true
                         select new jewelry.Model.Stock.Product.Image.List.Response()
                         {
                             Id = item.Id,

                             Name = item.Name,
                             Year = item.Year,

                             CreateBy = item.CreateBy,
                             CreateDate = item.CreateDate,
                             UpdateBy = item.UpdateBy,
                             UpdateDate = item.UpdateDate,

                             IsActive = item.IsActive,
                             Remark = item.Remark,
                             NamePath = item.NamePath,

                         }).AsNoTracking();

            if (!string.IsNullOrEmpty(request.Name))
            { 
                query = query.Where(x => x.Name.Contains(request.Name.ToUpper()));
            }
            if (request.Year.HasValue)
            {
                query = query.Where(x => x.Year == request.Year.Value);
            }

            return query;
        }
    }
}
