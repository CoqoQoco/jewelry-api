using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.Mold.PlanList;
using jewelry.Model.ProductionPlan.ProductionPlanPrice.Transection;
using jewelry.Model.Receipt.Production.PlanGet;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NPOI.OpenXmlFormats.Dml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static NPOI.HSSF.Util.HSSFColor;

namespace Jewelry.Service.Production.PlanBOM
{
    public class PlanBOMService : BaseService, IPlanBOMService
    {
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public PlanBOMService(JewelryContext JewelryContext, IHttpContextAccessor httpContextAccessor,
            IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public async Task<jewelry.Model.Production.PlanBOM.NewGet.Response> GetTransactionBOM(int productionPlanId)
        {
            var plan = (from p in _jewelryContext.TbtProductionPlan
                            //.Include(x => x.TbtProductionPlanPrice)
                        where p.Id == productionPlanId
                        select p).FirstOrDefault();

            if (plan == null)
            {
                throw new HandleException($"{ErrorMessage.NotFound} : {productionPlanId}");
            }

            var response = new jewelry.Model.Production.PlanBOM.NewGet.Response()
            {
                ProductionPlanId = plan.Id,
                Wo = plan.Wo,
                WoNumber = plan.WoNumber,
                WoText = plan.WoText,
                BOMs = new List<jewelry.Model.Production.PlanBOM.NewGet.BOM>()
            };

            var goldCost = (from item in _jewelryContext.TbtProductionPlanCostGoldItem
                                  .Include(x => x.TbtProductionPlanCostGold)
                            where item.TbtProductionPlanCostGold.IsActive == true
                            && item.ProductionPlanId == $"{plan.Wo}-{plan.WoNumber}"
                            select item);

            int count = 0;

            if (goldCost.Any())
            {
                var masterGoldSize = (from item in _jewelryContext.TbmGoldSize
                                      select item).ToList();

                var masterGold = (from item in _jewelryContext.TbmGold
                                  select item).ToList();

                foreach (var item in goldCost)
                {
                    var matchGold = masterGold.Where(x => x.Code == item.TbtProductionPlanCostGold.Gold).FirstOrDefault();

                    if (matchGold == null)
                    {
                        continue;
                    }

                    var matchGoldSize = masterGoldSize.Where(x => x.Code == item.TbtProductionPlanCostGold.GoldSize).FirstOrDefault();
                    string goldSize = matchGoldSize != null ? matchGoldSize.NameTh : string.Empty;

                    response.BOMs.Add(new jewelry.Model.Production.PlanBOM.NewGet.BOM()
                    {
                        No = count++,
                        Type = "Gold",

                        OriginCode = $"{matchGold.NameEn} {goldSize}",
                        OriginName = $"{matchGold.NameEn} {goldSize}",

                        MatchCode = matchGold.Code,
                        MatchName = matchGold.NameEn,

                        DisplayName = $"{matchGold.NameTh} {goldSize}",
                        Quantity = item.ReturnQty.HasValue ? item.ReturnQty.Value : 0,
                        Unit = $"g."
                    });
                }
            }


            var gems = (from detail in _jewelryContext.TbtProductionPlanStatusDetailGem
                                  .Include(x => x.Header)
                                  .ThenInclude(x => x.ProductionPlan)
                                  .ThenInclude(x => x.StatusNavigation)
                        join gem in _jewelryContext.TbtStockGem on detail.GemCode equals gem.Code
                        where detail.Header.ProductionPlan.Wo == plan.Wo
                        && detail.Header.ProductionPlan.WoNumber == plan.WoNumber
                        && detail.IsActive == true
                        && detail.Header.IsActive == true
                        select new { detail, gem });

            if (gems.Any())
            {

                var masterGem = (from item in _jewelryContext.TbmGem
                                 select item).ToList();



                foreach (var item in gems)
                {


                    var matchGems = masterGem.FirstOrDefault(x => x.NameEn.Contains(item.gem.GroupName));
                    if (matchGems == null)
                    {
                        matchGems = masterGem.FirstOrDefault(x => item.gem.GroupName.Contains(x.NameEn));
                    }

                    var addGem = new jewelry.Model.Production.PlanBOM.NewGet.BOM()
                    {
                        No = count++,
                        Type = "Gem",

                        OriginCode = item.detail.GemCode,
                        OriginName = item.detail.GemName,

                        MatchCode = null,
                        MatchName = null,

                        DisplayName = $"{matchGems?.NameTh}",

                        Quantity = item.detail.GemWeight.HasValue ? item.detail.GemWeight.Value : 0,
                        Unit = $"cts.",

                        //Price = gemStock.UnitCode == "Q" ? gemStock.PriceQty : gemStock.Price,
                    };

                    if (matchGems != null)
                    {
                        addGem.MatchCode = matchGems.Code;
                        addGem.MatchName = matchGems.NameEn;
                    }

                    response.BOMs.Add(addGem);
                }
            }

            return response;
        }

        public async Task<jewelry.Model.Base.Response> SavePlanBom(jewelry.Model.Production.PlanBOM.Save.Request request)
        {
            try
            {
                // Validate production plan exists
                var plan = await _jewelryContext.TbtProductionPlan
                    .FirstOrDefaultAsync(p => p.Id == request.Id);

                if (plan == null)
                {
                    throw new HandleException($"{ErrorMessage.NotFound} : Production Plan {request.Id}");
                }

                //using transactiob scope
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                // Remove existing BOM entries for this production plan
                var existingBOMs = _jewelryContext.TbtProductionPlanBom
                    .Where(b => b.ProductionId == request.Id);

                _jewelryContext.TbtProductionPlanBom.RemoveRange(existingBOMs);

                // Add new BOM entries
                foreach (var bomItem in request.BOMs)
                {
                    var running = await _runningNumberService.GenerateRunningNumber("BOM");

                    var newBOM = new Jewelry.Data.Models.Jewelry.TbtProductionPlanBom
                    {
                        Running = running,

                        Wo = plan.Wo,
                        WoNumber = plan.WoNumber,
                        WoText = plan.WoText,
                        ProductionId = request.Id,

                        Type = bomItem.Type,

                        OriginCode = bomItem.OriginCode,
                        OriginName = bomItem.OriginName,

                        MatchCode = bomItem.MatchCode ?? string.Empty,
                        MatchName = bomItem.MatchName ?? string.Empty,

                        DisplayName = bomItem.DisplayName,

                        Qty = bomItem.Quantity.HasValue ? bomItem.Quantity.Value : 0,
                        Unit = bomItem.Unit,
                        Price = bomItem.Price.HasValue ? bomItem.Price.Value : 0,


                        CreateDate = DateTime.UtcNow,
                        CreateBy = CurrentUsername
                    };

                    _jewelryContext.TbtProductionPlanBom.Add(newBOM);
                }

                await _jewelryContext.SaveChangesAsync();

                scope.Complete();

                return new jewelry.Model.Base.Response
                {
                    Message = "BOM saved successfully",
                };
            }
            catch (Exception ex)
            {
                throw new HandleException(ex.Message);
            }
        }

        public async Task<List<jewelry.Model.Production.PlanBOM.NewGet.BOM>> GetPlanBom(int productionPlanId)
        {
            try
            {
                // Validate production plan exists
                var plan = await _jewelryContext.TbtProductionPlan
                    .FirstOrDefaultAsync(p => p.Id == productionPlanId);

                if (plan == null)
                {
                    throw new HandleException($"{ErrorMessage.NotFound} : Production Plan {productionPlanId}");
                }

                var bomEntries = await _jewelryContext.TbtProductionPlanBom
                    .Where(b => b.ProductionId == productionPlanId)
                    .OrderBy(b => b.No)
                    .Select(b => new jewelry.Model.Production.PlanBOM.NewGet.BOM
                    {
                        No = b.No,

                        Type = b.Type,
                        OriginCode = b.OriginCode,
                        OriginName = b.OriginName,

                        MatchCode = b.MatchCode,
                        MatchName = b.MatchName,

                        DisplayName = b.DisplayName,

                        Quantity = b.Qty,
                        Unit = b.Unit,
                        Price = b.Price,

                        CreateDate = b.CreateDate,
                        CreateBy = b.CreateBy
                    })
                    .ToListAsync();

                return bomEntries;
            }
            catch (Exception ex)
            {
                throw new HandleException(ex.Message);
            }
        }


        public IQueryable<jewelry.Model.Production.PlanBOM.List.Response> ListBom(jewelry.Model.Production.PlanBOM.List.Criteria request)
        {
            var goldSpecificName = "น้ำหนักทองรวมหลังหักเพชรพลอย";

            var bomQuery = from bom in _jewelryContext.TbtProductionPlanPrice
                            .Include(x => x.Production)
                            .ThenInclude(x => x.TbtProductionPlanStatusHeader)
                            .Include(x => x.Production.CustomerTypeNavigation)
                            .Include(x => x.Production.ProductTypeNavigation)
                           where bom.Production.IsActive == true
                           && bom.Production.Status == ProductionPlanStatus.Completed
                           //&& bom.Production.CompletedDate >= request.Start.StartOfDayUtc()
                           //&& bom.Production.CompletedDate <= request.End.EndOfDayUtc()
                           //&& ((bom.NameGroup == "Gold" && bom.Name == goldSpecificName) || (bom.NameGroup == "Gem"))
                           && (bom.NameGroup == "Gold" || bom.NameGroup == "Gem")
                           select bom;

            //filte by date range
            if (request.Start.HasValue)
            {
                bomQuery = bomQuery.Where(x => x.Production.CompletedDate >= request.Start.Value.StartOfDayUtc());
            }
            if (request.End.HasValue)
            {
                bomQuery = bomQuery.Where(x => x.Production.CompletedDate <= request.End.Value.EndOfDayUtc());
            }

            // Apply text filters
            if (!string.IsNullOrEmpty(request.Text))
            {
                bomQuery = bomQuery.Where(x => x.Name.Contains(request.Text)
                    || x.NameDescription.Contains(request.Text)
                    || x.Production.ProductNumber.Contains(request.Text));
            }

            if (!string.IsNullOrEmpty(request.WoText))
            {
                bomQuery = bomQuery.Where(x => x.Production.WoText.Contains(request.WoText));
            }


            // Apply customer filters
            if (request.CustomerType != null && request.CustomerType.Length > 0)
            {
                bomQuery = bomQuery.Where(x => request.CustomerType.Contains(x.Production.CustomerType));
            }

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                bomQuery = bomQuery.Where(x => x.Production.CustomerTypeNavigation.Code.Contains(request.CustomerCode));
            }

            // Apply gold filters
            if (request.Gold != null && request.Gold.Length > 0)
            {
                bomQuery = bomQuery.Where(x => request.Gold.Contains(x.Production.Type));
            }

            if (request.GoldSize != null && request.GoldSize.Length > 0)
            {
                bomQuery = bomQuery.Where(x => request.GoldSize.Contains(x.Production.TypeSize));
            }

            // Apply mold filter
            if (!string.IsNullOrEmpty(request.Mold))
            {
                bomQuery = bomQuery.Where(x => x.Production.Mold.Contains(request.Mold));
            }

            // Apply product filters
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                bomQuery = bomQuery.Where(x => x.Production.ProductNumber.Contains(request.ProductNumber));
            }

