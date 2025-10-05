using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.SaleOrder
{
    public class SaleOrderService : BaseService, ISaleOrderService
    {
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;

        public SaleOrderService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor,
            IHostEnvironment hostingEnvironment,
            IRunningNumber runningNumberService) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _hostingEnvironment = hostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public async Task<string> Upsert(jewelry.Model.Sale.SaleOrder.Create.Request request)
        {
            //if (string.IsNullOrEmpty(request.SoNumber))
            //{
            //    throw new HandleException("Sale Order Number is Required.");
            //}

            //if (string.IsNullOrEmpty(request.SoNumber))
            //{
            //    throw new HandleException("Sale Order Number is Required.");
            //}

            var saleOrder = new TbtSaleOrder();

            if (!string.IsNullOrEmpty(request.SoNumber))
            {
                saleOrder = (from item in _jewelryContext.TbtSaleOrder
                             where item.SoNumber == request.SoNumber.ToUpper()
                             select item).FirstOrDefault();
            }

            var soNumber = string.Empty;

            if (string.IsNullOrEmpty(request.SoNumber))
            {
                soNumber = await _runningNumberService.GenerateRunningNumberForGold("SO");
                // Create new sale order
                saleOrder = new TbtSaleOrder
                {
                    SoNumber = soNumber,
                    Running = await _runningNumberService.GenerateRunningNumberForGold("RUNNING"),

                    SoDate = request.SODate.HasValue ? request.SODate.Value.UtcDateTime : null,
                    DeliveryDate = request.DeliveryDate.HasValue ? request.DeliveryDate.Value.UtcDateTime : null,
                    Status = request.Status,
                    StatusName = request.StatusName ?? "",

                    RefQuotation = request.RefQuotation,

                    Payment = request.Payment,
                    PaymantName = request.PaymentName,
                    DepositPercent = request.DepositPercent,

                    Priority = request.Priority ?? "ปกติ",

                    Data = request.Data,

                    // Customer Information
                    CustomerName = request.CustomerName ?? "",
                    CustomerCode = request.CustomerCode ?? "",
                    CustomerAddress = request.CustomerAddress,
                    CustomerTel = request.CustomerTel,
                    CustomerEmail = request.CustomerEmail,
                    CustomerRemark = request.CustomerRemark,

                    // Currency and Pricing
                    CurrencyUnit = request.CurrencyUnit ?? "THB",
                    CurrencyRate = request.CurrencyRate,
                    Markup = request.Markup,
                    Discount = request.Discount,
                    GoldRate = request.GoldRate,

                    Remark = request.Remark,

                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername
                };

                _jewelryContext.TbtSaleOrder.Add(saleOrder);
                await _jewelryContext.SaveChangesAsync();

                return soNumber;
            }

            if (saleOrder != null && !string.IsNullOrEmpty(saleOrder.SoNumber))
            {

                soNumber = saleOrder.SoNumber;

                // Update existing sale order
                saleOrder.SoDate = request.SODate.HasValue ? request.SODate.Value.UtcDateTime : saleOrder.SoDate;
                saleOrder.DeliveryDate = request.DeliveryDate.HasValue ? request.DeliveryDate.Value.UtcDateTime : null;
                saleOrder.Status = request.Status;
                saleOrder.StatusName = request.StatusName ?? saleOrder.StatusName;

                saleOrder.RefQuotation = request.RefQuotation;

                saleOrder.Payment = request.Payment;
                saleOrder.PaymantName = request.PaymentName;
                saleOrder.DepositPercent = request.DepositPercent;

                saleOrder.Priority = request.Priority ?? saleOrder.Priority;

                saleOrder.Data = request.Data;

                // Customer Information
                saleOrder.CustomerName = request.CustomerName ?? saleOrder.CustomerName;
                saleOrder.CustomerCode = request.CustomerCode ?? saleOrder.CustomerCode;
                saleOrder.CustomerAddress = request.CustomerAddress;
                saleOrder.CustomerTel = request.CustomerTel;
                saleOrder.CustomerEmail = request.CustomerEmail;
                saleOrder.CustomerRemark = request.CustomerRemark;

                // Currency and Pricing
                saleOrder.CurrencyUnit = request.CurrencyUnit ?? saleOrder.CurrencyUnit;
                saleOrder.CurrencyRate = request.CurrencyRate;
                saleOrder.Markup = request.Markup;
                saleOrder.Discount = request.Discount;
                saleOrder.GoldRate = request.GoldRate;

                saleOrder.Remark = request.Remark;

                saleOrder.UpdateDate = DateTime.UtcNow;
                saleOrder.UpdateBy = CurrentUsername;

                _jewelryContext.TbtSaleOrder.Update(saleOrder);
                await _jewelryContext.SaveChangesAsync();
            }

            return soNumber;
        }

        public async Task<jewelry.Model.Sale.SaleOrder.Get.Response> Get(jewelry.Model.Sale.SaleOrder.Get.Request request)
        {
            if (string.IsNullOrEmpty(request.SoNumber))
            {
                throw new HandleException("Sale Order Number is Required.");
            }

            var saleOrder = (from item in _jewelryContext.TbtSaleOrder
                             where item.SoNumber == request.SoNumber.ToUpper()
                             select item).FirstOrDefault();

            if (saleOrder == null)
            {
                throw new HandleException("Sale Order Not Found.");
            }

            var response = new jewelry.Model.Sale.SaleOrder.Get.Response
            {
                Running = saleOrder.Running,
                SoNumber = saleOrder.SoNumber,

                CreateDate = saleOrder.CreateDate,
                CreateBy = saleOrder.CreateBy,
                UpdateDate = saleOrder.UpdateDate,
                UpdateBy = saleOrder.UpdateBy,

                DeliveryDate = saleOrder.DeliveryDate,
                Status = saleOrder.Status,
                StatusName = saleOrder.StatusName,

                RefQuotation = saleOrder.RefQuotation,

                Payment = saleOrder.Payment,
                PaymentName = saleOrder.PaymantName,
                DepositPercent = saleOrder.DepositPercent,

                Priority = saleOrder.Priority,

                Data = saleOrder.Data,

                // Customer Information
                CustomerName = saleOrder.CustomerName,
                CustomerCode = saleOrder.CustomerCode,
                CustomerAddress = saleOrder.CustomerAddress,
                CustomerTel = saleOrder.CustomerTel,
                CustomerEmail = saleOrder.CustomerEmail,
                CustomerRemark = saleOrder.CustomerRemark,

                // Currency and Pricing
                CurrencyUnit = saleOrder.CurrencyUnit,
                CurrencyRate = saleOrder.CurrencyRate,
                Markup = saleOrder.Markup,
                Discount = saleOrder.Discount,
                GoldRate = saleOrder.GoldRate,

                Remark = saleOrder.Remark
            };

            response.StockConfirm = (from item in _jewelryContext.TbtSaleOrderProduct
                                     where item.SoNumber == response.SoNumber && item.Running == response.Running
                                     select item.StockNumber).ToArray();

            return response;
        }

        public IQueryable<jewelry.Model.Sale.SaleOrder.List.Response> List(jewelry.Model.Sale.SaleOrder.List.Request _request)
        {
            var request = _request.Search;
            var query = from saleOrder in _jewelryContext.TbtSaleOrder
                        select new jewelry.Model.Sale.SaleOrder.List.Response
                        {
                            Running = saleOrder.Running ?? string.Empty,
                            SoNumber = saleOrder.SoNumber ?? string.Empty,

                            CreateDate = saleOrder.CreateDate,
                            CreateBy = saleOrder.CreateBy ?? string.Empty,
                            UpdateDate = saleOrder.UpdateDate,
                            UpdateBy = saleOrder.UpdateBy ?? string.Empty,

                            DeliveryDate = saleOrder.DeliveryDate,
                            Status = saleOrder.Status,
                            StatusName = saleOrder.StatusName ?? string.Empty,

                            RefQuotation = saleOrder.RefQuotation ?? string.Empty,

                            Payment = saleOrder.Payment,
                            PaymentName = saleOrder.PaymantName ?? string.Empty,
                            DepositPercent = saleOrder.DepositPercent,

                            Priority = saleOrder.Priority ?? string.Empty,

                            // Customer Information
                            CustomerName = saleOrder.CustomerName ?? string.Empty,
                            CustomerCode = saleOrder.CustomerCode ?? string.Empty,
                            CustomerTel = saleOrder.CustomerTel ?? string.Empty,
                            CustomerEmail = saleOrder.CustomerEmail ?? string.Empty,

                            // Currency and Pricing
                            CurrencyUnit = saleOrder.CurrencyUnit ?? string.Empty,
                            CurrencyRate = saleOrder.CurrencyRate,
                            Markup = saleOrder.Markup,
                            Discount = saleOrder.Discount,
                            GoldRate = saleOrder.GoldRate
                        };

            // Apply filters
            if (!string.IsNullOrEmpty(request.SoNumber))
            {
                query = query.Where(x => x.SoNumber.Contains(request.SoNumber.ToUpper()));
            }

            if (!string.IsNullOrEmpty(request.CustomerName))
            {
                query = query.Where(x => x.CustomerName.Contains(request.CustomerName));
            }

            if (!string.IsNullOrEmpty(request.RefQuotation))
            {
                query = query.Where(x => x.RefQuotation.Contains(request.RefQuotation));
            }

            if (!string.IsNullOrEmpty(request.CurrencyUnit))
            {
                query = query.Where(x => x.CurrencyUnit.Contains(request.CurrencyUnit));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == request.Status.Value);
            }

            if (!string.IsNullOrEmpty(request.CreateBy))
            {
                query = query.Where(x => x.CreateBy.Contains(request.CreateBy));
            }

            if (request.CreateDateStart.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.CreateDateStart.Value.StartOfDayUtc());
            }

            if (request.CreateDateEnd.HasValue)
            {
                query = query.Where(x => x.CreateDate <= request.CreateDateEnd.Value.EndOfDayUtc());
            }

            if (request.DeliveryDateStart.HasValue)
            {
                query = query.Where(x => x.DeliveryDate.HasValue && x.DeliveryDate.Value >= request.DeliveryDateStart.Value.StartOfDayUtc());
            }

            if (request.DeliveryDateEnd.HasValue)
            {
                query = query.Where(x => x.DeliveryDate.HasValue && x.DeliveryDate.Value <= request.DeliveryDateEnd.Value.EndOfDayUtc());
            }

            return query;
        }

        public async Task<string> GenerateRunningNumber()
        {
            try
            {
                // Generate running number with "SO" prefix
                var runningNumber = await _runningNumberService.GenerateRunningNumberForGold("SO");
                return runningNumber;
            }
            catch (Exception ex)
            {
                throw new HandleException($"Error generating SO running number: {ex.Message}");
            }
        }

        public async Task<jewelry.Model.Sale.SaleOrder.ConfirmStock.Response> ConfirmStockItems(jewelry.Model.Sale.SaleOrder.ConfirmStock.Request request)
        {
            // Basic validation
            if (string.IsNullOrEmpty(request.SoNumber))
            {
                throw new HandleException("Sale Order Number is required.");
            }

            if (request.StockItems == null || !request.StockItems.Any())
            {
                throw new HandleException("No stock items provided for confirmation.");
            }

            // Validate Sale Order exists and is in correct state
            var saleOrder = await _jewelryContext.TbtSaleOrder
                .FirstOrDefaultAsync(so => so.SoNumber == request.SoNumber.ToUpper());

            if (saleOrder == null)
            {
                throw new HandleException($"Sale Order {request.SoNumber} not found.");
            }

            // Validate sale order status - only allow confirmation for specific statuses
            //if (saleOrder.Status != null && saleOrder.Status != 100 && saleOrder.Status != 200)
            //{
            //    throw new HandleException($"Cannot confirm stock items for Sale Order {request.SoNumber}. Invalid status: {saleOrder.StatusName}.");
            //}

            var arrayStock = request.StockItems.Select(i => i.StockNumber).ToArray();
            var stockProducts = await _jewelryContext.TbtStockProduct
                .Where(s => arrayStock.Contains(s.StockNumber))
                .ToListAsync();

            var confirmedStockNumbers = new List<string>();
            var errors = new List<string>();
            var confirmedDate = DateTime.UtcNow;

            // Validate each stock item before processing
            foreach (var stockItem in request.StockItems)
            {
                // Required field validation
                if (string.IsNullOrEmpty(stockItem.StockNumber))
                {
                    errors.Add("Stock Number is required for all items.");
                    continue;
                }

                // Quantity validation
                if (stockItem.Qty <= 0)
                {
                    errors.Add($"Invalid quantity ({stockItem.Qty}) for stock item {stockItem.StockNumber}. Quantity must be greater than 0.");
                    continue;
                }

                // Price validation
                if (stockItem.AppraisalPrice <= 0)
                {
                    errors.Add($"Invalid appraisal price ({stockItem.AppraisalPrice}) for stock item {stockItem.StockNumber}. Price must be greater than 0.");
                    continue;
                }

               
                var stockProduct = stockProducts.FirstOrDefault(s => s.StockNumber == stockItem.StockNumber);
                if(stockProduct == null)
                    {
                    errors.Add($"Stock item {stockItem.StockNumber} not found in inventory.");
                    continue;
                }

                if (stockProduct != null)
                {
                    var remainingQty = stockProduct.Qty - stockProduct.QtySale;
                    if (stockItem.Qty > remainingQty)
                    {
                        errors.Add($"Insufficient stock for item {stockItem.StockNumber}. Requested: {stockItem.Qty}, Available: {remainingQty}.");
                        continue;
                    }
                }

                //// Check if item is already confirmed in this sale order
                //var existingConfirmation = await _jewelryContext.TbtSaleOrderProduct
                //    .FirstOrDefaultAsync(p => p.SoNumber == request.SoNumber.ToUpper() &&
                //                              p.StockNumber == stockItem.StockNumber);

                //if (existingConfirmation != null)
                //{
                //    errors.Add($"Stock item {stockItem.StockNumber} is already confirmed in this sale order.");
                //    continue;
                //}
            }

            // If there are validation errors, return them
            if (errors.Any())
            {
                throw new HandleException($"Validation errors: {string.Join("; ", errors)}");
            }

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var stockItem in request.StockItems)
                {
                    // Create new confirmed product entry
                    var newProduct = new TbtSaleOrderProduct
                    {
                        Running = saleOrder.Running,
                        SoNumber = saleOrder.SoNumber,
                        StockNumber = stockItem.StockNumber,
                        Stocknumberorigin = stockItem.ProductNumber ?? stockItem.StockNumber,
                        PriceOrigin = stockItem.AppraisalPrice,
                        CurrencyUnit = saleOrder.CurrencyUnit ?? "THB",
                        CurrencyRate = saleOrder.CurrencyRate,
                        Qty = stockItem.Qty,
                        
                        // Calculate price after currency rate and discount
                        PriceAfterCurrecyRate = stockItem.AppraisalPrice / (saleOrder.CurrencyRate),
                        Markup = saleOrder.Markup ?? 0,
                        Discount = saleOrder.Discount ?? 0,
                        PriceDiscount = stockItem.AppraisalPrice * (1 - (saleOrder.Discount ?? 0) / 100),
                        GoldRate = saleOrder.GoldRate,
                        
                        CreateDate = confirmedDate,
                        CreateBy = CurrentUsername
                    };

                    _jewelryContext.TbtSaleOrderProduct.Add(newProduct);

                    // Update stock quantity (reduce available quantity)
                    var stockProduct = await _jewelryContext.TbtStockProduct
                        .FirstOrDefaultAsync(s => s.StockNumber == stockItem.StockNumber);

                    if (stockProduct != null)
                    {
                        stockProduct.QtySale += stockItem.Qty;
                        stockProduct.UpdateDate = confirmedDate;
                        stockProduct.UpdateBy = CurrentUsername;
                        _jewelryContext.TbtStockProduct.Update(stockProduct);
                    }

                    confirmedStockNumbers.Add(stockItem.StockNumber);
                }

                // Save all changes
                await _jewelryContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return new jewelry.Model.Sale.SaleOrder.ConfirmStock.Response
                {
                    Success = true,
                    Message = $"Successfully confirmed {confirmedStockNumbers.Count} stock items.",
                    ConfirmedItemsCount = confirmedStockNumbers.Count,
                    ConfirmedStockNumbers = confirmedStockNumbers,
                    ConfirmedDate = confirmedDate
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"Error confirming stock items: {ex.Message}");
            }
        }
    }
}