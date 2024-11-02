using Azure.Core;
using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanCreate;
using jewelry.Model.ProductionPlan.ProductionPlanDelete;
using jewelry.Model.ProductionPlan.ProductionPlanGet;
using jewelry.Model.ProductionPlan.ProductionPlanPrice.CreatePrice;
using jewelry.Model.ProductionPlan.ProductionPlanPrice.Transection;
using jewelry.Model.ProductionPlan.ProductionPlanReport;
using jewelry.Model.ProductionPlan.ProductionPlanStatus;
using jewelry.Model.ProductionPlan.ProductionPlanStatus.Transfer;
using jewelry.Model.ProductionPlan.ProductionPlanStatusList;
using jewelry.Model.ProductionPlan.ProductionPlanTracking;
using jewelry.Model.ProductionPlan.ProductionPlanUpdate;
using jewelry.Model.ProductionPlanCost.GoldCostItem;
using jewelry.Model.Worker.TrackingWorker;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Index.HPRtree;
using NetTopologySuite.Noding;
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
        IQueryable<ProductionPlanStatusListResponse> ListProductionPlanStatus(ProductionPlanStatusList request);


        Task<string> ProductionPlanUpdateStatus(ProductionPlanUpdateStatusRequest request);
        Task<string> ProductionPlanUpdateHeader(ProductionPlanUpdateHeaderRequest request);
        Task<string> ProductionPlanDeleteMaterial(ProductionPlanMaterialDeleteRequest request);
        Task<string> ProductionPlanUpdateMaterial(ProductionPlanUpdateMaterialRequest request);

        Task<TransferResponse> ProductionPlanTransfer(TransferRequest request);
        Task<string> UpdateProductionPlan(ProductionPlanStatusUpdateRequest request);

        Task<string> ProductionPlanAddStatusDetail(ProductionPlanStatusAddRequest request);
        Task<string> ProductionPlanUpdateStatusDetail(ProductionPlanStatusUpdateRequest request);
        Task<string> ProductionPlanDeleteStatusDetail(ProductionPlanStatusDeleteRequest request);

        IQueryable<TbmProductionPlanStatus> GetProductionPlanStatus();

        Task<TransectionResponse> GetAllTransectionPrice(string wo, int woNumber);
        Task<string> CreatePrice(CreatePriceRequest request);
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

                        Type = request.Gold,
                        TypeSize = request.GoldSize
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
            var succesStatus = new List<int> { ProductionPlanStatus.Completed, ProductionPlanStatus.Price };
            var query = (from item in _jewelryContext.TbtProductionPlan
                         //.Include(x => x.TbtProductionPlanImage)
                         //.Include(x => x.TbtProductionPlanMaterial)
                         .Include(x => x.StatusNavigation)
                         .Include(x => x.TbtProductionPlanStatusHeader
                         .Where(o => o.IsActive == true).OrderByDescending(x => x.UpdateDate))
                         .Include(o => o.ProductTypeNavigation)
                         .Include(o => o.CustomerTypeNavigation)

                         join customer in _jewelryContext.TbmCustomer on item.CustomerNumber equals customer.Code into customerJoin
                         from cj in customerJoin.DefaultIfEmpty()

                         where item.IsActive == true
                         let currentStatus = item.TbtProductionPlanStatusHeader.Where(x => x.IsActive == true && x.Status == item.Status).FirstOrDefault()
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
                             ProductQty = item.ProductQty,

                             CustomerNumber = item.CustomerNumber,
                             CustomerName = cj != null ? cj.NameTh : null,

                             CustomerType = item.CustomerType,
                             CustomerTypeName = item.CustomerTypeNavigation.NameTh,


                             CreateDate = item.CreateDate,
                             RequestDate = item.RequestDate,
                             LastUpdateStatus = currentStatus != null
                                                ? currentStatus.UpdateDate
                                                : item.CreateDate,
                             IsOverPlan = item.RequestDate < DateTime.UtcNow && !succesStatus.Contains(item.Status), // ประเมินราคา

                             ProductType = item.ProductType,
                             ProductTypeName = item.ProductTypeNavigation.NameTh,

                             Gold = item.Type,
                             GoldSize = item.TypeSize,
                         });

            //query = query.Where(x => x.GIDate >= request.DateFrom.StartOfDayUtc() && x.GIDate <= request.DateTo.EndOfDayUtc());

            if (request.Start.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.Start.Value.StartOfDayUtc());
            }
            if (request.End.HasValue)
            {
                query = query.Where(x => x.CreateDate <= request.End.Value.EndOfDayUtc());
            }

            if (request.SendStart.HasValue)
            {
                //query = query.Where(x => x.RequestDate >= request.SendStart.Value.StartOfDayUtc());
                query = query.Where(x => x.LastUpdateStatus >= request.SendStart.Value.StartOfDayUtc());
            }
            if (request.SendEnd.HasValue)
            {
                //query = query.Where(x => x.RequestDate <= request.SendEnd.Value.EndOfDayUtc());
                query = query.Where(x => x.LastUpdateStatus <= request.SendEnd.Value.EndOfDayUtc());
            }

            if (request.IsOverPlan.HasValue)
            {
                if (request.IsOverPlan == 1)
                {
                    query = query.Where(x => x.IsOverPlan == true);
                }
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
            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.Status));
            }
            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                query = query.Where(x => x.CustomerNumber.Contains(request.CustomerCode));
            }

            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => request.Gold.Contains(x.Gold));
            }
            if (request.GoldSize != null && request.GoldSize.Any())
            {
                query = query.Where(x => request.GoldSize.Contains(x.GoldSize));
            }
            if (request.CustomerType != null && request.CustomerType.Any())
            {
                query = query.Where(x => request.CustomerType.Contains(x.CustomerType));
            }
            if (request.ProductType != null && request.ProductType.Any())
            {
                query = query.Where(x => request.ProductType.Contains(x.ProductType));
            }

            if (!string.IsNullOrEmpty(request.Mold))
            {
                query = query.Where(x => x.Mold.Contains(request.Mold));
            }

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.ProductNumber.Contains(request.ProductNumber));
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

            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.Status));
            }
            //if (!string.IsNullOrEmpty(request.CustomerCode))
            //{
            //    query = query.Where(x => x.CustomerNumber == request.CustomerCode);
            //}

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
                         .Include(x => x.TbtProductionPlanStatusHeader
                            .Where(o => o.IsActive == true).OrderByDescending(x => x.UpdateDate))
                            .ThenInclude(x => x.TbtProductionPlanStatusDetailGem)
                        .Include(x => x.TbtProductionPlanPrice)

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

            var stockGem = (from item in _jewelryContext.TbtStockGem
                            select item);

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

                Gold = plan.item.Type,
                GoldSize = plan.item.TypeSize,

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
                                                                                          RequestDate = detail.RequestDate.HasValue ? detail.RequestDate.Value : null,

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

                                                     TbtProductionPlanStatusGem = (from gem in item.TbtProductionPlanStatusDetailGem
                                                                                   let gemStock = stockGem.Where(x => x.Code == gem.GemCode).FirstOrDefault()
                                                                                   select new StatusDetailGem()
                                                                                   {
                                                                                       ProductionPlanId = gem.ProductionPlanId,
                                                                                       HeaderId = gem.HeaderId,
                                                                                       ItemNo = gem.ItemNo,

                                                                                       Id = gem.GemId,
                                                                                       Code = gem.GemCode,
                                                                                       Name = gem.GemName,
                                                                                       QTY = gem.GemQty,
                                                                                       Price = gem.GemPrice,
                                                                                       Weight = gem.GemWeight,

                                                                                       Unit = gemStock != null ? gemStock.Unit : "",

                                                                                       OutboundDate = gem.OutboundDate,
                                                                                       OutboundName = gem.OutboundName,
                                                                                       OutboundRunning = gem.OutboundRunning,
                                                                                   }).ToList()

                                                 }).ToList(),

                PriceItems = plan.item.TbtProductionPlanPrice.Any() ?
                              ((from item in plan.item.TbtProductionPlanPrice
                                select new Price()
                                {
                                    No = item.No,
                                    Name = item.Name,
                                    NameDescription = item.NameDescription,
                                    NameGroup = item.NameGroup,

                                    Date = item.Date,

                                    Qty = item.Qty,
                                    QtyPrice = item.QtyPrice,
                                    QtyWeight = item.QtyWeight,
                                    QtyWeightPrice = item.QtyWeightPrice,

                                    TotalPrice = item.TotalPrice,
                                }).ToList())
                              : new List<Price>(),
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

        // ----- list plan status -----//
        public IQueryable<ProductionPlanStatusListResponse> ListProductionPlanStatus(ProductionPlanStatusList request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlanStatusDetail
                         .Include(x => x.Header)
                         .ThenInclude(x => x.ProductionPlan)
                             //join status in _jewelryContext.TbmProductionPlanStatus on item.Header.Status equals status.Id
                         join _worker in _jewelryContext.TbmWorker on item.Worker equals _worker.Code into _workerJpined
                         from worker in _workerJpined.DefaultIfEmpty()

                         where item.IsActive == true
                         && item.Header.IsActive == true
                         && item.Header.SendDate >= request.Start.StartOfDayUtc()
                         && item.Header.SendDate <= request.End.EndOfDayUtc()

                         select new ProductionPlanStatusListResponse()
                         {
                             Wo = item.Header.ProductionPlan.Wo,
                             WoNumber = item.Header.ProductionPlan.WoNumber,
                             WoText = item.Header.ProductionPlan.WoText,
                             ProductNumber = item.Header.ProductionPlan.ProductNumber,
                             ProductName = item.Header.ProductionPlan.ProductName,
                             Mold = item.Header.ProductionPlan.Mold,

                             HeaderId = item.HeaderId,

                             WorkerCode = item.Worker,
                             WorkerName = worker != null ? worker.NameTh : "",

                             Status = item.Header.ProductionPlan.Id,
                             StatusName = item.Header.ProductionPlan.StatusNavigation.NameTh,


                             TypeStatus = item.Header.Status,
                             TypeStatusName = item.Header.StatusNavigation.NameTh,
                             TypeStatusDescription = item.Header.StatusNavigation.Description,

                             Gold = item.Gold,

                             GoldQtySend = item.GoldQtySend,
                             GoldWeightSend = item.GoldWeightSend,
                             GoldQtyCheck = item.GoldQtyCheck,
                             GoldWeightCheck = item.GoldWeightCheck,

                             Description = item.Description,
                             Wages = item.Wages,
                             TotalWages = item.TotalWages,
                             WagesStatus = item.Wages.HasValue && item.Wages.Value > 0 ? 100 : 10,

                             ReceiveDate = item.Header.SendDate,
                             JobDate = item.RequestDate,
                         });

            //var test = query.ToList();


            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.TypeStatus));
            }
            if (!string.IsNullOrEmpty(request.WoText))
            {
                query = query.Where(x => x.WoText.Contains(request.WoText.ToUpper()));
            }
            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => request.Gold.Contains(x.Gold));
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.WoText.Contains(request.ProductNumber));
            }

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

            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanCompleted}");
            }
            if (plan.Status == ProductionPlanStatus.Melted)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanMelted}");
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
            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanCompleted}");
            }
            if (plan.Status == ProductionPlanStatus.Melted)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanMelted}");
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
            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanCompleted}");
            }
            if (plan.Status == ProductionPlanStatus.Melted)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanMelted}");
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
            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanCompleted}");
            }
            if (plan.Status == ProductionPlanStatus.Melted)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanMelted}");
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

        #region --- transfer production plan ---
        //public async Task<TransferResponse> ProductionPlanTransfer(TransferRequest request)
        //{
        //    if (request.FormerStatus == request.TargetStatus)
        //    {
        //        throw new HandleException($"{ErrorMessage.InvalidRequest}");
        //    }

        //    var dateNow = DateTime.UtcNow;

        //    var planIds = request.Plans.Select(x => x.Id).ToArray();
        //    var plans = (from item in _jewelryContext.TbtProductionPlan
        //                 .Include(x => x.TbtProductionPlanStatusHeader)
        //                 where planIds.Contains(item.Id)
        //                 select item).ToList();

        //    if (!plans.Any())
        //    {
        //        throw new HandleException(ErrorMessage.NotFound);
        //    }

        //    var response = new TransferResponse()
        //    {
        //        Message = "success",
        //    };
        //    var addNewStatus = new List<TbtProductionPlanStatusHeader>();
        //    var addTransferStatus = new List<TbtProductionPlanTransferStatus>();
        //    var updatePlans = new List<TbtProductionPlan>();

        //    var running = await _runningNumberService.GenerateRunningNumberForGold("PLT");

        //    foreach (var plan in request.Plans)
        //    {
        //        var error = new TransferResponseItem()
        //        {
        //            Id = plan.Id,
        //            Wo = plan.Wo,
        //            WoNumber = plan.WoNumber,
        //        };

        //        var targetPlan = plans.Where(x => x.Id == plan.Id).FirstOrDefault();
        //        if (targetPlan == null)
        //        {
        //            error.Message = ErrorMessage.NotFound;
        //            response.Errors.Add(error);
        //            continue;
        //        }
        //        if (targetPlan.Status == ProductionPlanStatus.Completed)
        //        {
        //            error.Message = ErrorMessage.PlanCompleted;
        //            response.Errors.Add(error);
        //            continue;
        //        }
        //        if (request.TargetStatus == ProductionPlanStatus.Completed && targetPlan.Status != ProductionPlanStatus.Price)
        //        {
        //            error.Message = ErrorMessage.PlanNeedPrice;
        //            response.Errors.Add(error);
        //            continue;
        //        }

        //        if (targetPlan.TbtProductionPlanStatusHeader.Any())
        //        {
        //            var alreadyStatus = targetPlan.TbtProductionPlanStatusHeader
        //                                           .Where(x => x.IsActive == true)
        //                                           .Select(x => x.Status).ToArray();

        //            if (alreadyStatus.Contains(request.TargetStatus))
        //            {
        //                error.Message = ErrorMessage.StatusAlready;
        //                response.Errors.Add(error);
        //                continue;
        //            }
        //        }

        //        var newStatus = new TbtProductionPlanStatusHeader()
        //        {
        //            CreateDate = dateNow,
        //            CreateBy = request.TransferBy ?? _admin,
        //            UpdateDate = dateNow,
        //            UpdateBy = request.TransferBy ?? _admin,

        //            IsActive = true,

        //            ProductionPlanId = targetPlan.Id,
        //            Status = request.TargetStatus,
        //        };
        //        addNewStatus.Add(newStatus);

        //        var newTransferStatus = new TbtProductionPlanTransferStatus()
        //        {
        //            Running = running,

        //            Wo = targetPlan.Wo,
        //            WoNumber = targetPlan.WoNumber,
        //            ProductionPlanId = targetPlan.Id,

        //            CreateDate = dateNow,
        //            CreateBy = request.TransferBy ?? _admin,

        //            FormerStatus = request.FormerStatus,
        //            TargetStatus = request.TargetStatus,
        //        };
        //        addTransferStatus.Add(newTransferStatus);

        //        targetPlan.Status = request.TargetStatus;
        //        targetPlan.UpdateDate = dateNow;
        //        targetPlan.UpdateBy = request.TransferBy ?? _admin;
        //        updatePlans.Add(targetPlan);

        //    }

        //    using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        //    {
        //        if (addNewStatus.Any())
        //        {
        //            _jewelryContext.TbtProductionPlanStatusHeader.AddRange(addNewStatus);
        //            await _jewelryContext.SaveChangesAsync();
        //        }
        //        if (addTransferStatus.Any())
        //        {
        //            foreach (var transfer in addTransferStatus)
        //            {
        //                var match = addNewStatus.Where(x => x.ProductionPlanId == transfer.ProductionPlanId).FirstOrDefault();
        //                if (match == null)
        //                {
        //                    throw new HandleException($"{ErrorMessage.NotFound}");
        //                }

        //                transfer.TargetStatusId = match.Id;
        //            }

        //            _jewelryContext.TbtProductionPlanTransferStatus.AddRange(addTransferStatus);
        //            await _jewelryContext.SaveChangesAsync();
        //        }
        //        if (updatePlans.Any())
        //        {
        //            _jewelryContext.TbtProductionPlan.UpdateRange(updatePlans);
        //            await _jewelryContext.SaveChangesAsync();
        //        }

        //        scope.Complete();
        //    }

        //    return response;
        //}
        #region --- method transfer plan ---
        public async Task<TransferResponse> ProductionPlanTransfer(TransferRequest request)
        {
            ValidateRequest(request);

            var plans = await GetProductionPlans(request.Plans.Select(x => x.Id).ToArray());
            var response = new TransferResponse { Message = "success" };

            var transferData = await PrepareTransferData(request, plans);

            if (transferData.HasAnyValidPlans)
            {
                await ProcessTransfer(transferData);
            }

            response.Errors.AddRange(transferData.Errors);
            return response;
        }
        private void ValidateRequest(TransferRequest request)
        {
            if (request.FormerStatus == request.TargetStatus)
            {
                throw new HandleException(ErrorMessage.InvalidRequest);
            }
        }
        private async Task<List<TbtProductionPlan>> GetProductionPlans(int[] planIds)
        {
            var plans = await _jewelryContext.TbtProductionPlan
                .Include(x => x.TbtProductionPlanStatusHeader)
                .Where(item => planIds.Contains(item.Id))
                .ToListAsync();

            if (!plans.Any())
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            return plans;
        }
        private async Task<TransferData> PrepareTransferData(TransferRequest request, List<TbtProductionPlan> plans)
        {
            var data = new TransferData
            {
                DateNow = DateTime.UtcNow,
                Running = await _runningNumberService.GenerateRunningNumberForGold("PLT")
            };

            foreach (var planRequest in request.Plans)
            {
                var validationResult = ValidatePlanForTransfer(planRequest, plans, request);

                if (validationResult.IsValid)
                {
                    var targetPlan = plans.First(x => x.Id == planRequest.Id);
                    AddValidPlanData(data, targetPlan, request);
                }
                else
                {
                    data.Errors.Add(new TransferResponseItem
                    {
                        Id = planRequest.Id,
                        Wo = planRequest.Wo,
                        WoNumber = planRequest.WoNumber,
                        Message = validationResult.ErrorMessage
                    });
                }
            }

            return data;
        }
        private (bool IsValid, string ErrorMessage) ValidatePlanForTransfer(
            TransferRequestItem planRequest,
            List<TbtProductionPlan> plans,
            TransferRequest request)
        {
            var targetPlan = plans.FirstOrDefault(x => x.Id == planRequest.Id);

            if (targetPlan == null)
                return (false, ErrorMessage.NotFound);

            if (targetPlan.Status == ProductionPlanStatus.Completed)
                return (false, ErrorMessage.PlanCompleted);

            if (request.TargetStatus == ProductionPlanStatus.Completed && targetPlan.Status != ProductionPlanStatus.Price)
                return (false, ErrorMessage.PlanNeedPrice);

            if (targetPlan.TbtProductionPlanStatusHeader.Any(x =>
                x.IsActive && x.Status == request.TargetStatus))
            {
                return (false, ErrorMessage.StatusAlready);
            }

            return (true, null);
        }
        private void AddValidPlanData(TransferData data, TbtProductionPlan plan, TransferRequest request)
        {
            var newStatus = CreateNewStatus(plan, request, data.DateNow);
            data.NewStatuses.Add(newStatus);

            var transferStatus = CreateTransferStatus(plan, request, data.Running, data.DateNow);
            data.TransferStatuses.Add(transferStatus);

            plan.Status = request.TargetStatus;
            plan.UpdateDate = data.DateNow;
            plan.UpdateBy = request.TransferBy ?? _admin;
            data.UpdatePlans.Add(plan);
        }
        private TbtProductionPlanStatusHeader CreateNewStatus(
            TbtProductionPlan plan,
            TransferRequest request,
            DateTime dateNow)
        {
            return new TbtProductionPlanStatusHeader
            {
                CreateDate = dateNow,
                CreateBy = request.TransferBy ?? _admin,
                UpdateDate = dateNow,
                UpdateBy = request.TransferBy ?? _admin,
                IsActive = true,
                ProductionPlanId = plan.Id,
                Status = request.TargetStatus
            };
        }
        private TbtProductionPlanTransferStatus CreateTransferStatus(
            TbtProductionPlan plan,
            TransferRequest request,
            string running,
            DateTime dateNow)
        {
            return new TbtProductionPlanTransferStatus
            {
                Running = running,
                Wo = plan.Wo,
                WoNumber = plan.WoNumber,
                ProductionPlanId = plan.Id,
                CreateDate = dateNow,
                CreateBy = request.TransferBy ?? _admin,
                FormerStatus = request.FormerStatus,
                TargetStatus = request.TargetStatus
            };
        }
        private async Task ProcessTransfer(TransferData data)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            if (data.NewStatuses.Any())
            {
                await _jewelryContext.TbtProductionPlanStatusHeader.AddRangeAsync(data.NewStatuses);
                await _jewelryContext.SaveChangesAsync();

                // Link transfer statuses with new status headers
                foreach (var transfer in data.TransferStatuses)
                {
                    var match = data.NewStatuses.First(x => x.ProductionPlanId == transfer.ProductionPlanId);
                    transfer.TargetStatusId = match.Id;
                }
            }

            if (data.TransferStatuses.Any())
            {
                await _jewelryContext.TbtProductionPlanTransferStatus.AddRangeAsync(data.TransferStatuses);
                await _jewelryContext.SaveChangesAsync();
            }

            if (data.UpdatePlans.Any())
            {
                _jewelryContext.TbtProductionPlan.UpdateRange(data.UpdatePlans);
                await _jewelryContext.SaveChangesAsync();
            }

            scope.Complete();
        }
        private class TransferData
        {
            public DateTime DateNow { get; set; }
            public string Running { get; set; }
            public List<TbtProductionPlanStatusHeader> NewStatuses { get; } = new();
            public List<TbtProductionPlanTransferStatus> TransferStatuses { get; } = new();
            public List<TbtProductionPlan> UpdatePlans { get; } = new();
            public List<TransferResponseItem> Errors { get; } = new();
            public bool HasAnyValidPlans => NewStatuses.Any();
        }

        #endregion
        #region --- method update production plan ---
        public async Task<string> UpdateProductionPlan(ProductionPlanStatusUpdateRequest request)
        {
            var plan = await GetProductionPlan(request);
            ValidatePlanStatus(plan, request);
            var statusHeader = await GetStatusHeader(request);

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {

                await UpdateStatusHeaderAndDetails(statusHeader, request);
                await UpdatePlan(plan, request);

                scope.Complete();
            }

            return $"{plan.Wo}-{plan.WoNumber}";
        }
        private async Task<TbtProductionPlan> GetProductionPlan(ProductionPlanStatusUpdateRequest request)
        {
            var plan = await _jewelryContext.TbtProductionPlan
                    .SingleOrDefaultAsync(item =>
                    item.Id == request.ProductionPlanId &&
                    item.Wo == request.Wo.ToUpper() &&
                    item.WoNumber == request.WoNumber);

            if (plan == null)
            {
                throw new HandleException($"{ErrorMessage.NotFound} --> {request.Wo}-{request.WoNumber}");
            }

            return plan;
        }
        private void ValidatePlanStatus(TbtProductionPlan plan, ProductionPlanStatusUpdateRequest request)
        {
            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"{ErrorMessage.PlanCompleted} --> {request.Wo}-{request.WoNumber}");
            }
            if (plan.Status == ProductionPlanStatus.Melted)
            {
                throw new HandleException($"{ErrorMessage.PlanMelted} --> {request.Wo}-{request.WoNumber}");
            }
        }
        private async Task<TbtProductionPlanStatusHeader> GetStatusHeader(ProductionPlanStatusUpdateRequest request)
        {
            var header = (from item in _jewelryContext.TbtProductionPlanStatusHeader
                              .Include(x => x.TbtProductionPlanStatusDetail)
                              .Include(x => x.TbtProductionPlanStatusDetailGem)
                          where item.ProductionPlanId == request.ProductionPlanId
                          && item.Status == request.Status
                          && item.Id == request.HeaderId
                          && item.IsActive
                          select item).SingleOrDefault();

            if (header == null)
            {
                throw new HandleException(ErrorMessage.PlanNeedTransfer);
            }

            return header;
        }
        private async Task UpdateStatusHeaderAndDetails(TbtProductionPlanStatusHeader header, ProductionPlanStatusUpdateRequest request)
        {
            var addStatusItems = new List<TbtProductionPlanStatusDetail>();
            var addStatusItemGems = new List<TbtProductionPlanStatusDetailGem>();
            var updateStatusItemGems = new List<TbtProductionPlanStatusDetailGem>();

            await UpdateStatusHeaderCommonFields(header, request);

            var status = (ProductionPlanStatusEnum)request.Status;
            switch (status)
            {
                case ProductionPlanStatusEnum.Casting:
                case ProductionPlanStatusEnum.Scrub:
                case ProductionPlanStatusEnum.Embed:
                case ProductionPlanStatusEnum.Plated:
                    await HandleProductionWoker(header, request, addStatusItems);
                    break;

                case ProductionPlanStatusEnum.Gems:
                    await HandleProductionGems(header, request, addStatusItems, addStatusItemGems, updateStatusItemGems);
                    break;

                case ProductionPlanStatusEnum.CVD:
                case ProductionPlanStatusEnum.Price:
                    await HandleProductionPrice(header, request);
                    break;
            }

            if (addStatusItems.Any())
                await _jewelryContext.TbtProductionPlanStatusDetail.AddRangeAsync(addStatusItems);

            if (addStatusItemGems.Any())
                await _jewelryContext.TbtProductionPlanStatusDetailGem.AddRangeAsync(addStatusItemGems);

            if (updateStatusItemGems.Any())
                _jewelryContext.TbtProductionPlanStatusDetailGem.UpdateRange(updateStatusItemGems);

            await _jewelryContext.SaveChangesAsync();
        }
        private async Task UpdateStatusHeaderCommonFields(TbtProductionPlanStatusHeader header, ProductionPlanStatusUpdateRequest request)
        {
            header.SendName = request.SendName;
            header.SendDate = request.SendDate?.UtcDateTime;
            header.CheckName = request.CheckName;
            header.CheckDate = request.CheckDate?.UtcDateTime;
            header.Remark1 = request.Remark1;
            header.Remark2 = request.Remark2;
            header.UpdateDate = DateTime.UtcNow;
            header.UpdateBy = _admin;

            _jewelryContext.TbtProductionPlanStatusHeader.Update(header);
            await _jewelryContext.SaveChangesAsync();
        }
        private async Task HandleProductionWoker(
           TbtProductionPlanStatusHeader header,
           ProductionPlanStatusUpdateRequest request,
           List<TbtProductionPlanStatusDetail> addStatusItems)
        {
            if (!request.Golds?.Any() ?? true)
            {
                throw new HandleException($"{ErrorMessage.PlanNeedGold}");
            }

            await RemoveExistingHeaderDetail(header);

            header.WagesTotal = request.Golds.Sum(x => x.TotalWages);

            foreach (var gold in request.Golds)
            {
                var detail = await CreateProductionHeaderDetail(request, header.Id, gold);
                addStatusItems.Add(detail);
            }
        }
        private async Task HandleReceiveStatus(
           TbtProductionPlanStatusHeader header,
           ProductionPlanStatusUpdateRequest request,
           List<TbtProductionPlanStatusDetail> addStatusItems)
        {
            if (!request.Golds?.Any() ?? true)
            {
                throw new HandleException("โปรดระบุรายละเอียดของทอง");
            }

            await RemoveExistingHeaderDetail(header);

            foreach (var gold in request.Golds)
            {
                var detail = await CreateReceiveStatusDetail(request, header.Id, gold);
                addStatusItems.Add(detail);
            }
        }
        private async Task HandleProductionGems(
           TbtProductionPlanStatusHeader header,
           ProductionPlanStatusUpdateRequest request,
           List<TbtProductionPlanStatusDetail> addStatusItems,
           List<TbtProductionPlanStatusDetailGem> addStatusItemGems,
           List<TbtProductionPlanStatusDetailGem> updateStatusItemGems)
        {
            if (!request.Golds?.Any() ?? true)
            {
                throw new HandleException("โปรดระบุรายละเอียดของทอง");
            }

            await RemoveExistingDetailsAndGems(header);

            foreach (var gold in request.Golds)
            {
                var detail = await CreateReceiveStatusDetail(request, header.Id, gold);
                addStatusItems.Add(detail);
            }

            if (request.Gems?.Any() == true)
            {
                await HandleGemDetails(header, request, addStatusItemGems, updateStatusItemGems);
            }
        }
        private async Task HandleProductionPrice(TbtProductionPlanStatusHeader header, ProductionPlanStatusUpdateRequest request)
        {
            // Simple status only updates header fields, which is handled in UpdateStatusHeaderCommonFields
            await Task.CompletedTask;
        }
        private async Task RemoveExistingHeaderDetail(TbtProductionPlanStatusHeader header)
        {
            _jewelryContext.TbtProductionPlanStatusDetail.RemoveRange(header.TbtProductionPlanStatusDetail);
            await _jewelryContext.SaveChangesAsync();
        }
        private async Task RemoveExistingDetailsAndGems(TbtProductionPlanStatusHeader header)
        {
            _jewelryContext.TbtProductionPlanStatusDetail.RemoveRange(header.TbtProductionPlanStatusDetail);

            var removeGems = header.TbtProductionPlanStatusDetailGem
                .Where(item => string.IsNullOrEmpty(item.OutboundRunning));

            if (removeGems.Any())
            {
                _jewelryContext.TbtProductionPlanStatusDetailGem.RemoveRange(removeGems);
            }

            await _jewelryContext.SaveChangesAsync();
        }
        private async Task<TbtProductionPlanStatusDetail> CreateProductionHeaderDetail(
            ProductionPlanStatusUpdateRequest request,
            int headerId,
            GoldItem gold)
        {
            var detail = new TbtProductionPlanStatusDetail
            {
                HeaderId = headerId,
                ProductionPlanId = request.ProductionPlanId,
                ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{request.ProductionPlanId}-{request.Status}"),
                IsActive = true,
                RequestDate = gold.RequestDate?.UtcDateTime,
                Gold = gold.Gold,
                GoldQtySend = gold.GoldQTYSend,
                GoldWeightSend = gold.GoldWeightSend,
                GoldQtyCheck = gold.GoldQTYCheck,
                GoldWeightCheck = gold.GoldWeightCheck,
                Worker = gold.Worker,
                WorkerSub = gold.WorkerSub,
                Description = gold.Description,
                Wages = gold.Wages ?? 0,
                TotalWages = gold.TotalWages ?? 0
            };

            if (gold.GoldWeightSend.HasValue && gold.GoldWeightCheck.HasValue)
            {
                detail.GoldWeightDiff = gold.GoldWeightSend - gold.GoldWeightCheck;
                decimal divide = gold.GoldWeightSend.Value <= 0 ? 1 : gold.GoldWeightSend.Value;
                detail.GoldWeightDiffPercent = 100 - ((gold.GoldWeightCheck.Value * 100) / divide);
            }

            return detail;
        }
        private async Task<TbtProductionPlanStatusDetail> CreateReceiveStatusDetail(
            ProductionPlanStatusUpdateRequest request,
            int headerId,
            GoldItem gold)
        {
            return new TbtProductionPlanStatusDetail
            {
                HeaderId = headerId,
                ProductionPlanId = request.ProductionPlanId,
                ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{request.ProductionPlanId}-{request.Status}"),
                IsActive = true,
                RequestDate = gold.RequestDate?.UtcDateTime,
                Worker = gold.Worker,
                WorkerSub = gold.WorkerSub,
                Gold = gold.Gold,
                GoldQtySend = gold.GoldQTYCheck,
                GoldWeightSend = gold.GoldWeightCheck,
                GoldQtyCheck = gold.GoldQTYCheck,
                GoldWeightCheck = gold.GoldWeightCheck
            };
        }
        private async Task HandleGemDetails(
            TbtProductionPlanStatusHeader header,
            ProductionPlanStatusUpdateRequest request,
            List<TbtProductionPlanStatusDetailGem> addStatusItemGems,
            List<TbtProductionPlanStatusDetailGem> updateStatusItemGems)
        {
            var validGems = request.Gems.Where(x => !string.IsNullOrEmpty(x.Code) && x.Id.HasValue).ToList();

            foreach (var gem in validGems)
            {
                var existingGem = header.TbtProductionPlanStatusDetailGem
                    .SingleOrDefault(x => x.OutboundRunning == gem.OutboundRunning && x.ItemNo == gem.ItemNo);

                if (existingGem != null)
                {
                    existingGem.GemPrice = gem.Price;
                    updateStatusItemGems.Add(existingGem);
                }
                else
                {
                    var newGem = new TbtProductionPlanStatusDetailGem
                    {
                        HeaderId = header.Id,
                        ProductionPlanId = request.ProductionPlanId,
                        ItemNo = await _runningNumberService.GenerateRunningNumber($"G-{request.ProductionPlanId}-{request.Status}"),
                        IsActive = true,
                        GemId = gem.Id.Value,
                        GemCode = gem.Code,
                        GemQty = gem.QTY,
                        GemWeight = gem.Weight,
                        GemName = gem.Name,
                        GemPrice = gem.Price
                    };
                    addStatusItemGems.Add(newGem);
                }
            }
        }
        private async Task UpdatePlan(TbtProductionPlan plan, ProductionPlanStatusUpdateRequest request)
        {
            var currentStatus = (ProductionPlanStatusEnum)plan.Status;
            var targetStatus = (ProductionPlanStatusEnum)request.Status;

            if (currentStatus.IsUpdateByWatingStatus(targetStatus))
            {
                plan.Status = currentStatus.GetNextStatus();
                plan.UpdateDate = DateTime.UtcNow;
                plan.UpdateBy = _admin;

                _jewelryContext.TbtProductionPlan.Update(plan);
                await _jewelryContext.SaveChangesAsync();
            }
        }

        #endregion
        #endregion

        #region --- old plan add/update ---
        // ----- old Add/Update Status Detail -----//
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
            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanCompleted}");
            }
            if (plan.Status == ProductionPlanStatus.Melted)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanMelted}");
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
                var addStatusItemGem = new List<TbtProductionPlanStatusDetailGem>();

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

                                    RequestDate = item.RequestDate.HasValue ? item.RequestDate.Value.UtcDateTime : null,

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

                                    decimal divide = item.GoldWeightSend.Value <= 0 ? 1 : item.GoldWeightSend.Value;
                                    newStatusItem.GoldWeightDiffPercent = 100 - ((item.GoldWeightCheck * 100) / divide);
                                }

                                addStatusItem.Add(newStatusItem);
                            }
                        }
                        break;
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

                                SendName = request.CheckName,
                                SendDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null,
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

                                    RequestDate = item.RequestDate.HasValue ? item.RequestDate.Value.UtcDateTime : null,

                                    Worker = item.Worker,
                                    WorkerSub = item.WorkerSub,

                                    Gold = item.Gold,
                                    GoldQtySend = item.GoldQTYCheck,
                                    GoldWeightSend = item.GoldWeightCheck,
                                    GoldQtyCheck = item.GoldQTYCheck,
                                    GoldWeightCheck = item.GoldWeightCheck,
                                };
                                addStatusItem.Add(newStatus);
                            }
                        }
                        break;
                    case 70://จ่ายขัดพลอย
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

                                SendName = request.CheckName,
                                SendDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null,
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

                                    RequestDate = item.RequestDate.HasValue ? item.RequestDate.Value.UtcDateTime : null,

                                    Worker = item.Worker,
                                    WorkerSub = item.WorkerSub,

                                    Gold = item.Gold,
                                    GoldQtySend = item.GoldQTYCheck,
                                    GoldWeightSend = item.GoldWeightCheck,
                                    GoldQtyCheck = item.GoldQTYCheck,
                                    GoldWeightCheck = item.GoldWeightCheck,
                                };
                                addStatusItem.Add(newStatus);
                            }

                            if (request.Gems != null && request.Gems.Any())
                            {
                                request.Gems = request.Gems.Where(x => !string.IsNullOrEmpty(x.Code) && x.Id.HasValue).ToList();

                                if (request.Gems.Count > 0)
                                {
                                    foreach (var item in request.Gems)
                                    {
                                        var newGem = new TbtProductionPlanStatusDetailGem()
                                        {
                                            HeaderId = addStatus.Id,
                                            ProductionPlanId = request.ProductionPlanId,
                                            ItemNo = await _runningNumberService.GenerateRunningNumber($"G-{request.ProductionPlanId}-{request.Status}"),
                                            IsActive = true,

                                            GemId = item.Id.Value,
                                            GemCode = item.Code,
                                            GemQty = item.QTY,
                                            GemWeight = item.Weight,
                                            GemName = item.Name,
                                            GemPrice = item.Price,
                                        };
                                        addStatusItemGem.Add(newGem);
                                    }
                                }
                            }
                        }
                        break;
                    case 85:// cvd
                    case 95:// ประเมินราคา
                    case 100: //สำเร็จ
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


                                SendName = request.CheckName,
                                SendDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null,
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
                    case 500:
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

                                SendName = request.SendName,
                                SendDate = request.SendDate.HasValue ? request.SendDate.Value.UtcDateTime : null,
                                CheckName = request.CheckName,
                                CheckDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null,

                                Remark1 = request.Remark1,
                                Remark2 = request.Remark2,
                            };
                            _jewelryContext.TbtProductionPlanStatusHeader.Add(addStatus);
                            await _jewelryContext.SaveChangesAsync();

                            if (request.Golds != null && request.Golds.Any())
                            {
                                foreach (var item in request.Golds)
                                {

                                    var newStatusItem = new TbtProductionPlanStatusDetail()
                                    {
                                        HeaderId = addStatus.Id,
                                        ProductionPlanId = request.ProductionPlanId,
                                        ItemNo = await _runningNumberService.GenerateRunningNumber($"S-{request.ProductionPlanId}-{request.Status}"),
                                        IsActive = true,

                                        RequestDate = item.RequestDate.HasValue ? item.RequestDate.Value.UtcDateTime : null,

                                        Gold = item.Gold,
                                        GoldQtySend = item.GoldQTYSend,
                                        GoldWeightSend = item.GoldWeightSend,
                                        GoldQtyCheck = item.GoldQTYSend,
                                        GoldWeightCheck = item.GoldWeightSend,
                                    };
                                    addStatusItem.Add(newStatusItem);
                                }
                            }
                        }
                        break;

                }

                _jewelryContext.TbtProductionPlanStatusDetail.AddRange(addStatusItem);

                if (addStatusItemGem.Any())
                {
                    _jewelryContext.TbtProductionPlanStatusDetailGem.AddRange(addStatusItemGem);
                }
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
            //if (plan.Status == ProductionPlanStatus.Completed)
            //{
            //    throw new HandleException($"ใบจ่าย-รับคืนงาน {request.Wo}-{request.WoNumber} อยู่ในสถานะดำเนินการเสร็จสิ้น กรุณาตรวจสอบอีกครั้ง");
            //}
            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanCompleted}");
            }
            if (plan.Status == ProductionPlanStatus.Melted)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanMelted}");
            }


            var checkStatus = (from item in _jewelryContext.TbtProductionPlanStatusHeader
                               .Include(x => x.TbtProductionPlanStatusDetail)
                               .Include(x => x.TbtProductionPlanStatusDetailGem)
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
                var addStatusItemGem = new List<TbtProductionPlanStatusDetailGem>();
                var updateStatusItemGem = new List<TbtProductionPlanStatusDetailGem>();

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

                                    RequestDate = item.RequestDate.HasValue ? item.RequestDate.Value.UtcDateTime : null,


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

                                    decimal divide = item.GoldWeightSend.Value <= 0 ? 1 : item.GoldWeightSend.Value;
                                    newStatusItem.GoldWeightDiffPercent = 100 - ((item.GoldWeightCheck * 100) / divide);
                                }

                                addStatusItem.Add(newStatusItem);
                            }
                        }
                        break;
                    case 55://
                        {
                            if (request.Golds == null || request.Golds.Any() == false)
                            {
                                throw new HandleException($"โปรดระบุรายละเอียดของทอง");
                            }

                            _jewelryContext.TbtProductionPlanStatusDetail.RemoveRange(checkStatus.TbtProductionPlanStatusDetail);
                            await _jewelryContext.SaveChangesAsync();

                            checkStatus.SendName = request.CheckName;
                            checkStatus.SendDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null;
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

                                    RequestDate = item.RequestDate.HasValue ? item.RequestDate.Value.UtcDateTime : null,

                                    Worker = item.Worker,
                                    WorkerSub = item.WorkerSub,

                                    Gold = item.Gold,
                                    GoldQtySend = item.GoldQTYCheck,
                                    GoldWeightSend = item.GoldWeightCheck,
                                    GoldQtyCheck = item.GoldQTYCheck,
                                    GoldWeightCheck = item.GoldWeightCheck,
                                };
                                addStatusItem.Add(newStatus);
                            }
                        }
                        break;
                    case 70://จ่ายขัดพลอย
                        {
                            if (request.Golds == null || request.Golds.Any() == false)
                            {
                                throw new HandleException($"โปรดระบุรายละเอียดของทอง");
                            }

                            _jewelryContext.TbtProductionPlanStatusDetail.RemoveRange(checkStatus.TbtProductionPlanStatusDetail);

                            //
                            var removeGem = (from item in checkStatus.TbtProductionPlanStatusDetailGem
                                             where string.IsNullOrEmpty(item.OutboundRunning)
                                             select item);


                            //_jewelryContext.TbtProductionPlanStatusDetailGem.RemoveRange(checkStatus.TbtProductionPlanStatusDetailGem);
                            if (removeGem.Any())
                            {
                                _jewelryContext.TbtProductionPlanStatusDetailGem.RemoveRange(removeGem);
                            }
                            await _jewelryContext.SaveChangesAsync();


                            checkStatus.SendName = request.CheckName;
                            checkStatus.SendDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null;
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

                                    RequestDate = item.RequestDate.HasValue ? item.RequestDate.Value.UtcDateTime : null,

                                    Worker = item.Worker,
                                    WorkerSub = item.WorkerSub,

                                    Gold = item.Gold,
                                    GoldQtySend = item.GoldQTYCheck,
                                    GoldWeightSend = item.GoldWeightCheck,
                                    GoldQtyCheck = item.GoldQTYCheck,
                                    GoldWeightCheck = item.GoldWeightCheck,
                                };
                                addStatusItem.Add(newStatus);
                            }

                            if (request.Gems != null && request.Gems.Any())
                            {
                                request.Gems = request.Gems.Where(x => !string.IsNullOrEmpty(x.Code) && x.Id.HasValue).ToList();

                                if (request.Gems.Count > 0)
                                {
                                    foreach (var item in request.Gems)
                                    {
                                        var matchGem = checkStatus.TbtProductionPlanStatusDetailGem.Where(x => x.OutboundRunning == item.OutboundRunning && x.ItemNo == item.ItemNo).SingleOrDefault();

                                        if (matchGem != null)
                                        {
                                            matchGem.GemPrice = item.Price;
                                            updateStatusItemGem.Add(matchGem);
                                        }
                                        else
                                        {
                                            var newGem = new TbtProductionPlanStatusDetailGem()
                                            {
                                                HeaderId = checkStatus.Id,
                                                ProductionPlanId = request.ProductionPlanId,
                                                ItemNo = await _runningNumberService.GenerateRunningNumber($"G-{request.ProductionPlanId}-{request.Status}"),
                                                IsActive = true,

                                                GemId = item.Id.Value,
                                                GemCode = item.Code,
                                                GemQty = item.QTY,
                                                GemWeight = item.Weight,
                                                GemName = item.Name,
                                                GemPrice = item.Price,
                                            };
                                            addStatusItemGem.Add(newGem);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case 85: //CVD
                    case 95: // ประเมินราคา
                        {
                            checkStatus.SendName = request.CheckName;
                            checkStatus.SendDate = request.CheckDate.HasValue ? request.CheckDate.Value.UtcDateTime : null;
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
                if (addStatusItemGem.Any())
                {
                    _jewelryContext.TbtProductionPlanStatusDetailGem.AddRange(addStatusItemGem);
                }
                if (updateStatusItemGem.Any())
                {
                    _jewelryContext.TbtProductionPlanStatusDetailGem.UpdateRange(updateStatusItemGem);
                }

                await _jewelryContext.SaveChangesAsync();

                //TODO: 28/09/2024 not update plan status
                //plan.Status = request.Status;
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

            //fix miss 
            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"ใบจ่าย-รับคืนงาน {request.Wo}-{request.WoNumber} อยู่ในสถานะดำเนินการเสร็จสิ้น กรุณาตรวจสอบอีกครั้ง");
            }
            if (plan.Status == ProductionPlanStatus.Melted)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanMelted}");
            }

            if (plan.Status == ProductionPlanStatus.Gems)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PermissionFail}");
            }

            var statusHeader = (from item in _jewelryContext.TbtProductionPlanStatusHeader
                                .Include(x => x.TbtProductionPlanStatusDetail)
                                .Include(x => x.TbtProductionPlanStatusDetailGem)
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

            if (statusHeader.TbtProductionPlanStatusDetailGem != null)
            {
                foreach (var item in statusHeader.TbtProductionPlanStatusDetailGem)
                {
                    item.IsActive = false;
                }
                _jewelryContext.TbtProductionPlanStatusDetailGem.UpdateRange(statusHeader.TbtProductionPlanStatusDetailGem);
            }

            //restore current plan status
            if (plan.Status == statusHeader.Status)
            {
                var lastStatus = (from item in _jewelryContext.TbtProductionPlanStatusHeader
                                  where item.ProductionPlanId == request.ProductionPlanId && item.IsActive
                                  orderby item.CreateDate descending
                                  select item).ToList();

                if (lastStatus != null && lastStatus.Count > 1)
                {
                    plan.Status = lastStatus[1].Status;
                }
                else
                {
                    plan.Status = ProductionPlanStatus.Designed;
                }
            }

            plan.UpdateBy = _admin;
            plan.UpdateDate = DateTime.UtcNow;



            await _jewelryContext.SaveChangesAsync();

            return $"{plan.Wo}-{plan.WoNumber}";
        }
        #endregion

        // ----- Master ----- //
        public IQueryable<TbmProductionPlanStatus> GetProductionPlanStatus()
        {
            return (from item in _jewelryContext.TbmProductionPlanStatus
                    select item).OrderBy(x => x.Id);
        }

        // ----- get all transaction ----- //
        public async Task<TransectionResponse> GetAllTransectionPrice(string wo, int woNumber)
        {
            var response = new TransectionResponse();

            var masterGoldSize = (from item in _jewelryContext.TbmGoldSize
                                  select item).ToList();

            var masterGold = (from item in _jewelryContext.TbmGold
                              select item).ToList();

            //group 1 --> get gold
            var transectionGold = (from item in _jewelryContext.TbtProductionPlanCostGoldItem
                                   .Include(x => x.TbtProductionPlanCostGold)
                                   where item.TbtProductionPlanCostGold.IsActive == true
                                   && item.ProductionPlanId == $"{wo}-{woNumber}"
                                   select new TransectionItem()
                                   {
                                       Name = item.TbtProductionPlanCostGold.Gold,
                                       NameGroup = TypeofPrice.Gold,

                                       Reference1 = item.TbtProductionPlanCostGold.GoldSize,
                                       Date = item.CreateDate,

                                       Qty = 0,
                                       QtyPrice = 0,

                                       QtyWeight = item.ReturnQty,
                                       QtyWeightPrice = 0,

                                       PriceReference = item.TbtProductionPlanCostGold.Cost,
                                   }).ToList();

            if (transectionGold.Any())
            {
                foreach (var gold in transectionGold)
                {
                    var goldSizeMaster = masterGoldSize.Where(x => x.Code == gold.Reference1).FirstOrDefault();
                    var goldMaster = masterGold.Where(x => x.Code == gold.Name).FirstOrDefault();

                    gold.NameDescription = $"{gold.Name}-{goldMaster.NameEn}-{goldSizeMaster.NameTh}";
                }

                var goldtest = transectionGold.ToList();
                response.Items.AddRange(transectionGold);
            }

            //group 2 --> get worker 

            int[] getSatus = new int[] { 50, 60, 80, 90 };
            var transectionWorker = (from item in _jewelryContext.TbtProductionPlanStatusDetail
                                     .Include(x => x.Header)
                                     .ThenInclude(x => x.ProductionPlan)
                                     .ThenInclude(x => x.StatusNavigation)

                                     join status in _jewelryContext.TbmProductionPlanStatus on item.Header.Status equals status.Id

                                     where item.Header.ProductionPlan.Wo == wo
                                     && item.Header.ProductionPlan.WoNumber == woNumber
                                     && item.IsActive == true
                                     && item.Header.IsActive == true
                                     && getSatus.Contains(item.Header.Status)

                                     select new TransectionItem()
                                     {
                                         Name = status.NameTh,
                                         NameDescription = $"[{status.NameTh}] {item.Description}",
                                         NameGroup = TypeofPrice.Worker,

                                         Date = item.RequestDate,

                                         Qty = item.GoldQtyCheck,
                                         QtyPrice = item.Wages,

                                         QtyWeight = 0,
                                         QtyWeightPrice = 0,

                                         PriceReference = item.TotalWages,
                                     });
            //var transectionWorker = (from item in _jewelryContext.TbtProductionPlanStatusHeader
            //                        .Include(x => x.ProductionPlan)
            //                        .ThenInclude(x => x.StatusNavigation)

            //                         join status in _jewelryContext.TbmProductionPlanStatus on item.Status equals status.Id

            //                         where item.ProductionPlan.Wo == wo
            //                         && item.ProductionPlan.WoNumber == woNumber
            //                         && item.IsActive == true
            //                         && item.IsActive == true

            //                         select new TransectionItem()
            //                         {
            //                             Name = status.NameTh,
            //                             NameDescription = status.NameTh,
            //                             NameGroup = TypeofPrice.Worker,

            //                             Date = item.CreateDate,

            //                             Qty = 1,
            //                             Price = item.WagesTotal,
            //                             //PriceReference = item.Wages,
            //                         }).ToList();

            if (transectionWorker.Any())
            {
                response.Items.AddRange(transectionWorker);
            }

            //group 3 --> get gem
            var transectionGem = (from item in _jewelryContext.TbtProductionPlanStatusDetailGem
                                     .Include(x => x.Header)
                                     .ThenInclude(x => x.ProductionPlan)
                                     .ThenInclude(x => x.StatusNavigation)

                                      //join status in _jewelryContext.TbmProductionPlanStatus on item.Header.Status equals status.Id
                                  join gem in _jewelryContext.TbtStockGem on item.GemCode equals gem.Code

                                  where item.Header.ProductionPlan.Wo == wo


                                  where item.Header.ProductionPlan.Wo == wo
                                  && item.Header.ProductionPlan.WoNumber == woNumber
                                  && item.IsActive == true
                                  && item.Header.IsActive == true

                                  select new TransectionItem()
                                  {
                                      Name = item.GemName ?? item.GemCode,
                                      NameDescription = item.GemName ?? item.GemCode,
                                      NameGroup = TypeofPrice.Gem,

                                      Date = item.RequestDate,

                                      Qty = item.GemQty,
                                      QtyPrice = gem.PriceQty,

                                      QtyWeight = item.GemWeight,
                                      QtyWeightPrice = gem.Price,

                                  }).ToList();

            if (transectionGem.Any())
            {
                response.Items.AddRange(transectionGem);
            }

            return response;
        }

        public async Task<string> CreatePrice(CreatePriceRequest request)
        {

            var plan = (from item in _jewelryContext.TbtProductionPlan
                        where item.Wo == request.Wo
                        && item.WoNumber == request.WoNumber
                        && item.Id == request.ProductionPlanId
                        select item).FirstOrDefault();

            if (plan == null)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.NotFound}");
            }
            if (plan.Status == ProductionPlanStatus.Completed)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanCompleted}");
            }
            if (plan.Status == ProductionPlanStatus.Melted)
            {
                throw new HandleException($"{request.Wo}-{request.WoNumber} --> {ErrorMessage.PlanMelted}");
            }

            var oldPrice = (from item in _jewelryContext.TbtProductionPlanPrice
                            where item.Wo == request.Wo
                            && item.WoNumber == request.WoNumber
                            select item);

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var newPrices = new List<TbtProductionPlanPrice>();
                var running = await _runningNumberService.GenerateRunningNumberForGold("PRI");

                foreach (var item in request.Item)
                {
                    var newPrice = new TbtProductionPlanPrice()
                    {
                        ProductionId = plan.Id,
                        Running = running,

                        Wo = request.Wo,
                        WoNumber = request.WoNumber,
                        WoText = request.WoText,

                        No = item.No,
                        Name = item.Name,
                        NameDescription = item.NameDescription,
                        NameGroup = item.NameGroup,

                        Qty = item.Qty,
                        QtyPrice = item.QtyPrice,

                        QtyWeight = item.QtyWeight,
                        QtyWeightPrice = item.QtyWeightPrice,

                        TotalPrice = item.TotalPrice,

                        Date = item.Date.HasValue ? item.Date.Value.UtcDateTime : null,

                        CreateBy = _admin,
                        CreateDate = DateTime.UtcNow,
                    };
                    newPrices.Add(newPrice);
                }

                if (oldPrice.Any())
                {
                    _jewelryContext.TbtProductionPlanPrice.RemoveRange(oldPrice);
                }
                if (newPrices.Any())
                {
                    _jewelryContext.TbtProductionPlanPrice.AddRange(newPrices);
                }

                plan.UpdateDate = DateTime.UtcNow;
                plan.UpdateBy = _admin;

                if (plan.Status == ProductionPlanStatus.WaitPrice)
                {
                    plan.Status = ProductionPlanStatus.Price;
                }

                _jewelryContext.TbtProductionPlan.Update(plan);

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return "success";
        }

        #endregion

        //private int GetTransferStatus(int status)
        //{
        //    switch (status)
        //    {
        //        case ProductionPlanStatus.Casting:
        //            return 60;
        //        case 60:
        //            return 70;
        //        case 70:
        //            return 80;
        //        case 80:
        //            return 90;
        //        case 90:
        //            return 100;
        //        case 100:
        //            return 500;
        //        default:
        //            return 0;
        //    }
        //}
    }
}
