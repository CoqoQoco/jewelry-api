using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanCreate;
using jewelry.Model.ProductionPlan.ProductionPlanTracking;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NPOI.HPSF;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.ProductionPlan
{
    public interface IProductionPlanService
    {
        Task<ProductionPlanCreateResponse> ProductionPlanCreate(ProductionPlanCreateRequest request);
        Task<ProductionPlanCreateResponse> ProductionPlanCreateImage(List<IFormFile> images, string wo, int woNumber);
        IQueryable<TbtProductionPlan> ProductionPlanSearch(ProductionPlanTracking request);
    }
    public class ProductionPlanService : IProductionPlanService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        public ProductionPlanService(JewelryContext JewelryContext)
        {
            _jewelryContext = JewelryContext;
        }

        #region ----- Production Plan -----
        public async Task<ProductionPlanCreateResponse> ProductionPlanCreate(ProductionPlanCreateRequest request)
        {

            if (request.Material.Count <= 0)
            {
                throw new HandleException($"กรุณาระบุส่วนประกอบใบจ่าย-รับคืนงาน");
            }

            var checkDubPlan = (from item in _jewelryContext.TbtProductionPlan
                                where item.Wo == request.Wo.ToUpper()
                                && item.WoNumber == request.WoNumber
                                select item).SingleOrDefault();

            if (checkDubPlan != null)
            {
                throw new HandleException($"ใบจ่าย-รับคืนงาน {request.Wo}-{request.WoNumber} ทำซ้ำ กรุณาสร้างหมายเลขใหม่");
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var createPlan = new TbtProductionPlan()
                {
                    Wo = request.Wo.ToUpper().Trim(),
                    WoNumber = request.WoNumber,
                    RequestDate = request.RequestDate.UtcDateTime,

                    Mold = request.Mold.Trim(),
                    ProductNumber = request.ProductNumber.Trim(),
                    CustomerNumber = request.CustomerNumber.Trim(),
                    Remark = !string.IsNullOrEmpty(request.Remark) ? request.Remark.Trim() : string.Empty,

                    Qty = request.Qty,
                    QtyFinish = request.QtyFinish,
                    QtySemiFinish = request.QtySemiFinish,
                    QtyCast = request.QtyCast,
                    QtyUnit = request.QtyUnit,

                    IsActive = true,
                    Status = ProductionPlanStatus.Processing,

                    CreateDate = DateTime.UtcNow,
                    CreateBy = _admin,
                };
                _jewelryContext.TbtProductionPlan.Add(createPlan);
                _jewelryContext.SaveChanges();

                if (!request.Material.Any())
                {
                    throw new HandleException($"กรุณาระบุส่วนประกอบใบจ่าย-รับคืนงาน");
                }

                var createMaterials = new List<TbtProductionPlanMaterial>();
                foreach (var material in request.Material)
                {
                    var createMaterial = new TbtProductionPlanMaterial()
                    {
                        Material = material.Material.ToUpper().Trim(),
                        MaterialSize = material.MaterialSize.Trim(),
                        MaterialType = material.MaterialType.Trim(),
                        MaterialQty = material.MaterialQty.Trim(),

                        MaterialRemark = !string.IsNullOrEmpty(material.MaterialRemark) ? material.MaterialRemark.Trim() : string.Empty,

                        ProductionPlanId = createPlan.Id,

                        IsActive = true,
                        CreateDate = DateTime.UtcNow,
                        CreateBy = _admin,
                    };
                    createMaterials.Add(createMaterial);
                }
                _jewelryContext.TbtProductionPlanMaterial.AddRange(createMaterials);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }
            return new ProductionPlanCreateResponse();
        }
        public async Task<ProductionPlanCreateResponse> ProductionPlanCreateImage(List<IFormFile> images, string wo, int woNumber)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan
                        where item.Wo == wo.ToUpper().Trim()
                        && item.WoNumber == woNumber
                        select item).SingleOrDefault();

            if (plan == null)
            {
                return new ProductionPlanCreateResponse() 
                { 
                    Code = 400,
                    Message = "บันทึกรุปไม่สำเร็จ กรุุณาทำรายการภายหลังอีกครั้ง"
                };
            }

            try
            {
                var createImages = new List<TbtProductionPlanImage>();

                // เรียกใช้งาน request.Images เพื่อเข้าถึงรูปภาพแต่ละรูปใน List
                foreach (var image in images)
                {
                    int no = 1;

                    // ตรวจสอบว่าไดรฟ์ D: มีสิทธิ์ในการเขียนไฟล์หรือไม่
                    string imageName = $"{wo.ToUpper().Trim()}-{woNumber}-{no}";
                    string baseDirectory = "D:\\Jewelry\\Image\\ProductionPlan";
                    string destinationPath = Path.Combine(baseDirectory, imageName);
                    if (System.IO.File.Exists(destinationPath))
                    {
                        System.IO.File.Delete(destinationPath);
                    }

                    using (FileStream fs = new FileStream(destinationPath, FileMode.CreateNew))
                    {
                        image.CopyTo(fs);
                    }

                    var createImage = new TbtProductionPlanImage()
                    {
                        ProductionPlanId = plan.Id,
                        Number = no,

                        Path = imageName,

                        IsActive = true,
                        CreateDate = DateTime.UtcNow,
                        CreateBy = _admin,
                    };

                    createImages.Add(createImage);
                    no += 1;
                }
                _jewelryContext.TbtProductionPlanImage.AddRange(createImages);
                await _jewelryContext.SaveChangesAsync();
                return new ProductionPlanCreateResponse();
            }
            catch (Exception ex)
            {
                return new ProductionPlanCreateResponse()
                {
                    Code = 400,
                    Message = "บันทึกรุปไม่สำเร็จ กรุุณาทำรายการภายหลังอีกครั้ง",
                    Error = ex.Message
                };
            }
        }
        public IQueryable<TbtProductionPlan> ProductionPlanSearch(ProductionPlanTracking request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlan.Include(x => x.TbtProductionPlanImage)
                         select item);

            //query = query.Where(x => x.GIDate >= request.DateFrom.StartOfDayUtc() && x.GIDate <= request.DateTo.EndOfDayUtc());

            if (request.Start.HasValue)
            {
                query = query.Where(x => x.RequestDate >= request.Start.Value.StartOfDayUtc());
            }
            if (request.End.HasValue)
            {
                query = query.Where(x => x.RequestDate <= request.End.Value.StartOfDayUtc());
            }
            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query
                         where item.Wo.Contains(request.Text.ToUpper())
                         || item.Mold.Contains(request.Text)
                         || item.ProductNumber.Contains(request.Text)
                         || item.CustomerNumber.Contains(request.Text)
                         || item.CreateBy.Contains(request.Text)
                         select item);
            }

            return query.OrderByDescending(x => x.RequestDate) ;
        }

        
        #endregion
    }
}
