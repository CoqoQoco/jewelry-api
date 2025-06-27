using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Gem.Option;
using jewelry.Model.Stock.Gem.Price;
using jewelry.Model.Stock.Gem.PriceEdit;
using jewelry.Model.Stock.Gem.Search;
using jewelry.Model.Stock.Gem.Dashboard;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
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
        IQueryable<SearchGemResponse> SearchGemData(SearchGem request);
        IQueryable<OptionResponse> GroupGemData(OptionRequest request);

        Task<string> Price(PriceEditRequest request);
        IQueryable<TbtStockGemTransectionPrice> PriceHistory(Price request);

        // Dashboard APIs
        Task<DashboardResponse> GetStockGemDashboard(DashboardRequest request);
        Task<TodayReportResponse> GetTodayReport(DashboardRequest request);
        Task<WeeklyReportResponse> GetWeeklyReport(DashboardRequest request);
        Task<MonthlyReportResponse> GetMonthlyReport(DashboardRequest request);
    }
    public class StockGemService : IStockGemService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly bool _valPass = false;
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

        public IQueryable<SearchGemResponse> SearchGemData(SearchGem request)
        {
            var query = (from item in _jewelryContext.TbtStockGem
                         select item).AsNoTracking();
           
            if (request.Id.HasValue)
            {
                query = (from item in query
                         where item.Id == request.Id.Value
                         select item);
            }
            if (!string.IsNullOrEmpty(request.Code))
            {
                query = (from item in query
                         where item.Code.Contains(request.Code.ToUpper())
                         select item);
            }
            if (request.GroupName != null && request.GroupName.Length > 0)
            {
                query = (from item in query
                         where request.GroupName.Contains(item.GroupName)
                         select item);
            }
            if (request.Size != null && request.Size.Length > 0)
            {
                query = (from item in query
                         where request.Size.Contains(item.Size)
                         select item);
            }
            if (request.Shape != null && request.Shape.Length > 0)
            {
                query = (from item in query
                         where request.Shape.Contains(item.Shape)
                         select item);
            }
            if (request.Grade != null && request.Grade.Length > 0)
            {
                query = (from item in query
                         where request.Grade.Contains(item.Grade)
                         select item);
            }

            if (request.TypeCheck != null && request.TypeCheck.Length > 0)
            {
                var typeCheckLower = request.TypeCheck.Select(tc => tc.ToLower()).ToArray();

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
                            });

            return response;
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
            var now = DateTime.UtcNow;
            var startDate = request.StartDate ?? now.Date.AddDays(-30);
            var endDate = request.EndDate ?? now.Date.AddDays(1);

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
            var now = DateTime.UtcNow;
            var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);

            var response = new WeeklyReportResponse
            {
                WeekStartDate = startOfWeek,
                WeekEndDate = endOfWeek,
                WeekNumber = $"Week {GetWeekOfYear(now)}"
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
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var response = new MonthlyReportResponse
            {
                Year = now.Year,
                Month = now.Month,
                MonthName = now.ToString("MMMM"),
                MonthStartDate = startOfMonth,
                MonthEndDate = endOfMonth.AddDays(-1)
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
                    TotalValue = g.Sum(x => x.Quantity * x.Price),
                    TotalOnProcessQuantity = g.Sum(x => x.QuantityOnProcess),
                    TotalOnProcessQuantityWeight = g.Sum(x => x.QuantityWeightOnProcess),
                    AvailableQuantity = g.Sum(x => x.Quantity - x.QuantityOnProcess),
                    AvailableQuantityWeight = g.Sum(x => x.QuantityWeight - x.QuantityWeightOnProcess),
                    LowStockCount = g.Count(x => x.Quantity <= 10), // Assuming 10 as low stock threshold
                    ZeroStockCount = g.Count(x => x.Quantity == 0)
                })
                .FirstOrDefaultAsync();

            return summary ?? new StockSummary();
        }

        private async Task<List<GemCategoryBreakdown>> GetCategoryBreakdown(DashboardRequest request)
        {
            var query = BuildStockQuery(request);

            return await query
                .GroupBy(x => new { x.GroupName, x.Shape, x.Grade })
                .Select(g => new GemCategoryBreakdown
                {
                    GroupName = g.Key.GroupName,
                    Shape = g.Key.Shape,
                    Grade = g.Key.Grade,
                    Count = g.Count(),
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalQuantityWeight = g.Sum(x => x.QuantityWeight),
                    TotalValue = g.Sum(x => x.Quantity * x.Price),
                    AveragePrice = g.Average(x => x.Price)
                })
                .OrderByDescending(x => x.TotalValue)
                .Take(20)
                .ToListAsync();
        }

        private async Task<List<TransactionTrend>> GetTransactionTrends(DateTime startDate, DateTime endDate, DashboardRequest request)
        {
            var transactionQuery = _jewelryContext.TbtStockGemTransection
                .Where(x => x.CreateDate >= startDate && x.CreateDate < endDate);

            if (!string.IsNullOrEmpty(request.GroupName))
            {
                var gemCodes = await _jewelryContext.TbtStockGem
                    .Where(x => x.GroupName == request.GroupName)
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
                    TotalQuantityIn = g.Where(x => x.Type == 1).Sum(x => x.Qty), // Assuming Type 1 = IN
                    TotalQuantityOut = g.Where(x => x.Type == 2).Sum(x => x.Qty), // Assuming Type 2 = OUT
                    TotalQuantityWeightIn = g.Where(x => x.Type == 1).Sum(x => x.QtyWeight),
                    TotalQuantityWeightOut = g.Where(x => x.Type == 2).Sum(x => x.QtyWeight),
                    NetQuantityChange = g.Where(x => x.Type == 1).Sum(x => x.Qty) - g.Where(x => x.Type == 2).Sum(x => x.Qty),
                    NetQuantityWeightChange = g.Where(x => x.Type == 1).Sum(x => x.QtyWeight) - g.Where(x => x.Type == 2).Sum(x => x.QtyWeight)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();
        }

        private async Task<List<TopGemMovement>> GetTopGemMovements(DateTime startDate, DateTime endDate, DashboardRequest request)
        {
            var transactionQuery = _jewelryContext.TbtStockGemTransection
                .Where(x => x.CreateDate >= startDate && x.CreateDate < endDate);

            return await (from trans in transactionQuery
                         join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                         where (string.IsNullOrEmpty(request.GroupName) || gem.GroupName == request.GroupName) &&
                               (string.IsNullOrEmpty(request.Shape) || gem.Shape == request.Shape) &&
                               (string.IsNullOrEmpty(request.Grade) || gem.Grade == request.Grade)
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

        private async Task<List<PriceChangeAlert>> GetPriceChangeAlerts(DateTime startDate, DateTime endDate, DashboardRequest request)
        {
            var priceChangeQuery = _jewelryContext.TbtStockGemTransectionPrice
                .Where(x => x.CreateDate >= startDate && x.CreateDate < endDate);

            return await (from price in priceChangeQuery
                         join gem in _jewelryContext.TbtStockGem on price.Code equals gem.Code
                         where (string.IsNullOrEmpty(request.GroupName) || gem.GroupName == request.GroupName) &&
                               (string.IsNullOrEmpty(request.Shape) || gem.Shape == request.Shape) &&
                               (string.IsNullOrEmpty(request.Grade) || gem.Grade == request.Grade)
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

        private IQueryable<TbtStockGem> BuildStockQuery(DashboardRequest request)
        {
            var query = _jewelryContext.TbtStockGem.AsQueryable();

            if (!string.IsNullOrEmpty(request.GroupName))
                query = query.Where(x => x.GroupName == request.GroupName);
            
            if (!string.IsNullOrEmpty(request.Shape))
                query = query.Where(x => x.Shape == request.Shape);
            
            if (!string.IsNullOrEmpty(request.Grade))
                query = query.Where(x => x.Grade == request.Grade);

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

        private async Task<TodayStockSummary> GetTodaySummary(DateTime today, DateTime tomorrow, DashboardRequest request)
        {
            var transactionQuery = _jewelryContext.TbtStockGemTransection
                .Where(x => x.CreateDate >= today && x.CreateDate < tomorrow);

            var priceChangeQuery = _jewelryContext.TbtStockGemTransectionPrice
                .Where(x => x.CreateDate >= today && x.CreateDate < tomorrow);

            var newStockQuery = _jewelryContext.TbtStockGem
                .Where(x => x.CreateDate >= today && x.CreateDate < tomorrow);

            return new TodayStockSummary
            {
                TotalTransactions = await transactionQuery.CountAsync(),
                PriceChanges = await priceChangeQuery.CountAsync(),
                NewStockItems = await newStockQuery.CountAsync(),
                TotalQuantityIn = await transactionQuery.Where(x => x.Type == 1).SumAsync(x => x.Qty),
                TotalQuantityOut = await transactionQuery.Where(x => x.Type == 2).SumAsync(x => x.Qty),
                TotalQuantityWeightIn = await transactionQuery.Where(x => x.Type == 1).SumAsync(x => x.QtyWeight),
                TotalQuantityWeightOut = await transactionQuery.Where(x => x.Type == 2).SumAsync(x => x.QtyWeight)
            };
        }

        private async Task<List<TodayTransaction>> GetTodayTransactions(DateTime today, DateTime tomorrow, DashboardRequest request)
        {
            return await (from trans in _jewelryContext.TbtStockGemTransection
                         join gem in _jewelryContext.TbtStockGem on trans.Code equals gem.Code
                         where trans.CreateDate >= today && trans.CreateDate < tomorrow
                         select new TodayTransaction
                         {
                             Running = trans.Running,
                             Code = trans.Code,
                             GroupName = gem.GroupName,
                             Shape = gem.Shape,
                             Grade = gem.Grade,
                             Type = trans.Type,
                             TypeName = trans.Type == 1 ? "IN" : trans.Type == 2 ? "OUT" : "OTHER",
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

        private async Task<List<TodayPriceChange>> GetTodayPriceChanges(DateTime today, DateTime tomorrow, DashboardRequest request)
        {
            return await (from price in _jewelryContext.TbtStockGemTransectionPrice
                         join gem in _jewelryContext.TbtStockGem on price.Code equals gem.Code
                         where price.CreateDate >= today && price.CreateDate < tomorrow
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

        private async Task<List<TodayNewStock>> GetTodayNewStocks(DateTime today, DateTime tomorrow, DashboardRequest request)
        {
            return await _jewelryContext.TbtStockGem
                .Where(x => x.CreateDate >= today && x.CreateDate < tomorrow)
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
                .Where(x => x.Quantity <= 10) // Assuming 10 as low stock threshold
                .Select(x => new TodayLowStock
                {
                    Code = x.Code,
                    GroupName = x.GroupName,
                    Shape = x.Shape,
                    Grade = x.Grade,
                    CurrentQuantity = x.Quantity,
                    CurrentQuantityWeight = x.QuantityWeight,
                    MinimumLevel = 10, // This should come from a configuration table
                    AlertLevel = x.Quantity == 0 ? "ZERO" : x.Quantity <= 5 ? "CRITICAL" : "LOW"
                })
                .OrderBy(x => x.CurrentQuantity)
                .ToListAsync();
        }

        // Placeholder methods for weekly and monthly reports
        // These would need similar detailed implementations

        private async Task<WeeklyStockSummary> GetWeeklySummary(DateTime startOfWeek, DateTime endOfWeek, DashboardRequest request)
        {
            // Implementation similar to daily but aggregated for week
            return new WeeklyStockSummary();
        }

        private async Task<List<DailyMovement>> GetDailyMovements(DateTime startOfWeek, DateTime endOfWeek, DashboardRequest request)
        {
            // Implementation for daily breakdown within the week
            return new List<DailyMovement>();
        }

        private async Task<List<WeeklyTopMovement>> GetWeeklyTopMovements(DateTime startOfWeek, DateTime endOfWeek, DashboardRequest request)
        {
            // Implementation for weekly top movements
            return new List<WeeklyTopMovement>();
        }

        private async Task<List<WeeklyPerformance>> GetWeeklyPerformance(DateTime startOfWeek, DateTime endOfWeek, DashboardRequest request)
        {
            // Implementation for weekly performance metrics
            return new List<WeeklyPerformance>();
        }

        private async Task<List<WeeklyTrendAnalysis>> GetWeeklyTrendAnalysis(DateTime startOfWeek, DateTime endOfWeek, DashboardRequest request)
        {
            // Implementation for weekly trend analysis
            return new List<WeeklyTrendAnalysis>();
        }

        private async Task<MonthlyStockSummary> GetMonthlySummary(DateTime startOfMonth, DateTime endOfMonth, DashboardRequest request)
        {
            // Implementation for monthly summary
            return new MonthlyStockSummary();
        }

        private async Task<List<WeeklyComparison>> GetWeeklyComparisons(DateTime startOfMonth, DateTime endOfMonth, DashboardRequest request)
        {
            // Implementation for weekly comparisons within month
            return new List<WeeklyComparison>();
        }

        private async Task<List<MonthlyTopPerformer>> GetMonthlyTopPerformers(DateTime startOfMonth, DateTime endOfMonth, DashboardRequest request)
        {
            // Implementation for monthly top performers
            return new List<MonthlyTopPerformer>();
        }

        private async Task<List<MonthlyInventoryAnalysis>> GetMonthlyInventoryAnalysis(DateTime startOfMonth, DateTime endOfMonth, DashboardRequest request)
        {
            // Implementation for monthly inventory analysis
            return new List<MonthlyInventoryAnalysis>();
        }

        private async Task<List<MonthlyPriceAnalysis>> GetMonthlyPriceAnalysis(DateTime startOfMonth, DateTime endOfMonth, DashboardRequest request)
        {
            // Implementation for monthly price analysis
            return new List<MonthlyPriceAnalysis>();
        }

        private async Task<List<MonthlySupplierAnalysis>> GetMonthlySupplierAnalysis(DateTime startOfMonth, DateTime endOfMonth, DashboardRequest request)
        {
            // Implementation for monthly supplier analysis
            return new List<MonthlySupplierAnalysis>();
        }

        #endregion

    }
}
