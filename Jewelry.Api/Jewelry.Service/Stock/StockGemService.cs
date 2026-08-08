using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Gem.Option;
using jewelry.Model.Stock.Gem.Price;
using jewelry.Model.Stock.Gem.PriceEdit;
using jewelry.Model.Stock.Gem.Search;
using jewelry.Model.Stock.Gem.Dashboard;
using jewelry.Model.Stock.Gem.Report;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Kendo.DynamicLinqCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Index.HPRtree;
using NPOI.HSSF.Record;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Stock
{
    public interface IStockGemService
    {
        List<SearchGemResponse> SearchGem(SearchGem request);
        DataSourceResult SearchGemData(SearchGemRequest request);
        IQueryable<OptionResponse> GroupGemData(OptionRequest request);

        Task<string> Price(PriceEditRequest request);
        IQueryable<TbtStockGemTransectionPrice> PriceHistory(Price request);

        // Dashboard APIs
        Task<DashboardResponse> GetStockGemDashboard(DashboardRequest request);
        Task<TodayReportResponse> GetTodayReport(DashboardRequest request);
        Task<WeeklyReportResponse> GetWeeklyReport(DashboardRequest request);
        Task<MonthlyReportResponse> GetMonthlyReport(DashboardRequest request);

        Task<List<TransactionTypeCategorySummary>> GetTransactionSummariesByType(DashboardRequest request);

        Task<AgingReportResponse> GetAgingReport(DashboardRequest request);

        List<MovementReportResponse> GetMovementReport(MovementReportRequest request);
    }
    public class StockGemService : IStockGemService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly bool _valPass = false;
        private static readonly int[] InboundTypes = { 1, 2, 3, 6 };
        private static readonly int[] OutboundTypes = { 4, 5, 7 };
        private static readonly int[] ConsumedTypes = { 4, 7 };
        private const int LowStockThreshold = 10;
        private const decimal InventoryDaysLowThreshold = 30;
        private const decimal InventoryDaysExcessThreshold = 90;

        private static readonly (string Key, int SortOrder)[] AgingBucketDefinitions = new[]
        {
            ("d0_30", 1),
            ("d31_90", 2),
            ("d91_180", 3),
            ("d181_365", 4),
            ("over365", 5),
            ("never", 6)
        };
        public StockGemService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
        }

        public List<SearchGemResponse> SearchGem(SearchGem request)
        {
            var query = (from item in _jewelryContext.TbtStockGem
                         select new SearchGemResponse()
                         {
                             Id = item.Id,
                             Name = $"{item.Code}-{item.GroupName}-{item.Shape}-{item.Size}-{item.Grade}",
                             Code = item.Code,

                             Price = item.Price,
                             PriceQty = item.PriceQty,
                             Unit = item.Unit,

                         }).ToList();

            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query
                         where item.Name.Contains(request.Text)
                         select item).ToList();
            }
            if (request.Id.HasValue)
            {
                query = (from item in query
                         where item.Id == request.Id.Value
                         select item).ToList();
            }

            return query;
        }

        public DataSourceResult SearchGemData(SearchGemRequest request)
        {
            var search = request.Search ?? new SearchGem();

            var query = (from item in _jewelryContext.TbtStockGem
                         select item).AsNoTracking();

            if (search.Id.HasValue)
            {
                query = (from item in query
                         where item.Id == search.Id.Value
                         select item);
            }
            if (!string.IsNullOrEmpty(search.Code))
            {
                query = (from item in query
                         where item.Code.Contains(search.Code.ToUpper())
                         select item);
            }
            if (search.GroupName != null && search.GroupName.Length > 0)
            {
                query = (from item in query
                         where search.GroupName.Contains(item.GroupName)
                         select item);
            }
            if (search.Size != null && search.Size.Length > 0)
            {
                query = (from item in query
                         where search.Size.Contains(item.Size)
                         select item);
            }
            if (search.Shape != null && search.Shape.Length > 0)
            {
                query = (from item in query
                         where search.Shape.Contains(item.Shape)
                         select item);
            }
            if (search.Grade != null && search.Grade.Length > 0)
            {
                query = (from item in query
                         where search.Grade.Contains(item.Grade)
                         select item);
            }

            if (search.TypeCheck != null && search.TypeCheck.Length > 0)
            {
                var typeCheckLower = search.TypeCheck.Select(tc => tc.ToLower()).ToArray();

                if (typeCheckLower.Contains("qty-remain"))
                {
                    query = query.Where(item => item.Quantity > 0);
                }

                if (typeCheckLower.Contains("qty-process-remain"))
                {
                    query = query.Where(item => item.QuantityOnProcess > 0);
                }

                if (typeCheckLower.Contains("qty-weight-remain"))
                {
                    query = query.Where(item => item.QuantityWeight > 0);
                }

                if (typeCheckLower.Contains("qty-weight-process-remain"))
                {
                    query = query.Where(item => item.QuantityWeightOnProcess > 0);
                }
            }

            var response = (from item in query
                            select new SearchGemResponse()
                            {
                                Id = item.Id,
                                Name = $"{item.Code}-{item.Shape}-{item.Size}-{item.Grade}",
                                Code = item.Code,
                                GroupName = item.GroupName,

                                Size = item.Size,
                                Shape = item.Shape,
                                Grade = item.Grade,

                                Quantity = item.Quantity,
                                QuantityOnProcess = item.QuantityOnProcess,
                                QuantityWeight = item.QuantityWeight,
                                QuantityWeightOnProcess = item.QuantityWeightOnProcess,

                                Price = item.Price,
                                PriceQty = item.PriceQty,
                                Unit = item.Unit,
                                UnitCode = item.UnitCode,

                                Remark1 = item.Remark1,
                                Remark2 = item.Remark2,

                                Region = item.Region
                            });

            var dataSource = response.ToDataSourceResult(request);
            var pageItems = dataSource.Data.Cast<SearchGemResponse>().ToList();

            var pageCodes = pageItems.Select(x => x.Code).Distinct().ToList();
            var lastMovements = _jewelryContext.TbtStockGemTransection
                .Where(x => pageCodes.Contains(x.Code))
                .GroupBy(x => x.Code)
                .Select(g => new { Code = g.Key, Last = g.Max(x => x.RequestDate) })
                .ToList();
            var lastMovementByCode = lastMovements.ToDictionary(x => x.Code, x => x.Last);

            var now = DateTime.UtcNow;
            foreach (var item in pageItems)
            {
                if (lastMovementByCode.TryGetValue(item.Code, out var lastMovementDate))
                {
                    item.LastMovementDate = lastMovementDate;
                    item.DaysSinceLastMovement = (int)(now - lastMovementDate).TotalDays;
                }
            }

            dataSource.Data = pageItems;
            return dataSource;
        }
        public IQueryable<OptionResponse> GroupGemData(OptionRequest request)
        {
            var result = new List<OptionResponse>().AsQueryable();

            var query = (from item in _jewelryContext.TbtStockGem
                         select item);

            if (request.Type == "GROUPGEM")
            {
                result = (from item in query
                          group item by item.GroupName into g
                          select new OptionResponse()
                          {
                              Value = g.Key,
                          });
            }
            if (request.Type == "GRADE")
            {
                result = (from item in query
                          group item by item.Grade into g
                          select new OptionResponse()
                          {
                              Value = g.Key,
                          });
            }
            if (request.Type == "SHAPE")
            {
                result = (from item in query
                          group item by item.Shape into g
                          select new OptionResponse()
                          {
                              Value = g.Key,
                          });
            }
            if (request.Type == "SIZE")
            {
                result = (from item in query
                          group item by item.Size into g
                          select new OptionResponse()
                          {
                              Value = g.Key,
                          });
            }

            if (request.Value != null && request.Value.Length > 0)
            {
                result = (from item in result
                          where request.Value.Contains(item.Value)
                          select item);
            }

            return result.OrderBy(x => x.Value);
        }

        public async Task<string> Price(PriceEditRequest request)
        {
            if (_valPass)
            {
                var account = (from item in _jewelryContext.TbmAccount
                               where item.Username == "GI-GEM"
                               && item.TempPass == request.Pass
                               select item);

                if (!account.Any())
                {
                    throw new HandleException(ErrorMessage.PermissionFail);
                }
            }

            var gem = (from _gem in _jewelryContext.TbtStockGem
                       where request.Code == _gem.Code && _gem.Id == request.Id
                       select _gem).FirstOrDefault();

            if (gem == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var priceTtransection = new TbtStockGemTransectionPrice()
                {
                    Code = gem.Code,

                    PreviousPrice = gem.Price,
                    NewPrice = request.NewPrice,

                    PreviousPriceUnit = gem.PriceQty,
                    NewPriceUnit = request.NewPriceUnit,

                    Unit = request.Unit,
                    UnitCode = request.UnitCode,

                    Remark = request.Pass,

                    CreateBy = _admin,
                    CreateDate = DateTime.UtcNow
                };

                gem.Price = request.NewPrice;
                gem.PriceQty = request.NewPriceUnit;

                gem.Unit = request.Unit;
                gem.UnitCode = request.UnitCode;

                gem.UpdateBy = _admin;
                gem.UpdateDate = DateTime.UtcNow;

                _jewelryContext.TbtStockGemTransectionPrice.Add(priceTtransection);
                _jewelryContext.TbtStockGem.Update(gem);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            return "success";
        }
        public IQueryable<TbtStockGemTransectionPrice> PriceHistory(Price request)
        {
            var query = (from item in _jewelryContext.TbtStockGemTransectionPrice
                         where item.Code == request.Code
                         select item);

            return query.OrderByDescending(x => x.CreateDate);
        }

        #region Dashboard APIs

        public async Task<DashboardResponse> GetStockGemDashboard(DashboardRequest request)
        {
            var response = new DashboardResponse();
            var now = DateTimeOffset.UtcNow;
            var startDate = request.StartDate?.StartOfDayUtc() ?? now.Date.AddDays(-30);
            var endDate = request.EndDate?.EndOfDayUtc() ?? now.Date.AddDays(1);

            // Get stock summary
            response.Summary = await GetStockSummary(request);

            // Get category breakdown
            response.Categories = await GetCategoryBreakdown(request);

            // Get transaction trends
            response.Trends = await GetTransactionTrends(startDate, endDate, request);

            // Get top gem movements
            response.TopMovements = await GetTopGemMovements(startDate, endDate, request);

            // Get price change alerts
            response.PriceAlerts = await GetPriceChangeAlerts(startDate, endDate, request);

            // Get last activities (10 recent transactions)
            response.LastActivities = await GetLastActivities(request);

            return response;
        }

        public async Task<TodayReportResponse> GetTodayReport(DashboardRequest request)
        {
            var today = DateTimeOffset.UtcNow;
            var tomorrow = today.AddDays(1);

            var response = new TodayReportResponse
            {
                ReportDate = today.DateTime
            };

            // Today's summary
            response.Summary = await GetTodaySummary(today, tomorrow, request);

            // Today's transactions
            response.Transactions = await GetTodayTransactions(today, tomorrow, request);

            // Today's price changes
            response.PriceChanges = await GetTodayPriceChanges(today, tomorrow, request);

            // New stocks today
            response.NewStocks = await GetTodayNewStocks(today, tomorrow, request);

            // Low stock alerts
            response.LowStocks = await GetTodayLowStocks(request);

            return response;
        }

        public async Task<WeeklyReportResponse> GetWeeklyReport(DashboardRequest request)
        {
            var now = DateTimeOffset.UtcNow;
            var startOfWeek = new DateTimeOffset(now.Date.AddDays(-(int)now.DayOfWeek), now.Offset);
            var endOfWeek = startOfWeek.AddDays(7);

            var response = new WeeklyReportResponse
            {
                WeekStartDate = startOfWeek.DateTime,
                WeekEndDate = endOfWeek.DateTime,
                WeekNumber = $"Week {GetWeekOfYear(now.DateTime)}"
            };

            // Weekly summary
            response.Summary = await GetWeeklySummary(startOfWeek, endOfWeek, request);

            // Daily movements
            response.DailyMovements = await GetDailyMovements(startOfWeek, endOfWeek, request);

            // Top movements
            response.TopMovements = await GetWeeklyTopMovements(startOfWeek, endOfWeek, request);

            // Performance analysis
            response.Performance = await GetWeeklyPerformance(startOfWeek, endOfWeek, request);

            // Trend analysis
            response.TrendAnalysis = await GetWeeklyTrendAnalysis(startOfWeek, endOfWeek, request);

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

            // Top performers
            response.TopPerformers = await GetMonthlyTopPerformers(startOfMonth, endOfMonth, request);

            // Inventory analysis
            response.InventoryAnalysis = await GetMonthlyInventoryAnalysis(startOfMonth, endOfMonth, request);

            // Price analysis
            response.PriceAnalysis = await GetMonthlyPriceAnalysis(startOfMonth, endOfMonth, request);

            // Supplier analysis
            response.SupplierAnalysis = await GetMonthlySupplierAnalysis(startOfMonth, endOfMonth, request);

            return response;
        }

        #endregion

        #region Private Helper Methods

        private async Task<StockSummary> GetStockSummary(DashboardRequest request)
        {
            var query = BuildStockQuery(request);

            var summary = await query
                .GroupBy(x => 1)
                .Select(g => new StockSummary
                {
                    TotalGemTypes = g.Count(),
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalQuantityWeight = g.Sum(x => x.QuantityWeight),
                    TotalValue = g.Sum(x => x.PriceQty > 0 ? (x.Quantity * x.PriceQty) : (x.QuantityWeight * x.Price)),
                    TotalOnProcessQuantity = g.Sum(x => x.QuantityOnProcess),
                    TotalOnProcessQuantityWeight = g.Sum(x => x.QuantityWeightOnProcess),
                    // Quantity ถูกหัก QuantityOnProcess ออกไปแล้วตั้งแต่ตอน PickOff จึงไม่ต้องลบซ้ำ
                    AvailableQuantity = g.Sum(x => x.Quantity),
                    AvailableQuantityWeight = g.Sum(x => x.QuantityWeight),
                    LowStockCount = g.Count(x => x.Quantity > 0 && x.Quantity <= LowStockThreshold),
                    ZeroStockCount = g.Count(x => x.Quantity == 0)
                })
                .FirstOrDefaultAsync();

            return summary ?? new StockSummary();
        }

        private async Task<List<GemCategoryBreakdown>> GetCategoryBreakdown(DashboardRequest request)
        {
            var query = BuildStockQuery(request);

            var groupBy = string.IsNullOrEmpty(request.GroupBy) ? "group" : request.GroupBy.ToLower();

            IQueryable<GemCategoryBreakdown> group;

            if (groupBy == "shape")
            {
                group = query.GroupBy(x => new { x.Shape })
                             .Select(g => new GemCategoryBreakdown
                             {
                                 Shape = g.Key.Shape,
                                 Count = g.Count(),
                                 TotalQuantity = g.Sum(x => x.Quantity),
                                 TotalOnProcessQuantity = g.Sum(x => x.QuantityOnProcess),
                                 TotalQuantityWeight = g.Sum(x => x.QuantityWeight),
                                 TotalOnProcessQuantityWeight = g.Sum(x => x.QuantityWeightOnProcess),
                                 TotalValue = g.Sum(x => x.PriceQty > 0 ? (x.Quantity * x.PriceQty) : (x.QuantityWeight * x.Price)),
                                 AveragePrice = g.Any() ? g.Average(x => x.Price) : 0
                             })
                             .OrderByDescending(x => x.Shape);
            }
            else if (groupBy == "grade")
            {
                group = query.GroupBy(x => new { x.Grade })
                             .Select(g => new GemCategoryBreakdown
                             {
                                 Grade = g.Key.Grade,
                                 Count = g.Count(),
                                 TotalQuantity = g.Sum(x => x.Quantity),
                                 TotalOnProcessQuantity = g.Sum(x => x.QuantityOnProcess),
                                 TotalQuantityWeight = g.Sum(x => x.QuantityWeight),
                                 TotalOnProcessQuantityWeight = g.Sum(x => x.QuantityWeightOnProcess),
                                 TotalValue = g.Sum(x => x.PriceQty > 0 ? (x.Quantity * x.PriceQty) : (x.QuantityWeight * x.Price)),
                                 AveragePrice = g.Any() ? g.Average(x => x.Price) : 0
                             })
                             .OrderByDescending(x => x.Grade);
            }
            else
            {
                group = query.GroupBy(x => new { x.GroupName })
                             .Select(g => new GemCategoryBreakdown
                             {
                                 GroupName = g.Key.GroupName,
                                 Count = g.Count(),
                                 TotalQuantity = g.Sum(x => x.Quantity),
                                 TotalOnProcessQuantity = g.Sum(x => x.QuantityOnProcess),
                                 TotalQuantityWeight = g.Sum(x => x.QuantityWeight),
                                 TotalOnProcessQuantityWeight = g.Sum(x => x.QuantityWeightOnProcess),
                                 TotalValue = g.Sum(x => x.PriceQty > 0 ? (x.Quantity * x.PriceQty) : (x.QuantityWeight * x.Price)),
                                 AveragePrice = g.Any() ? g.Average(x => x.Price) : 0
                             })
                             .OrderByDescending(x => x.GroupName);
            }

            return await group.Where(x => x.TotalQuantity > 0
                                       || x.TotalQuantityWeight > 0
                                       || x.TotalOnProcessQuantity > 0
                                       || x.TotalOnProcessQuantityWeight > 0)
                              .ToListAsync();
        }

        private async Task<List<TransactionTrend>> GetTransactionTrends(DateTimeOffset startDate, DateTimeOffset endDate, DashboardRequest request)
        {
            var transactionQuery = _jewelryContext.TbtStockGemTransection
                .Where(x => x.CreateDate >= startDate.StartOfDayUtc() && x.CreateDate < endDate.EndOfDayUtc());

            if (request.GroupName != null && request.GroupName.Length > 0)
            {
                var gemCodes = await _jewelryContext.TbtStockGem
                    .Where(x => request.GroupName.Contains(x.GroupName))
                    .Select(x => x.Code)
                    .ToListAsync();
                transactionQuery = transactionQuery.Where(x => gemCodes.Contains(x.Code));
            }

            return await transactionQuery
                .GroupBy(x => x.CreateDate.Date)
                .Select(g => new TransactionTrend
                {
                    Date = g.Key,
                    TransactionCount = g.Count(),
                    TotalQuantityIn = g.Where(x => InboundTypes.Contains(x.Type)).Sum(x => x.Qty),
                    TotalQuantityOut = g.Where(x => OutboundTypes.Contains(x.Type)).Sum(x => x.Qty),
                    TotalQuantityWeightIn = g.Where(x => InboundTypes.Contains(x.Type)).Sum(x => x.QtyWeight),
                    TotalQuantityWeightOut = g.Where(x => OutboundTypes.Contains(x.Type)).Sum(x => x.QtyWeight),
                    NetQuantityChange = g.Where(x => InboundTypes.Contains(x.Type)).Sum(x => x.Qty) - g.Where(x => OutboundTypes.Contains(x.Type)).Sum(x => x.Qty),
                    NetQuantityWeightChange = g.Where(x => InboundTypes.Contains(x.Type)).Sum(x => x.QtyWeight) - g.Where(x => OutboundTypes.Contains(x.Type)).Sum(x => x.QtyWeight),
                    TotalQuantityConsumed = g.Where(x => ConsumedTypes.Contains(x.Type)).Sum(x => x.Qty),
                    TotalQuantityWeightConsumed = g.Where(x => ConsumedTypes.Contains(x.Type)).Sum(x => x.QtyWeight)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();
        }

        private async Task<List<TopGemMovement>> GetTopGemMovements(DateTimeOffset startDate, DateTimeOffset endDate, DashboardRequest request)
        {
            var transactionQuery = _jewelryContext.TbtStockGemTransection
                .Where(x => x.CreateDate >= startDate.StartOfDayUtc() && x.CreateDate < endDate.EndOfDayUtc());

            return await (from trans in transactionQuery
                          join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                          where (request.GroupName == null || request.GroupName.Length == 0 || request.GroupName.Contains(gem.GroupName)) &&
                                (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape)) &&
                                (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                          group new { trans, gem } by new { gem.Code, gem.GroupName, gem.Shape, gem.Grade, gem.Size } into g
                          select new TopGemMovement
                          {
                              Code = g.Key.Code,
                              GroupName = g.Key.GroupName,
                              Shape = g.Key.Shape,
                              Grade = g.Key.Grade,
                              Size = g.Key.Size,
                              TransactionCount = g.Count(),
                              TotalQuantityMoved = g.Sum(x => x.trans.Qty),
                              TotalQuantityWeightMoved = g.Sum(x => x.trans.QtyWeight),
                              CurrentQuantity = g.Max(x => x.gem.Quantity),
                              CurrentQuantityWeight = g.Max(x => x.gem.QuantityWeight),
                              CurrentPrice = g.Max(x => x.gem.Price)
                          })
                .OrderByDescending(x => x.TransactionCount)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<PriceChangeAlert>> GetPriceChangeAlerts(DateTimeOffset startDate, DateTimeOffset endDate, DashboardRequest request)
        {
            var priceChangeQuery = _jewelryContext.TbtStockGemTransectionPrice
                .Where(x => x.CreateDate >= startDate.StartOfDayUtc() && x.CreateDate < endDate.EndOfDayUtc());

            return await (from price in priceChangeQuery
                          join gem in _jewelryContext.TbtStockGem on price.Code equals gem.Code
                          where (request.GroupName == null || request.GroupName.Length == 0 || request.GroupName.Contains(gem.GroupName)) &&
                                (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape)) &&
                                (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                          select new PriceChangeAlert
                          {
                              Code = price.Code,
                              GroupName = gem.GroupName,
                              Shape = gem.Shape,
                              Grade = gem.Grade,
                              PreviousPrice = price.PreviousPrice,
                              NewPrice = price.NewPrice,
                              ChangePercentage = price.PreviousPrice > 0 ? ((price.NewPrice - price.PreviousPrice) / price.PreviousPrice) * 100 : 0,
                              ChangeDate = price.CreateDate,
                              ChangeType = price.NewPrice > price.PreviousPrice ? "INCREASE" : "DECREASE"
                          })
                .Where(x => Math.Abs(x.ChangePercentage) > 5) // Only show changes > 5%
                .OrderByDescending(x => Math.Abs(x.ChangePercentage))
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<LastActivity>> GetLastActivities(DashboardRequest request)
        {
            var query = _jewelryContext.TbtStockGemTransection.AsQueryable();

            if (request.GroupName != null && request.GroupName.Length > 0)
            {
                var gemCodes = await _jewelryContext.TbtStockGem
                    .Where(x => request.GroupName.Contains(x.GroupName))
                    .Select(x => x.Code)
                    .ToListAsync();
                query = query.Where(x => gemCodes.Contains(x.Code));
            }

            return await (from trans in query
                          join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                          where (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape)) &&
                                (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                          orderby trans.CreateDate descending
                          select new LastActivity
                          {
                              Code = trans.Code,
                              GroupName = gem.GroupName,
                              Shape = gem.Shape,
                              Grade = gem.Grade,
                              Size = gem.Size,
                              Type = trans.Type,
                              TypeName = StockGemServiceStatic.GetTransactionTypeName(trans.Type),
                              Qty = trans.Qty,
                              QtyWeight = trans.QtyWeight,
                              Status = trans.Stastus,
                              JobOrPo = trans.JobOrPo,
                              Running = trans.Running,
                              CreateDate = trans.CreateDate,
                              CreateBy = trans.CreateBy ?? string.Empty,
                              UpdateBy = trans.UpdateBy ?? string.Empty
                          })
                .Take(10)
                .ToListAsync();
        }



        private IQueryable<TbtStockGem> BuildStockQuery(DashboardRequest request)
        {
            var query = _jewelryContext.TbtStockGem.AsQueryable();

            if (request.GroupName != null && request.GroupName.Length > 0)
                query = query.Where(x => request.GroupName.Contains(x.GroupName));

            if (request.Shape != null && request.Shape.Length > 0)
                query = query.Where(x => request.Shape.Contains(x.Shape));

            if (request.Grade != null && request.Grade.Length > 0)
                query = query.Where(x => request.Grade.Contains(x.Grade));

            return query;
        }

        private int GetWeekOfYear(DateTime date)
        {
            var jan1 = new DateTime(date.Year, 1, 1);
            var daysOffset = (int)jan1.DayOfWeek;
            var firstWeekDay = jan1.AddDays(-daysOffset);
            var weekNum = (int)((date - firstWeekDay).TotalDays / 7) + 1;
            return weekNum;
        }

        // Note: The following methods are simplified implementations
        // In a production environment, you would implement more sophisticated logic for each

        private async Task<TodayStockSummary> GetTodaySummary(DateTimeOffset today, DateTimeOffset tomorrow, DashboardRequest request)
        {
            var transactionQuery = _jewelryContext.TbtStockGemTransection
                .Where(x => x.CreateDate >= today.StartOfDayUtc() && x.CreateDate < tomorrow.EndOfDayUtc());

            var priceChangeQuery = _jewelryContext.TbtStockGemTransectionPrice
                .Where(x => x.CreateDate >= today.StartOfDayUtc() && x.CreateDate < tomorrow.EndOfDayUtc());

            var newStockQuery = _jewelryContext.TbtStockGem
                .Where(x => x.CreateDate >= today.StartOfDayUtc() && x.CreateDate < tomorrow.EndOfDayUtc());

            var lowStockAlerts = await BuildStockQuery(request)
                .CountAsync(x => x.Quantity > 0 && x.Quantity <= LowStockThreshold);

            return new TodayStockSummary
            {
                TotalTransactions = await transactionQuery.CountAsync(),
                PriceChanges = await priceChangeQuery.CountAsync(),
                NewStockItems = await newStockQuery.CountAsync(),
                LowStockAlerts = lowStockAlerts,
                TotalQuantityIn = await transactionQuery.Where(x => InboundTypes.Contains(x.Type)).SumAsync(x => x.Qty),
                TotalQuantityOut = await transactionQuery.Where(x => OutboundTypes.Contains(x.Type)).SumAsync(x => x.Qty),
                TotalQuantityWeightIn = await transactionQuery.Where(x => InboundTypes.Contains(x.Type)).SumAsync(x => x.QtyWeight),
                TotalQuantityWeightOut = await transactionQuery.Where(x => OutboundTypes.Contains(x.Type)).SumAsync(x => x.QtyWeight)
            };
        }

        private async Task<List<TodayTransaction>> GetTodayTransactions(DateTimeOffset today, DateTimeOffset tomorrow, DashboardRequest request)
        {
            return await (from trans in _jewelryContext.TbtStockGemTransection
                          join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                          where trans.CreateDate >= today.StartOfDayUtc() && trans.CreateDate < tomorrow.EndOfDayUtc()
                          select new TodayTransaction
                          {
                              Running = trans.Running,
                              Code = trans.Code,
                              GroupName = gem.GroupName,
                              Shape = gem.Shape,
                              Grade = gem.Grade,
                              Type = trans.Type,
                              TypeName = InboundTypes.Contains(trans.Type) ? "IN" : OutboundTypes.Contains(trans.Type) ? "OUT" : "OTHER",
                              Qty = trans.Qty,
                              QtyWeight = trans.QtyWeight,
                              JobOrPo = trans.JobOrPo,
                              Status = trans.Stastus,
                              CreateDate = trans.CreateDate,
                              CreateBy = trans.CreateBy
                          })
                .OrderByDescending(x => x.CreateDate)
                .Take(50)
                .ToListAsync();
        }

        private async Task<List<TodayPriceChange>> GetTodayPriceChanges(DateTimeOffset today, DateTimeOffset tomorrow, DashboardRequest request)
        {
            return await (from price in _jewelryContext.TbtStockGemTransectionPrice
                          join gem in _jewelryContext.TbtStockGem on price.Code equals gem.Code
                          where price.CreateDate >= today.StartOfDayUtc() && price.CreateDate < tomorrow.EndOfDayUtc()
                          select new TodayPriceChange
                          {
                              Code = price.Code,
                              GroupName = gem.GroupName,
                              PreviousPrice = price.PreviousPrice,
                              NewPrice = price.NewPrice,
                              ChangeAmount = price.NewPrice - price.PreviousPrice,
                              ChangePercentage = price.PreviousPrice > 0 ? ((price.NewPrice - price.PreviousPrice) / price.PreviousPrice) * 100 : 0,
                              ChangeDate = price.CreateDate,
                              ChangeBy = price.CreateBy
                          })
                .OrderByDescending(x => x.ChangeDate)
                .ToListAsync();
        }

        private async Task<List<TodayNewStock>> GetTodayNewStocks(DateTimeOffset today, DateTimeOffset tomorrow, DashboardRequest request)
        {
            return await _jewelryContext.TbtStockGem
                .Where(x => x.CreateDate >= today.StartOfDayUtc() && x.CreateDate < tomorrow.EndOfDayUtc())
                .Select(x => new TodayNewStock
                {
                    Code = x.Code,
                    GroupName = x.GroupName,
                    Shape = x.Shape,
                    Grade = x.Grade,
                    Size = x.Size,
                    Quantity = x.Quantity,
                    QuantityWeight = x.QuantityWeight,
                    Price = x.Price,
                    CreateDate = x.CreateDate,
                    CreateBy = x.CreateBy
                })
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        private async Task<List<TodayLowStock>> GetTodayLowStocks(DashboardRequest request)
        {
            var query = BuildStockQuery(request);

            return await query
                .Where(x => x.Quantity > 0 && x.Quantity <= LowStockThreshold)
                .Select(x => new TodayLowStock
                {
                    Code = x.Code,
                    GroupName = x.GroupName,
                    Shape = x.Shape,
                    Grade = x.Grade,
                    CurrentQuantity = x.Quantity,
                    CurrentQuantityWeight = x.QuantityWeight,
                    MinimumLevel = LowStockThreshold, // This should come from a configuration table
                    AlertLevel = x.Quantity <= 5 ? "CRITICAL" : "LOW"
                })
                .OrderBy(x => x.CurrentQuantity)
                .ToListAsync();
        }

        // Placeholder methods for weekly and monthly reports
        // These would need similar detailed implementations

        private async Task<WeeklyStockSummary> GetWeeklySummary(DateTimeOffset startOfWeek, DateTimeOffset endOfWeek, DashboardRequest request)
        {
            var transactionQuery = from trans in _jewelryContext.TbtStockGemTransection
                                    join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                                    where trans.CreateDate >= startOfWeek.StartOfDayUtc() && trans.CreateDate < endOfWeek.EndOfDayUtc()
                                          && (request.GroupName == null || request.GroupName.Length == 0 || request.GroupName.Contains(gem.GroupName))
                                          && (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape))
                                          && (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                                    select new { trans, gem };

            var transactions = await transactionQuery.ToListAsync();

            var totalTransactions = transactions.Count;

            var totalPriceChanges = await _jewelryContext.TbtStockGemTransectionPrice
                .Where(x => x.CreateDate >= startOfWeek.StartOfDayUtc() && x.CreateDate < endOfWeek.EndOfDayUtc())
                .CountAsync();

            var newStockItems = await _jewelryContext.TbtStockGem
                .Where(x => x.CreateDate >= startOfWeek.StartOfDayUtc() && x.CreateDate < endOfWeek.EndOfDayUtc())
                .CountAsync();

            var inbound = transactions.Where(x => InboundTypes.Contains(x.trans.Type)).ToList();
            var outbound = transactions.Where(x => OutboundTypes.Contains(x.trans.Type)).ToList();

            var totalQtyIn = inbound.Sum(x => x.trans.Qty);
            var totalQtyOut = outbound.Sum(x => x.trans.Qty);
            var totalQtyWeightIn = inbound.Sum(x => x.trans.QtyWeight);
            var totalQtyWeightOut = outbound.Sum(x => x.trans.QtyWeight);

            const int daysInWeek = 7;
            var averageTransactionsPerDay = (decimal)totalTransactions / daysInWeek;

            var dayGroups = transactions
                .GroupBy(x => (int)x.trans.CreateDate.DayOfWeek)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            // PeakTransactionDay/LowestTransactionDay ใช้ค่า DayOfWeek แบบ .NET (0=Sunday...6=Saturday)
            var peakTransactionDay = dayGroups.Any() ? dayGroups.First().Day : 0;
            var lowestTransactionDay = dayGroups.Any() ? dayGroups.Last().Day : 0;

            var weekClosingValue = await BuildStockQuery(request)
                .Select(x => x.PriceQty > 0 ? (x.Quantity * x.PriceQty) : (x.QuantityWeight * x.Price))
                .SumAsync();

            var inboundValue = inbound.Sum(x => x.gem.PriceQty > 0 ? (x.trans.Qty * x.gem.PriceQty) : (x.trans.QtyWeight * x.gem.Price));
            var outboundValue = outbound.Sum(x => x.gem.PriceQty > 0 ? (x.trans.Qty * x.gem.PriceQty) : (x.trans.QtyWeight * x.gem.Price));
            var netValueChange = inboundValue - outboundValue;

            // WeekOpeningValue เป็นค่าประมาณ: มูลค่าคลังปัจจุบันหักด้วยการเปลี่ยนแปลงสุทธิของสัปดาห์ (ไม่มี historical snapshot รายวันให้ใช้ยอดยกมาจริง)
            var weekOpeningValue = weekClosingValue - netValueChange;

            return new WeeklyStockSummary
            {
                TotalTransactions = totalTransactions,
                TotalPriceChanges = totalPriceChanges,
                NewStockItems = newStockItems,
                WeekOpeningValue = weekOpeningValue,
                WeekClosingValue = weekClosingValue,
                NetValueChange = netValueChange,
                TotalQuantityIn = totalQtyIn,
                TotalQuantityOut = totalQtyOut,
                TotalQuantityWeightIn = totalQtyWeightIn,
                TotalQuantityWeightOut = totalQtyWeightOut,
                AverageTransactionsPerDay = averageTransactionsPerDay,
                PeakTransactionDay = peakTransactionDay,
                LowestTransactionDay = lowestTransactionDay
            };
        }

        private async Task<List<DailyMovement>> GetDailyMovements(DateTimeOffset startOfWeek, DateTimeOffset endOfWeek, DashboardRequest request)
        {
            var transactionQuery = from trans in _jewelryContext.TbtStockGemTransection
                                    join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                                    where trans.CreateDate >= startOfWeek.StartOfDayUtc() && trans.CreateDate < endOfWeek.EndOfDayUtc()
                                          && (request.GroupName == null || request.GroupName.Length == 0 || request.GroupName.Contains(gem.GroupName))
                                          && (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape))
                                          && (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                                    select new { trans, gem };

            var transactions = await transactionQuery.ToListAsync();

            var priceChangeCounts = await _jewelryContext.TbtStockGemTransectionPrice
                .Where(x => x.CreateDate >= startOfWeek.StartOfDayUtc() && x.CreateDate < endOfWeek.EndOfDayUtc())
                .GroupBy(x => x.CreateDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new List<DailyMovement>();
            var weekStartDate = startOfWeek.Date;

            for (int i = 0; i < 7; i++)
            {
                var date = weekStartDate.AddDays(i);
                var dayTransactions = transactions.Where(x => x.trans.CreateDate.Date == date).ToList();
                var inbound = dayTransactions.Where(x => InboundTypes.Contains(x.trans.Type)).ToList();
                var outbound = dayTransactions.Where(x => OutboundTypes.Contains(x.trans.Type)).ToList();

                var totalQtyIn = inbound.Sum(x => x.trans.Qty);
                var totalQtyOut = outbound.Sum(x => x.trans.Qty);
                var totalQtyWeightIn = inbound.Sum(x => x.trans.QtyWeight);
                var totalQtyWeightOut = outbound.Sum(x => x.trans.QtyWeight);

                var totalValue = dayTransactions.Sum(x => x.gem.PriceQty > 0 ? (x.trans.Qty * x.gem.PriceQty) : (x.trans.QtyWeight * x.gem.Price));
                var priceChangeCount = priceChangeCounts.FirstOrDefault(x => x.Date == date)?.Count ?? 0;

                result.Add(new DailyMovement
                {
                    Date = date,
                    DayOfWeek = date.DayOfWeek.ToString(),
                    TransactionCount = dayTransactions.Count,
                    TotalQuantityIn = totalQtyIn,
                    TotalQuantityOut = totalQtyOut,
                    TotalQuantityWeightIn = totalQtyWeightIn,
                    TotalQuantityWeightOut = totalQtyWeightOut,
                    NetQuantityChange = totalQtyIn - totalQtyOut,
                    NetQuantityWeightChange = totalQtyWeightIn - totalQtyWeightOut,
                    PriceChanges = priceChangeCount,
                    TotalValue = totalValue
                });
            }

            return result;
        }

        private async Task<List<WeeklyTopMovement>> GetWeeklyTopMovements(DateTimeOffset startOfWeek, DateTimeOffset endOfWeek, DashboardRequest request)
        {
            var transactionQuery = from trans in _jewelryContext.TbtStockGemTransection
                                    join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                                    where trans.CreateDate >= startOfWeek.StartOfDayUtc() && trans.CreateDate < endOfWeek.EndOfDayUtc()
                                          && (request.GroupName == null || request.GroupName.Length == 0 || request.GroupName.Contains(gem.GroupName))
                                          && (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape))
                                          && (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                                    orderby trans.CreateDate
                                    select new { trans, gem };

            var transactions = await transactionQuery.ToListAsync();

            var movements = transactions
                .GroupBy(x => new { x.gem.Code, x.gem.GroupName, x.gem.Shape, x.gem.Grade })
                .Select(g =>
                {
                    var totalIn = g.Where(x => InboundTypes.Contains(x.trans.Type)).Sum(x => x.trans.Qty);
                    var totalOut = g.Where(x => OutboundTypes.Contains(x.trans.Type)).Sum(x => x.trans.Qty);
                    var totalConsumed = g.Where(x => ConsumedTypes.Contains(x.trans.Type)).Sum(x => x.trans.Qty);

                    string movementType;
                    if (totalIn >= totalOut)
                        movementType = "HIGH_IN";
                    else if (totalConsumed * 2 >= totalOut)
                        movementType = "HIGH_USAGE";
                    else
                        movementType = "HIGH_OUT";

                    var weekStartQuantity = g.First().trans.PreviousRemainQty ?? 0;
                    var weekEndQuantity = g.Last().trans.PointRemianQty ?? 0;

                    return new WeeklyTopMovement
                    {
                        Code = g.Key.Code,
                        GroupName = g.Key.GroupName,
                        Shape = g.Key.Shape,
                        Grade = g.Key.Grade,
                        TransactionCount = g.Count(),
                        TotalQuantityMoved = g.Sum(x => x.trans.Qty),
                        TotalQuantityWeightMoved = g.Sum(x => x.trans.QtyWeight),
                        WeekStartQuantity = weekStartQuantity,
                        WeekEndQuantity = weekEndQuantity,
                        QuantityChange = weekEndQuantity - weekStartQuantity,
                        MovementType = movementType
                    };
                })
                .OrderByDescending(x => x.TotalQuantityMoved + x.TotalQuantityWeightMoved)
                .Take(10)
                .ToList();

            return movements;
        }

        private async Task<List<WeeklyPerformance>> GetWeeklyPerformance(DateTimeOffset startOfWeek, DateTimeOffset endOfWeek, DashboardRequest request)
        {
            var transactionQuery = from trans in _jewelryContext.TbtStockGemTransection
                                    join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                                    where trans.CreateDate >= startOfWeek.StartOfDayUtc() && trans.CreateDate < endOfWeek.EndOfDayUtc()
                                          && (request.GroupName == null || request.GroupName.Length == 0 || request.GroupName.Contains(gem.GroupName))
                                          && (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape))
                                          && (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                                    select new { trans, gem };

            var transactions = await transactionQuery.ToListAsync();

            var priceChangeQuery = from price in _jewelryContext.TbtStockGemTransectionPrice
                                    join gem in _jewelryContext.TbtStockGem on price.Code equals gem.Code
                                    where price.CreateDate >= startOfWeek.StartOfDayUtc() && price.CreateDate < endOfWeek.EndOfDayUtc()
                                          && (request.GroupName == null || request.GroupName.Length == 0 || request.GroupName.Contains(gem.GroupName))
                                          && (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape))
                                          && (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                                    select new { price, gem };

            var priceChanges = await priceChangeQuery.ToListAsync();

            var priceChangesByGroup = priceChanges
                .GroupBy(x => x.gem.GroupName)
                .ToDictionary(g => g.Key, g => new
                {
                    Count = g.Count(),
                    AverageChangePercent = g.Average(x => x.price.PreviousPrice > 0 ? ((x.price.NewPrice - x.price.PreviousPrice) / x.price.PreviousPrice) * 100 : 0)
                });

            var performance = transactions
                .GroupBy(x => x.gem.GroupName)
                .Select(g =>
                {
                    var transactionCount = g.Count();
                    var totalValue = g.Sum(x => x.gem.PriceQty > 0 ? (x.trans.Qty * x.gem.PriceQty) : (x.trans.QtyWeight * x.gem.Price));
                    var totalQtyIn = g.Where(x => InboundTypes.Contains(x.trans.Type)).Sum(x => x.trans.Qty);
                    var totalQtyOut = g.Where(x => OutboundTypes.Contains(x.trans.Type)).Sum(x => x.trans.Qty);
                    var totalQtyWeightIn = g.Where(x => InboundTypes.Contains(x.trans.Type)).Sum(x => x.trans.QtyWeight);
                    var totalQtyWeightOut = g.Where(x => OutboundTypes.Contains(x.trans.Type)).Sum(x => x.trans.QtyWeight);

                    priceChangesByGroup.TryGetValue(g.Key, out var priceInfo);

                    return new WeeklyPerformance
                    {
                        GroupName = g.Key,
                        TransactionCount = transactionCount,
                        TotalValue = totalValue,
                        AverageTransactionValue = transactionCount > 0 ? totalValue / transactionCount : 0,
                        QuantityTurnover = totalQtyIn + totalQtyOut,
                        QuantityWeightTurnover = totalQtyWeightIn + totalQtyWeightOut,
                        PriceChanges = priceInfo?.Count ?? 0,
                        AveragePriceChange = priceInfo?.AverageChangePercent ?? 0
                    };
                })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            return performance;
        }

        private async Task<List<WeeklyTrendAnalysis>> GetWeeklyTrendAnalysis(DateTimeOffset startOfWeek, DateTimeOffset endOfWeek, DashboardRequest request)
        {
            var previousStart = startOfWeek.AddDays(-7);
            var previousEnd = startOfWeek;

            var currentQuery = from trans in _jewelryContext.TbtStockGemTransection
                                join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                                where trans.CreateDate >= startOfWeek.StartOfDayUtc() && trans.CreateDate < endOfWeek.EndOfDayUtc()
                                      && (request.GroupName == null || request.GroupName.Length == 0 || request.GroupName.Contains(gem.GroupName))
                                      && (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape))
                                      && (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                                select new { trans, gem };

            var previousQuery = from trans in _jewelryContext.TbtStockGemTransection
                                 join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                                 where trans.CreateDate >= previousStart.StartOfDayUtc() && trans.CreateDate < previousEnd.EndOfDayUtc()
                                       && (request.GroupName == null || request.GroupName.Length == 0 || request.GroupName.Contains(gem.GroupName))
                                       && (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape))
                                       && (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                                 select new { trans, gem };

            var currentTransactions = await currentQuery.ToListAsync();
            var previousTransactions = await previousQuery.ToListAsync();

            var currentByGroup = currentTransactions
                .GroupBy(x => x.gem.GroupName)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.trans.Qty) + g.Sum(x => x.trans.QtyWeight));

            var previousByGroup = previousTransactions
                .GroupBy(x => x.gem.GroupName)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.trans.Qty) + g.Sum(x => x.trans.QtyWeight));

            var groupNames = currentByGroup.Keys.Union(previousByGroup.Keys).Distinct();

            var result = new List<WeeklyTrendAnalysis>();
            foreach (var groupName in groupNames)
            {
                var currentVolume = currentByGroup.TryGetValue(groupName, out var cv) ? cv : 0;
                var previousVolume = previousByGroup.TryGetValue(groupName, out var pv) ? pv : 0;

                decimal changePercentage;
                if (previousVolume > 0)
                    changePercentage = ((currentVolume - previousVolume) / previousVolume) * 100;
                else if (currentVolume > 0)
                    changePercentage = 100;
                else
                    changePercentage = 0;

                string trendDirection;
                if (Math.Abs(changePercentage) < 5)
                    trendDirection = "STABLE";
                else if (changePercentage > 0)
                    trendDirection = "UP";
                else
                    trendDirection = "DOWN";

                var trendIndicator = trendDirection switch
                {
                    "UP" => "INCREASING_DEMAND",
                    "DOWN" => "DECREASING_DEMAND",
                    _ => "STABLE_DEMAND"
                };

                result.Add(new WeeklyTrendAnalysis
                {
                    Category = "GROUP",
                    CategoryValue = groupName,
                    TrendDirection = trendDirection,
                    ChangePercentage = changePercentage,
                    TrendIndicator = trendIndicator,
                    WeekOverWeekChange = currentVolume - previousVolume
                });
            }

            return result.OrderByDescending(x => Math.Abs(x.ChangePercentage)).ToList();
        }

        private async Task<MonthlyStockSummary> GetMonthlySummary(DateTimeOffset startOfMonth, DateTimeOffset endOfMonth, DashboardRequest request)
        {
            var transactionQuery = _jewelryContext.TbtStockGemTransection
                .Where(x => x.CreateDate >= startOfMonth.StartOfDayUtc() && x.CreateDate < endOfMonth.EndOfDayUtc());

            var completedTransactions = transactionQuery.Where(x => x.Stastus == "completed");

            var priceChangeQuery = _jewelryContext.TbtStockGemTransectionPrice
                .Where(x => x.CreateDate >= startOfMonth.StartOfDayUtc() && x.CreateDate < endOfMonth.EndOfDayUtc());

            var newStockQuery = _jewelryContext.TbtStockGem
                .Where(x => x.CreateDate >= startOfMonth.StartOfDayUtc() && x.CreateDate < endOfMonth.EndOfDayUtc());

            // Calculate totals from completed transactions only
            var inboundTransactions = completedTransactions.Where(x => InboundTypes.Contains(x.Type));
            var outboundTransactions = completedTransactions.Where(x => OutboundTypes.Contains(x.Type));

            var totalQtyIn = await inboundTransactions.SumAsync(x => x.Qty);
            var totalQtyOut = await outboundTransactions.SumAsync(x => x.Qty);
            var totalQtyWeightIn = await inboundTransactions.SumAsync(x => x.QtyWeight);
            var totalQtyWeightOut = await outboundTransactions.SumAsync(x => x.QtyWeight);

            var totalTransactions = await completedTransactions.CountAsync();
            var totalPriceChanges = await priceChangeQuery.CountAsync();
            var newStockItems = await newStockQuery.CountAsync();

            // Calculate average transactions per day
            var daysInMonth = (endOfMonth - startOfMonth).Days;
            var averageTransactionsPerDay = daysInMonth > 0 ? (decimal)totalTransactions / daysInMonth : 0;

            // Calculate supplier costs
            var totalSupplierCost = await completedTransactions.SumAsync(x => x.SupplierCost ?? 0);
            var averageSupplierCost = totalTransactions > 0 ? totalSupplierCost / totalTransactions : 0;

            return new MonthlyStockSummary
            {
                TotalTransactions = totalTransactions,
                TotalPriceChanges = totalPriceChanges,
                NewStockItems = newStockItems,
                TotalQuantityIn = totalQtyIn,
                TotalQuantityOut = totalQtyOut,
                TotalQuantityWeightIn = totalQtyWeightIn,
                TotalQuantityWeightOut = totalQtyWeightOut,
                AverageTransactionsPerDay = averageTransactionsPerDay,
                TotalSupplierCost = totalSupplierCost,
                AverageSupplierCost = averageSupplierCost,
                // TODO: Implement more sophisticated calculations for other fields
                MonthOpeningValue = 0,
                MonthClosingValue = 0,
                NetValueChange = 0,
                MonthOverMonthGrowth = 0,
                InventoryTurnoverRatio = 0,
                PeakTransactionWeek = 0,
                LowestTransactionWeek = 0
            };
        }

        private async Task<List<WeeklyComparison>> GetWeeklyComparisons(DateTimeOffset startOfMonth, DateTimeOffset endOfMonth, DashboardRequest request)
        {
            var weeklyData = new List<WeeklyComparison>();

            // Get all weeks in the month
            var current = startOfMonth;
            var weekNumber = 1;

            while (current < endOfMonth)
            {
                var weekEnd = current.AddDays(7);
                if (weekEnd > endOfMonth) weekEnd = endOfMonth;

                var weeklyTransactions = await _jewelryContext.TbtStockGemTransection
                    .Where(x => x.CreateDate >= current && x.CreateDate < weekEnd && x.Stastus == "completed")
                    .ToListAsync();

                var inboundTransactions = weeklyTransactions.Where(x => InboundTypes.Contains(x.Type));
                var outboundTransactions = weeklyTransactions.Where(x => OutboundTypes.Contains(x.Type));

                var priceChanges = await _jewelryContext.TbtStockGemTransectionPrice
                    .Where(x => x.CreateDate >= current && x.CreateDate < weekEnd)
                    .CountAsync();

                weeklyData.Add(new WeeklyComparison
                {
                    WeekNumber = weekNumber,
                    WeekStartDate = current.DateTime,
                    WeekEndDate = weekEnd.AddDays(-1).DateTime,
                    TransactionCount = weeklyTransactions.Count,
                    QuantityIn = inboundTransactions.Sum(x => x.Qty),
                    QuantityOut = outboundTransactions.Sum(x => x.Qty),
                    QuantityWeightIn = inboundTransactions.Sum(x => x.QtyWeight),
                    QuantityWeightOut = outboundTransactions.Sum(x => x.QtyWeight),
                    PriceChanges = priceChanges,
                    TotalValue = weeklyTransactions.Sum(x => x.SupplierCost ?? 0),
                    WeekOverWeekChange = 0 // TODO: Calculate week-over-week change
                });

                current = weekEnd;
                weekNumber++;
            }

            return weeklyData;
        }

        private async Task<List<MonthlyTopPerformer>> GetMonthlyTopPerformers(DateTimeOffset startOfMonth, DateTimeOffset endOfMonth, DashboardRequest request)
        {
            var completedTransactions = await (from trans in _jewelryContext.TbtStockGemTransection
                                               join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                                               where trans.CreateDate >= startOfMonth.StartOfDayUtc() && trans.CreateDate < endOfMonth.EndOfDayUtc()
                                                     && trans.Stastus == "completed"
                                               select new { trans, gem })
                                              .ToListAsync();

            // Group by gem type and calculate performance metrics
            var gemPerformance = completedTransactions
                .GroupBy(x => new { x.gem.Code, x.gem.GroupName, x.gem.Shape, x.gem.Grade })
                .Select(g => new MonthlyTopPerformer
                {
                    Code = g.Key.Code,
                    GroupName = g.Key.GroupName,
                    Shape = g.Key.Shape,
                    Grade = g.Key.Grade,
                    TransactionCount = g.Count(),
                    TotalQuantityMoved = g.Sum(x => x.trans.Qty),
                    TotalQuantityWeightMoved = g.Sum(x => x.trans.QtyWeight),
                    TotalValue = g.Sum(x => x.trans.SupplierCost ?? 0),
                    MonthStartQuantity = g.FirstOrDefault()?.trans.PreviousRemainQty ?? 0,
                    MonthEndQuantity = g.LastOrDefault()?.trans.PointRemianQty ?? 0,
                    TurnoverRate = 0, // TODO: Calculate turnover rate
                    PerformanceType = "MOST_ACTIVE",
                    Ranking = 0
                })
                .OrderByDescending(x => x.TransactionCount)
                .ThenByDescending(x => x.TotalQuantityMoved)
                .Take(20)
                .ToList();

            // Assign rankings
            for (int i = 0; i < gemPerformance.Count; i++)
            {
                gemPerformance[i].Ranking = i + 1;
            }

            return gemPerformance;
        }

        private async Task<List<MonthlyInventoryAnalysis>> GetMonthlyInventoryAnalysis(DateTimeOffset startOfMonth, DateTimeOffset endOfMonth, DashboardRequest request)
        {
            var completedTransactions = await (from trans in _jewelryContext.TbtStockGemTransection
                                               join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                                               where trans.CreateDate >= startOfMonth.StartOfDayUtc() && trans.CreateDate < endOfMonth.EndOfDayUtc()
                                                     && trans.Stastus == "completed"
                                               select new { trans, gem })
                                              .ToListAsync();

            var daysInMonth = (endOfMonth - startOfMonth).Days;

            // Group by gem characteristics and calculate inventory metrics
            var inventoryAnalysis = completedTransactions
                .GroupBy(x => new { x.gem.GroupName, x.gem.Shape, x.gem.Grade })
                .Select(g =>
                {
                    var currentStockQuantity = g.GroupBy(x => x.gem.Code).Select(gg => gg.First().gem.Quantity).Sum();
                    var outboundQuantity = g.Where(x => OutboundTypes.Contains(x.trans.Type)).Sum(x => x.trans.Qty);
                    var usageRatePerDay = daysInMonth > 0 ? outboundQuantity / daysInMonth : 0;

                    decimal inventoryDays;
                    string inventoryStatus;
                    if (usageRatePerDay <= 0)
                    {
                        inventoryDays = 0;
                        inventoryStatus = "STAGNANT"; // ไม่มีการเบิกใช้ในเดือนนี้ คำนวณจำนวนวันคงคลังไม่ได้
                    }
                    else
                    {
                        inventoryDays = currentStockQuantity / usageRatePerDay;
                        inventoryStatus = inventoryDays < InventoryDaysLowThreshold
                            ? "LOW"
                            : inventoryDays <= InventoryDaysExcessThreshold
                                ? "OPTIMAL"
                                : "EXCESS";
                    }

                    return new MonthlyInventoryAnalysis
                    {
                        GroupName = g.Key.GroupName,
                        Shape = g.Key.Shape,
                        Grade = g.Key.Grade,
                        ItemCount = g.Select(x => x.gem.Code).Distinct().Count(),
                        TotalQuantity = g.Sum(x => x.trans.Qty),
                        TotalQuantityWeight = g.Sum(x => x.trans.QtyWeight),
                        TotalValue = g.Sum(x => x.trans.SupplierCost ?? 0),
                        AverageQuantityPerItem = g.Select(x => x.gem.Code).Distinct().Count() > 0 ?
                            g.Sum(x => x.trans.Qty) / g.Select(x => x.gem.Code).Distinct().Count() : 0,
                        AveragePricePerUnit = g.Where(x => x.gem.Price > 0).Any() ? g.Where(x => x.gem.Price > 0).Average(x => x.gem.Price) : 0,
                        InventoryDays = inventoryDays,
                        InventoryStatus = inventoryStatus,
                        RecommendedOrderQuantity = 0, // TODO: Calculate based on demand
                        MonthOverMonthChange = 0 // TODO: Calculate month-over-month change
                    };
                })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            return inventoryAnalysis;
        }

        private async Task<List<MonthlyPriceAnalysis>> GetMonthlyPriceAnalysis(DateTimeOffset startOfMonth, DateTimeOffset endOfMonth, DashboardRequest request)
        {
            var priceQuery = from price in _jewelryContext.TbtStockGemTransectionPrice
                              join gem in _jewelryContext.TbtStockGem on price.Code equals gem.Code
                              where price.CreateDate >= startOfMonth.StartOfDayUtc() && price.CreateDate < endOfMonth.EndOfDayUtc()
                                    && (request.GroupName == null || request.GroupName.Length == 0 || request.GroupName.Contains(gem.GroupName))
                                    && (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape))
                                    && (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                              select new { price, gem };

            var priceChanges = await priceQuery.ToListAsync();

            var analysis = priceChanges
                .GroupBy(x => new { x.gem.GroupName, x.gem.Shape, x.gem.Grade })
                .Select(g =>
                {
                    var changePercentages = g
                        .Where(x => x.price.PreviousPrice > 0)
                        .Select(x => ((x.price.NewPrice - x.price.PreviousPrice) / x.price.PreviousPrice) * 100)
                        .ToList();

                    var averageChange = changePercentages.Any() ? changePercentages.Average() : 0;
                    var variance = changePercentages.Any()
                        ? changePercentages.Sum(x => (x - averageChange) * (x - averageChange)) / changePercentages.Count
                        : 0;
                    var standardDeviation = (decimal)Math.Sqrt((double)variance);

                    string priceTrend;
                    if (standardDeviation > 15)
                        priceTrend = "VOLATILE";
                    else if (averageChange > 5)
                        priceTrend = "INCREASING";
                    else if (averageChange < -5)
                        priceTrend = "DECREASING";
                    else
                        priceTrend = "STABLE";

                    return new MonthlyPriceAnalysis
                    {
                        GroupName = g.Key.GroupName,
                        Shape = g.Key.Shape,
                        Grade = g.Key.Grade,
                        PriceChangeCount = g.Count(),
                        AveragePriceStart = g.Average(x => x.price.PreviousPrice),
                        AveragePriceEnd = g.Average(x => x.price.NewPrice),
                        PriceVolatility = standardDeviation,
                        MaxPriceIncrease = changePercentages.Any() ? changePercentages.Max() : 0,
                        MaxPriceDecrease = changePercentages.Any() ? changePercentages.Min() : 0,
                        PriceTrend = priceTrend,
                        StandardDeviation = standardDeviation,
                        MostRecentPriceChange = g.Max(x => x.price.CreateDate)
                    };
                })
                .OrderByDescending(x => x.PriceChangeCount)
                .ToList();

            return analysis;
        }

        private async Task<List<MonthlySupplierAnalysis>> GetMonthlySupplierAnalysis(DateTimeOffset startOfMonth, DateTimeOffset endOfMonth, DashboardRequest request)
        {
            // ข้อมูลจริงมีแต่ type 1 (รับเข้าคลัง [พลอยใหม่]) เท่านั้นที่กรอก SubpplierName ต้อง filter type 1 + ชื่อ supplier ไม่ว่าง
            // มิฉะนั้นจะได้ group ก้อนยักษ์ที่ชื่อ supplier ว่างจาก type อื่นทั้งหมด
            var transactionQuery = from trans in _jewelryContext.TbtStockGemTransection
                                    join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                                    where trans.CreateDate >= startOfMonth.StartOfDayUtc() && trans.CreateDate < endOfMonth.EndOfDayUtc()
                                          && trans.Type == 1
                                          && !string.IsNullOrEmpty(trans.SubpplierName)
                                          && (request.GroupName == null || request.GroupName.Length == 0 || request.GroupName.Contains(gem.GroupName))
                                          && (request.Shape == null || request.Shape.Length == 0 || request.Shape.Contains(gem.Shape))
                                          && (request.Grade == null || request.Grade.Length == 0 || request.Grade.Contains(gem.Grade))
                                    select new { trans, gem };

            var transactions = await transactionQuery.ToListAsync();

            var supplierAnalysis = transactions
                .GroupBy(x => x.trans.SubpplierName)
                .Select(g =>
                {
                    var totalCost = g.Sum(x => x.trans.SupplierCost ?? 0);
                    var totalQuantity = g.Sum(x => x.trans.Qty);
                    var totalQuantityWeight = g.Sum(x => x.trans.QtyWeight);
                    var transactionCount = g.Count();

                    var preferredGemCategory = g
                        .GroupBy(x => x.gem.GroupName)
                        .OrderByDescending(gg => gg.Sum(x => x.trans.Qty) + gg.Sum(x => x.trans.QtyWeight))
                        .Select(gg => gg.Key)
                        .FirstOrDefault() ?? string.Empty;

                    return new MonthlySupplierAnalysis
                    {
                        SupplierName = g.Key,
                        TransactionCount = transactionCount,
                        TotalQuantity = totalQuantity,
                        TotalQuantityWeight = totalQuantityWeight,
                        TotalCost = totalCost,
                        AverageCostPerUnit = totalQuantity > 0 ? totalCost / totalQuantity : 0,
                        AverageCostPerWeight = totalQuantityWeight > 0 ? totalCost / totalQuantityWeight : 0,
                        GemTypes = g.Select(x => x.gem.GroupName).Distinct().ToList(),
                        PreferredGemCategory = preferredGemCategory,
                        DeliveryCount = transactionCount,
                        // ไม่มี data source สำหรับ performance score / reliability rating ในระบบ ปล่อยเป็นค่า default
                        SupplierPerformanceScore = 0,
                        ReliabilityRating = string.Empty
                    };
                })
                .OrderByDescending(x => x.TotalCost)
                .ToList();

            return supplierAnalysis;
        }

        // New method that categorizes results by transaction type (not inbound/outbound)
        public async Task<List<TransactionTypeCategorySummary>> GetTransactionSummariesByType(DashboardRequest request)
        {
            if (!request.StartDate.HasValue || !request.EndDate.HasValue)
            {
                throw new ArgumentException("StartDate and EndDate must be provided.");
            }

            var startDate = request.StartDate.Value.StartOfDayUtc();
            var endDate = request.EndDate.Value.EndOfDayUtc();

            // Step 1: Get TbtStockGemTransection with joined TbtStockGem filter by startDate && endDate
            var transactionsQuery = from trans in _jewelryContext.TbtStockGemTransection
                                    join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                                    where trans.CreateDate >= startDate && trans.CreateDate < endDate
                                    select new { trans, gem };

            var allTransactions = await transactionsQuery.ToListAsync();

            if (!allTransactions.Any())
            {
                return new List<TransactionTypeCategorySummary>();
            }

            // Step 2: Categorized data by TbtStockGemTransection.type map to TransactionTypeCategorySummary
            var transactionTypeSummaries = new List<TransactionTypeCategorySummary>();
            var transactionsByType = allTransactions.GroupBy(x => x.trans.Type);

            foreach (var typeGroup in transactionsByType)
            {
                if (typeGroup.Key == 7)
                {
                    // แยก IDs ออกมาก่อน
                    var type7TransactionIds = typeGroup.Select(tg => tg.trans.Id).ToList();

                    // Step 3: In type 7 must do join tbtproductionplan to, then categorized data type by tbtproductionplan.type and tbtproductionplan.typeSize
                    var type7TransactionsWithPlan = await (from trans in _jewelryContext.TbtStockGemTransection
                                                           join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                                                           join plan in _jewelryContext.TbtProductionPlan
                                                           on new { Wo = trans.ProductionPlanWo, WoNumber = trans.ProductionPlanWoNumber ?? 0 }
                                                           equals new { Wo = plan.Wo, WoNumber = plan.WoNumber }
                                                           where trans.CreateDate >= startDate && trans.CreateDate < endDate
                                                                 && trans.Type == 7
                                                                 && type7TransactionIds.Contains(trans.Id) // ใช้ Contains แทน Any
                                                           select new { trans, gem, plan })
                                                          .ToListAsync();

                    var type7Summary = new TransactionTypeCategorySummary
                    {
                        Type = typeGroup.Key,
                        TypeName = StockGemServiceStatic.GetTransactionTypeName(typeGroup.Key),
                        TotalTransactions = typeGroup.Count(),
                        TotalQuantity = typeGroup.Sum(x => x.trans.Qty),
                        TotalWeight = typeGroup.Sum(x => x.trans.QtyWeight),
                        //TotalCost = typeGroup.Sum(x => x.trans.SupplierCost ?? 0),

                        // Group by groupname, production.type, production.typeSize for Type 7
                        GemDetails = type7TransactionsWithPlan
                            .GroupBy(x => new {
                                x.gem.GroupName,
                                x.plan.Type,
                                x.plan.TypeSize
                            })
                            .Select(gemGroup => new GemTransactionDetail
                            {
                                Code = string.Empty,
                                GroupName = gemGroup.Key.GroupName,
                                TransactionCount = gemGroup.Count(),
                                TotalQuantity = gemGroup.Sum(x => x.trans.Qty),
                                TotalWeight = gemGroup.Sum(x => x.trans.QtyWeight),
                                //TotalCost = gemGroup.Sum(x => x.trans.SupplierCost ?? 0),
                                CurrentQuantity = gemGroup.FirstOrDefault()?.gem.Quantity ?? 0,
                                CurrentWeight = gemGroup.FirstOrDefault()?.gem.QuantityWeight ?? 0,
                                LastTransactionDate = gemGroup.Max(x => x.trans.CreateDate),

                                // Production categorization for Type 7
                                ProductionType = gemGroup.Key.Type,
                                ProductionTypeName = gemGroup.Key.TypeSize
                            })
                            .OrderBy(x => x.ProductionType)
                            .ThenBy(x => x.GroupName)
                            .ToList()
                    };

                    transactionTypeSummaries.Add(type7Summary);
                }
                else
                {
                    // For other types: Group by groupname only
                    var standardSummary = new TransactionTypeCategorySummary
                    {
                        Type = typeGroup.Key,
                        TypeName = StockGemServiceStatic.GetTransactionTypeName(typeGroup.Key),
                        TotalTransactions = typeGroup.Count(),
                        TotalQuantity = typeGroup.Sum(x => x.trans.Qty),
                        TotalWeight = typeGroup.Sum(x => x.trans.QtyWeight), // เอา comment ออก
                        TotalCost = typeGroup.Sum(x => x.trans.SupplierCost ?? 0),

                        GemDetails = typeGroup
                            .GroupBy(x => x.gem.GroupName)
                            .Select(gemGroup => new GemTransactionDetail
                            {
                                Code = string.Empty,
                                GroupName = gemGroup.Key,
                                TransactionCount = gemGroup.Count(),
                                TotalQuantity = gemGroup.Sum(x => x.trans.Qty),
                                TotalWeight = gemGroup.Sum(x => x.trans.QtyWeight),
                                //TotalCost = gemGroup.Sum(x => x.trans.SupplierCost ?? 0),
                                CurrentQuantity = gemGroup.FirstOrDefault()?.gem.Quantity ?? 0,
                                CurrentWeight = gemGroup.FirstOrDefault()?.gem.QuantityWeight ?? 0,
                                LastTransactionDate = gemGroup.Max(x => x.trans.CreateDate)
                            })
                            .OrderBy(x => x.GroupName)
                            .ToList()
                    };

                    transactionTypeSummaries.Add(standardSummary);
                }
            }

            return transactionTypeSummaries.OrderBy(x => x.Type).ToList();
        }

        public async Task<AgingReportResponse> GetAgingReport(DashboardRequest request)
        {
            var stockQuery = BuildStockQuery(request)
                .Where(x => x.Quantity > 0 || x.QuantityWeight > 0);

            var lastTxQuery = _jewelryContext.TbtStockGemTransection
                .GroupBy(x => x.Code)
                .Select(g => new { Code = g.Key, LastTx = g.Max(x => x.CreateDate) });

            var raw = await (from s in stockQuery
                             join t in lastTxQuery on s.Code equals t.Code into txGroup
                             from t in txGroup.DefaultIfEmpty()
                             select new
                             {
                                 s.Quantity,
                                 s.QuantityWeight,
                                 s.Price,
                                 s.PriceQty,
                                 LastTx = (DateTime?)t.LastTx
                             }).ToListAsync();

            var now = DateTime.UtcNow;

            var items = raw.Select(x => new
            {
                x.Quantity,
                x.QuantityWeight,
                Value = x.PriceQty > 0 ? (x.Quantity * x.PriceQty) : (x.QuantityWeight * x.Price),
                BucketKey = GetAgingBucketKey(x.LastTx, now)
            }).ToList();

            var grouped = items
                .GroupBy(x => x.BucketKey)
                .ToDictionary(g => g.Key, g => new AgingBucket
                {
                    BucketKey = g.Key,
                    GemCodes = g.Count(),
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalQuantityWeight = g.Sum(x => x.QuantityWeight),
                    TotalValue = g.Sum(x => x.Value)
                });

            var buckets = AgingBucketDefinitions
                .Select(def =>
                {
                    var bucket = grouped.TryGetValue(def.Key, out var found) ? found : new AgingBucket { BucketKey = def.Key };
                    bucket.SortOrder = def.SortOrder;
                    return bucket;
                })
                .OrderBy(b => b.SortOrder)
                .ToList();

            var deadStock = buckets.First(b => b.BucketKey == "over365");

            return new AgingReportResponse
            {
                Buckets = buckets,
                TotalGemCodes = items.Count,
                TotalValue = items.Sum(x => x.Value),
                DeadStockCodes = deadStock.GemCodes,
                DeadStockValue = deadStock.TotalValue
            };
        }

        private static string GetAgingBucketKey(DateTime? lastTx, DateTime now)
        {
            if (!lastTx.HasValue)
                return "never";

            var days = (now - lastTx.Value).TotalDays;

            if (days <= 30) return "d0_30";
            if (days <= 90) return "d31_90";
            if (days <= 180) return "d91_180";
            if (days <= 365) return "d181_365";
            return "over365";
        }

        public List<MovementReportResponse> GetMovementReport(MovementReportRequest request)
        {
            var nowOffset = DateTimeOffset.UtcNow;
            var rangeStart = (request.StartDate ?? nowOffset.AddDays(-90)).StartOfDayUtc().UtcDateTime;
            var rangeEnd = (request.EndDate ?? nowOffset).EndOfDayUtc().UtcDateTime;
            var daysInRange = Math.Max(1, (rangeEnd.Date - rangeStart.Date).Days + 1);
            var monthsInRange = Math.Max(1m, (decimal)daysInRange / 30m);

            var stockQuery = _jewelryContext.TbtStockGem.AsNoTracking().AsQueryable();

            if (request.GroupName != null && request.GroupName.Length > 0)
                stockQuery = stockQuery.Where(x => request.GroupName.Contains(x.GroupName));

            if (request.Shape != null && request.Shape.Length > 0)
                stockQuery = stockQuery.Where(x => request.Shape.Contains(x.Shape));

            if (request.Grade != null && request.Grade.Length > 0)
                stockQuery = stockQuery.Where(x => request.Grade.Contains(x.Grade));

            if (!string.IsNullOrEmpty(request.Code))
                stockQuery = stockQuery.Where(x => x.Code.Contains(request.Code.ToUpper()));

            var stocks = stockQuery.ToList();
            var stockCodes = stocks.Select(x => x.Code).Distinct().ToList();

            // Materialize raw transactions for the candidate codes only (bounded set), then compute
            // both the all-time last movement date and the in-range aggregates in memory.
            // EF Core 8 cannot be trusted to translate a coalesce(RequestDate, CreateDate) inside a
            // grouped aggregate/left-join reliably, so classification happens after materialization.
            var rawTransactions = _jewelryContext.TbtStockGemTransection
                .AsNoTracking()
                .Where(x => stockCodes.Contains(x.Code))
                .Select(x => new
                {
                    x.Code,
                    x.RequestDate,
                    x.CreateDate,
                    x.Type,
                    x.Qty,
                    x.QtyWeight
                })
                .ToList();

            var txByCode = rawTransactions
                .Select(x => new
                {
                    x.Code,
                    MoveDate = x.RequestDate != default(DateTime) ? x.RequestDate : x.CreateDate,
                    x.Type,
                    x.Qty,
                    x.QtyWeight
                })
                .GroupBy(x => x.Code)
                .ToDictionary(g => g.Key, g => new
                {
                    LastMovementDate = g.Max(x => x.MoveDate),
                    InRange = g.Where(x => x.MoveDate >= rangeStart && x.MoveDate <= rangeEnd).ToList()
                });

            var now = DateTime.UtcNow;
            var result = new List<MovementReportResponse>();

            foreach (var stock in stocks)
            {
                var onHandQuantity = stock.Quantity + stock.QuantityOnProcess;
                var onHandQuantityWeight = stock.QuantityWeight + stock.QuantityWeightOnProcess;

                var hasTxInfo = txByCode.TryGetValue(stock.Code, out var txInfo);

                // Exclude junk rows: no on-hand stock and no transaction has ever existed for this code
                if (onHandQuantity == 0 && onHandQuantityWeight == 0 && !hasTxInfo)
                    continue;

                var transactionCount = hasTxInfo ? txInfo.InRange.Count : 0;
                var quantityIn = hasTxInfo ? txInfo.InRange.Where(x => InboundTypes.Contains(x.Type)).Sum(x => x.Qty) : 0m;
                var quantityWeightIn = hasTxInfo ? txInfo.InRange.Where(x => InboundTypes.Contains(x.Type)).Sum(x => x.QtyWeight) : 0m;
                var quantityOut = hasTxInfo ? txInfo.InRange.Where(x => ConsumedTypes.Contains(x.Type)).Sum(x => x.Qty) : 0m;
                var quantityWeightOut = hasTxInfo ? txInfo.InRange.Where(x => ConsumedTypes.Contains(x.Type)).Sum(x => x.QtyWeight) : 0m;

                DateTime? lastMovementDate = hasTxInfo ? txInfo.LastMovementDate : (DateTime?)null;
                int? daysSinceLastMovement = lastMovementDate.HasValue ? (int)(now - lastMovementDate.Value).TotalDays : (int?)null;

                var avgDailyConsumption = quantityOut / daysInRange;
                decimal? daysOfSupply = avgDailyConsumption > 0 ? onHandQuantity / avgDailyConsumption : (decimal?)null;

                string movementStatus;
                if (transactionCount == 0 || (daysSinceLastMovement.HasValue && daysSinceLastMovement.Value > request.DeadDays))
                {
                    movementStatus = "DEAD";
                }
                else if ((decimal)transactionCount / monthsInRange >= request.FastTxPerMonth)
                {
                    movementStatus = "FAST";
                }
                else
                {
                    movementStatus = "SLOW";
                }

                string stockAlertLevel;
                if (onHandQuantity == 0 && onHandQuantityWeight == 0)
                {
                    stockAlertLevel = "OUT";
                }
                else if (daysOfSupply.HasValue && daysOfSupply.Value < request.CriticalDaysOfSupply)
                {
                    stockAlertLevel = "CRITICAL";
                }
                else if (daysOfSupply.HasValue && daysOfSupply.Value < request.LowDaysOfSupply)
                {
                    stockAlertLevel = "LOW";
                }
                else
                {
                    stockAlertLevel = "OK";
                }

                result.Add(new MovementReportResponse
                {
                    Code = stock.Code,
                    GroupName = stock.GroupName,
                    Shape = stock.Shape,
                    Grade = stock.Grade,
                    Size = stock.Size,
                    Quantity = onHandQuantity,
                    QuantityWeight = onHandQuantityWeight,
                    TransactionCount = transactionCount,
                    QuantityIn = quantityIn,
                    QuantityWeightIn = quantityWeightIn,
                    QuantityOut = quantityOut,
                    QuantityWeightOut = quantityWeightOut,
                    LastMovementDate = lastMovementDate,
                    DaysSinceLastMovement = daysSinceLastMovement,
                    AvgDailyConsumption = avgDailyConsumption,
                    DaysOfSupply = daysOfSupply,
                    MovementStatus = movementStatus,
                    StockAlertLevel = stockAlertLevel,
                    Price = stock.Price,
                    PriceQty = stock.PriceQty
                });
            }

            if (request.MovementStatus != null && request.MovementStatus.Length > 0)
            {
                var statusFilters = request.MovementStatus.Select(x => x.ToUpper()).ToList();

                var movementFilters = statusFilters.Where(x => x == "FAST" || x == "SLOW" || x == "DEAD").ToList();
                var alertFilters = statusFilters.Where(x => x == "LOW" || x == "OUT").ToList();

                result = result.Where(x =>
                    movementFilters.Contains(x.MovementStatus) ||
                    (alertFilters.Contains("LOW") && (x.StockAlertLevel == "CRITICAL" || x.StockAlertLevel == "LOW")) ||
                    (alertFilters.Contains("OUT") && x.StockAlertLevel == "OUT")
                ).ToList();
            }

            return result;
        }

        #endregion

    }

    public static class StockGemServiceStatic
    {
        public static string GetTransactionTypeName(int type)
        {
            return type switch
            {
                1 => "รับเข้าคลัง [พลอยใหม่]",
                2 => "รับเข้าคลัง [พลอยนอกสต๊อก]",
                3 => "รับเข้าคลัง [พลอยคืน]",
                4 => "จ่ายออกคลัง",

                5 => "ยืมออกคลัง",

                6 => "คืนเข้าคลัง",
                7 => "เบิกออกคลัง",
                _ => "อื่นๆ"
            };
        }
    }

    public class TransactionWithPlanDto
    {
        public TbtStockGemTransection Transaction { get; set; }
        public TbtStockGem Gem { get; set; }
        public TbtProductionPlan Plan { get; set; }
    }
}
