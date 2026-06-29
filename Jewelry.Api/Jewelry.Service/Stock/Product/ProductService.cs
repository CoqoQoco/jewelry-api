using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanGet;
using jewelry.Model.Stock.Product.Dashboard;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.ProductionPlan;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using NetTopologySuite.Index.HPRtree;
using NPOI.SS.Formula.Atp;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Jewelry.Service.Stock.Product
{
    public class ProductService : BaseService, IProductService
    {

        private readonly string _admin = "@ADMIN";

        private readonly JewelryContext _jewelryContext;

        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        private readonly IProductionPlanService _productionPlanService;
        public ProductService(JewelryContext JewelryContext, IHttpContextAccessor httpContextAccessor,
            IHostEnvironment HostingEnvironment,
            IProductionPlanService ProductionPlanService,
            IRunningNumber runningNumberService) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
            _productionPlanService = ProductionPlanService;
        }

        public IQueryable<jewelry.Model.Stock.Product.List.Response> List(jewelry.Model.Stock.Product.List.Search request)
        {
            var pieces = _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Include(x => x.SkuCodeNavigation)
                .Include(x => x.TbtStockPieceMaterial)
                .AsQueryable();

            if (request.ReceiptType != null && request.ReceiptType.Any())
            {
                var receiptTypeArray = request.ReceiptType.ToArray();
                pieces = pieces.Where(x => receiptTypeArray.Contains(x.SkuCodeNavigation.ProductionType));
            }
            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                pieces = pieces.Where(x => x.StockNumber.Contains(request.StockNumber));
            }
            if (!string.IsNullOrEmpty(request.StockNumberOrigin))
            {
                pieces = pieces.Where(x => x.StockNumberOrigin != null && x.StockNumberOrigin.Contains(request.StockNumberOrigin));
            }
            if (!string.IsNullOrEmpty(request.Mold))
            {
                pieces = pieces.Where(x => x.SkuCodeNavigation.MoldDesign != null && x.SkuCodeNavigation.MoldDesign.Contains(request.Mold));
            }
            if (request.ProductType != null && request.ProductType.Any())
            {
                var productTypeArray = request.ProductType.ToArray();
                pieces = pieces.Where(x => productTypeArray.Contains(x.SkuCodeNavigation.ProductType));
            }
            if (request.Gold != null && request.Gold.Any())
            {
                var goldArray = request.Gold.ToArray();
                pieces = pieces.Where(x => goldArray.Contains(x.SkuCodeNavigation.ProductionType));
            }
            if (request.GoldSize != null && request.GoldSize.Any())
            {
                var goldSizeArray = request.GoldSize.ToArray();
                pieces = pieces.Where(x => goldSizeArray.Contains(x.SkuCodeNavigation.ProductionTypeSize));
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                pieces = pieces.Where(x => x.ProductCode.Contains(request.ProductNumber));
            }
            if (request.ProductNumbers != null && request.ProductNumbers.Any())
            {
                var productNumbersArray = request.ProductNumbers.ToArray();
                pieces = pieces.Where(x => x.SkuCodeNavigation.ProductNumber != null
                    && productNumbersArray.Contains(x.SkuCodeNavigation.ProductNumber));
            }
            if (!string.IsNullOrEmpty(request.ProductNameTh))
            {
                pieces = pieces.Where(x => x.SkuCodeNavigation.ProductNameTh.Contains(request.ProductNameTh));
            }
            if (!string.IsNullOrEmpty(request.ProductNameEn))
            {
                pieces = pieces.Where(x => x.SkuCodeNavigation.ProductNameEn.Contains(request.ProductNameEn));
            }
            if (!string.IsNullOrEmpty(request.Size))
            {
                pieces = pieces.Where(x => x.SkuCodeNavigation.Size != null && x.SkuCodeNavigation.Size.Contains(request.Size));
            }
            if (request.HasCostDetail.HasValue)
            {
                if (request.HasCostDetail.Value)
                {
                    pieces = pieces.Where(x => x.ProductCostDetail != null && x.ProductCostDetail != "");
                }
                else
                {
                    pieces = pieces.Where(x => x.ProductCostDetail == null || x.ProductCostDetail == "");
                }
            }
            if (!string.IsNullOrEmpty(request.PieceStatus))
            {
                pieces = pieces.Where(x => x.Status == request.PieceStatus);
            }

            var response = from item in pieces
                           select new jewelry.Model.Stock.Product.List.Response()
                           {
                               StockNumber = item.StockNumber,
                               StockNumberOrigin = item.StockNumberOrigin,
                               Status = item.Status,

                               ReceiptNumber = item.ReceiptNumber,
                               ReceiptDate = item.ReceiptDate ?? item.CreateDate,
                               ReceiptType = item.ReceiptType,

                               Mold = item.SkuCodeNavigation.MoldDesign ?? item.SkuCodeNavigation.Mold,

                               Qty = 1,
                               ProductPrice = item.SkuCodeNavigation.DefaultPrice ?? 0,

                               ProductNumber = item.SkuCodeNavigation.ProductNumber,
                               ProductNameTh = item.SkuCodeNavigation.ProductNameTh,
                               ProductNameEn = item.SkuCodeNavigation.ProductNameEn,

                               ProductType = item.SkuCodeNavigation.ProductType,
                               ProductTypeName = item.SkuCodeNavigation.ProductTypeName,

                               ImageName = item.SkuCodeNavigation.ImageName,
                               ImagePath = item.SkuCodeNavigation.ImagePath,

                               Wo = item.Wo,
                               WoNumber = item.WoNumber,
                               WoText = string.IsNullOrEmpty(item.Wo) ? null : $"{item.Wo}{item.WoNumber.ToString()}",

                               ProductionDate = item.ProductionDate ?? item.CreateDate,
                               ProductionType = item.SkuCodeNavigation.ProductionType,
                               ProductionTypeSize = item.SkuCodeNavigation.ProductionTypeSize,

                               Size = item.SizeActual ?? item.SkuCodeNavigation.Size,
                               EarringStemSize = item.SkuCodeNavigation.EarringStemSize,
                               Location = item.LocationCode,
                               Remark = item.Remark,

                               CreateBy = item.CreateBy,
                               CreateDate = item.CreateDate,
                               UpdateDate = item.UpdateDate,
                               UpdateBy = item.UpdateBy,

                               TagPriceMultiplier = item.SkuCodeNavigation.TagPriceMultiplier ?? 1,

                               Materials = item.TbtStockPieceMaterial.Any()
                                   ? (from material in item.TbtStockPieceMaterial
                                      select new jewelry.Model.Stock.Product.List.Material()
                                      {
                                          Type = material.Type,
                                          TypeName = material.TypeName,
                                          TypeCode = material.TypeCode,
                                          TypeBarcode = material.TypeBarcode,
                                          TypeOrigin = material.TypeOrigin,
                                          Qty = material.Qty,
                                          QtyUnit = material.QtyUnit,
                                          Weight = material.Weight,
                                          WeightUnit = material.WeightUnit,
                                          Size = material.Size,
                                          Region = material.Region,
                                          Price = material.Price
                                      }).ToList()
                                   : new List<jewelry.Model.Stock.Product.List.Material>(),
                           };

            return response;
        }
        public IQueryable<jewelry.Model.Stock.Product.List.PriceTransection> GetStockCostDetail(string stockNumber)
        {

            var piece = (from item in _jewelryContext.TbtStockPiece
                         where item.StockNumber == stockNumber
                         select item).FirstOrDefault();

            if (piece == null)
            {
                return new List<jewelry.Model.Stock.Product.List.PriceTransection>().AsQueryable();
            }

            if (piece.ProductCostDetail == null || string.IsNullOrEmpty(piece.ProductCostDetail))
            {
                return new List<jewelry.Model.Stock.Product.List.PriceTransection>().AsQueryable();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<List<jewelry.Model.Stock.Product.List.PriceTransection>>(piece.ProductCostDetail, options)!.AsQueryable();
        }

        public async Task<jewelry.Model.Stock.Product.Get.Response> Get(jewelry.Model.Stock.Product.Get.Request request)
        {
            if (string.IsNullOrEmpty(request.StockNumber)
                && string.IsNullOrEmpty(request.ProductNumber)
                && string.IsNullOrEmpty(request.StockNumberOrigin))
            {
                throw new HandleException("StockNumber or ProductNumber or StockNumberOrigin is Required");
            }

            var query = _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Include(x => x.SkuCodeNavigation)
                .Include(x => x.TbtStockPieceMaterial)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                query = query.Where(x => x.StockNumber == request.StockNumber);
            }

            if (!string.IsNullOrEmpty(request.StockNumberOrigin))
            {
                query = query.Where(x => x.StockNumberOrigin == request.StockNumberOrigin);
            }

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.ProductCode == request.ProductNumber);
            }

            var piece = query.FirstOrDefault();

            if (piece == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var sku = piece.SkuCodeNavigation;

            var response = new jewelry.Model.Stock.Product.Get.Response()
            {
                StockNumber = piece.StockNumber,
                StockNumberOrigin = piece.StockNumberOrigin,

                ReceiptNumber = piece.ReceiptNumber,
                ReceiptType = piece.ReceiptType,
                ReceiptDate = piece.ReceiptDate ?? piece.CreateDate,

                ProductNumber = sku.ProductNumber,
                ProductNameTh = sku.ProductNameTh,
                ProductNameEn = sku.ProductNameEn,
                ProductType = sku.ProductType,
                ProductTypeName = sku.ProductTypeName,
                ProductPrice = sku.DefaultPrice ?? 0,
                Wo = piece.Wo,
                WoNumber = piece.WoNumber,
                WoText = $"{piece.Wo}{piece.WoNumber.ToString()}",
                ProductionDate = piece.ProductionDate ?? piece.CreateDate,
                ProductionType = sku.ProductionType,
                ProductionTypeSize = sku.ProductionTypeSize,
                Mold = sku.MoldDesign ?? sku.Mold,
                ImageName = sku.ImageName,
                ImagePath = sku.ImagePath,
                Qty = 1,
                Location = piece.LocationCode,
                Size = piece.SizeActual ?? sku.Size,
                EarringStemSize = sku.EarringStemSize,
                Remark = piece.Remark,
                CreateBy = piece.CreateBy,
                CreateDate = piece.CreateDate,
                UpdateBy = piece.UpdateBy,
                UpdateDate = piece.UpdateDate,
                TagPriceMultiplier = sku.TagPriceMultiplier ?? 1,
                Materials = (from material in piece.TbtStockPieceMaterial
                             select new jewelry.Model.Stock.Product.Get.Material()
                             {
                                 Type = material.Type,
                                 TypeName = material.TypeName,
                                 TypeCode = material.TypeCode,
                                 TypeBarcode = material.TypeBarcode,
                                 Qty = material.Qty,
                                 QtyUnit = material.QtyUnit,
                                 Weight = material.Weight,
                                 WeightUnit = material.WeightUnit,
                                 Size = material.Size,
                                 Region = material.Region,
                                 Price = material.Price
                             }).ToList(),
            };

            TbtProductionPlan plan = null;
            if (!string.IsNullOrEmpty(response.Wo) && response.WoNumber.HasValue)
            {
                plan = (from item in _jewelryContext.TbtProductionPlan
                             .Include(x => x.TbtProductionPlanPrice)
                        where item.Wo == response.Wo
                        && item.WoNumber == response.WoNumber.Value
                        select item).FirstOrDefault();
            }

            if (piece.ProductCostDetail != null)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                response.PriceTransactions = JsonSerializer.Deserialize<List<jewelry.Model.Stock.Product.Get.PriceTransaction>>(piece.ProductCostDetail, options) ?? new List<jewelry.Model.Stock.Product.Get.PriceTransaction>();
            }

            if (!response.PriceTransactions.Any())
            {
                if (plan != null && plan.TbtProductionPlanPrice != null && plan.TbtProductionPlanPrice.Any())
                {
                    response.PlanQty = plan.ProductQty;

                    var goldItems = plan.TbtProductionPlanPrice.Where(x => x.NameGroup == "Gold").ToList();
                    var nonGoldItems = plan.TbtProductionPlanPrice.Where(x => x.NameGroup != "Gold").ToList();
                    var transactions = new List<jewelry.Model.Stock.Product.Get.PriceTransaction>();

                    var getGoldCalWeight = goldItems.Where(x => x.NameDescription == "น้ำหนักทองรวมหลังหักเพชรพลอย").FirstOrDefault();
                    if (getGoldCalWeight != null)
                    {
                        transactions.Add(new jewelry.Model.Stock.Product.Get.PriceTransaction()
                        {
                            No = getGoldCalWeight.No,
                            Name = getGoldCalWeight.Name,
                            NameDescription = getGoldCalWeight.NameDescription,
                            NameGroup = getGoldCalWeight.NameGroup,
                            Date = getGoldCalWeight.Date,
                            Qty = Math.Round(getGoldCalWeight.Qty / plan.ProductQty, 2),
                            QtyPrice = Math.Round(getGoldCalWeight.QtyPrice, 2),
                            QtyWeight = Math.Round(getGoldCalWeight.QtyWeight / plan.ProductQty, 2),
                            QtyWeightPrice = Math.Round(getGoldCalWeight.QtyWeightPrice, 2),
                        });
                    }

                    transactions.AddRange(nonGoldItems.Select(x => new jewelry.Model.Stock.Product.Get.PriceTransaction()
                    {
                        No = x.No,
                        Name = x.Name,
                        NameDescription = x.NameDescription,
                        NameGroup = x.NameGroup,
                        Date = x.Date,
                        Qty = Math.Round(x.Qty, 2),
                        QtyPrice = Math.Round(x.QtyPrice, 2),
                        QtyWeight = Math.Round(x.QtyWeight, 2),
                        QtyWeightPrice = Math.Round(x.QtyWeightPrice, 2),
                    }));

                    response.PriceTransactions = transactions;
                }
                else
                {
                    var materials = response.Materials.Where(x => x.Type == "Gold" || x.Type == "Gem" || x.Type == "Diamond").ToList();
                    int no = 1;
                    foreach (var mat in materials)
                    {
                        response.PriceTransactions.Add(new jewelry.Model.Stock.Product.Get.PriceTransaction()
                        {
                            No = no,
                            Name = mat.TypeName,
                            NameDescription = mat.TypeCode,
                            NameGroup = GetNameGroupGroup(mat.Type),
                            Date = piece.CreateDate,
                            Qty = mat.Qty,
                            QtyPrice = 0m,
                            QtyWeight = mat.Weight,
                            QtyWeightPrice = mat.Price,
                        });
                        no++;
                    }
                }
            }

            if (plan != null && plan.TbtProductionPlanPrice != null && plan.TbtProductionPlanPrice.Any())
            {
                response.PlanQty = plan.ProductQty;
                response.PlanPriceItems = plan.TbtProductionPlanPrice
                    .OrderBy(x => x.No)
                    .Select(x => new jewelry.Model.Stock.Product.Get.PlanPriceItem
                    {
                        No = x.No,
                        Name = x.Name,
                        NameDescription = x.NameDescription,
                        NameGroup = x.NameGroup,
                        Date = x.Date,
                        Qty = x.Qty,
                        QtyPrice = x.QtyPrice,
                        QtyWeight = x.QtyWeight,
                        QtyWeightPrice = x.QtyWeightPrice,
                        TotalPrice = x.TotalPrice
                    }).ToList();
            }

            return response;
        }
        public async Task<string> Update(jewelry.Model.Stock.Product.Update.Request request)
        {
            CheckPermissionLevel("update_stock");

            var piece = await _jewelryContext.TbtStockPiece
                .Include(x => x.TbtStockPieceMaterial)
                .FirstOrDefaultAsync(x => x.StockNumber == request.StockNumber);

            if (piece == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var sku = await _jewelryContext.TbtSku
                .FirstOrDefaultAsync(x => x.SkuCode == piece.SkuCode);

            if (sku == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var now = DateTime.UtcNow;

            TbmProductType? productTypeMaster = null;
            if (!string.IsNullOrEmpty(request.ProductType))
            {
                productTypeMaster = await _jewelryContext.TbmProductType
                    .FirstOrDefaultAsync(x => x.Code == request.ProductType && x.IsActive == true);
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                piece.LocationCode = request.Location ?? piece.LocationCode;
                piece.SizeActual = request.Size;
                piece.Remark = request.Remark;
                piece.ProductCost = request.ProductCost ?? piece.ProductCost;
                piece.UpdateDate = now;
                piece.UpdateBy = CurrentUsername;

                sku.ProductNameEn = request.ProductNameEn;
                sku.ProductNameTh = request.ProductNameTh;
                sku.MoldDesign = request.Mold;
                sku.DefaultPrice = request.ProductPrice;
                sku.ImageName = request.ImageName;
                sku.ImagePath = request.ImagePath;
                sku.EarringStemSize = request.EarringStemSize;
                if (!string.IsNullOrEmpty(request.ProductType))
                {
                    sku.ProductType = request.ProductType;
                    sku.ProductTypeName = productTypeMaster?.NameTh;
                }
                sku.UpdateDate = now;
                sku.UpdateBy = CurrentUsername;

                if (piece.TbtStockPieceMaterial.Any())
                {
                    _jewelryContext.TbtStockPieceMaterial.RemoveRange(piece.TbtStockPieceMaterial);
                }

                var newMats = new List<TbtStockPieceMaterial>();
                if (request.Materials.Any())
                {
                    foreach (var item in request.Materials)
                    {
                        newMats.Add(new TbtStockPieceMaterial
                        {
                            StockNumber = piece.StockNumber,
                            ProductCode = piece.ProductCode,

                            Type = item.Type,
                            TypeName = item.TypeName,
                            TypeCode = item.TypeCode,
                            TypeBarcode = item.TypeBarcode,
                            TypeOrigin = item.TypeOrigin,

                            Qty = item.Qty,
                            QtyUnit = item.QtyUnit,
                            Weight = item.Weight,
                            WeightUnit = item.WeightUnit,

                            Size = item.Size,
                            Region = item.Region,
                            Price = item.Price,

                            CreateBy = CurrentUsername,
                            CreateDate = now
                        });
                    }
                }

                _jewelryContext.TbtStockPiece.Update(piece);
                _jewelryContext.TbtSku.Update(sku);
                if (newMats.Any())
                {
                    _jewelryContext.TbtStockPieceMaterial.AddRange(newMats);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return "success";
        }



        public async Task<string> CreateProductCostDeatialPlan(jewelry.Model.Stock.Product.PlanPeoductCost.Request request)
        {
            var _running = await _runningNumberService.GenerateRunningNumber("CP");

            var piece = (from item in _jewelryContext.TbtStockPiece
                         where item.Status == "IN_STOCK" && item.StockNumber == request.StockNumber
                         select item).FirstOrDefault();

            if (piece == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var newPlan = new TbtStockPieceCostPlan()
            {
                Running = _running,
                StockNumber = request.StockNumber,
                ProductCode = piece.ProductCode,
                StockNumberOrigin = piece.StockNumberOrigin,

                Remark = request.Remark,

                StatusId = JobStatus.Pending,
                StatusName = JobStatus.GetStatusNameEn(JobStatus.Pending),

                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow,
            };

            var myJob = new TbtMyJob()
            {
                JobTypeId = MyJobType.PlanStockCost,
                JobTypeName = MyJobType.GetTypeNameEn(MyJobType.PlanStockCost),
                JobRunning = _running,

                StatusId = JobStatus.Pending,
                StatusName = JobStatus.GetStatusNameEn(JobStatus.Pending),

                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow,
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            myJob.DataJob = JsonSerializer.Serialize(newPlan, options);

            _jewelryContext.TbtStockPieceCostPlan.Add(newPlan);
            _jewelryContext.TbtMyJob.Add(myJob);

            await _jewelryContext.SaveChangesAsync();
            return _running;
        }
        public async Task<string> AddProductCostDeatialVersion(jewelry.Model.Stock.Product.AddProductCost.Request request)
        {
            var piece = (from item in _jewelryContext.TbtStockPiece
                         where item.StockNumber == request.StockNumber
                         select item).FirstOrDefault();

            if (piece == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var priceTransactionList = new TbtStockPieceCostVersion()
            {
                Running = await _runningNumberService.GenerateRunningNumber("CV"),
                JobRunning = request.PlanRunning,

                StockNumber = request.StockNumber,
                ProductCode = piece.ProductCode,
                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow,

                CustomerCode = request.CustomerCode,
                CustomerName = request.CustomerName,
                CustomerAddress = request.CustomerAddress,
                CustomerTel = request.CustomerTel,
                CustomerEmail = request.CustomerEmail,
                Remark = request.Remark,
                TagPriceMultiplier = request.TagPriceMultiplier,
                CurrencyUnit = request.CurrencyUnit,
                CurrencyRate = request.CurrencyRate,

                ProductCostDetail = JsonSerializer.Serialize(request.Prictransection, options),
                CustomStockInfo = request.CustomStockInfo != null && request.CustomStockInfo.Any()
                    ? JsonSerializer.Serialize(request.CustomStockInfo, options)
                    : null,
            };

            _jewelryContext.TbtStockPieceCostVersion.Add(priceTransactionList);

            if (request.IsOriginCost)
            {
                piece.ProductCostDetail = priceTransactionList.ProductCostDetail;
                piece.ProductCost = request.Prictransection.Sum(x => x.TotalPrice);

                piece.UpdateBy = CurrentUsername;
                piece.UpdateDate = DateTime.UtcNow;
                _jewelryContext.TbtStockPiece.Update(piece);
            }

            if (!string.IsNullOrEmpty(request.PlanRunning))
            {
                var costPlan = (from item in _jewelryContext.TbtStockPieceCostPlan
                                where item.Running == request.PlanRunning
                                select item).FirstOrDefault();

                if (costPlan != null)
                {
                    costPlan.VersionRunning = priceTransactionList.Running;
                    costPlan.UpdateBy = CurrentUsername;
                    costPlan.UpdateDate = DateTime.UtcNow;

                    costPlan.StatusId = JobStatus.Completed;
                    costPlan.StatusName = JobStatus.GetStatusNameEn(JobStatus.Completed);

                    _jewelryContext.TbtStockPieceCostPlan.Update(costPlan);
                }

                var myJob = (from job in _jewelryContext.TbtMyJob
                             where job.JobRunning == request.PlanRunning
                             select job).FirstOrDefault();
                if (myJob != null)
                {
                    myJob.StatusId = JobStatus.Completed;
                    myJob.StatusName = JobStatus.GetStatusNameEn(JobStatus.Completed);
                    myJob.UpdateBy = CurrentUsername;
                    myJob.UpdateDate = DateTime.UtcNow;

                    _jewelryContext.TbtMyJob.Update(myJob);
                }
            }

            await _jewelryContext.SaveChangesAsync();
            return "success";
        }
        public IQueryable<jewelry.Model.Stock.Product.ListProductCost.Response> GetProductCostDetailVersion(string stockNumber)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = (from item in _jewelryContext.TbtStockPieceCostVersion
                            where item.StockNumber == stockNumber
                            select new jewelry.Model.Stock.Product.ListProductCost.Response()
                            {
                                Running = item.Running,
                                StockNumber = item.StockNumber,
                                CustomerCode = item.CustomerCode,
                                CustomerName = item.CustomerName,
                                CustomerAddress = item.CustomerAddress,
                                CustomerTel = item.CustomerTel,
                                CustomerEmail = item.CustomerEmail,
                                Remark = item.Remark,
                                TagPriceMultiplier = item.TagPriceMultiplier ?? 1,
                                CurrencyUnit = item.CurrencyUnit,
                                CurrencyRate = item.CurrencyRate,
                                CreateBy = item.CreateBy,
                                CreateDate = item.CreateDate,
                                UpdateBy = item.UpdateBy,
                                UpdateDate = item.UpdateDate,
                                Prictransection = JsonSerializer.Deserialize<List<jewelry.Model.Stock.Product.ListProductCost.ResponseItem>>(item.ProductCostDetail, options),
                                CustomStockInfo = !string.IsNullOrEmpty(item.CustomStockInfo)
                                    ? JsonSerializer.Deserialize<List<jewelry.Model.Stock.Product.ListProductCost.CustomStockInfoItem>>(item.CustomStockInfo, options)
                                    : null
                            });

            return response;
        }

        public jewelry.Model.Stock.Product.GetCostVersion.Response GetCostVersion(jewelry.Model.Stock.Product.GetCostVersion.Request request)
        {
            if (string.IsNullOrEmpty(request.PlanRunning))
            {
                throw new HandleException("PlanRunning is Required");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var costVersion = (from item in _jewelryContext.TbtStockPieceCostVersion
                               where item.JobRunning == request.PlanRunning
                               select item).FirstOrDefault();

            if (costVersion == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var response = new jewelry.Model.Stock.Product.GetCostVersion.Response()
            {
                Running = costVersion.Running,
                JobRunning = costVersion.JobRunning,
                StockNumber = costVersion.StockNumber,
                CustomerCode = costVersion.CustomerCode,
                CustomerName = costVersion.CustomerName,
                CustomerAddress = costVersion.CustomerAddress,
                CustomerTel = costVersion.CustomerTel,
                CustomerEmail = costVersion.CustomerEmail,
                Remark = costVersion.Remark,
                TagPriceMultiplier = costVersion.TagPriceMultiplier ?? 1,
                CurrencyUnit = costVersion.CurrencyUnit,
                CurrencyRate = costVersion.CurrencyRate,
                CreateBy = costVersion.CreateBy,
                CreateDate = costVersion.CreateDate,
                UpdateBy = costVersion.UpdateBy,
                UpdateDate = costVersion.UpdateDate,
                Prictransection = JsonSerializer.Deserialize<List<jewelry.Model.Stock.Product.GetCostVersion.ResponseItem>>(costVersion.ProductCostDetail, options) ?? new List<jewelry.Model.Stock.Product.GetCostVersion.ResponseItem>(),
                CustomStockInfo = !string.IsNullOrEmpty(costVersion.CustomStockInfo)
                    ? JsonSerializer.Deserialize<List<jewelry.Model.Stock.Product.GetCostVersion.CustomStockInfoItem>>(costVersion.CustomStockInfo, options)
                    : null
            };

            return response;
        }

        public IQueryable<jewelry.Model.Stock.Product.ListStockCostPlan.Response> ListStockCostPlan(jewelry.Model.Stock.Product.ListStockCostPlan.Search request)
        {
            var query = (from item in _jewelryContext.TbtStockPieceCostPlan
                         select item);

            // Apply filters
            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                query = query.Where(x => x.StockNumber.Contains(request.StockNumber));
            }

            if (!string.IsNullOrEmpty(request.Running))
            {
                query = query.Where(x => x.Running.Contains(request.Running));
            }

            if (request.StatusId.HasValue)
            {
                query = query.Where(x => x.StatusId == request.StatusId.Value);
            }

            if (!string.IsNullOrEmpty(request.StatusName))
            {
                query = query.Where(x => x.StatusName.Contains(request.StatusName));
            }

            if (!string.IsNullOrEmpty(request.CreateBy))
            {
                query = query.Where(x => x.CreateBy.Contains(request.CreateBy));
            }

            if (request.CreateDateFrom.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.CreateDateFrom.Value);
            }

            if (request.CreateDateTo.HasValue)
            {
                var endDate = request.CreateDateTo.Value.AddDays(1);
                query = query.Where(x => x.CreateDate < endDate);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            var response = from item in query
                           select new jewelry.Model.Stock.Product.ListStockCostPlan.Response()
                           {
                               Running = item.Running,
                               StockNumber = item.StockNumber,
                               Remark = item.Remark,
                               StatusId = item.StatusId,
                               StatusName = item.StatusName,
                               VersionRunning = item.VersionRunning,
                               IsMobileActive = item.IsMobileActive,
                               IsActive = item.IsActive,
                               CreateBy = item.CreateBy,
                               CreateDate = item.CreateDate,
                               UpdateBy = item.UpdateBy,
                               UpdateDate = item.UpdateDate
                           };

            return response;
        }
        public IQueryable<jewelry.Model.Stock.Product.ListCostVersion.Response> ListCostVersion(jewelry.Model.Stock.Product.ListCostVersion.Search request)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var query = (from item in _jewelryContext.TbtStockPieceCostVersion
                         select item);

            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                query = query.Where(x => x.StockNumber.Contains(request.StockNumber));
            }

            if (!string.IsNullOrEmpty(request.Running))
            {
                query = query.Where(x => x.Running.Contains(request.Running));
            }

            if (!string.IsNullOrEmpty(request.CreateBy))
            {
                query = query.Where(x => x.CreateBy.Contains(request.CreateBy));
            }

            if (request.CreateDateFrom.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.CreateDateFrom.Value);
            }

            if (request.CreateDateTo.HasValue)
            {
                var endDate = request.CreateDateTo.Value.AddDays(1);
                query = query.Where(x => x.CreateDate < endDate);
            }

            var response = from item in query
                           select new jewelry.Model.Stock.Product.ListCostVersion.Response()
                           {
                               Running = item.Running,
                               StockNumber = item.StockNumber,
                               CustomerCode = item.CustomerCode,
                               CustomerName = item.CustomerName,
                               CustomerAddress = item.CustomerAddress,
                               CustomerTel = item.CustomerTel,
                               CustomerEmail = item.CustomerEmail,
                               Remark = item.Remark,
                               TagPriceMultiplier = item.TagPriceMultiplier ?? 1,
                               CurrencyUnit = item.CurrencyUnit,
                               CurrencyRate = item.CurrencyRate,
                               CreateBy = item.CreateBy,
                               CreateDate = item.CreateDate,
                               UpdateBy = item.UpdateBy,
                               UpdateDate = item.UpdateDate,
                               Prictransection = JsonSerializer.Deserialize<List<jewelry.Model.Stock.Product.ListCostVersion.ResponseItem>>(item.ProductCostDetail, options),
                               CustomStockInfo = !string.IsNullOrEmpty(item.CustomStockInfo)
                                   ? JsonSerializer.Deserialize<List<jewelry.Model.Stock.Product.ListCostVersion.CustomStockInfoItem>>(item.CustomStockInfo, options)
                                   : null
                           };

            return response;
        }

        public IQueryable<jewelry.Model.Stock.Product.ListName.Response> ListName(jewelry.Model.Stock.Product.ListName.Request request)
        {
            if (request.Mode == "TH")
            {
                var response = (
                    from item in _jewelryContext.TbtSku
                    where item.ProductNameTh.Contains(request.Text)
                    select new jewelry.Model.Stock.Product.ListName.Response()
                    {
                        Text = item.ProductNameTh
                    }).Distinct();

                return response;
            }

            if (request.Mode == "EN")
            {
                var response = (
                    from item in _jewelryContext.TbtSku
                    where item.ProductNameEn.Contains(request.Text)
                    select new jewelry.Model.Stock.Product.ListName.Response()
                    {
                        Text = item.ProductNameEn
                    }).Distinct();

                return response;
            }

            throw new HandleException("Mode is Required");
        }



        #region Dashboard APIs

        public async Task<DashboardResponse> GetProductDashboard(DashboardRequest request)
        {
            var response = new DashboardResponse
            {
                DataAtDate = DateTimeOffset.UtcNow.DateTime
            };

            // Get stock summary
            response.Summary = await GetStockSummary(request);

            // Get category breakdown (grouped by ProductTypeName, ProductionType, ProductionTypeSize)
            response.Categories = await GetCategoryBreakdown(request);

            // Get last activities (recent 10 products)
            response.LastActivities = await GetLastActivities(request);

            return response;
        }

        public async Task<TodayReportResponse> GetTodayReport(DashboardRequest request)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var response = new TodayReportResponse
            {
                ReportDate = today
            };

            // Today's summary
            response.Summary = await GetTodaySummary(today, tomorrow, request);

            // Today's transactions (products created today)
            response.Transactions = await GetTodayTransactions(today, tomorrow, request);

            return response;
        }

        public async Task<WeeklyReportResponse> GetWeeklyReport(DashboardRequest request)
        {
            var now = DateTimeOffset.UtcNow;
            var startOfWeek = new DateTimeOffset(now.Date.AddDays(-(int)now.DayOfWeek), now.Offset);
            var endOfWeek = startOfWeek.AddDays(7);

            var response = new WeeklyReportResponse
            {
                WeekStartDate = startOfWeek.UtcDateTime,
                WeekEndDate = endOfWeek.DateTime,
                WeekNumber = $"Week {GetWeekOfYear(now.DateTime)}"
            };

            // Weekly summary
            response.Summary = await GetWeeklySummary(startOfWeek, endOfWeek, request);

            // Daily movements
            response.DailyMovements = await GetDailyMovements(startOfWeek, endOfWeek, request);

            return response;
        }

        public async Task<MonthlyReportResponse> GetMonthlyReport(DashboardRequest request)
        {
            var now = DateTimeOffset.UtcNow;
            var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
            var endOfMonth = startOfMonth.AddMonths(1);

            var response = new MonthlyReportResponse
            {
                Year = now.Year,
                Month = now.Month,
                MonthName = now.ToString("MMMM"),
                MonthStartDate = startOfMonth.DateTime,
                MonthEndDate = endOfMonth.AddDays(-1).DateTime
            };

            // Monthly summary
            response.Summary = await GetMonthlySummary(startOfMonth, endOfMonth, request);

            // Weekly comparisons
            response.WeeklyComparisons = await GetWeeklyComparisons(startOfMonth, endOfMonth, request);

            return response;
        }

        #endregion

        #region Private Helper Methods

        private IQueryable<StockDashboardItem> BuildStockQuery(DashboardRequest request)
        {
            var query = from piece in _jewelryContext.TbtStockPiece.AsNoTracking()
                        join sku in _jewelryContext.TbtSku.AsNoTracking() on piece.SkuCode equals sku.SkuCode
                        where piece.Status == "IN_STOCK" || piece.Status == "RESERVED"
                        select new StockDashboardItem
                        {
                            StockNumber = piece.StockNumber,
                            ProductNumber = sku.ProductNumber,
                            ProductNameTh = sku.ProductNameTh,
                            ProductNameEn = sku.ProductNameEn,
                            ProductType = sku.ProductType,
                            ProductTypeName = sku.ProductTypeName,
                            ProductionType = sku.ProductionType,
                            ProductionTypeSize = sku.ProductionTypeSize,
                            Status = piece.Status,
                            Qty = 1m,
                            ProductPrice = sku.DefaultPrice ?? 0m,
                            Mold = sku.Mold,
                            MoldDesign = sku.MoldDesign,
                            Wo = piece.Wo,
                            WoNumber = piece.WoNumber,
                            CreateDate = piece.CreateDate,
                            CreateBy = piece.CreateBy
                        };

            if (request.ProductType != null && request.ProductType.Any())
            {
                query = query.Where(x => request.ProductType.Contains(x.ProductType));
            }

            if (request.ProductionType != null && request.ProductionType.Any())
            {
                query = query.Where(x => request.ProductionType.Contains(x.ProductionType));
            }

            if (request.ProductionTypeSize != null && request.ProductionTypeSize.Any())
            {
                query = query.Where(x => request.ProductionTypeSize.Contains(x.ProductionTypeSize));
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(x => x.Status == request.Status);
            }

            return query;
        }

        private async Task<StockSummary> GetStockSummary(DashboardRequest request)
        {
            var query = BuildStockQuery(request);

            var summary = await query
                .GroupBy(x => 1)
                .Select(g => new StockSummary
                {
                    TotalProducts = g.Count(),
                    TotalQuantity = g.Sum(x => x.Qty),
                    TotalValue = g.Sum(x => x.ProductPrice * x.Qty),
                    AvailableQuantity = g.Where(x => x.Status == "Available").Sum(x => x.Qty),
                    OnProcessQuantity = g.Where(x => x.Status != "Available").Sum(x => x.Qty),
                    AvailableCount = g.Count(x => x.Status == "Available"),
                    OnProcessCount = g.Count(x => x.Status != "Available")
                })
                .FirstOrDefaultAsync();

            return summary ?? new StockSummary();
        }

        private async Task<List<ProductCategoryBreakdown>> GetCategoryBreakdown(DashboardRequest request)
        {
            var query = BuildStockQuery(request);

            var categories = await query
                .GroupBy(x => new
                {
                    x.ProductTypeName,
                    x.ProductionType,
                    x.ProductionTypeSize
                })
                .Select(g => new ProductCategoryBreakdown
                {
                    ProductTypeName = g.Key.ProductTypeName ?? "Unknown",
                    ProductionType = g.Key.ProductionType ?? "Unknown",
                    ProductionTypeSize = g.Key.ProductionTypeSize ?? "Unknown",
                    Count = g.Count(),
                    TotalQuantity = g.Sum(x => x.Qty),
                    TotalOnProcessQuantity = g.Where(x => x.Status != "Available").Sum(x => x.Qty),
                    TotalValue = g.Sum(x => x.ProductPrice * x.Qty),
                    AveragePrice = g.Average(x => x.ProductPrice)
                })
                .OrderByDescending(x => x.TotalValue)
                .ToListAsync();

            return categories;
        }

        private async Task<List<LastActivity>> GetLastActivities(DashboardRequest request)
        {
            var query = BuildStockQuery(request);

            var activities = await query
                .OrderByDescending(x => x.CreateDate)
                .Take(10)
                .Select(x => new LastActivity
                {
                    StockNumber = x.StockNumber,
                    ProductNumber = x.ProductNumber,
                    ProductNameTh = x.ProductNameTh,
                    ProductNameEn = x.ProductNameEn,
                    ProductTypeName = x.ProductTypeName,
                    ProductionType = x.ProductionType,
                    ProductionTypeSize = x.ProductionTypeSize,
                    Status = x.Status,
                    Qty = x.Qty,
                    ProductPrice = x.ProductPrice,
                    Mold = x.MoldDesign ?? x.Mold,
                    WoText = x.Wo + x.WoNumber.ToString(),
                    CreateDate = x.CreateDate,
                    CreateBy = x.CreateBy
                })
                .ToListAsync();

            return activities;
        }

        private async Task<TodaySummary> GetTodaySummary(DateTimeOffset today, DateTimeOffset tomorrow, DashboardRequest request)
        {
            var query = BuildStockQuery(request)
                .Where(x => x.CreateDate >= today.StartOfDayUtc() && x.CreateDate < tomorrow.EndOfDayUtc());

            var summary = await query
                .GroupBy(x => 1)
                .Select(g => new TodaySummary
                {
                    TotalTransactions = g.Count(),
                    NewStockItems = g.Count(),
                    TotalValue = g.Sum(x => x.ProductPrice * x.Qty),
                    PriceChanges = 0, // TODO: Implement price change tracking if needed
                    LowStockAlerts = 0 // TODO: Implement low stock logic if needed
                })
                .FirstOrDefaultAsync();

            return summary ?? new TodaySummary();
        }

        private async Task<List<LastActivity>> GetTodayTransactions(DateTimeOffset today, DateTimeOffset tomorrow, DashboardRequest request)
        {
            var query = BuildStockQuery(request)
                .Where(x => x.CreateDate >= today.StartOfDayUtc() && x.CreateDate < tomorrow.EndOfDayUtc());

            var transactions = await query
                .OrderByDescending(x => x.CreateDate)
                .Select(x => new LastActivity
                {
                    StockNumber = x.StockNumber,
                    ProductNumber = x.ProductNumber,
                    ProductNameTh = x.ProductNameTh,
                    ProductNameEn = x.ProductNameEn,
                    ProductTypeName = x.ProductTypeName,
                    ProductionType = x.ProductionType,
                    ProductionTypeSize = x.ProductionTypeSize,
                    Status = x.Status,
                    Qty = x.Qty,
                    ProductPrice = x.ProductPrice,
                    Mold = x.MoldDesign ?? x.Mold,
                    WoText = x.Wo + x.WoNumber.ToString(),
                    CreateDate = x.CreateDate,
                    CreateBy = x.CreateBy
                })
                .ToListAsync();

            return transactions;
        }

        private async Task<WeeklySummary> GetWeeklySummary(DateTimeOffset startOfWeek, DateTimeOffset endOfWeek, DashboardRequest request)
        {
            var query = BuildStockQuery(request)
                .Where(x => x.CreateDate >= startOfWeek.StartOfDayUtc() && x.CreateDate < endOfWeek.EndOfDayUtc());

            var summary = await query
                .GroupBy(x => 1)
                .Select(g => new WeeklySummary
                {
                    TotalTransactions = g.Count(),
                    NewStockItems = g.Count(),
                    TotalValue = g.Sum(x => x.ProductPrice * x.Qty),
                    PriceChanges = 0,
                    LowStockAlerts = 0
                })
                .FirstOrDefaultAsync();

            return summary ?? new WeeklySummary();
        }

        private async Task<List<DailyMovement>> GetDailyMovements(DateTimeOffset startOfWeek, DateTimeOffset endOfWeek, DashboardRequest request)
        {
            var query = BuildStockQuery(request)
                .Where(x => x.CreateDate >= startOfWeek.StartOfDayUtc() && x.CreateDate < endOfWeek.EndOfDayUtc());

            var movements = await query
                .GroupBy(x => x.CreateDate.Date)
                .Select(g => new DailyMovement
                {
                    Date = g.Key,
                    TransactionCount = g.Count(),
                    NewStockCount = g.Count(),
                    TotalValue = g.Sum(x => x.ProductPrice * x.Qty)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return movements;
        }

        private async Task<MonthlySummary> GetMonthlySummary(DateTimeOffset startOfMonth, DateTimeOffset endOfMonth, DashboardRequest request)
        {
            var query = BuildStockQuery(request)
                .Where(x => x.CreateDate >= startOfMonth.StartOfDayUtc() && x.CreateDate < endOfMonth.EndOfDayUtc());

            var summary = await query
                .GroupBy(x => 1)
                .Select(g => new MonthlySummary
                {
                    TotalTransactions = g.Count(),
                    NewStockItems = g.Count(),
                    TotalValue = g.Sum(x => x.ProductPrice * x.Qty),
                    PriceChanges = 0,
                    TotalAvailableProducts = g.Count(x => x.Status == "Available")
                })
                .FirstOrDefaultAsync();

            return summary ?? new MonthlySummary();
        }

        private async Task<List<WeeklyComparison>> GetWeeklyComparisons(DateTimeOffset startOfMonth, DateTimeOffset endOfMonth, DashboardRequest request)
        {
            var query = BuildStockQuery(request)
                .Where(x => x.CreateDate >= startOfMonth.StartOfDayUtc() && x.CreateDate < endOfMonth.EndOfDayUtc());

            var data = await query.ToListAsync();

            var weeklyComparisons = data
                .GroupBy(x => GetWeekOfYear(x.CreateDate))
                .Select(g => new WeeklyComparison
                {
                    WeekNumber = g.Key,
                    WeekStartDate = g.Min(x => x.CreateDate.Date),
                    WeekEndDate = g.Max(x => x.CreateDate.Date).AddDays(6),
                    TransactionCount = g.Count(),
                    NewStockCount = g.Count(),
                    TotalValue = g.Sum(x => x.ProductPrice * x.Qty)
                })
                .OrderBy(x => x.WeekNumber)
                .ToList();

            return weeklyComparisons;
        }

        private static int GetWeekOfYear(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var calendar = culture.Calendar;
            var dateTimeFormatInfo = culture.DateTimeFormat;
            return calendar.GetWeekOfYear(date, dateTimeFormatInfo.CalendarWeekRule, dateTimeFormatInfo.FirstDayOfWeek);
        }

        #endregion

        private string GetNameGroupGroup(string type)
        {
            switch (type)
            {
                case "Gold":
                    return "Gold";
                case "Gem":
                case "Diamond":
                    return "Gem";
                default:
                    return "Other";

            }
        }
    }

    internal class StockDashboardItem
    {
        public string StockNumber { get; set; } = null!;
        public string? ProductNumber { get; set; }
        public string ProductNameTh { get; set; } = null!;
        public string ProductNameEn { get; set; } = null!;
        public string? ProductType { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductionType { get; set; }
        public string? ProductionTypeSize { get; set; }
        public string? Status { get; set; }
        public decimal Qty { get; set; }
        public decimal ProductPrice { get; set; }
        public string? Mold { get; set; }
        public string? MoldDesign { get; set; }
        public string? Wo { get; set; }
        public int? WoNumber { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = null!;
    }
}
