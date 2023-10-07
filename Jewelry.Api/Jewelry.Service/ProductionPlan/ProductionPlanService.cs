using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanCreate;
using jewelry.Model.ProductionPlan.ProductionPlanDelete;
using jewelry.Model.ProductionPlan.ProductionPlanTracking;
using jewelry.Model.ProductionPlan.ProductionPlanUpdate;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Index.HPRtree;
using Newtonsoft.Json;
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
        TbtProductionPlan ProductionPlanGet(int id);
        IQueryable<TbtProductionPlanMaterial> ProductionPlanMateriaGet(ProductionPlanTrackingMaterialRequest request);
        Task<string> ProductionPlanUpdateStatus(ProductionPlanUpdateStatusRequest request);
        Task<string> ProductionPlanUpdateHeader(ProductionPlanUpdateHeaderRequest request);
        Task<string> ProductionPlanDeleteMaterial(ProductionPlanMaterialDeleteRequest request);
        Task<string> ProductionPlanUpdateMaterial(ProductionPlanUpdateMaterialRequest request);


        IQueryable<TbmProductionPlanStatus> GetProductionPlanStatus();
    }
    public class ProductionPlanService : IProductionPlanService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        public ProductionPlanService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
        }

        #region ----- Production Plan -----

        // ----- Create ----- //
        public async Task<ProductionPlanCreateResponse> ProductionPlanCreate(ProductionPlanCreateRequest request)
        {

            //if (request.Material.Count <= 0)
            //{
            //    throw new HandleException($"กรุณาระบุส่วนประกอบใบจ่าย-รับคืนงาน");
            //}
            try
            {

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
                        Mold = request.Mold.Trim(),

                        CustomerNumber = request.CustomerNumber.Trim(),
                        CustomerType = request.CustomerType.Trim(),
                        RequestDate = request.RequestDate.UtcDateTime,

                        ProductNumber = request.ProductNumber.Trim(),
                        ProductName = request.ProductName.Trim(),
                        ProductType = request.ProductType.Trim(),

                        ProductQty = request.ProductQty,
                        ProductQtyUnit = request.ProductQtyUnit,

                        ProductDetail = request.ProductDetail.Trim(),
                        Remark = !string.IsNullOrEmpty(request.Remark) ? request.Remark.Trim() : string.Empty,

                        IsActive = true,
                        Status = ProductionPlanStatus.Designed,

                        CreateDate = DateTime.UtcNow,
                        CreateBy = _admin,
                    };
                    _jewelryContext.TbtProductionPlan.Add(createPlan);
                    _jewelryContext.SaveChanges();

                    if (!request.Material.Any())
                    {
                        throw new HandleException($"กรุณาระบุส่วนประกอบใบจ่าย-รับคืนงาน");
                    }

                    var materials = JsonConvert.DeserializeObject<List<ProductionPlanMaterialCreateRequest>>(request.Material);


                    if (materials == null || !materials.Any())
                    {
                        throw new HandleException($"กรุณาระบุส่วนประกอบใบจ่าย-รับคืนงาน");
                    }

                    var createMaterials = new List<TbtProductionPlanMaterial>();
                    foreach (var material in materials)
                    {
                        var createMaterial = new TbtProductionPlanMaterial()
                        {
                            Gold = material.Gold.Code,
                            GoldSize = material.GoldSize?.Code,
                            GoldQty = material.GoldQty,

                            Gem = material.Gem?.Code,
                            GemShape = material.GemShape?.Code,
                            GemQty = material.GemQty ?? default,
                            GemUnit = material.GemUnit,
                            GemSize = material.GemSize,
                            GemWeight = material.GemWeight,
                            GemWeightUnit = material.GemWeightUnit,

                            DiamondQty = material.DiamondQty ?? default,
                            DiamondUnit = material.DiamondUnit,
                            DiamondQuality = material.DiamondQuality,
                            DiamondWeight = material.DiamondWeight,
                            DiamondWeightUnit = material.DiamondWeightUnit,
                            DiamondSize = material.DiamondSize,

                            ProductionPlanId = createPlan.Id,

                            IsActive = true,
                            CreateDate = DateTime.UtcNow,
                            CreateBy = _admin,
                        };
                        createMaterials.Add(createMaterial);
                    }
                    _jewelryContext.TbtProductionPlanMaterial.AddRange(createMaterials);
                    await _jewelryContext.SaveChangesAsync();

                    if (request.Images == null)
                    {
                        throw new HandleException($"กรุณาระบุรูปภาพสินค้า");
                    }

                    string _imageName = $"{request.Wo.ToUpper().Trim()}-{request.WoNumber}-Product.png";
                    try
                    {

                        // combind path
                        //string imagePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/OrderPlan");
                        string imagePath = Path.Combine("Images", "/OrderPlan");

                        //check CreateDirectory
                        if (!Directory.Exists(imagePath))
                        {
                            Directory.CreateDirectory(imagePath);
                        }

                        //combine image name
                        string imagePathWithFileName = Path.Combine(imagePath, _imageName);

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


                    var createImage = new TbtProductionPlanImage()
                    {
                        ProductionPlanId = createPlan.Id,
                        Number = 0,

                        Path = _imageName,

                        IsActive = true,
                        CreateDate = DateTime.UtcNow,
                        CreateBy = _admin,
                    };
                    _jewelryContext.TbtProductionPlanImage.Add(createImage);
                    await _jewelryContext.SaveChangesAsync();

                    scope.Complete();
                }
                return new ProductionPlanCreateResponse();
            }
            catch (HandleException ex)
            {
                throw new HandleException(ex.Message);
            }
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
            var query = (from item in _jewelryContext.TbtProductionPlan
                         .Include(x => x.TbtProductionPlanImage)
                         //.Include(x => x.TbtProductionPlanMaterial)
                         .Include(x => x.StatusNavigation)
                         where item.IsActive == true
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

            return query.OrderByDescending(x => x.CreateDate);
        }
        public TbtProductionPlan ProductionPlanGet(int id)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan
                         .Include(x => x.ProductTypeNavigation)
                         //.Include(x => x.CustomerTypeNavigation)
                         .Include(x => x.TbtProductionPlanImage)
                         //.Include(x => x.TbtProductionPlanMaterial)
                         .Include(x => x.StatusNavigation)
                        where item.IsActive == true
                        && item.Id == id
                        select item).SingleOrDefault();

            if (plan == null)
            {
                throw new HandleException("ไม่พบข้อมูล กรุณาลองใหม่อีกครั้ง");
            }

            return plan;
        }
        public IQueryable<TbtProductionPlanMaterial> ProductionPlanMateriaGet(ProductionPlanTrackingMaterialRequest request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlanMaterial
                         .Include(x => x.GoldNavigation)
                         .Include(x => x.GoldSizeNavigation)
                         .Include(x => x.GemNavigation)
                         .Include(x => x.GemShapeNavigation)
                         where item.ProductionPlanId == request.Id
                         && item.IsActive == true
                         select item);

            return query;
        }

        // ----- Update ----- //
        public async Task<string> ProductionPlanUpdateStatus(ProductionPlanUpdateStatusRequest request)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan
                        where item.Id == request.Id
                        && item.Wo == request.Wo
                        && item.WoNumber == request.WoNumber
                        select item).SingleOrDefault();

            if (plan == null)
            {
                throw new HandleException($"ไม่พบใบจ่ายขรับคืนงาน {request.Wo}-{request.WoNumber}");
            }

            plan.Status = request.Status;
            plan.UpdateDate = DateTime.UtcNow;
            plan.UpdateBy = _admin;

            _jewelryContext.TbtProductionPlan.Update(plan);
            await _jewelryContext.SaveChangesAsync();


            return $"{plan.Wo}-{plan.WoNumber}";
        }
        public async Task<string> ProductionPlanUpdateHeader(ProductionPlanUpdateHeaderRequest request)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan
                        where item.Id == request.Id
                        && item.Wo == request.Wo
                        && item.WoNumber == request.WoNumber
                        select item).SingleOrDefault();

            if (plan == null)
            {
                throw new HandleException($"ไม่พบใบจ่ายขรับคืนงาน {request.Wo}-{request.WoNumber}");
            }

            plan.RequestDate = request.RequestDate.UtcDateTime;

            plan.ProductQty = request.ProductQty;
            plan.ProductQtyUnit = request.ProductQtyUnit;

            plan.ProductNumber = request.ProductNumber;
            plan.ProductName = request.ProductName;
            plan.ProductDetail = request.ProductDetail;

            plan.Remark = request.Remark ?? plan.Remark;

            plan.UpdateDate = DateTime.UtcNow;
            plan.UpdateBy = _admin;

            _jewelryContext.TbtProductionPlan.Update(plan);
            await _jewelryContext.SaveChangesAsync();

            return $"{plan.Wo}-{plan.WoNumber}";
        }
        public async Task<string> ProductionPlanUpdateMaterial(ProductionPlanUpdateMaterialRequest request)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan
                        where item.Id == request.Id
                        && item.Wo == request.Wo
                        && item.WoNumber == request.WoNumber
                        select item).SingleOrDefault();

            if (plan == null)
            {
                throw new HandleException($"ไม่พบใบจ่ายขรับคืนงาน {request.Wo}-{request.WoNumber}");
            }

            var createMaterial = new TbtProductionPlanMaterial()
            {
                Gold = request.Material.Gold.Code,
                GoldSize = request.Material.GoldSize?.Code,
                GoldQty = request.Material.GoldQty,

                Gem = request.Material.Gem?.Code,
                GemShape = request.Material.GemShape?.Code,
                GemQty = request.Material.GemQty ?? default,
                GemUnit = request.Material.GemUnit,
                GemSize = request.Material.GemSize,
                GemWeight = request.Material.GemWeight,
                GemWeightUnit = request.Material.GemWeightUnit,

                DiamondQty = request.Material.DiamondQty ?? default,
                DiamondUnit = request.Material.DiamondUnit,
                DiamondQuality = request.Material.DiamondQuality,
                DiamondWeight = request.Material.DiamondWeight,
                DiamondWeightUnit = request.Material.DiamondWeightUnit,
                DiamondSize = request.Material.DiamondSize,

                ProductionPlanId = plan.Id,

                IsActive = true,
                CreateDate = DateTime.UtcNow,
                CreateBy = _admin,
            };
            _jewelryContext.TbtProductionPlanMaterial.Add(createMaterial);
            await _jewelryContext.SaveChangesAsync();

            return $"{plan.Wo}-{plan.WoNumber}";
        }


        // ----- Delete ----- //
        public async Task<string> ProductionPlanDeleteMaterial(ProductionPlanMaterialDeleteRequest request)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan.Include(x => x.TbtProductionPlanMaterial)
                        where item.Id == request.PlanId
                        && item.Wo == request.Wo
                        && item.WoNumber == request.WoNumber
                        select item).SingleOrDefault();

            if (plan == null)
            {
                throw new HandleException($"ไม่พบใบจ่ายรับคืนงาน {request.Wo}-{request.WoNumber}");
            }

            if (plan.TbtProductionPlanMaterial.Any() == false)
            {
                throw new HandleException($"ไม่มีส่วนประกอบในใบจ่ายรับคืนงาน {request.Wo}-{request.WoNumber}");
            }

            var material = plan.TbtProductionPlanMaterial.Where(x => x.ProductionPlanId == request.PlanId && x.Id == request.MaterialId).SingleOrDefault();
            if (material == null)
            {
                throw new HandleException($"ไม่พบส่วนประกอบใบจ่ายรับคืนงาน {request.Wo}-{request.WoNumber}");
            }

            material.IsActive = false;

            material.UpdateDate = DateTime.UtcNow;
            material.UpdateBy = _admin;

            _jewelryContext.TbtProductionPlanMaterial.Update(material);
            await _jewelryContext.SaveChangesAsync();

            return $"{plan.Wo}-{plan.WoNumber}";
        }

        // ----- Master ----- //
        public IQueryable<TbmProductionPlanStatus> GetProductionPlanStatus()
        {
            return (from item in _jewelryContext.TbmProductionPlanStatus
                    select item);
        }

        #endregion
    }
}
