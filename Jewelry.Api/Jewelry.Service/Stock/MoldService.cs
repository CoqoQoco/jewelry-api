using jewelry.Model.Exceptions;
using jewelry.Model.Mold;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
        IQueryable<SearchMoldResponse> SearchMold(SearchMold request);
        Task<int> GetProductionCount(string moldCode);
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
        public async Task<int> GetProductionCount(string moldCode)
        {
            return await _jewelryContext.TbtProductionPlan
                .Where(p => p.Mold == moldCode)
                .CountAsync();
        }

        public IQueryable<SearchMoldResponse> SearchMold(SearchMold request)
        {
            var mold = _jewelryContext.TbtProductMold
                .Where(x => x.IsActive == true);

            if (!string.IsNullOrEmpty(request.Text))
            {
                mold = mold.Where(x =>
                    x.Code.Contains(request.Text.ToUpper())
                    || x.Category.Contains(request.Text)
                    || x.Description.Contains(request.Text)
                    || x.MoldBy.Contains(request.Text));
            }

            if (request.UpdateStart.HasValue)
            {
                mold = mold.Where(x => x.UpdateDate >= request.UpdateStart.Value.StartOfDayUtc());
            }
            if (request.UpdateEnd.HasValue)
            {
                mold = mold.Where(x => x.UpdateDate <= request.UpdateEnd.Value.EndOfDayUtc());
            }

            var result = from item in mold
                         join plan in _jewelryContext.TbtProductMoldPlan
                             on item.PlanId equals plan.Id into planGroup
                         from plan in planGroup.DefaultIfEmpty()
                         join design in _jewelryContext.TbtProductMoldPlanDesign
                             on plan.Id equals design.PlanId into designGroup
                         from design in designGroup.DefaultIfEmpty()
                         join productType in _jewelryContext.TbmProductType
                             on design.CategoryCode equals productType.Code into ptGroup
                         from productType in ptGroup.DefaultIfEmpty()
                         select new SearchMoldResponse
                         {
                             Code = item.Code,
                             Description = item.Description,
                             Image = item.Image,
                             ImageDraft1 = item.ImageDraft1,
                             ImageDraft2 = item.ImageDraft2,
                             ImageDraft3 = item.ImageDraft3,
                             Category = item.Category,
                             CategoryCode = item.CategoryCode,
                             MoldBy = item.MoldBy,
                             CodeDraft = item.CodeDraft,
                             ReModelMold = item.ReModelMold,
                             PlanId = item.PlanId,
                             Status = item.Status,
                             IsActive = item.IsActive,
                             CreateDate = item.CreateDate,
                             CreateBy = item.CreateBy,
                             UpdateDate = item.UpdateDate,
                             UpdateBy = item.UpdateBy,
                             DesignImage = design != null ? design.ImageUrl : null,
                             ProductTypeCode = productType != null ? productType.Code : null,
                             ProductTypeDescription = productType != null ? productType.NameTh : null,
                         };

            return result;
        }
    }

}
