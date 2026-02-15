using jewelry.Model.Exceptions;
using jewelry.Model.Mold;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Jewelry.Service.Stock
{
    public interface IMoldService
    {
        Task<string> CreateMold(CreateMoldRequest request);
        Task<string> UpdateMold(UpdateMoldRequest request);
        IQueryable<TbtProductMold> SearchMold(SearchMold request);
    }
    public class MoldService : BaseService, IMoldService
    {
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IAzureBlobStorageService _azureBlobService;

        public MoldService(JewelryContext JewelryContext,
             IHostEnvironment HostingEnvironment,
             IAzureBlobStorageService azureBlobService,
             IHttpContextAccessor httpContextAccessor) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _azureBlobService = azureBlobService;
        }
        public async Task<string> CreateMold(CreateMoldRequest request)
        {
            var mold = (from item in _jewelryContext.TbtProductMold
                        where item.Code == request.Code.ToUpper()
                        select item).SingleOrDefault();

            if (mold != null)
            {
                throw new HandleException($"มีข้อมูลเเม่พิมพ์รหัส {request.Code.ToUpper()} อยู่เเล้ว ไม่สามารถสร้างรายการซ้ำได้");
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var addMold = new TbtProductMold()
                {
                    Code = request.Code.Trim().ToUpper(),
                    Category = request.Category,
                    CategoryCode = request.CategoryCode,
                    Description = request.Description,

                    MoldBy = request.MoldBy,

                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername,
                    IsActive = true,
                    Image = $"{request.Code.ToUpper().Trim()}-Mold.png",
                };

                _jewelryContext.TbtProductMold.Add(addMold);
                await _jewelryContext.SaveChangesAsync();

                try
                {
                    // Upload to Azure Blob Storage (Single Container Architecture)
                    using var stream = request.Images.OpenReadStream();
                    var result = await _azureBlobService.UploadImageAsync(
                        stream,
                        "Mold",  // folder name in jewelry-images container
                        addMold.Image
                    );

                    if (!result.Success)
                    {
                        throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                    }

                    // อัพเดท Image path: "Mold/filename.jpg"
                    addMold.Image = result.BlobName;
                    _jewelryContext.TbtProductMold.Update(addMold);
                    await _jewelryContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                }

                scope.Complete();
            }

            return "success";
        }
        public async Task<string> UpdateMold(UpdateMoldRequest request)
        {
            var mold = (from item in _jewelryContext.TbtProductMold
                        where item.Code == request.Code.ToUpper()
                        select item).SingleOrDefault();
            if (mold == null)
            {
                throw new HandleException($"ไม่พบข้อมูลเเม่พิมพ์รหัส {request.Code.ToUpper()} กรุณาลองอีกครั้ง");
            }
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                if (request.ImagesMain != null)
                {
                    var fileName = $"{request.Code.ToUpper().Trim()}-Mold.png";

                    // Upload to Azure Blob Storage (Single Container Architecture)
                    using var stream = request.ImagesMain.OpenReadStream();
                    var result = await _azureBlobService.UploadImageAsync(
                        stream,
                        "Mold",  // folder name in jewelry-images container
                        fileName
                    );

                    if (!result.Success)
                    {
                        throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                    }

                    mold.Image = result.BlobName;
                }
                if (request.ImagesSub != null)
                {
                    var fileName = $"{request.Code.ToUpper().Trim()}-Sub-Mold.png";

                    // Upload to Azure Blob Storage (Single Container Architecture)
                    using var stream = request.ImagesSub.OpenReadStream();
                    var result = await _azureBlobService.UploadImageAsync(
                        stream,
                        "Mold",  // folder name in jewelry-images container
                        fileName
                    );

                    if (!result.Success)
                    {
                        throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                    }

                    mold.ImageDraft1 = result.BlobName;
                }
                mold.Category = request.Category;
                mold.CategoryCode = request.CategoryCode;
                mold.Description = request.Description;
                mold.MoldBy = request.MoldBy;
                mold.UpdateDate = DateTime.UtcNow;
                mold.UpdateBy = CurrentUsername;
                _jewelryContext.TbtProductMold.Update(mold);
                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }
            return "success";
        }
        public IQueryable<TbtProductMold> SearchMold(SearchMold request)
        {
            var mold = (from item in _jewelryContext.TbtProductMold
                        where item.IsActive == true
                        select item);

            if (!string.IsNullOrEmpty(request.Text))
            {
                mold = (from item in mold
                        where item.Code.Contains(request.Text.ToUpper())
                        || item.Category.Contains(request.Text)
                        || item.Description.Contains(request.Text)
                        || item.MoldBy.Contains(request.Text)
                        select item);
            }

            if (request.UpdateStart.HasValue)
            {
                mold = mold.Where(x => x.UpdateDate >= request.UpdateStart.Value.StartOfDayUtc());
            }
            if (request.UpdateEnd.HasValue)
            {
                mold = mold.Where(x => x.UpdateDate <= request.UpdateEnd.Value.EndOfDayUtc());
            }

            return mold;
        }
    }

}
