using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanCreate;
using jewelry.Model.ProductionPlan.ProductionPlanDelete;
using jewelry.Model.ProductionPlan.ProductionPlanGet;
using jewelry.Model.ProductionPlan.ProductionPlanReport;
using jewelry.Model.ProductionPlan.ProductionPlanStatus;
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
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Jewelry.Service.ProductionPlan
{
    public interface IProductionPlanService
    {
        Task<ProductionPlanCreateResponse> ProductionPlanCreate(ProductionPlanCreateRequest request);
        Task<ProductionPlanCreateResponse> ProductionPlanCreateImage(List<IFormFile> images, string wo, int woNumber);
        IQueryable<ProductionPlanTrackingResponse> ProductionPlanSearch(ProductionPlanTracking request);
        public IQueryable<ProductionPlanTrackingResponse> ProductionPlanSearchByProductionPlanId(ProductionPlanTracking request);

        TbtProductionPlan ProductionPlanGet(int id);
        ProductionPlanGetResponse NewProductionPlanGet(int id);
        IQueryable<ProductionPlanGetResponse> ReportProductionPlan(ProductionPlanReport request);

        IQueryable<TbtProductionPlanMaterial> ProductionPlanMateriaGet(ProductionPlanTrackingMaterialRequest request);
        Task<string> ProductionPlanUpdateStatus(ProductionPlanUpdateStatusRequest request);
        Task<string> ProductionPlanUpdateHeader(ProductionPlanUpdateHeaderRequest request);
        Task<string> ProductionPlanDeleteMaterial(ProductionPlanMaterialDeleteRequest request);
        Task<string> ProductionPlanUpdateMaterial(ProductionPlanUpdateMaterialRequest request);

        Task<string> ProductionPlanAddStatusDetail(ProductionPlanStatusAddRequest request);
        Task<string> ProductionPlanUpdateStatusDetail(ProductionPlanStatusUpdateRequest request);
        Task<string> ProductionPlanDeleteStatusDetail(ProductionPlanStatusDeleteRequest request);

        IQueryable<TbmProductionPlanStatus> GetProductionPlanStatus();
    }
    public class ProductionPlanService : IProductionPlanService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public ProductionPlanService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
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

                var running = await _runningNumberService.GenerateRunningNumber("PLAN");


                using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var createPlan = new TbtProductionPlan()
                    {
                        Wo = request.Wo.ToUpper().Trim(),
                        WoNumber = request.WoNumber,
                        WoText = $"{request.Wo.ToUpper().Trim()}{request.WoNumber.ToString()}",
                        Mold = request.Mold.Trim(),

                        CustomerNumber = request.CustomerNumber.Trim(),
                        CustomerType = request.CustomerType.Trim(),
                        RequestDate = request.RequestDate.UtcDateTime,

                        ProductRunning = running,
                        ProductNumber = request.ProductNumber,

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
                        if (material.Gold == null)
                        {
                            throw new HandleException($"กรุณาระบุส่วนประกอบใบจ่าย-รับคืนงาน");
                        }

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

                    //if (request.Images == null)
                    //{
                    //    throw new HandleException($"กรุณาระบุรูปภาพสินค้า");
                    //}

                    //string _imageName = $"{request.Wo.ToUpper().Trim()}-{request.WoNumber}-Product.png";
                    //try
                    //{

                    //    // combind path
                    //    string imagePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/OrderPlan");

                    //    //check CreateDirectory
                    //    if (!Directory.Exists(imagePath))
                    //    {
                    //        Directory.CreateDirectory(imagePath);
                    //    }

                    //    //combine image name
                    //    string imagePathWithFileName = Path.Combine(imagePath, _imageName);

                    //    //https://www.thecodebuzz.com/how-to-save-iformfile-to-disk/
                    //    using (Stream fileStream = new FileStream(imagePathWithFileName, FileMode.Create, FileAccess.Write))
                    //    {
                    //        request.Images.CopyTo(fileStream);
                    //        fileStream.Close();
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                    //}


                    //var createImage = new TbtProductionPlanImage()
                    //{
                    //    ProductionPlanId = createPlan.Id,
                    //    Number = 0,

                    //    Path = _imageName,

                    //    IsActive = true,
                    //    CreateDate = DateTime.UtcNow,
                    //    CreateBy = _admin,
                    //};
                    //_jewelryContext.TbtProductionPlanImage.Add(createImage);
                    //await _jewelryContext.SaveChangesAsync();

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
        public IQueryable<ProductionPlanTrackingResponse> ProductionPlanSearch(ProductionPlanTracking request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlan
                         //.Include(x => x.TbtProductionPlanImage)
                         //.Include(x => x.TbtProductionPlanMaterial)
                         .Include(x => x.StatusNavigation)
                         where item.IsActive == true
                         select new ProductionPlanTrackingResponse()
                         {
                             Id = item.Id,
                             Wo = item.Wo,
                             WoNumber = item.WoNumber,
                             WoText = item.WoText,

                             Mold = item.Mold,
                             Status = item.Status,
                             StatusName = item.StatusNavigation.NameTh,

                             ProductNumber = item.ProductNumber,
                             CustomerNumber = item.CustomerNumber,
                             CreateDate = item.CreateDate,
                             RequestDate = item.RequestDate,
                         });

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
                          || item.WoText.Contains(request.Text)
                         || item.Mold.Contains(request.Text)
                         || item.ProductNumber.Contains(request.Text)
                         || item.CustomerNumber.Contains(request.Text)
                         //|| item.CreateBy.Contains(request.Text)
                         select item);
            }

            return query.OrderByDescending(x => x.CreateDate);
        }

        public IQueryable<ProductionPlanTrackingResponse> ProductionPlanSearchByProductionPlanId(ProductionPlanTracking request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlan
                             //.Include(x => x.TbtProductionPlanImage)
                             //.Include(x => x.TbtProductionPlanMaterial)
                             //.Include(x => x.StatusNavigation)
                         where item.IsActive == true
                         select new ProductionPlanTrackingResponse()
                         {
                             Id = item.Id,
                             Wo = item.Wo,
                             WoNumber = item.WoNumber,
                             WoText = item.WoText,

                             Mold = item.Mold,
                             Status = item.Status,
                             StatusName = item.StatusNavigation.NameTh,

                             ProductNumber = item.ProductNumber,
                             CustomerNumber = item.CustomerNumber,
                             CreateDate = item.CreateDate,
                             RequestDate = item.RequestDate,
                         });

            //query = query.Where(x => x.GIDate >= request.DateFrom.StartOfDayUtc() && x.GIDate <= request.DateTo.EndOfDayUtc());

            if (request.CreateStart.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.CreateStart.Value.StartOfDayUtc());
            }
            if (request.CreateEnd.HasValue)
            {
                query = query.Where(x => x.CreateDate <= request.CreateEnd.Value.StartOfDayUtc());
            }
            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query
                             //where item.Wo.Contains(request.Text.ToUpper())
                         where item.WoText.Contains(request.Text)
                         //|| item.Mold.Contains(request.Text)
                         //|| item.ProductNumber.Contains(request.Text)
                         //|| item.CustomerNumber.Contains(request.Text)
                         //|| item.CreateBy.Contains(request.Text)
                         select item);
            }
            if (!string.IsNullOrEmpty(request.WoText))
            {
                query = (from item in query
                             //where item.Wo.Contains(request.Text.ToUpper())
                         where item.WoText == request.Text
                         //|| item.Mold.Contains(request.Text)
                         //|| item.ProductNumber.Contains(request.Text)
                         //|| item.CustomerNumber.Contains(request.Text)
                         //|| item.CreateBy.Contains(request.Text)
                         select item);
            }

            //return query.OrderByDescending(x => x.CreateDate);
            return query;
        }
        public TbtProductionPlan ProductionPlanGet(int id)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan
                         .Include(x => x.ProductTypeNavigation)
                         .Include(x => x.CustomerTypeNavigation)
                         //.Include(x => x.TbtProductionPlanImage)
                         //.Include(x => x.TbtProductionPlanMaterial)
                         .Include(x => x.StatusNavigation)
                         .Include(x => x.TbtProductionPlanStatusHeader
                            .Where(o => o.IsActive == true).OrderByDescending(x => x.UpdateDate))
                            .ThenInclude(x => x.TbtProductionPlanStatusDetail)
                        where item.IsActive == true
                        && item.Id == id
                        //&& item.TbtProductionPlanStatusDetail.Any(x => x.IsActive == true)
                        select item).SingleOrDefault();

            if (plan == null)
            {
                throw new HandleException("ไม่พบข้อมูล กรุณาลองใหม่อีกครั้ง");
            }

            return plan;
        }
        public ProductionPlanGetResponse NewProductionPlanGet(int id)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan
                         .Include(x => x.ProductTypeNavigation)
                         .Include(x => x.CustomerTypeNavigation)
                         //.Include(x => x.TbtProductionPlanImage)
                         //.Include(x => x.TbtProductionPlanMaterial)
                         .Include(x => x.StatusNavigation)
                         .Include(x => x.TbtProductionPlanStatusHeader
                            .Where(o => o.IsActive == true).OrderByDescending(x => x.UpdateDate))
                            .ThenInclude(x => x.TbtProductionPlanStatusDetail)

                        join customer in _jewelryContext.TbmCustomer on item.CustomerNumber equals customer.Code into customerJoin
                        from cj in customerJoin.DefaultIfEmpty()

                        where item.IsActive == true
                        && item.Id == id
                        //&& item.TbtProductionPlanStatusDetail.Any(x => x.IsActive == true)
                        select new { item, cj }).SingleOrDefault();

            var listWork = (from item in _jewelryContext.TbmWorker
                            select item);


            if (plan == null)
            {
                throw new HandleException("ไม่พบข้อมูล กรุณาลองใหม่อีกครั้ง");
            }

            var response = new ProductionPlanGetResponse()
            {
                Id = plan.item.Id,
                Wo = plan.item.Wo,
                WoNumber = plan.item.WoNumber,

                CreateDate = plan.item.CreateDate,
                CreateBy = plan.item.CreateBy,
                UpdateBy = plan.item.UpdateBy,
                UpdateDate = plan.item.UpdateDate,

                RequestDate = plan.item.RequestDate,
                Mold = plan.item.Mold,

                ProductRunning = plan.item.ProductRunning,
                ProductName = plan.item.ProductName,
                ProductNumber = plan.item.ProductNumber,
                ProductDetail = plan.item.ProductDetail,

                ProductType = plan.item.ProductType,
                ProductTypeName = plan.item.ProductTypeNavigation != null ? plan.item.ProductTypeNavigation.NameTh : null,

                ProductQty = plan.item.ProductQty,
                ProductQtyUnit = plan.item.ProductQtyUnit,

                CustomerNumber = plan.item.CustomerNumber,
                CustomerName = plan.cj != null ? plan.cj.NameTh : null,
                CustomerType = plan.item.CustomerType,
                CustomerTypeName = plan.item.CustomerTypeNavigation != null ? plan.item.CustomerTypeNavigation.NameTh : null,

                IsActive = plan.item.IsActive,
                Status = plan.item.Status,
                StatusName = plan.item.StatusNavigation.NameTh,
                Remark = plan.item.Remark,

                TbtProductionPlanStatusHeader = (from item in plan.item.TbtProductionPlanStatusHeader
                                                 select new StatusDetailHeader()
                                                 {
                                                     Id = item.Id,
                                                     ProductionPlanId = item.Id,

                                                     CreateDate = item.CreateDate,
                                                     CreateBy = item.CreateBy,
                                                     UpdateDate = item.UpdateDate,
                                                     UpdateBy = item.UpdateBy,

                                                     Status = item.Status,

                                                     SendDate = item.SendDate,
                                                     SendName = item.SendName,
                                                     CheckDate = item.CheckDate,
                                                     CheckName = item.CheckName,

                                                     IsActive = item.IsActive,
                                                     Remark1 = item.Remark1,
                                                     Remark2 = item.Remark2,
                                                     WagesTotal = item.WagesTotal,

                                                     TbtProductionPlanStatusDetail = (from detail in item.TbtProductionPlanStatusDetail
                                                                                      select new StatusDetailDetail()
                                                                                      {
                                                                                          ProductionPlanId = detail.ProductionPlanId,
                                                                                          HeaderId = detail.HeaderId,
                                                                                          ItemNo = detail.ItemNo,

                                                                                          Gold = detail.Gold,
                                                                                          GoldQtySend = detail.GoldQtySend,
                                                                                          GoldWeightSend = detail.GoldWeightSend,
                                                                                          GoldQtyCheck = detail.GoldQtyCheck,
                                                                                          GoldWeightCheck = detail.GoldWeightCheck,
                                                                                          GoldWeightDiff = detail.GoldWeightDiff,
                                                                                          GoldWeightDiffPercent = detail.GoldWeightDiffPercent,

                                                                                          IsActive = detail.IsActive,

                                                                                          Description = detail.Description,

                                                                                          Worker = detail.Worker,
                                                                                          WorkerName = !string.IsNullOrEmpty(detail.Worker) && listWork.Any() ? (from item in listWork
                                                                                                                                                                 where item.Code == detail.Worker.ToUpper()
                                                                                                                                                                 select item.NameTh).SingleOrDefault()
                                                                                                                                                                 : null,

                                                                                          WorkerSub = detail.WorkerSub,
                                                                                          WorkerSubName = !string.IsNullOrEmpty(detail.WorkerSub) && listWork.Any() ? (from item in listWork
                                                                                                                                                                       where item.Code == detail.WorkerSub.ToUpper()
                                                                                                                                                                       select item.NameTh).SingleOrDefault()
                                                                                                                                                                 : null,

                                                                                          Wages = detail.Wages,
                                                                                          TotalWages = detail.TotalWages,
                                                                                      }).ToList(),
                                                 }).ToList()
            };

            return response;
        }
        public IQueryable<ProductionPlanGetResponse> ReportProductionPlan(ProductionPlanReport request)
        {
            var response = (from item in _jewelryContext.TbtProductionPlan
                         .Include(x => x.ProductTypeNavigation)
                         .Include(x => x.CustomerTypeNavigation)
                                //.Include(x => x.TbtProductionPlanImage)
                                //.Include(x => x.TbtProductionPlanMaterial)
                                //.Include(x => x.StatusNavigation)
                                //.Include(x => x.TbtProductionPlanStatusHeader
                                //   .Where(o => o.IsActive == true).OrderByDescending(x => x.UpdateDate))
                                //   .ThenInclude(x => x.TbtProductionPlanStatusDetail)

                            join customer in _jewelryContext.TbmCustomer on item.CustomerNumber equals customer.Code
                            //from cj in customerJoin.DefaultIfEmpty()

                            where item.IsActive == true
                            && item.CreateDate >= request.CreateStart.StartOfDayUtc()
                            && item.CreateDate <= request.CreateEnd.EndOfDayUtc()
                            //&& item.WoText.Contains(request.WoText.ToUpper())
                            //&& item.Id == id
                            //&& item.TbtProductionPlanStatusDetail.Any(x => x.IsActive == true)
                            select new ProductionPlanGetResponse
                            {
                                Id = item.Id,
                                Wo = item.Wo,
                                WoNumber = item.WoNumber,
                                WoText = item.WoText,

                                CreateDate = item.CreateDate,
                                CreateBy = item.CreateBy,
                                UpdateBy = item.UpdateBy,
                                UpdateDate = item.UpdateDate,

                                RequestDate = item.RequestDate,
                                Mold = item.Mold,

                                ProductRunning = item.ProductRunning,
                                ProductName = item.ProductName,
                                ProductNumber = item.ProductNumber,
                                ProductDetail = item.ProductDetail,

                                ProductType = item.ProductType,
                                ProductTypeName = item.ProductTypeNavigation != null ? item.ProductTypeNavigation.NameTh : null,

                                ProductQty = item.ProductQty,
                                ProductQtyUnit = item.ProductQtyUnit,

                                CustomerNumber = item.CustomerNumber,
                                CustomerTypeName = item.CustomerTypeNavigation != null ? item.CustomerTypeNavigation.NameTh : null,
                                CustomerType = item.CustomerType,
                                //CustomerTypeName = cj != null ? cj.NameTh : null,
                                CustomerName = customer.NameTh,

                                IsActive = item.IsActive,
                                //Status = item.Status,
                                //StatusName = item.StatusNavigation.NameTh,
                                Remark = item.Remark,
                            });

            if (!string.IsNullOrEmpty(request.WoText))
            { 
                response = response.Where(x => x.WoText.Contains(request.WoText.ToUpper()));
            }

            if (response.Any())
            {
                var test = response.ToList();
            }
            return response;
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

            return query.OrderByDescending(x => x.Gold == "WG").ThenBy(x => x.GoldQty);
            //return query.OrderBy(x => x.Gold);
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
                throw new HandleException($"ไม่พบใบจ่ายรับคืนงาน {request.Wo}-{request.WoNumber}");
            }

            plan.Mold = request.Mold.ToUpper();
            if (request.RequestDate.HasValue)
            {
                plan.RequestDate = request.RequestDate.Value.UtcDateTime;
            }

            plan.CustomerNumber = request.CustomerNumber.Trim();
            if (!string.IsNullOrEmpty(request.CustomerType))
            {
                plan.CustomerType = request.CustomerType.Trim();
            }

            plan.ProductQty = request.ProductQty;
            plan.ProductQtyUnit = request.ProductQtyUnit;

            plan.ProductName = request.ProductName;
            plan.ProductNumber = request.ProductNumber;

            if (!string.IsNullOrEmpty(request.ProductType))
            {
                plan.ProductType = request.ProductType.Trim();
            }

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

        // ----- Add/Update Status Detail -----//
        public async Task<string> ProductionPlanAddStatusDetail(ProductionPlanStatusAddRequest request)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan
                        where item.Id == request.ProductionPlanId
                        && item.Wo == request.Wo.ToUpper()
                        && item.WoNumber == request.WoNumber
                        select item).SingleOrDefault();

            if (plan == null)
            {
                throw new HandleException($"ไม่พบใบจ่ายรับคืนงาน {request.Wo}-{request.WoNumber}");
            }
            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"ใบจ่าย-รับคืนงาน {request.Wo}-{request.WoNumber} อยู่ในสถานะดำเนินการเสร็จสิ้น กรุณาตรวจสอบอีกครั้ง");
            }

            var checkDubStatus = (from item in _jewelryContext.TbtProductionPlanStatusHeader
                                  where item.ProductionPlanId == request.ProductionPlanId && item.Status == request.Status && item.IsActive
                                  select item);

            if (checkDubStatus.Any())
            {
                throw new HandleException($"ใบจ่าย-รับคืนงาน {request.Wo}-{request.WoNumber}  ถูกระบุสถานะดำเนินการเเล้ว กรุณาใช้การเเก้ไขข้อมูล");
            }
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var addStatusItem = new List<TbtProductionPlanStatusDetail>();

                switch (request.Status)
                {
                    case 50: //จ่ายเเต่ง
                    case 60: //จ่ายขัดดิบ
                    case 80: //จ่ายฝัง
                    case 90: //จ่ายขัดชุบ
                        {
                            if (request.Golds == null || request.Golds.Any() == false)
                            {
                                throw new HandleException($"โปรดระบุรายละเอียดของทอง");
                            }
                            var addStatus = new TbtProductionPlanStatusHeader
                            {
                                ProductionPlanId = request.ProductionPlanId,
                                Status = request.Status,
                                IsActive = true,

                                CreateDate = DateTime.UtcNow,
                                CreateBy = _admin,
                                UpdateDate = DateTime.UtcNow,
                                UpdateBy = _admin,

                                SendName = request.SendName,
                                SendDate = request.SendDate.HasValue ? request.SendDate.Value.UtcDateTime : null,
                                CheckName = request.CheckName,
                                CheckDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null,

                                Remark1 = request.Remark1,
                                Remark2 = request.Remark2,
                                WagesTotal = request.Golds.Any() ? request.Golds.Sum(x => x.TotalWages) : 0,
                            };
                            _jewelryContext.TbtProductionPlanStatusHeader.Add(addStatus);
                            await _jewelryContext.SaveChangesAsync();

                            foreach (var item in request.Golds)
                            {

                                var newStatusItem = new TbtProductionPlanStatusDetail()
                                {
                                    HeaderId = addStatus.Id,
                                    ProductionPlanId = request.ProductionPlanId,
                                    ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{request.ProductionPlanId}-{request.Status}"),
                                    IsActive = true,

                                    Gold = item.Gold,
                                    GoldQtySend = item.GoldQTYSend,
                                    GoldWeightSend = item.GoldWeightSend,
                                    GoldQtyCheck = item.GoldQTYCheck,
                                    GoldWeightCheck = item.GoldWeightCheck,
                                    ////GoldWeightDiff = item.GoldWeightSend - item.GoldWeightCheck,
                                    //GoldWeightDiffPercent = 100 - ((item.GoldWeightCheck * 100) / item.GoldWeightSend),
                                    Worker = item.Worker,
                                    WorkerSub = item.WorkerSub,
                                    Description = item.Description,

                                    Wages = item.Wages ?? 0,
                                    TotalWages = item.TotalWages ?? 0,

                                };

                                if (item.GoldWeightSend.HasValue && item.GoldWeightCheck.HasValue)
                                {
                                    newStatusItem.GoldWeightDiff = item.GoldWeightSend - item.GoldWeightCheck;
                                    newStatusItem.GoldWeightDiffPercent = 100 - ((item.GoldWeightCheck * 100) / item.GoldWeightSend);
                                }

                                addStatusItem.Add(newStatusItem);
                            }
                        }
                        break;
                    case 70:
                    case 55://จ่ายขัดพลอย
                        {
                            if (request.Golds == null || request.Golds.Any() == false)
                            {
                                throw new HandleException($"โปรดระบุรายละเอียดของทอง");
                            }

                            var addStatus = new TbtProductionPlanStatusHeader
                            {
                                ProductionPlanId = request.ProductionPlanId,
                                Status = request.Status,
                                IsActive = true,

                                CreateDate = DateTime.UtcNow,
                                CreateBy = _admin,
                                UpdateDate = DateTime.UtcNow,
                                UpdateBy = _admin,

                                CheckName = request.CheckName,
                                CheckDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null,

                                Remark1 = request.Remark1,
                                Remark2 = request.Remark2,
                                WagesTotal = request.TotalWages ?? 0,
                            };
                            _jewelryContext.TbtProductionPlanStatusHeader.Add(addStatus);
                            await _jewelryContext.SaveChangesAsync();

                            foreach (var item in request.Golds)
                            {
                                //var itemNo = await _runningNumberService.GenerateRunningNumber($"S-{request.ProductionPlanId}-{request.Status}");
                                var newStatus = new TbtProductionPlanStatusDetail()
                                {
                                    HeaderId = addStatus.Id,
                                    ProductionPlanId = request.ProductionPlanId,
                                    ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{request.ProductionPlanId}-{request.Status}"),
                                    IsActive = true,

                                    Gold = item.Gold,
                                    GoldQtyCheck = item.GoldQTYCheck,
                                    GoldWeightCheck = item.GoldWeightCheck,
                                };
                                addStatusItem.Add(newStatus);
                            }
                        }
                        break;
                    case 85: //CVD
                        {
                            var addStatus = new TbtProductionPlanStatusHeader
                            {
                                ProductionPlanId = request.ProductionPlanId,
                                Status = request.Status,
                                IsActive = true,

                                CreateDate = DateTime.UtcNow,
                                CreateBy = _admin,
                                UpdateDate = DateTime.UtcNow,
                                UpdateBy = _admin,

                                CheckName = request.CheckName,
                                CheckDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null,

                                Remark1 = request.Remark1,
                                Remark2 = request.Remark2,
                                WagesTotal = request.TotalWages ?? 0,
                            };
                            _jewelryContext.TbtProductionPlanStatusHeader.Add(addStatus);
                            await _jewelryContext.SaveChangesAsync();
                        }
                        break;
                }

                _jewelryContext.TbtProductionPlanStatusDetail.AddRange(addStatusItem);
                await _jewelryContext.SaveChangesAsync();

                plan.Status = request.Status;
                plan.UpdateDate = DateTime.UtcNow;
                plan.UpdateBy = _admin;

                _jewelryContext.TbtProductionPlan.Update(plan);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }


            return $"{plan.Wo}-{plan.WoNumber}";
        }
        public async Task<string> ProductionPlanUpdateStatusDetail(ProductionPlanStatusUpdateRequest request)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan
                        where item.Id == request.ProductionPlanId
                        && item.Wo == request.Wo.ToUpper()
                        && item.WoNumber == request.WoNumber
                        select item).SingleOrDefault();

            if (plan == null)
            {
                throw new HandleException($"ไม่พบใบจ่ายรับคืนงาน {request.Wo}-{request.WoNumber}");
            }
            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"ใบจ่าย-รับคืนงาน {request.Wo}-{request.WoNumber} อยู่ในสถานะดำเนินการเสร็จสิ้น กรุณาตรวจสอบอีกครั้ง");
            }


            var checkStatus = (from item in _jewelryContext.TbtProductionPlanStatusHeader.Include(x => x.TbtProductionPlanStatusDetail)
                               where item.ProductionPlanId == request.ProductionPlanId
                               && item.Status == request.Status
                               && item.Id == request.HeaderId
                               && item.IsActive
                               select item).SingleOrDefault();

            if (checkStatus == null)
            {
                throw new HandleException($"ไม่พบสถานะการผลิต");
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var addStatusItem = new List<TbtProductionPlanStatusDetail>();
                switch (request.Status)
                {
                    case 50: //จ่ายเเต่ง
                    case 60: //จ่ายขัดดิบ
                    case 80: //จ่ายฝัง
                    case 90: //จ่ายขัดชุบ
                        {

                            if (request.Golds == null || request.Golds.Any() == false)
                            {
                                throw new HandleException($"โปรดระบุรายละเอียดของทอง");
                            }

                            _jewelryContext.TbtProductionPlanStatusDetail.RemoveRange(checkStatus.TbtProductionPlanStatusDetail);
                            await _jewelryContext.SaveChangesAsync();


                            checkStatus.SendName = request.SendName;
                            checkStatus.SendDate = request.SendDate.HasValue ? request.SendDate.Value.UtcDateTime : null;
                            checkStatus.CheckName = request.CheckName;
                            checkStatus.CheckDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null;
                            checkStatus.Remark1 = request.Remark1;
                            checkStatus.Remark2 = request.Remark2;
                            checkStatus.WagesTotal = request.Golds.Any() ? request.Golds.Sum(x => x.TotalWages) : 0;
                            checkStatus.UpdateDate = DateTime.UtcNow;
                            checkStatus.UpdateBy = _admin;


                            _jewelryContext.TbtProductionPlanStatusHeader.Update(checkStatus);
                            await _jewelryContext.SaveChangesAsync();

                            foreach (var item in request.Golds)
                            {

                                var newStatusItem = new TbtProductionPlanStatusDetail()
                                {
                                    HeaderId = checkStatus.Id,
                                    ProductionPlanId = request.ProductionPlanId,
                                    ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{request.ProductionPlanId}-{request.Status}"),
                                    IsActive = true,

                                    Gold = item.Gold,
                                    GoldQtySend = item.GoldQTYSend,
                                    GoldWeightSend = item.GoldWeightSend,
                                    GoldQtyCheck = item.GoldQTYCheck,
                                    GoldWeightCheck = item.GoldWeightCheck,
                                    ////GoldWeightDiff = item.GoldWeightSend - item.GoldWeightCheck,
                                    //GoldWeightDiffPercent = 100 - ((item.GoldWeightCheck * 100) / item.GoldWeightSend),
                                    Worker = item.Worker,
                                    WorkerSub = item.WorkerSub,

                                    Description = item.Description,
                                    Wages = item.Wages ?? 0,
                                    TotalWages = item.TotalWages ?? 0,

                                };

                                if (item.GoldWeightSend.HasValue && item.GoldWeightCheck.HasValue)
                                {
                                    newStatusItem.GoldWeightDiff = item.GoldWeightSend - item.GoldWeightCheck;
                                    newStatusItem.GoldWeightDiffPercent = 100 - ((item.GoldWeightCheck * 100) / item.GoldWeightSend);
                                }

                                addStatusItem.Add(newStatusItem);
                            }
                        }
                        break;
                    case 70:
                    case 55://จ่ายขัดพลอย
                        {
                            if (request.Golds == null || request.Golds.Any() == false)
                            {
                                throw new HandleException($"โปรดระบุรายละเอียดของทอง");
                            }

                            _jewelryContext.TbtProductionPlanStatusDetail.RemoveRange(checkStatus.TbtProductionPlanStatusDetail);
                            await _jewelryContext.SaveChangesAsync();

                            checkStatus.CheckName = request.CheckName;
                            checkStatus.CheckDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null;

                            checkStatus.Remark1 = request.Remark1;
                            checkStatus.Remark2 = request.Remark2;
                            //checkStatus.WagesTotal = request.TotalWages ?? 0;

                            checkStatus.UpdateDate = DateTime.UtcNow;
                            checkStatus.UpdateBy = _admin;

                            _jewelryContext.TbtProductionPlanStatusHeader.Update(checkStatus);
                            await _jewelryContext.SaveChangesAsync();

                            foreach (var item in request.Golds)
                            {
                                //var itemNo = await _runningNumberService.GenerateRunningNumber($"S-{request.ProductionPlanId}-{request.Status}");
                                var newStatus = new TbtProductionPlanStatusDetail()
                                {
                                    HeaderId = checkStatus.Id,
                                    ProductionPlanId = request.ProductionPlanId,
                                    ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{request.ProductionPlanId}-{request.Status}"),
                                    IsActive = true,

                                    Gold = item.Gold,
                                    GoldQtyCheck = item.GoldQTYCheck,
                                    GoldWeightCheck = item.GoldWeightCheck,
                                };
                                addStatusItem.Add(newStatus);
                            }
                        }
                        break;
                    case 85: //CVD
                        {


                            checkStatus.CheckName = request.CheckName;
                            checkStatus.CheckDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null;

                            checkStatus.Remark1 = request.Remark1;
                            checkStatus.Remark2 = request.Remark2;
                            //checkStatus.WagesTotal = request.TotalWages ?? 0;

                            checkStatus.UpdateDate = DateTime.UtcNow;
                            checkStatus.UpdateBy = _admin;
                            _jewelryContext.TbtProductionPlanStatusHeader.Update(checkStatus);
                            await _jewelryContext.SaveChangesAsync();
                        }
                        break;
                }

                _jewelryContext.TbtProductionPlanStatusDetail.AddRange(addStatusItem);
                await _jewelryContext.SaveChangesAsync();

                plan.Status = request.Status;
                plan.UpdateDate = DateTime.UtcNow;
                plan.UpdateBy = _admin;

                _jewelryContext.TbtProductionPlan.Update(plan);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            return $"{plan.Wo}-{plan.WoNumber}";
        }
        public async Task<string> ProductionPlanDeleteStatusDetail(ProductionPlanStatusDeleteRequest request)
        {
            var plan = (from item in _jewelryContext.TbtProductionPlan
                        where item.Id == request.ProductionPlanId
                        && item.Wo == request.Wo.ToUpper()
                        && item.WoNumber == request.WoNumber
                        select item).SingleOrDefault();

            if (plan == null)
            {
                throw new HandleException($"ไม่พบใบจ่ายรับคืนงาน {request.Wo}-{request.WoNumber}");
            }
            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"ใบจ่าย-รับคืนงาน {request.Wo}-{request.WoNumber} อยู่ในสถานะดำเนินการเสร็จสิ้น กรุณาตรวจสอบอีกครั้ง");
            }

            var statusHeader = (from item in _jewelryContext.TbtProductionPlanStatusHeader.Include(x => x.TbtProductionPlanStatusDetail)
                                where item.Id == request.Id
                                select item).SingleOrDefault();
            if (statusHeader == null)
            {
                throw new HandleException($"ไม่พบสถานะงาน กรุณาตรวจสอบอีกครั้ง");
            }

            statusHeader.IsActive = false;
            statusHeader.UpdateDate = DateTime.UtcNow;
            statusHeader.UpdateBy = _admin;

            _jewelryContext.TbtProductionPlanStatusHeader.Update(statusHeader);

            if (statusHeader.TbtProductionPlanStatusDetail != null)
            {
                foreach (var item in statusHeader.TbtProductionPlanStatusDetail)
                {
                    item.IsActive = false;
                }
                _jewelryContext.TbtProductionPlanStatusDetail.UpdateRange(statusHeader.TbtProductionPlanStatusDetail);
            }

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
