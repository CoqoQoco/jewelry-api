using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.Mold.PlanDesign;
using jewelry.Model.Mold.PlanGet;
using jewelry.Model.Mold.PlanList;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
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

        Task<string> PlanDesign(PlanDesignRequest request);
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
                         where plan.IsActive == true
                         select new PlanListReponse()
                         {
                             Id = plan.Id,
                             MoldCode = plan.TbtProductMoldPlanDesign.First().CodePlan,

                             CreateDate = plan.CreateDate,
                             UpdateDate = plan.UpdateDate ?? default,

                             Status = plan.Status,
                             StatusName = plan.StatusNavigation.NameTh,

                             Image = plan.TbtProductMoldPlanDesign.First().ImageUrl ?? string.Empty
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
                            .Include(x => x.StatusNavigation)
                            .Include(x => x.TbtProductMoldPlanDesign)
                            where plan.Id == id
                            select new PlanGetResponse()
                            {
                                Id = plan.Id,
                                CreateDate = plan.CreateDate,
                                UpdateDate = plan.UpdateDate ?? default,

                                Status = plan.Status,
                                StatusName = plan.StatusNavigation.NameTh,

                                Design = new PlanGetGesign()
                                {
                                    MoldCode = plan.TbtProductMoldPlanDesign.First().CodePlan,
                                    Remark = plan.TbtProductMoldPlanDesign.First().Remark,
                                    SizeGem = plan.TbtProductMoldPlanDesign.First().SizeGem,
                                    SizeDiamond = plan.TbtProductMoldPlanDesign.First().SizeDiamond,
                                    QtyGem = plan.TbtProductMoldPlanDesign.First().QtyGem,
                                    QtyDiamond = plan.TbtProductMoldPlanDesign.First().QtyDiamond,
                                    QtyBeforeCasting = plan.TbtProductMoldPlanDesign.First().QtyBeforeCasting,
                                    QtyBeforeSend = plan.TbtProductMoldPlanDesign.First().QtyBeforeSend,
                                    Image = plan.TbtProductMoldPlanDesign.First().ImageUrl ?? string.Empty
                                }


                            }).FirstOrDefault();

            if (planMold == null)
            {
                throw new HandleException("ไม่พบข้อมูล กรุณาลองใหม่อีกครั้ง");
            }

            return planMold;
        }

        public async Task<string> PlanDesign(PlanDesignRequest request)
        {
            #region *** validate ***
            var codeplan = (from item in _jewelryContext.TbtProductMoldPlanDesign
                            where item.CodePlan == request.MoldCode
                            select item).FirstOrDefault();
            if (codeplan != null)
            {
                throw new HandleException("ไม่สามารถสร้างรหัสเเม่พิมพ์ซ้ำได้");
            }

            if (request.Images.Any() && request.Images.Count > 3)
            {
                throw new HandleException("ไม่สามารถบันทึกรูปต้นเเเบบเเม่พิพม์มากกว่า 3 รูป");
            }
            #endregion
            #region *** create ***
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {

                var plan = new TbtProductMoldPlan()
                {
                    CreateBy = _admin,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = _admin,
                    UpdateDate = DateTime.UtcNow,

                    IsActive = true,
                    Status = MoldPlanStatus.Designed
                };

                _jewelryContext.TbtProductMoldPlan.Add(plan);
                await _jewelryContext.SaveChangesAsync();

                var design = new TbtProductMoldPlanDesign()
                {
                    CodePlan = request.MoldCode,
                    Remark = request.Remark,
                    SizeGem = request.SizeGem,
                    SizeDiamond = request.SizeDiamond,
                    QtyGem = request.QtyGem,
                    QtyDiamond = request.QtyDiamond,
                    QtyBeforeSend = request.QtyBeforeSend,
                    QtyBeforeCasting = request.QtyBeforeCasting,
                    CreateBy = _admin,
                    CreateDate = DateTime.UtcNow,
                    UpdateBy = _admin,
                    UpdateDate = DateTime.UtcNow,
                    PlanId = plan.Id
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
                            string imageName = $"{request.MoldCode.ToUpper().Trim()}-{count}-MoldPlan.png";
                            string imagePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Images/MoldPlan");
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

                            design.ImageUrl = imageName;

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

                scope.Complete();
            }

            #endregion

            return "success";
        }

    }
}
