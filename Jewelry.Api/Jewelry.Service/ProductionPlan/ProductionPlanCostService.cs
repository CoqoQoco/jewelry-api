using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlanCost.GoldCostCreate;
using jewelry.Model.ProductionPlanCost.GoldCostList;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.ProductionPlan
{
    public interface IProductionPlanCostService
    {
        IQueryable<GoldCostListResponse> ListGoldCost(GoldCostList request);
        Task<string> CreateGoldCost(GoldCostCreateRequest request);
    }
    public class ProductionPlanCostService : IProductionPlanCostService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public ProductionPlanCostService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public IQueryable<GoldCostListResponse> ListGoldCost(GoldCostList request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlanCostGold
                         .Include(x => x.GoldNavigation)
                         .Include(x => x.GoldSizeNavigation)
                         where item.IsActive == true
                         select item);

            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query
                         where item.No.Contains(request.Text.ToUpper())
                          || item.BookNo.Contains(request.Text)
                         || item.AssignBy.Contains(request.Text)
                         || item.ReceiveBy.Contains(request.Text)
                         || item.Remark.Contains(request.Text)
                         select item);
            }

            var response = (from item in query
                            select new GoldCostListResponse()
                            {
                                No = item.No,
                                BookNo = item.BookNo,
                                AssignDate = item.AssignDate,

                                GoldCode = item.GoldNavigation.Code,
                                GoldName = item.GoldNavigation.NameTh,
                                GoldSizeCode = item.GoldSizeNavigation.Code,
                                GoldSizeName = item.GoldSizeNavigation.NameTh,
                                GoldReceipt = item.GoldReceipt,

                                MeltDate = item.MeltDate,
                                MeltWeight = item.MeltWeight,
                                ReturnMeltWeight = item.ReturnMeltWeight,
                                ReturnMeltScrapWeight = item.ReturnMeltScrapWeight,
                                MeltWeightLoss = item.MeltWeightLoss,
                                MeltWeightOver = item.MeltWeightOver,

                                CastDate = item.CastDate,
                                CastWeight = item.CastWeight,
                                GemWeight = item.GemWeight,
                                ReturnCastWeight = item.ReturnCastWeight,
                                ReturnCastBodyWeight = item.ReturnCastBodyWeight,
                                ReturnCastScrapWeight = item.ReturnCastScrapWeight,
                                ReturnCastPowderWeight = item.ReturnCastPowderWeight,
                                CastWeightLoss = item.CastWeightLoss,
                                CastWeightOver = item.CastWeightOver,

                                AssignBy = item.AssignBy,
                                ReceiveBy = item.ReceiveBy,
                                Remark = item.Remark,
                            });

            return response;
        }
        public async Task<string> CreateGoldCost(GoldCostCreateRequest request)
        {

            var dub = (from item in _jewelryContext.TbtProductionPlanCostGold
                       where item.No == request.No.ToUpper() && item.BookNo == request.BookNo.ToUpper()
                       select item).SingleOrDefault();

            if (dub != null)
            {
                throw new HandleException($"ใบเบิกผสมทอง เลขที่:{request.No} เล่มที่:{request.BookNo} ทำซ้ำ กรุณาสร้างหมายเลขใหม่");
            }

            var create = new TbtProductionPlanCostGold()
            {
                No = request.No.ToUpper(),
                BookNo = request.BookNo.ToUpper(),
                AssignDate = request.AssignDateFormat.UtcDateTime,

                Gold = request.GoldCode,
                GoldSize = request.GoldSizeCode,
                GoldReceipt = request.GoldReceipt,

                Remark = request.Remark,
                AssignBy = request.AssignBy,
                ReceiveBy = request.ReceiveBy,

                MeltDate = request.MeltDateFormat.HasValue ? request.MeltDateFormat.Value.UtcDateTime : null,
                MeltWeight = request.MeltWeight,
                ReturnMeltWeight = request.ReturnMeltWeight,
                ReturnMeltScrapWeight = request.ReturnMeltScrapWeight,
                MeltWeightLoss = request.MeltWeightLoss,
                MeltWeightOver = request.MeltWeightOver,

                CastDate = request.CastDateFormat.HasValue ? request.CastDateFormat.Value.UtcDateTime : null,
                CastWeight = request.CastWeight,
                GemWeight = request.GemWeight,
                ReturnCastWeight = request.ReturnCastWeight,
                ReturnCastBodyWeight = request.ReturnCastBodyWeight,
                ReturnCastScrapWeight = request.ReturnCastScrapWeight,
                ReturnCastPowderWeight = request.ReturnCastPowderWeight,
                CastWeightLoss = request.CastWeightLoss,
                CastWeightOver = request.CastWeightOver,

                CreateDate = DateTime.UtcNow,
                CreateBy = _admin,

                IsActive = true,
            };

            _jewelryContext.TbtProductionPlanCostGold.Add(create);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }
    }
}
