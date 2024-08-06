using Azure.Core;
using ICSharpCode.SharpZipLib.Zip;
using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.Mold;
using jewelry.Model.Mold.PlanCasting;
using jewelry.Model.Mold.PlanCastingSilver;
using jewelry.Model.Mold.PlanCutting;
using jewelry.Model.Mold.PlanDesign;
using jewelry.Model.Mold.PlanGems;
using jewelry.Model.Mold.PlanGet;
using jewelry.Model.Mold.PlanList;
using jewelry.Model.Mold.PlanResin;
using jewelry.Model.Mold.PlanStore;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Mold
{
    public interface IMoldPlanService
    {
        IQueryable<PlanListReponse> PlanList(PlanListRequestModel request);
        PlanGetResponse PlanGet(int id);

        Task<string> PlanGems(PlanGemsRequest request);
        Task<string> PlanDesign(PlanDesignRequest request);
        Task<string> PlanResin(PlanResinRequest request);
        Task<string> PlanCastingSilver(PlanCastingSilverRequest request);
        Task<string> PlanCasting(PlanCastingRequest request);
        Task<string> PlanCutting(PlanCuttingRequest request);
        Task<string> PlanStore(PlanStoreRequest request);
    }
    public class MoldPlanService : IMoldPlanService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;

        public MoldPlanService(JewelryContext jewelryContext, IHostEnvironment hostingEnvironment, IRunningNumber runningNumberService)
        {
            _jewelryContext = jewelryContext;
            _hostingEnvironment = hostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public IQueryable<PlanListReponse> PlanList(PlanListRequestModel request)
        {
            var query = (from plan in _jewelryContext.TbtProductMoldPlan
                         .Include(x => x.StatusNavigation)
                         .Include(x => x.TbtProductMoldPlanDesign)
                         .Include(x => x.TbtProductMoldPlanResin)
                         .Include(x => x.TbtProductMoldPlanCastingSilver)
                         .Include(x => x.TbtProductMoldPlanCasting)
                         .Include(x => x.TbtProductMoldPlanCutting)
                         where plan.IsActive == true
                         select new PlanListReponse()
                         {
                             Id = plan.Id,
                             MoldCode = plan.TbtProductMoldPlanDesign.First().CodePlan,
                             Code = plan.TbtProductMoldPlanCutting.Any() ? plan.TbtProductMoldPlanCutting.First().Code : string.Empty,

                             CreateDate = plan.CreateDate,
                             UpdateDate = plan.UpdateDate ?? default,

                             Status = plan.Status,
                             StatusName = plan.StatusNavigation.NameTh,
                             NextStatus = plan.NextStatus,
                             NextStatusName = plan.NextStatusNavigation.NameTh,

                             CatagoryName = plan.TbtProductMoldPlanDesign.First().CategoryCodeNavigation.NameTh,
                             DesignBy = plan.TbtProductMoldPlanDesign.First().DesignBy,

                             ImgDesign = plan.TbtProductMoldPlanDesign.First().ImageUrl ?? string.Empty,
                             ImgResin = plan.TbtProductMoldPlanResin.Any()
                                ? plan.TbtProductMoldPlanResin.First().ImageUrl ?? string.Empty : string.Empty,
                             ImgCastingSilver = plan.TbtProductMoldPlanCastingSilver.Any()
                                ? plan.TbtProductMoldPlanCastingSilver.First().ImageUrl ?? string.Empty : string.Empty,
                             ImgCasting = plan.TbtProductMoldPlanCasting.Any()
                                ? plan.TbtProductMoldPlanCasting.First().ImageUrl ?? string.Empty : string.Empty,
                             ImgCutting = plan.TbtProductMoldPlanCutting.Any()
                                ? plan.TbtProductMoldPlanCutting.First().ImageUrl ?? string.Empty : string.Empty,
                             ImgStore = plan.TbtProductMoldPlanStore.Any()
                                ? plan.TbtProductMoldPlanStore.First().ImageUrl ?? string.Empty : string.Empty,
                         });

            if (!string.IsNullOrEmpty(request.MoldCode))
            {
                query = query.Where(x => x.MoldCode.Contains(request.MoldCode));
            }

            if (request.CreateStart.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.CreateStart);
            }
            if (request.CreateEnd.HasValue)
            {
                query = query.Where(x => x.CreateDate <= request.CreateEnd);
            }

            if (request.UpdateStart.HasValue)
            {
                query = query.Where(x => x.UpdateDate >= request.UpdateStart);
            }
            if (request.UpdateEnd.HasValue)
            {
                query = query.Where(x => x.UpdateDate <= request.UpdateEnd);
            }

            return query;
        }
        public PlanGetResponse PlanGet(int id)
        {
            var planMold = (from plan in _jewelryContext.TbtProductMoldPlan
                            .Include(x => x.TbtProductMoldPlanGem)
                                .ThenInclude(x => x.GemCodeNavigation)
                            .Include(x => x.TbtProductMoldPlanGem)
                                .ThenInclude(x => x.GemShapeCodeNavigation)
                            .Include(x => x.StatusNavigation)
                            .Include(x => x.TbtProductMoldPlanDesign)
                            .Include(x => x.TbtProductMoldPlanResin)
                            .Include(x => x.TbtProductMoldPlanCastingSilver)
                            where plan.Id == id
                            select new PlanGetResponse()
                            {
                                Id = plan.Id,
                                CreateDate = plan.CreateDate,
                                UpdateDate = plan.UpdateDate ?? default,

                                Status = plan.Status,
                                StatusName = plan.StatusNavigation.NameTh,
                                NextStatus = plan.NextStatus,
                                NextStatusName = plan.NextStatusNavigation.NameTh,

                                Design = new PlanGetItemStatus()
                                {
                                    MoldCode = plan.TbtProductMoldPlanDesign.First().CodePlan,
                                    Remark = plan.TbtProductMoldPlanDesign.First().Remark,
                                    //SizeGem = plan.TbtProductMoldPlanDesign.First().SizeGem,
                                    //SizeDiamond = plan.TbtProductMoldPlanDesign.First().SizeDiamond,
                                    //QtyGem = plan.TbtProductMoldPlanDesign.First().QtyGem,
                                    //QtyDiamond = plan.TbtProductMoldPlanDesign.First().QtyDiamond,
                                    QtyReceive = plan.TbtProductMoldPlanDesign.First().QtyReceive,
                                    QtySend = plan.TbtProductMoldPlanDesign.First().QtySend,
                                    Image = plan.TbtProductMoldPlanDesign.First().ImageUrl ?? string.Empty,

                                    CreateBy = plan.TbtProductMoldPlanDesign.First().CreateBy,
                                    CreateDate = plan.TbtProductMoldPlanDesign.First().CreateDate,
                                    UpdateBy = plan.TbtProductMoldPlanDesign.First().UpdateBy,
                                    UpdateDate = plan.TbtProductMoldPlanDesign.First().UpdateDate ?? default,

                                    Catagory = plan.TbtProductMoldPlanDesign.First().CategoryCode,
                                    CatagoryName = plan.TbtProductMoldPlanDesign.First().CategoryCodeNavigation.NameTh,
                                    WorkBy = plan.TbtProductMoldPlanDesign.First().DesignBy
                                },

                                Gems = plan.TbtProductMoldPlanGem.Any() ? plan.TbtProductMoldPlanGem.Select(x => new ModelGem()
                                {
                                    GemCode = x.GemCode,
                                    GemNameTH = x.GemCodeNavigation.NameTh,
                                    GemNameEN = x.GemCodeNavigation.NameEn,

                                    GemShapeCode = x.GemShapeCode,
                                    GemShapeNameTH = x.GemShapeCodeNavigation.NameTh,
                                    GemShapeNameEN = x.GemShapeCodeNavigation.NameEn,

                                    Size = x.Size,
                                    Qty = x.Qty
                                }).ToList()
                                : new List<ModelGem>(),

                                Resin = plan.TbtProductMoldPlanResin.Any() ? new PlanGetItemStatus()
                                {
                                    QtyReceive = plan.TbtProductMoldPlanResin.First().QtyReceive,
                                    QtySend = plan.TbtProductMoldPlanResin.First().QtySend,

                                    WorkBy = plan.TbtProductMoldPlanResin.First().ResinBy,
                                    Remark = plan.TbtProductMoldPlanResin.First().Remark,
                                    Image = plan.TbtProductMoldPlanResin.First().ImageUrl ?? string.Empty,

                                    CreateBy = plan.TbtProductMoldPlanResin.First().CreateBy,
                                    CreateDate = plan.TbtProductMoldPlanResin.First().CreateDate,
                                    UpdateBy = plan.TbtProductMoldPlanResin.First().UpdateBy,
                                    UpdateDate = plan.TbtProductMoldPlanResin.First().UpdateDate ?? default,
                                } : null,

                                CastingSilver = plan.TbtProductMoldPlanCastingSilver.Any() ? new PlanGetItemStatus()
                                {
                                    QtyReceive = plan.TbtProductMoldPlanCastingSilver.First().QtyReceive,
                                    QtySend = plan.TbtProductMoldPlanCastingSilver.First().QtySend,

                                    WorkBy = plan.TbtProductMoldPlanCastingSilver.First().CastBy,
                                    Remark = plan.TbtProductMoldPlanCastingSilver.First().Remark,
                                    Image = plan.TbtProductMoldPlanCastingSilver.First().ImageUrl ?? string.Empty,

                                    CreateBy = plan.TbtProductMoldPlanCastingSilver.First().CreateBy,
                                    CreateDate = plan.TbtProductMoldPlanCastingSilver.First().CreateDate,
                                    UpdateBy = plan.TbtProductMoldPlanCastingSilver.First().UpdateBy,
                                    UpdateDate = plan.TbtProductMoldPlanCastingSilver.First().UpdateDate ?? default,
                                } : null,

                                Casting = plan.TbtProductMoldPlanCasting.Any() ? new PlanGetItemStatus()
                                {
                                    QtyReceive = plan.TbtProductMoldPlanCasting.First().QtyReceive,
                                    QtySend = plan.TbtProductMoldPlanCasting.First().QtySend,

                                    WorkBy = plan.TbtProductMoldPlanCasting.First().CastBy,
                                    Remark = plan.TbtProductMoldPlanCasting.First().Remark,
                                    Image = plan.TbtProductMoldPlanCasting.First().ImageUrl ?? string.Empty,

                                    CreateBy = plan.TbtProductMoldPlanCasting.First().CreateBy,
                                    CreateDate = plan.TbtProductMoldPlanCasting.First().CreateDate,
                                    UpdateBy = plan.TbtProductMoldPlanCasting.First().UpdateBy,
                                    UpdateDate = plan.TbtProductMoldPlanCasting.First().UpdateDate ?? default,
                                } : null,

                                Cutting = plan.TbtProductMoldPlanCutting.Any() ? new PlanGetItemStatus()
                                {
                                    Code = plan.TbtProductMoldPlanCutting.First().Code,

                                    QtyReceive = plan.TbtProductMoldPlanCutting.First().QtyReceive,
                                    QtySend = plan.TbtProductMoldPlanCutting.First().QtySend,

                                    WorkBy = plan.TbtProductMoldPlanCutting.First().CuttingBy,
                                    Remark = plan.TbtProductMoldPlanCutting.First().Remark,
                                    Image = plan.TbtProductMoldPlanCutting.First().ImageUrl ?? string.Empty,

                                    CreateBy = plan.TbtProductMoldPlanCutting.First().CreateBy,
                                    CreateDate = plan.TbtProductMoldPlanCutting.First().CreateDate,
                                    UpdateBy = plan.TbtProductMoldPlanCutting.First().UpdateBy,
                                    UpdateDate = plan.TbtProductMoldPlanCutting.First().UpdateDate ?? default,
                                } : null,

                                Store = plan.TbtProductMoldPlanStore.Any() ? new PlanGetItemStatus()
                                {
                                    Code = plan.TbtProductMoldPlanStore.First().Code,

                                    QtyReceive = plan.TbtProductMoldPlanStore.First().QtyReceive,
                                    QtySend = plan.TbtProductMoldPlanStore.First().QtySend,

                                    WorkBy = plan.TbtProductMoldPlanStore.First().WorkerBy,
                                    Remark = plan.TbtProductMoldPlanStore.First().Remark,
                                    Image = plan.TbtProductMoldPlanStore.First().ImageUrl ?? string.Empty,

                                    Location = plan.TbtProductMoldPlanStore.First().Location,

                                    CreateBy = plan.TbtProductMoldPlanStore.First().CreateBy,
                                    CreateDate = plan.TbtProductMoldPlanStore.First().CreateDate,
                                    UpdateBy = plan.TbtProductMoldPlanStore.First().UpdateBy,
                                    UpdateDate = plan.TbtProductMoldPlanStore.First().UpdateDate ?? default,
                                } : null,



                            }).FirstOrDefault();

            if (planMold == null)
            {
                throw new HandleException("ไม่พบข้อมูล กรุณาลองใหม่อีกครั้ง");
            }

            return planMold;
        }

        public async Task<string> PlanGems(PlanGemsRequest request)
        {
            var plan = (from item in _jewelryContext.TbtProductMoldPlan
                        where item.Id == request.Id
                        select item).FirstOrDefault();

            if (plan == null)
            {
                throw new HandleException("ไม่พบข้อมูลเเบบเเม่พิมพ์ กรุณาลองใหม่อีกครั้ง");
            }

            var oldGems = (from item in _jewelryContext.TbtProductMoldPlanGem
                           where item.PlanId == request.Id
                           select item).ToList();

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var gems = new List<TbtProductMoldPlanGem>();

                foreach (var item in request.Gems)
                {
                    var gem = new TbtProductMoldPlanGem()
                    {
                        PlanId = plan.Id,
                        CreateBy = _admin,
                        CreateDate = DateTime.UtcNow,
                        UpdateBy = _admin,
                        UpdateDate = DateTime.UtcNow,

                        Size = item.Size,
                        Qty = item.Qty,

                        GemCode = item.GemCode,
                        GemShapeCode = item.GemShapeCode,
                    };

                    gems.Add(gem);
                }

                if (oldGems.Any())
                {
                    _jewelryContext.TbtProductMoldPlanGem.RemoveRange(oldGems);
                }
                if (gems.Any())
                {
                    _jewelryContext.TbtProductMoldPlanGem.AddRange(gems);
                }
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            return "success";
        }
        public async Task<string> PlanDesign(PlanDesignRequest request)
        {
            #region *** validate ***
            var codeplan = (from item in _jewelryContext.TbtProductMoldPlanDesign
                            where item.CodePlan == request.MoldCode.ToUpper()
                            select item).FirstOrDefault();
            if (codeplan != null)
            {
                throw new HandleException("ไม่สามารถสร้างรหัสเเม่พิมพ์ซ้ำได้");
            }

            if (request.Images.Any() && request.Images.Count > 2)
            {
                throw new HandleException("ไม่สามารถบันทึกรูปต้นเเเบบเเม่พิมพ์มากกว่า 2 รูป");
            }


            #endregion
            #region *** create ***
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var running = await _runningNumberService.GenerateRunningNumberForGold("MOLD");
                var plan = new TbtProductMoldPlan()
                {
                    CreateBy = _admin,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = _admin,
                    UpdateDate = DateTime.UtcNow,

                    Running = running,

                    IsActive = true,
                    Status = MoldPlanStatus.Designed,
                    NextStatus = MoldPlanStatus.Resin

                };

                _jewelryContext.TbtProductMoldPlan.Add(plan);
                await _jewelryContext.SaveChangesAsync();

                var design = new TbtProductMoldPlanDesign()
                {
                    CodePlan = request.MoldCode.ToUpper(),
                    Remark = request.Remark,
                    QtySend = request.QtySend,
                    QtyReceive = request.QtyReceive,
                    CreateBy = _admin,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = _admin,
                    UpdateDate = DateTime.UtcNow,
                    PlanId = plan.Id,

                    CategoryCode = request.Catagory,
                    DesignBy = request.DesignBy
                };

                if (request.Images.Any())
                {
                    int count = 1;
                    //array to store stirng name image url
                    List<string> imagesUrl = new List<string>();
                    foreach (var item in request.Images)
                    {
                        try
                        {
                            string imageName = $"{request.MoldCode.ToUpper().Trim()}-{count}-Design.png";
                            string imagePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/MoldPlanDesign");
                            if (!Directory.Exists(imagePath))
                            {
                                Directory.CreateDirectory(imagePath);
                            }
                            string imagePathWithFileName = Path.Combine(imagePath, imageName);

                            //https://www.thecodebuzz.com/how-to-save-iformfile-to-disk/
                            using (Stream fileStream = new FileStream(imagePathWithFileName, FileMode.Create, FileAccess.Write))
                            {
                                item.CopyTo(fileStream);
                                fileStream.Close();
                            }

                            //design.ImageUrl = imageName;

                            //add image url to array
                            imagesUrl.Add(imageName);

                            count++;
                        }
                        catch (Exception ex)
                        {
                            throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                        }

                    }
                    if (imagesUrl != null)
                    {
                        //split image url by comma
                        design.ImageUrl = string.Join(",", imagesUrl);
                    }
                }

                _jewelryContext.TbtProductMoldPlanDesign.Add(design);
                await _jewelryContext.SaveChangesAsync();

                if (request.Gems.Any())
                {
                    var gems = new List<TbtProductMoldPlanGem>();

                    foreach (var item in request.Gems)
                    {
                        var gem = new TbtProductMoldPlanGem()
                        {
                            PlanId = plan.Id,
                            CreateBy = _admin,
                            CreateDate = DateTime.UtcNow,
                            UpdateBy = _admin,
                            UpdateDate = DateTime.UtcNow,

                            Size = item.Size,
                            Qty = item.Qty,

                            GemCode = item.GemCode,
                            GemShapeCode = item.GemShapeCode,
                        };

                        gems.Add(gem);
                    }

                    if (gems.Any())
                    {
                        _jewelryContext.TbtProductMoldPlanGem.AddRange(gems);
                        await _jewelryContext.SaveChangesAsync();
                    }
                }

                scope.Complete();
            }

            #endregion

            return "success";
        }
        public async Task<string> PlanResin(PlanResinRequest request)
        {
            #region *** validate ***
            var codeplan = (from item in _jewelryContext.TbtProductMoldPlanResin
                            where item.CodePlan == request.MoldCode
                            && item.PlanId == request.PlanId
                            select item).FirstOrDefault();
            if (codeplan != null)
            {
                throw new HandleException("ไม่สามารถสร้างรหัสเเม่พิมพ์ซ้ำได้");
            }

            if (request.Images.Any() && request.Images.Count > 1)
            {
                throw new HandleException("ไม่สามารถบันทึกเรซิ่นมากกว่า 1 รูป");
            }

            var plan = (from item in _jewelryContext.TbtProductMoldPlan
                        where item.Id == request.PlanId
                        select item).FirstOrDefault();

            if (plan == null)
            {
                throw new HandleException("ไม่พบข้อมูลเเม่พิมพ์ กรุณาลองใหม่อีกครั้ง");
            }
            if (plan.Status != MoldPlanStatus.Designed)
            {
                throw new HandleException("ไม่สามารถทำรายการได้[invalid status] กรุณาลองใหม่อีกครั้ง");
            }


            #endregion
            #region *** create ***
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                plan.UpdateBy = _admin;
                plan.UpdateDate = DateTime.UtcNow;
                plan.Status = MoldPlanStatus.Resin;
                plan.NextStatus = MoldPlanStatus.CastingSilver;

                _jewelryContext.TbtProductMoldPlan.Update(plan);
                await _jewelryContext.SaveChangesAsync();

                var resin = new TbtProductMoldPlanResin()
                {
                    CodePlan = request.MoldCode,
                    PlanId = plan.Id,

                    QtySend = request.QtySend,
                    QtyReceive = request.QtyReceive,

                    ResinBy = request.ResinBy,
                    Remark = request.Remark,

                    CreateBy = _admin,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = _admin,
                    UpdateDate = DateTime.UtcNow,
                };

                if (request.Images.Any())
                {
                    int count = 1;
                    //array to store stirng name image url
                    List<string> imagesUrl = new List<string>();
                    foreach (var item in request.Images)
                    {
                        try
                        {
                            string imageName = $"{request.MoldCode.ToUpper().Trim()}-{count}-Resin.png";
                            string imagePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/MoldPlanResin");
                            if (!Directory.Exists(imagePath))
                            {
                                Directory.CreateDirectory(imagePath);
                            }
                            string imagePathWithFileName = Path.Combine(imagePath, imageName);

                            //https://www.thecodebuzz.com/how-to-save-iformfile-to-disk/
                            using (Stream fileStream = new FileStream(imagePathWithFileName, FileMode.Create, FileAccess.Write))
                            {
                                item.CopyTo(fileStream);
                                fileStream.Close();
                            }

                            //add image url to array
                            imagesUrl.Add(imageName);

                            count++;
                        }
                        catch (Exception ex)
                        {
                            throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                        }

                    }
                    if (imagesUrl != null)
                    {
                        //split image url by comma
                        resin.ImageUrl = string.Join(",", imagesUrl);
                    }
                }

                _jewelryContext.TbtProductMoldPlanResin.Add(resin);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            #endregion

            return "success";
        }
        public async Task<string> PlanCastingSilver(PlanCastingSilverRequest request)
        {
            #region *** validate ***
            var codeplan = (from item in _jewelryContext.TbtProductMoldPlanCastingSilver
                            where item.CodePlan == request.MoldCode
                            && item.PlanId == request.PlanId
                            select item).FirstOrDefault();
            if (codeplan != null)
            {
                throw new HandleException("ไม่สามารถสร้างรหัสเเม่พิมพ์ซ้ำได้");
            }

            if (request.Images.Any() && request.Images.Count > 1)
            {
                throw new HandleException("ไม่สามารถบันทึกหล่อพิมพ์เงินมากกว่า 1 รูป");
            }

            var plan = (from item in _jewelryContext.TbtProductMoldPlan
                        where item.Id == request.PlanId
                        select item).FirstOrDefault();

            if (plan == null)
            {
                throw new HandleException("ไม่พบข้อมูลเเม่พิมพ์ กรุณาลองใหม่อีกครั้ง");
            }
            if (plan.Status != MoldPlanStatus.Resin)
            {
                throw new HandleException("ไม่สามารถทำรายการได้[invalid status] กรุณาลองใหม่อีกครั้ง");
            }


            #endregion
            #region *** create ***
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                plan.UpdateBy = _admin;
                plan.UpdateDate = DateTime.UtcNow;
                plan.Status = MoldPlanStatus.CastingSilver;
                plan.NextStatus = MoldPlanStatus.Casting;

                _jewelryContext.TbtProductMoldPlan.Update(plan);
                await _jewelryContext.SaveChangesAsync();

                var casting = new TbtProductMoldPlanCastingSilver()
                {
                    CodePlan = request.MoldCode,
                    PlanId = plan.Id,

                    QtySend = request.QtySend,
                    QtyReceive = request.QtyReceive,

                    CastBy = request.CastBy,
                    Remark = request.Remark,

                    CreateBy = _admin,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = _admin,
                    UpdateDate = DateTime.UtcNow,
                };

                if (request.Images.Any())
                {
                    int count = 1;
                    //array to store stirng name image url
                    List<string> imagesUrl = new List<string>();
                    foreach (var item in request.Images)
                    {
                        try
                        {
                            string imageName = $"{request.MoldCode.ToUpper().Trim()}-{count}-CastingSilver.png";
                            string imagePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/MoldPlanCastingSilver");
                            if (!Directory.Exists(imagePath))
                            {
                                Directory.CreateDirectory(imagePath);
                            }
                            string imagePathWithFileName = Path.Combine(imagePath, imageName);

                            //https://www.thecodebuzz.com/how-to-save-iformfile-to-disk/
                            using (Stream fileStream = new FileStream(imagePathWithFileName, FileMode.Create, FileAccess.Write))
                            {
                                item.CopyTo(fileStream);
                                fileStream.Close();
                            }

                            //add image url to array
                            imagesUrl.Add(imageName);

                            count++;
                        }
                        catch (Exception ex)
                        {
                            throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                        }

                    }
                    if (imagesUrl != null)
                    {
                        //split image url by comma
                        casting.ImageUrl = string.Join(",", imagesUrl);
                    }
                }

                _jewelryContext.TbtProductMoldPlanCastingSilver.Add(casting);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            #endregion

            return "success";
        }
        public async Task<string> PlanCasting(PlanCastingRequest request)
        {
            #region *** validate ***
            var codeplan = (from item in _jewelryContext.TbtProductMoldPlanCasting
                            where item.CodePlan == request.MoldCode
                            && item.PlanId == request.PlanId
                            select item).FirstOrDefault();
            if (codeplan != null)
            {
                throw new HandleException("ไม่สามารถสร้างรหัสเเม่พิมพ์ซ้ำได้");
            }

            if (request.Images.Any() && request.Images.Count > 1)
            {
                throw new HandleException("ไม่สามารถบันทึกเเต่งพิมพ์มากกว่า 1 รูป");
            }

            var plan = (from item in _jewelryContext.TbtProductMoldPlan
                        where item.Id == request.PlanId
                        select item).FirstOrDefault();

            if (plan == null)
            {
                throw new HandleException("ไม่พบข้อมูลเเม่พิมพ์ กรุณาลองใหม่อีกครั้ง");
            }
            if (plan.Status != MoldPlanStatus.CastingSilver)
            {
                throw new HandleException("ไม่สามารถทำรายการได้[invalid status] กรุณาลองใหม่อีกครั้ง");
            }


            #endregion
            #region *** create ***
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                plan.UpdateBy = _admin;
                plan.UpdateDate = DateTime.UtcNow;
                plan.Status = MoldPlanStatus.Casting;
                plan.NextStatus = MoldPlanStatus.Cuttig;

                _jewelryContext.TbtProductMoldPlan.Update(plan);
                await _jewelryContext.SaveChangesAsync();

                var casting = new TbtProductMoldPlanCasting()
                {
                    CodePlan = request.MoldCode,
                    PlanId = plan.Id,

                    QtySend = request.QtySend,
                    QtyReceive = request.QtyReceive,

                    CastBy = request.CastBy,
                    Remark = request.Remark,

                    CreateBy = _admin,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = _admin,
                    UpdateDate = DateTime.UtcNow,
                };

                if (request.Images.Any())
                {
                    int count = 1;
                    //array to store stirng name image url
                    List<string> imagesUrl = new List<string>();
                    foreach (var item in request.Images)
                    {
                        try
                        {
                            string imageName = $"{request.MoldCode.ToUpper().Trim()}-{count}-Casting.png";
                            string imagePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/MoldPlanCasting");
                            if (!Directory.Exists(imagePath))
                            {
                                Directory.CreateDirectory(imagePath);
                            }
                            string imagePathWithFileName = Path.Combine(imagePath, imageName);

                            //https://www.thecodebuzz.com/how-to-save-iformfile-to-disk/
                            using (Stream fileStream = new FileStream(imagePathWithFileName, FileMode.Create, FileAccess.Write))
                            {
                                item.CopyTo(fileStream);
                                fileStream.Close();
                            }

                            //add image url to array
                            imagesUrl.Add(imageName);

                            count++;
                        }
                        catch (Exception ex)
                        {
                            throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                        }

                    }
                    if (imagesUrl != null)
                    {
                        //split image url by comma
                        casting.ImageUrl = string.Join(",", imagesUrl);
                    }
                }

                _jewelryContext.TbtProductMoldPlanCasting.Add(casting);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            #endregion

            return "success";
        }
        public async Task<string> PlanCutting(PlanCuttingRequest request)
        {
            #region *** validate ***
            var codeplan = (from item in _jewelryContext.TbtProductMoldPlanCutting
                            where item.Code == request.Code.ToUpper()
                            //&& item.PlanId == request.PlanId
                            //&& item.Code == request.Code
                            select item).FirstOrDefault();

            if (codeplan != null)
            {
                throw new HandleException("ไม่สามารถสร้างรหัสเเม่พิมพ์ซ้ำได้");
            }

            //if (codeplan.PlanId != request.PlanId || codeplan.CodePlan != request.MoldCode)
            //{
            //    throw new HandleException("ไม่สามารถทำรายการได้[invalid code/id] กรุณาลองใหม่อีกครั้ง");
            //}

            if (request.Images.Any() && request.Images.Count > 1)
            {
                throw new HandleException("ไม่สามารถบันทึกผ่าพิมพ์ยางมากกว่า 1 รูป");
            }

            var plan = (from item in _jewelryContext.TbtProductMoldPlan
                        where item.Id == request.PlanId
                        select item).FirstOrDefault();

            if (plan == null)
            {
                throw new HandleException("ไม่พบข้อมูลเเม่พิมพ์ กรุณาลองใหม่อีกครั้ง");
            }
            if (plan.Status != MoldPlanStatus.Casting)
            {
                throw new HandleException("ไม่สามารถทำรายการได้[invalid status] กรุณาลองใหม่อีกครั้ง");
            }

            var mold = (from item in _jewelryContext.TbtProductMold
                        where item.Code == request.Code.ToUpper()
                        select item).FirstOrDefault();

            if (mold != null)
            {
                throw new HandleException($"มีข้อมูลเเม่พิมพ์สำเร็จรหัส {request.Code.ToUpper()} อยู่เเล้ว ไม่สามารถสร้างรายการซ้ำได้");
            }


            #endregion
            #region *** create ***
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                plan.UpdateBy = _admin;
                plan.UpdateDate = DateTime.UtcNow;
                plan.Status = MoldPlanStatus.Cuttig;
                plan.NextStatus = MoldPlanStatus.Store;

                _jewelryContext.TbtProductMoldPlan.Update(plan);
                await _jewelryContext.SaveChangesAsync();

                var cutting = new TbtProductMoldPlanCutting()
                {
                    CodePlan = request.MoldCode,
                    PlanId = plan.Id,
                    Code = request.Code.ToUpper(),

                    QtySend = request.QtySend,
                    QtyReceive = request.QtyReceive,

                    CuttingBy = request.CuttingBy,
                    Remark = request.Remark,

                    CreateBy = _admin,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = _admin,
                    UpdateDate = DateTime.UtcNow,
                };

                if (request.Images.Any())
                {
                    int count = 1;
                    //array to store stirng name image url
                    List<string> imagesUrl = new List<string>();
                    foreach (var item in request.Images)
                    {
                        try
                        {
                            string imageName = $"{request.MoldCode.ToUpper().Trim()}-{count}-Cutting.png";
                            string imagePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/MoldPlanCutting");
                            if (!Directory.Exists(imagePath))
                            {
                                Directory.CreateDirectory(imagePath);
                            }
                            string imagePathWithFileName = Path.Combine(imagePath, imageName);

                            //https://www.thecodebuzz.com/how-to-save-iformfile-to-disk/
                            using (Stream fileStream = new FileStream(imagePathWithFileName, FileMode.Create, FileAccess.Write))
                            {
                                item.CopyTo(fileStream);
                                fileStream.Close();
                            }

                            //add image url to array
                            imagesUrl.Add(imageName);

                            count++;
                        }
                        catch (Exception ex)
                        {
                            throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                        }

                    }
                    if (imagesUrl != null)
                    {
                        //split image url by comma
                        cutting.ImageUrl = string.Join(",", imagesUrl);
                    }
                }

                _jewelryContext.TbtProductMoldPlanCutting.Add(cutting);
                await _jewelryContext.SaveChangesAsync();

                var addMold = new TbtProductMold()
                {
                    Code = request.Code.Trim().ToUpper(),
                    Category = string.Empty,
                    CategoryCode = string.Empty,
                    Description = string.Empty,

                    MoldBy = string.Empty,

                    CreateDate = DateTime.UtcNow,
                    CreateBy = _admin,

                    IsActive = false,
                    Image = string.Empty,
                };
                _jewelryContext.TbtProductMold.Add(addMold);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            #endregion

            return "success";
        }
        public async Task<string> PlanStore(PlanStoreRequest request)
        {
            #region *** validate ***
            var codeplan = (from item in _jewelryContext.TbtProductMoldPlanStore
                            where item.CodePlan == request.MoldCode
                            && item.PlanId == request.PlanId
                            && item.Code == request.Code
                            select item).FirstOrDefault();
            if (codeplan != null)
            {
                throw new HandleException("ไม่สามารถสร้างรหัสเเม่พิมพ์ซ้ำได้");
            }

            if (request.Images.Any() && request.Images.Count > 1)
            {
                throw new HandleException("ไม่สามารถบันทึกเเม่พิมพ์สำเร็จมากกว่า 1 รูป");
            }

            var plan = (from item in _jewelryContext.TbtProductMoldPlan
                        .Include(x => x.TbtProductMoldPlanDesign)
                        .ThenInclude(x => x.CategoryCodeNavigation)
                        where item.Id == request.PlanId
                        select item).FirstOrDefault();

            if (plan == null)
            {
                throw new HandleException("ไม่พบข้อมูลเเม่พิมพ์ กรุณาลองใหม่อีกครั้ง");
            }
            if (plan.Status != MoldPlanStatus.Cuttig)
            {
                throw new HandleException("ไม่สามารถทำรายการได้[invalid status] กรุณาลองใหม่อีกครั้ง");
            }

            var mold = (from item in _jewelryContext.TbtProductMold
                        where item.Code == request.Code.ToUpper()
                        && item.IsActive == false
                        select item).FirstOrDefault();

            if (mold == null)
            {
                throw new HandleException($"ไม่พบข้อมูลเเม่พิมพ์[สำเร็จ] กรุณาลองใหม่อีกครั้ง");
            }


            #endregion
            #region *** create ***
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                plan.UpdateBy = _admin;
                plan.UpdateDate = DateTime.UtcNow;
                plan.Status = MoldPlanStatus.Store;
                plan.NextStatus = MoldPlanStatus.Success;

                _jewelryContext.TbtProductMoldPlan.Update(plan);
                await _jewelryContext.SaveChangesAsync();

                var store = new TbtProductMoldPlanStore()
                {
                    CodePlan = request.MoldCode,
                    PlanId = plan.Id,
                    Code = request.Code,

                    QtySend = request.QtySend,
                    QtyReceive = request.QtyReceive,

                    WorkerBy = request.WorkerBy,
                    Remark = request.Remark,

                    Location = request.Location,

                    CreateBy = _admin,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = _admin,
                    UpdateDate = DateTime.UtcNow,
                };

                if (request.Images.Any())
                {
                    var img = request.Images[0];
                    try
                    {
                        string imageName = $"{request.Code.ToUpper().Trim()}-Mold.png";
                        string imagePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/Mold");
                        if (!Directory.Exists(imagePath))
                        {
                            Directory.CreateDirectory(imagePath);
                        }
                        string imagePathWithFileName = Path.Combine(imagePath, imageName);

                        //https://www.thecodebuzz.com/how-to-save-iformfile-to-disk/
                        using (Stream fileStream = new FileStream(imagePathWithFileName, FileMode.Create, FileAccess.Write))
                        {
                            img.CopyTo(fileStream);
                            fileStream.Close();
                        }

                        store.ImageUrl = imageName;
                    }
                    catch (Exception ex)
                    {
                        throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                    }

                }

                _jewelryContext.TbtProductMoldPlanStore.Add(store);
                await _jewelryContext.SaveChangesAsync();


                mold.Category = plan.TbtProductMoldPlanDesign.First().CategoryCodeNavigation.NameTh;
                mold.CategoryCode = plan.TbtProductMoldPlanDesign.First().CategoryCodeNavigation.Code;
                mold.Description = request.Remark;
                mold.MoldBy = request.WorkerBy;
                mold.UpdateDate = DateTime.UtcNow;
                mold.UpdateBy = _admin;
                mold.IsActive = true;
                mold.Image = store.ImageUrl ?? string.Empty;
                mold.PlanId = plan.Id;

                _jewelryContext.TbtProductMold.Update(mold);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            #endregion

            return "success";
        }

    }
}