            if (request.ProductType != null && request.ProductType.Length > 0)
            {
                bomQuery = bomQuery.Where(x => request.ProductType.Contains(x.Production.ProductType));
            }

            bomQuery = bomQuery.Where(x => (x.NameGroup == "Gold" && x.Name.Contains("ทอง") || x.NameGroup == "Gem"));

            return bomQuery.Select(b => new jewelry.Model.Production.PlanBOM.List.Response
            {
                Name = b.NameGroup == "Gold"  ? (b.Production.Type == "Silver" ? $"{b.Name} ({b.Production.Type})" : $"{b.Name} ({b.Production.Type} {b.Production.TypeSize})")
                                              : b.Name,
                NameDescription = b.NameDescription,
                NameGroup = b.NameGroup,

                Date = b.Production.CompletedDate,

                Qty = b.Qty,
                QtyPrice = b.QtyPrice,

                QtyWeight = b.QtyWeight,
                QtyWeightPrice = b.QtyWeightPrice,

                // Enhanced fields
                ProductionPlanId = b.Production.Id,
                Wo = b.Production.Wo,
                WoNumber = b.Production.WoNumber,
                WoText = b.Production.WoText,

                CompletedDate = b.Production.CompletedDate,
                ProductNumber = b.Production.ProductNumber,

                CustomerCode = b.Production.CustomerTypeNavigation != null ? b.Production.CustomerTypeNavigation.Code : string.Empty,
                CustomerName = b.Production.CustomerTypeNavigation != null ? b.Production.CustomerTypeNavigation.NameTh : string.Empty,

                Gold = b.Production.Type,
                GoldSize = b.Production.TypeSize,

                Mold = b.Production.Mold,
                ProductType = b.Production.ProductTypeNavigation.NameTh,

            });
        }
    }
}
