using jewelry.Model.Exceptions;
using jewelry.Model.Mold;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.Extensions.Hosting;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Stock
{
    public interface IMoldService
    {
        Task<string> CreateMold(CreateMoldRequest request);
        Task<string> UpdateMold(UpdateMoldRequest request);
        IQueryable<TbtProductMold> SearchMold(SearchMold request);
    }
    public class MoldService : IMoldService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        public MoldService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
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

                    CreateDate = DateTime.UtcNow,
                    CreateBy = _admin,
                    IsActive = true,
                    Image = $"{request.Code.ToUpper().Trim()}-Mold.png",
                };

                _jewelryContext.TbtProductMold.Add(addMold);
                await _jewelryContext.SaveChangesAsync();

                try
                {

                    string imagePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/Mold");
                    if (!Directory.Exists(imagePath))
                    {
                        Directory.CreateDirectory(imagePath);
                    }
                    string imagePathWithFileName = Path.Combine(imagePath, addMold.Image);

                    //https://www.thecodebuzz.com/how-to-save-iformfile-to-disk/
                    using (Stream fileStream = new FileStream(imagePathWithFileName, FileMode.Create, FileAccess.Write))
                    {
                        request.Images.CopyTo(fileStream);
                        fileStream.Close();
                    }
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

            mold.Category = request.Category;
            mold.CategoryCode = request.CategoryCode;
            mold.Description = request.Description;

            mold.UpdateDate = DateTime.UtcNow;
            mold.UpdateBy = _admin;

            _jewelryContext.TbtProductMold.Update(mold);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }
        public IQueryable<TbtProductMold> SearchMold(SearchMold request)
        {
            var mold = (from item in _jewelryContext.TbtProductMold
                        select item);

            if (!string.IsNullOrEmpty(request.Text))
            {
                mold = (from item in mold
                        where item.Code.Contains(request.Text.ToUpper())
                        || item.Category.Contains(request.Text)
                        || item.Description.Contains(request.Text)
                        select item);
            }

            return mold.OrderByDescending(x => x.CreateDate);
        }
    }

}
