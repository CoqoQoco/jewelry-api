using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Product.Dashboard;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.ProductionPlan;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using NetTopologySuite.Index.HPRtree;
using NPOI.SS.Formula.Atp;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
            var stock = (from item in _jewelryContext.TbtStockProduct
                         .Include(x => x.TbtStockProductMaterial)
                         where item.Status == "Available"
                         select item);

            if (request.ReceiptType != null && request.ReceiptType.Any())
            {
                var stockTypeArray = request.ReceiptType.Select(x => x).ToArray();
                stock = stock.Where(x => stockTypeArray.Contains(x.ProductionType));
            }
            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                stock = stock.Where(x => x.StockNumber.Contains(request.StockNumber));
            }
            if (!string.IsNullOrEmpty(request.StockNumberOrigin))
            {
                stock = stock.Where(x => x.ProductCode.Contains(request.StockNumberOrigin));
            }
            if (!string.IsNullOrEmpty(request.Mold))
            {
                stock = stock.Where(x => x.MoldDesign.Contains(request.Mold));
            }
            if (request.ProductType != null && request.ProductType.Any())
            {
                var productTypeArray = request.ProductType.Select(x => x).ToArray();
                stock = stock.Where(x => productTypeArray.Contains(x.ProductType));
            }

            if (request.Gold != null && request.Gold.Any())
            {
                var productTypeArray = request.Gold.Select(x => x).ToArray();
                stock = stock.Where(x => productTypeArray.Contains(x.ProductionType));
            }
            if (request.GoldSize != null && request.GoldSize.Any())
            {
                var productTypeArray = request.GoldSize.Select(x => x).ToArray();
                stock = stock.Where(x => productTypeArray.Contains(x.ProductionTypeSize));
            }

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                stock = stock.Where(x => x.ProductNumber.Contains(request.ProductNumber));
            }
            if (!string.IsNullOrEmpty(request.ProductNameTh))
            {
                stock = stock.Where(x => x.ProductNameTh.Contains(request.ProductNameTh));
            }
            if (!string.IsNullOrEmpty(request.ProductNameEn))
            {
                stock = stock.Where(x => x.ProductNameEn.Contains(request.ProductNameEn));
            }
            if (!string.IsNullOrEmpty(request.Size))
            {
                stock = stock.Where(x => x.Size.Contains(request.Size));
            }

            var response = from item in stock
                           select new jewelry.Model.Stock.Product.List.Response()
                           {
                               StockNumber = item.StockNumber,
                               StockNumberOrigin = item.ProductCode,
                               Status = item.Status,

                               ReceiptNumber = item.ReceiptNumber,
                               ReceiptDate = item.ReceiptDate,
                               ReceiptType = item.ProductionType,

                               Mold = item.MoldDesign ?? item.Mold,

                               Qty = item.Qty,
                               ProductPrice = item.ProductPrice,

                               ProductNumber = item.ProductNumber,
                               ProductNameTh = item.ProductNameTh,
                               ProductNameEn = item.ProductNameEn,

                               ProductType = item.ProductType,
                               ProductTypeName = item.ProductTypeName,

                               ImageName = item.ImageName,
                               ImagePath = item.ImagePath,

                               Wo = item.Wo,
                               WoNumber = item.WoNumber,
                               WoText = $"{item.Wo}{item.WoNumber.ToString()}",

                               ProductionDate = item.CreateDate,
                               ProductionType = item.ProductionType,
                               ProductionTypeSize = item.ProductionTypeSize,

                               Size = item.Size,
                               Location = item.Location,
                               Remark = item.Remark,

                               CreateBy = item.CreateBy,
                               CreateDate = item.CreateDate,

                               Materials = item.TbtStockProductMaterial.Any() ?
                                            (from material in item.TbtStockProductMaterial
                                             select new jewelry.Model.Stock.Product.List.Material()
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
                                             }).ToList()
                                             : new List<jewelry.Model.Stock.Product.List.Material>()
                           };

            return response;
        }
        public async Task<jewelry.Model.Stock.Product.Get.Response> Get(jewelry.Model.Stock.Product.Get.Request request)
        {
            if (string.IsNullOrEmpty(request.StockNumber)
                && string.IsNullOrEmpty(request.ProductNumber)
                && string.IsNullOrEmpty(request.StockNumberOrigin))
            {
                throw new HandleException("StockNumber or ProductNumber or StockNumberOrigin is Required");
            }

            var query = (from item in _jewelryContext.TbtStockProduct
                        .Include(x => x.TbtStockProductMaterial)
                         where item.Status == "Available"
                         select item);

            query = query.Where(x => x.QtyRemaining > 0);

            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                query = query.Where(x => x.StockNumber == request.StockNumber);
            }
            if (!string.IsNullOrEmpty(request.StockNumberOrigin))
            {
                query = query.Where(x => x.ProductCode == request.StockNumberOrigin);
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.ProductNumber == request.ProductNumber);
            }

            if (!query.Any())
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            var stock = query.FirstOrDefault();
            var response = new jewelry.Model.Stock.Product.Get.Response()
            {
                StockNumber = stock.StockNumber,
                StockNumberOrigin = stock.ProductCode ?? stock.StockNumber,

                ReceiptNumber = stock.ReceiptNumber,
                ReceiptType = stock.ProductionType,
                ReceiptDate = stock.ReceiptDate,

                ProductNumber = stock.ProductNumber,
                ProductNameTh = stock.ProductNameTh,
                ProductNameEn = stock.ProductNameEn,
                ProductType = stock.ProductType,
                ProductTypeName = stock.ProductTypeName,
                ProductPrice = stock.ProductPrice,
                Wo = stock.Wo,
                WoNumber = stock.WoNumber,
                WoText = $"{stock.Wo}{stock.WoNumber.ToString()}",
                ProductionDate = stock.CreateDate,
                ProductionTypeSize = stock.ProductionTypeSize,
                Mold = stock.MoldDesign ?? stock.Mold,
                ImageName = stock.ImageName,
                ImagePath = stock.ImagePath,
                Qty = stock.Qty,
                Location = stock.Location,
                Size = stock.Size,
                Remark = stock.Remark,
                CreateBy = stock.CreateBy,
                CreateDate = stock.CreateDate,
                UpdateBy = stock.UpdateBy,
                UpdateDate = stock.UpdateDate,
                Materials = (from material in stock.TbtStockProductMaterial
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
                             }).ToList()
            };


            if (!string.IsNullOrEmpty(response.Wo) && response.WoNumber.HasValue)
            {

                var plan = (from item in _jewelryContext.TbtProductionPlan
                             .Include(x => x.TbtProductionPlanPrice)
                            where item.Wo == response.Wo
                            && item.WoNumber == response.WoNumber.Value
                            select item).FirstOrDefault();


                if (plan != null && plan.TbtProductionPlanPrice != null && plan.TbtProductionPlanPrice.Any())
                {
                    response.PlanQty = plan.ProductQty;
                    string[] nameGroupMatch = new[] { "Gold", "Gem" };
                    response.PriceTransactions = plan.TbtProductionPlanPrice.Select(x => new jewelry.Model.Stock.Product.Get.PriceTransaction()
                    {
                        No = x.No,
                        Name = x.Name,
                        NameDescription = x.NameDescription,
                        NameGroup = x.NameGroup,

                        Date = x.Date,

                        Qty = Math.Round(x.Qty / plan.ProductQty, 2),
                        QtyPrice = Math.Round(x.QtyPrice, 2),
                        QtyWeight = Math.Round(x.QtyWeight / plan.ProductQty, 2),
                        QtyWeightPrice = Math.Round(x.QtyWeightPrice, 2),

                        //TotalPrice = Math.Round(x.TotalPrice / plan.ProductQty, 2),
                    }).ToList();

                }
            }

            return response;
        }


        public async Task<string> Update(jewelry.Model.Stock.Product.Update.Request request)
        {
            CheckPermissionLevel("update_stock");

            var stock = (from item in _jewelryContext.TbtStockProduct
                         .Include(x => x.TbtStockProductMaterial)
                         where item.StockNumber == request.StockNumber
                         && item.ReceiptNumber == request.ReceiptNumber
                         select item).FirstOrDefault();

            if (stock == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                stock.ProductNameEn = request.ProductNameEn;
                stock.ProductNameTh = request.ProductNameTh;

                stock.ImagePath = request.ImagePath;
                stock.ImageName = request.ImageName;

                stock.Qty = request.Qty;
                stock.ProductPrice = request.ProductPrice;

                stock.MoldDesign = request.Mold;

                stock.Size = request.Size;
                stock.Location = request.Location;

                stock.UpdateDate = DateTime.UtcNow;
                stock.UpdateBy = CurrentUsername;


                if (stock.TbtStockProductMaterial.Any())
                {
                    _jewelryContext.TbtStockProductMaterial.RemoveRange(stock.TbtStockProductMaterial);
                }

                var newMats = new List<TbtStockProductMaterial>();
                if (request.Materials.Any())
                {
                    foreach (var item in request.Materials)
                    {
                        var newMat = new TbtStockProductMaterial
                        {
                            StockNumber = request.StockNumber,

                            Type = item.Type,
                            TypeName = item.TypeName,
                            TypeCode = item.TypeCode,
                            TypeBarcode = item.TypeBarcode,

                            Qty = item.Qty,
                            QtyUnit = item.QtyUnit,
                            Weight = item.Weight,
                            WeightUnit = item.WeightUnit,

                            Size = item.Size,
                            Region = item.Region,
                            Price = item.Price,

                            CreateBy = CurrentUsername,
                            CreateDate = DateTime.UtcNow
                        };
                        newMats.Add(newMat);
                    }
                }

                _jewelryContext.TbtStockProduct.Update(stock);
                if (newMats.Any())
                {
                    _jewelryContext.TbtStockProductMaterial.AddRange(newMats);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return "success";
        }

        public IQueryable<jewelry.Model.Stock.Product.ListName.Response> ListName(jewelry.Model.Stock.Product.ListName.Request request)
        {
            if (request.Mode == "TH")
            {
                var response = (
                    from item in _jewelryContext.TbtStockProduct
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
                    from item in _jewelryContext.TbtStockProduct
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

        private IQueryable<TbtStockProduct> BuildStockQuery(DashboardRequest request)
        {
            var query = _jewelryContext.TbtStockProduct
                .Where(x => x.QtyRemaining > 0)
                .AsNoTracking();

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

    }
}
