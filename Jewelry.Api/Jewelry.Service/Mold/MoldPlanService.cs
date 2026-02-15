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
using jewelry.Model.Mold.PlanMelting;
using jewelry.Model.Mold.PlanRemodel;
using jewelry.Model.Mold.PlanResin;
using jewelry.Model.Mold.PlanStore;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
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
        Task<string> PlanMelting(PlanMeltingRequest request);

        Task<string> PlanGems(PlanGemsRequest request);
        Task<string> PlanDesign(PlanDesignRequest request);
        Task<string> PlanResin(PlanResinRequest request);
        Task<string> PlanCastingSilver(PlanCastingSilverRequest request);
        Task<string> PlanCasting(PlanCastingRequest request);
        Task<string> PlanCutting(PlanCuttingRequest request);
        Task<string> PlanStore(PlanStoreRequest request);
        Task<string> PlanRemodel(PlanRemodelRequest request);

        Task<string> NewPlanDesign(PlanDesignRequest request);
        Task<string> NewPlanStore(PlanStoreRequest request);
    }
    public class MoldPlanService : BaseService, IMoldPlanService
    {
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        private readonly IAzureBlobStorageService _azureBlobService;

        public MoldPlanService(JewelryContext jewelryContext,
            IHostEnvironment hostingEnvironment,
            IRunningNumber runningNumberService,
            IAzureBlobStorageService azureBlobService,
            IHttpContextAccessor httpContextAccessor) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _hostingEnvironment = hostingEnvironment;
            _runningNumberService = runningNumberService;
            _azureBlobService = azureBlobService;
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
                             MoldCode = plan.TbtProductMoldPlanDesign.CodePlan,
                             Code = plan.TbtProductMoldPlanCutting.Any() ? plan.TbtProductMoldPlanCutting.First().Code : string.Empty,

                             CreateDate = plan.CreateDate,
                             UpdateDate = plan.UpdateDate ?? default,

                             Status = plan.Status,
                             StatusName = plan.StatusNavigation.NameTh,
                             NextStatus = plan.NextStatus,
                             NextStatusName = plan.NextStatusNavigation.NameTh,

                             IsNewProcess = plan.IsNewProcess,

                             CatagoryName = plan.TbtProductMoldPlanDesign.CategoryCodeNavigation.NameTh,
                             DesignBy = plan.TbtProductMoldPlanDesign.DesignBy,

                             ImgDesign = plan.TbtProductMoldPlanDesign.ImageUrl ?? string.Empty,
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
                query = query.Where(x => x.CreateDate >= request.CreateStart.Value.StartOfDayUtc());
            }
            if (request.CreateEnd.HasValue)
            {
                query = query.Where(x => x.CreateDate <= request.CreateEnd.Value.EndOfDayUtc());
            }

            if (request.UpdateStart.HasValue)
            {
                query = query.Where(x => x.UpdateDate >= request.UpdateStart.Value.StartOfDayUtc());
            }
            if (request.UpdateEnd.HasValue)
            {
                query = query.Where(x => x.UpdateDate <= request.UpdateEnd.Value.EndOfDayUtc());
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

                                IsNewProcess = plan.IsNewProcess,

                                Design = new PlanGetItemStatus()
                                {
                                    MoldCode = plan.TbtProductMoldPlanDesign.CodePlan,
                                    Remark = plan.TbtProductMoldPlanDesign.Remark,
                                    //SizeGem = plan.TbtProductMoldPlanDesign.First().SizeGem,
                                    //SizeDiamond = plan.TbtProductMoldPlanDesign.First().SizeDiamond,
                                    //QtyGem = plan.TbtProductMoldPlanDesign.First().QtyGem,
                                    //QtyDiamond = plan.TbtProductMoldPlanDesign.First().QtyDiamond,
                                    QtyReceive = plan.TbtProductMoldPlanDesign.QtyReceive,
                                    QtySend = plan.TbtProductMoldPlanDesign.QtySend,
                                    Image = plan.TbtProductMoldPlanDesign.ImageUrl ?? string.Empty,

                                    CreateBy = plan.TbtProductMoldPlanDesign.CreateBy,
                                    CreateDate = plan.TbtProductMoldPlanDesign.CreateDate,
                                    UpdateBy = plan.TbtProductMoldPlanDesign.UpdateBy,
                                    UpdateDate = plan.TbtProductMoldPlanDesign.UpdateDate ?? default,

                                    Catagory = plan.TbtProductMoldPlanDesign.CategoryCode,
                                    CatagoryName = plan.TbtProductMoldPlanDesign.CategoryCodeNavigation.NameTh,

                                    WorkBy = plan.TbtProductMoldPlanDesign.DesignBy,
                                    ResinBy = plan.TbtProductMoldPlanDesign.ResinBy
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

                                    QtyResin = plan.TbtProductMoldPlanStore.First().QtyResin,
                                    QtySilverCast = plan.TbtProductMoldPlanStore.First().QtySilvercast,

                                    PrintBy = plan.TbtProductMoldPlanStore.First().PrintBy,
                                    CuttingBy = plan.TbtProductMoldPlanStore.First().CuttingBy,
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
                        CreateBy = CurrentUsername,
                        CreateDate = DateTime.UtcNow,
                        UpdateBy = CurrentUsername,
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
        public async Task<string> PlanMelting(PlanMeltingRequest request)
        {
            var plan = (from item in _jewelryContext.TbtProductMoldPlan
                        where item.Id == request.Id
                        select item).FirstOrDefault();

            if (plan == null)
            {
                throw new HandleException("ไม่พบเเม่พิมพ์");
            }
            if (plan.Status == MoldPlanStatus.Success || plan.Status == MoldPlanStatus.Melting)
            {
                throw new HandleException("เเม่พิมพ์ถูกหลอมหรืออยู่ในการจัดเก็บเเล้ว ไม่สามารถดำเนินการได้");
            }

            plan.UpdateBy = CurrentUsername;
            plan.UpdateDate = DateTime.UtcNow;
            plan.Status = MoldPlanStatus.Melting;

            _jewelryContext.TbtProductMoldPlan.Update(plan);
            await _jewelryContext.SaveChangesAsync();

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
                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = CurrentUsername,
                    UpdateDate = DateTime.UtcNow,

                    Running = running,
                    IsNewProcess = false,

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
                    QtySend = request.QtySend.Value,
                    QtyReceive = request.QtyReceive.Value,
                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = CurrentUsername,
                    UpdateDate = DateTime.UtcNow,
                    PlanId = plan.Id,

                    IsNewProcess = false,

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
                            string fileName = $"{request.MoldCode.ToUpper().Trim()}-{count}-Design.png";

                            // Upload to Azure Blob Storage (Single Container Architecture)
                            using var stream = item.OpenReadStream();
                            var result = await _azureBlobService.UploadImageAsync(
                                stream,
                                "MoldPlanDesign",  // folder name in jewelry-images container
                                fileName
                            );

                            if (!result.Success)
                            {
                                throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                            }

                            //add blob name to array - "MoldPlanDesign/filename.png"
                            imagesUrl.Add(result.BlobName);

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
                            CreateBy = CurrentUsername,
                            CreateDate = DateTime.UtcNow,
                            UpdateBy = CurrentUsername,
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
                plan.UpdateBy = CurrentUsername;
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

                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = CurrentUsername,
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
                            string fileName = $"{request.MoldCode.ToUpper().Trim()}-{count}-Resin.png";

                            // Upload to Azure Blob Storage (Single Container Architecture)
                            using var stream = item.OpenReadStream();
                            var result = await _azureBlobService.UploadImageAsync(
                                stream,
                                "MoldPlanResin",  // folder name in jewelry-images container
                                fileName
                            );

                            if (!result.Success)
                            {
                                throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                            }

                            //add blob name to array - "MoldPlanResin/filename.png"
                            imagesUrl.Add(result.BlobName);

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
                plan.UpdateBy = CurrentUsername;
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

                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = CurrentUsername,
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
                            string fileName = $"{request.MoldCode.ToUpper().Trim()}-{count}-CastingSilver.png";

                            // Upload to Azure Blob Storage (Single Container Architecture)
                            using var stream = item.OpenReadStream();
                            var result = await _azureBlobService.UploadImageAsync(
                                stream,
                                "MoldPlanCastingSilver",  // folder name in jewelry-images container
                                fileName
                            );

                            if (!result.Success)
                            {
                                throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                            }

                            //add blob name to array - "MoldPlanCastingSilver/filename.png"
                            imagesUrl.Add(result.BlobName);

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
                plan.UpdateBy = CurrentUsername;
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

                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = CurrentUsername,
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
                            string fileName = $"{request.MoldCode.ToUpper().Trim()}-{count}-Casting.png";

                            // Upload to Azure Blob Storage (Single Container Architecture)
                            using var stream = item.OpenReadStream();
                            var result = await _azureBlobService.UploadImageAsync(
                                stream,
                                "MoldPlanCasting",  // folder name in jewelry-images container
                                fileName
                            );

                            if (!result.Success)
                            {
                                throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                            }

                            //add blob name to array - "MoldPlanCasting/filename.png"
                            imagesUrl.Add(result.BlobName);

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
                plan.UpdateBy = CurrentUsername;
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

                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = CurrentUsername,
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
                            string fileName = $"{request.MoldCode.ToUpper().Trim()}-{count}-Cutting.png";

                            // Upload to Azure Blob Storage (Single Container Architecture)
                            using var stream = item.OpenReadStream();
                            var result = await _azureBlobService.UploadImageAsync(
                                stream,
                                "MoldPlanCutting",  // folder name in jewelry-images container
                                fileName
                            );

                            if (!result.Success)
                            {
                                throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                            }

                            //add blob name to array - "MoldPlanCutting/filename.png"
                            imagesUrl.Add(result.BlobName);

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
                    CreateBy = CurrentUsername,

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
                plan.UpdateBy = CurrentUsername;
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

                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = CurrentUsername,
                    UpdateDate = DateTime.UtcNow,
                };

                if (request.Images.Any())
                {
                    var img = request.Images[0];
                    try
                    {
                        string fileName = $"{request.Code.ToUpper().Trim()}-Mold.png";

                        // Upload to Azure Blob Storage (Single Container Architecture)
                        using var stream = img.OpenReadStream();
                        var result = await _azureBlobService.UploadImageAsync(
                            stream,
                            "Mold",  // folder name in jewelry-images container
                            fileName
                        );

                        if (!result.Success)
                        {
                            throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                        }

                        store.ImageUrl = result.BlobName;  // "Mold/filename.png"
                    }
                    catch (Exception ex)
                    {
                        throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                    }

                }

                _jewelryContext.TbtProductMoldPlanStore.Add(store);
                await _jewelryContext.SaveChangesAsync();


                mold.Category = plan.TbtProductMoldPlanDesign.CategoryCodeNavigation.NameTh;
                mold.CategoryCode = plan.TbtProductMoldPlanDesign.CategoryCodeNavigation.Code;
                mold.Description = request.Remark;
                mold.MoldBy = request.WorkerBy;
                mold.UpdateDate = DateTime.UtcNow;
                mold.UpdateBy = CurrentUsername;
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

        public async Task<string> PlanRemodel(PlanRemodelRequest request)
        {
            var mold = (from item in _jewelryContext.TbtProductMold
                        where item.Code == request.Code.ToUpper()
                        select item).FirstOrDefault();

            if (mold != null)
            {
                throw new HandleException($"มีข้อมูลเเม่พิมพ์สำเร็จรหัส {request.Code.ToUpper()} อยู่เเล้ว ไม่สามารถสร้างรายการซ้ำได้");
            }

            if (!request.Images.Any())
            {
                throw new HandleException("โปรดระบุรูปแแม่พิมพ์");
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var addMold = new TbtProductMold()
                {
                    Code = request.Code.Trim().ToUpper(),
                    Category = request.Catagory,
                    CategoryCode = request.CatagoryCode,
                    Description = request.Remark,

                    MoldBy = request.WorkBy,
                    ReModelMold = request.Remodel,
                    IsActive = true,

                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                };

                if (request.Images.Any())
                {
                    var img = request.Images[0];
                    try
                    {
                        string fileName = $"{request.Code.ToUpper().Trim()}-Mold.png";

                        // Upload to Azure Blob Storage (Single Container Architecture)
                        using var stream = img.OpenReadStream();
                        var result = await _azureBlobService.UploadImageAsync(
                            stream,
                            "Mold",  // folder name in jewelry-images container
                            fileName
                        );

                        if (!result.Success)
                        {
                            throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                        }

                        addMold.Image = result.BlobName;  // "Mold/filename.png"
                    }
                    catch (Exception ex)
                    {
                        throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                    }

                }

                _jewelryContext.TbtProductMold.Add(addMold);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            return "success";
        }

        public async Task<string> NewPlanDesign(PlanDesignRequest request)
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
                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = CurrentUsername,
                    UpdateDate = DateTime.UtcNow,

                    Running = running,
                    IsNewProcess = true,

                    IsActive = true,
                    Status = MoldPlanStatus.Designed,
                    NextStatus = MoldPlanStatus.Store

                };

                _jewelryContext.TbtProductMoldPlan.Add(plan);
                await _jewelryContext.SaveChangesAsync();

                var design = new TbtProductMoldPlanDesign()
                {
                    CodePlan = request.MoldCode.ToUpper(),
                    Remark = request.Remark,

                    QtySend = 0,
                    QtyReceive = 0,

                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = CurrentUsername,
                    UpdateDate = DateTime.UtcNow,

                    PlanId = plan.Id,
                    IsNewProcess = true,

                    CategoryCode = request.Catagory,
                    DesignBy = request.DesignBy,
                    ResinBy = request.ResinBy,
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
                            string fileName = $"{request.MoldCode.ToUpper().Trim()}-{count}-Design.png";

                            // Upload to Azure Blob Storage (Single Container Architecture)
                            using var stream = item.OpenReadStream();
                            var result = await _azureBlobService.UploadImageAsync(
                                stream,
                                "MoldPlanDesign",  // folder name in jewelry-images container
                                fileName
                            );

                            if (!result.Success)
                            {
                                throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                            }

                            //add blob name to array - "MoldPlanDesign/filename.png"
                            imagesUrl.Add(result.BlobName);

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
                            CreateBy = CurrentUsername,
                            CreateDate = DateTime.UtcNow,
                            UpdateBy = CurrentUsername,
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
        public async Task<string> NewPlanStore(PlanStoreRequest request)
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


            var designList = (from item in _jewelryContext.TbtProductMoldPlanDesign
                          .Include(x => x.CategoryCodeNavigation)
                                  //where item.PlanId == request.PlanId
                              select item);

            var design = designList.FirstOrDefault(x => x.PlanId == request.PlanId);
            if (design == null)
            {
                throw new HandleException("ไม่พบข้อมูลเเม่พิมพ์ [Design] กรุณาลองใหม่อีกครั้ง");
            }


            if (design.CodePlan != request.MoldCode.ToUpper())
            {
                var dubplicateMold = designList.FirstOrDefault(x => x.CodePlan == request.MoldCode.ToUpper());
                if (dubplicateMold != null)
                {
                    throw new HandleException("ไม่สามารถสร้างรหัสเเม่พิมพ์ซ้ำได้");
                }
            }


            if (request.Images.Any() && request.Images.Count > 1)
            {
                throw new HandleException("ไม่สามารถบันทึกเเม่พิมพ์สำเร็จมากกว่า 1 รูป");
            }

            var plan = (from item in _jewelryContext.TbtProductMoldPlan
                            //.Include(x => x.TbtProductMoldPlanDesign)
                            //.ThenInclude(x => x.CategoryCodeNavigation)
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

            var mold = (from item in _jewelryContext.TbtProductMold
                        where item.Code == request.Code.ToUpper()
                        && item.IsActive == false
                        select item).FirstOrDefault();

            if (mold != null)
            {
                throw new HandleException($"มีข้อมูลเเม่พิมพ์สำเร็จรหัส {request.Code.ToUpper()} อยู่เเล้ว ไม่สามารถสร้างรายการซ้ำได้");
            }



            #endregion
            #region *** create ***
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {

                plan.UpdateBy = CurrentUsername;
                plan.UpdateDate = DateTime.UtcNow;
                plan.Status = MoldPlanStatus.Store;
                plan.NextStatus = MoldPlanStatus.Success;

                _jewelryContext.TbtProductMoldPlan.Update(plan);
                await _jewelryContext.SaveChangesAsync();

                design.CodePlan = request.MoldCode.ToUpper();
                design.UpdateBy = CurrentUsername;
                design.UpdateDate = DateTime.UtcNow;

                _jewelryContext.TbtProductMoldPlanDesign.Update(design);
                await _jewelryContext.SaveChangesAsync();


                var store = new TbtProductMoldPlanStore()
                {
                    CodePlan = request.MoldCode,
                    PlanId = plan.Id,
                    Code = request.Code,

                    QtySend = 0,
                    QtyReceive = 0,

                    WorkerBy = request.WorkerBy,
                    Remark = request.Remark,

                    Location = request.Location,

                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow,

                    QtyResin = request.QtyResin,
                    QtySilvercast = request.QtySilverCast,

                    PrintBy = request.PrintBy,
                    CuttingBy = request.CuttingBy,

                    //UpdateBy = CurrentUsername,
                    //UpdateDate = DateTime.UtcNow,
                };

                if (request.Images.Any())
                {
                    var img = request.Images[0];
                    try
                    {
                        string fileName = $"{request.Code.ToUpper().Trim()}-Mold.png";

                        // Upload to Azure Blob Storage (Single Container Architecture)
                        using var stream = img.OpenReadStream();
                        var result = await _azureBlobService.UploadImageAsync(
                            stream,
                            "Mold",  // folder name in jewelry-images container
                            fileName
                        );

                        if (!result.Success)
                        {
                            throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                        }

                        store.ImageUrl = result.BlobName;  // "Mold/filename.png"
                    }
                    catch (Exception ex)
                    {
                        throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                    }

                }

                _jewelryContext.TbtProductMoldPlanStore.Add(store);
                await _jewelryContext.SaveChangesAsync();

                ///create mold
                var addMold = new TbtProductMold()
                {
                    Code = request.Code.Trim().ToUpper(),
                    Category = design.CategoryCodeNavigation.NameTh,
                    CategoryCode = design.CategoryCodeNavigation.Code,
                    Description = request.Remark,

                    MoldBy = request.WorkerBy,

                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername,

                    IsActive = true,
                    Image = store.ImageUrl ?? string.Empty,
                    PlanId = plan.Id
                };
                _jewelryContext.TbtProductMold.Add(addMold);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            #endregion

            return "success";
        }
    }
}
