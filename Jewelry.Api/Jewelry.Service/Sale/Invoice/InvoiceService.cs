using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.Invoice
{
    public class InvoiceService : BaseService, IInvoiceService
    {
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;

        public InvoiceService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor,
            IHostEnvironment hostingEnvironment,
            IRunningNumber runningNumberService) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _hostingEnvironment = hostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public async Task<string> Create(jewelry.Model.Sale.Invoice.Create.Request request)
        {
            // Validate required fields
            if (string.IsNullOrEmpty(request.SoNumber))
            {
                throw new HandleException("Sale Order Number is Required.");
            }

            if (string.IsNullOrEmpty(request.CustomerCode))
            {
                throw new HandleException("Customer Code is Required.");
            }

            if (string.IsNullOrEmpty(request.CustomerName))
            {
                throw new HandleException("Customer Name is Required.");
            }

            if (request.Items == null || !request.Items.Any())
            {
                throw new HandleException("Invoice items are required.");
            }

            //check all stock not exist invoice
            var stockArray = request.Items.Select(i => i.StockNumber).ToArray();
            var getstockConfrim = await _jewelryContext.TbtSaleOrderProduct
                .Where(x => x.SoNumber == request.SoNumber && stockArray.Contains(x.StockNumber))
                .ToListAsync();

            if (!getstockConfrim.Any())
            {
                throw new HandleException("No matching Sale Order Products found for the provided items.");
            }
            if (getstockConfrim.Any(x => !string.IsNullOrEmpty(x.Invoice)))
            { 
                throw new HandleException("One or more items have already been invoiced.");
            }

            // Generate invoice number
            var invoiceNumber = await GenerateInvoiceNumber();

            // Create invoice header
            var invoiceHeader = new TbtSaleInvoiceHeader
            {
                Running = invoiceNumber,
                SoRunning = request.SoNumber,

                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow,

                CurrencyRate = request.CurrencyRate,
                CurrencyUnit = request.CurrencyUnit,
                CustomerAddress = request.CustomerAddress,
                CustomerCode = request.CustomerCode,
                CustomerEmail = request.CustomerEmail,
                CustomerName = request.CustomerName,
                CustomerRemark = request.CustomerRemark,
                CustomerTel = request.CustomerTel,

                DeliveryDate = request.DeliveryDate.HasValue ? request.DeliveryDate.Value.UtcDateTime : null,
                Deposit = request.Deposit,

                GoldRate = request.GoldRate,
                Markup = request.Markup,

                PaymantName = request.PaymentName,
                Payment = request.Payment,
                PaymentDay = request.PaymentDay,

                Priority = request.Priority,
                RefQuotation = request.RefQuotation,
                Remark = request.Remark,

                Status = 100,
                StatusName = "invoice",

                SpecialDiscount = request.SpecialDiscount,
                SpecialAddition = request.SpecialAddition,
                FreightAndInsurance = request.FreightAndInsurance
            };

            _jewelryContext.TbtSaleInvoiceHeader.Add(invoiceHeader);

            // Update sale order products with invoice information
            foreach (var item in getstockConfrim)
            {
                //var saleOrderProduct = await _jewelryContext.TbtSaleOrderProduct
                //    .FirstOrDefaultAsync(x => x.SoNumber == request.SoNumber 
                //                            && x.StockNumber == item.StockNumber 
                //                            && x.Id == item.Id);
                item.Invoice = invoiceNumber;
                item.InvoiceItem = $"{invoiceNumber}-{item.Id}";
                
                item.UpdateBy = CurrentUsername;
                item.UpdateDate = DateTime.UtcNow;
            }
            _jewelryContext.TbtSaleOrderProduct.UpdateRange(getstockConfrim);

            await _jewelryContext.SaveChangesAsync();

            return invoiceNumber;
        }

        public async Task<jewelry.Model.Sale.Invoice.Get.Response> Get(jewelry.Model.Sale.Invoice.Get.Request request)
        {
            if (string.IsNullOrEmpty(request.InvoiceNumber))
            {
                throw new HandleException("Invoice Number is Required.");
            }

            var invoiceHeader = await _jewelryContext.TbtSaleInvoiceHeader
                .FirstOrDefaultAsync(x => x.Running == request.InvoiceNumber);

            if (invoiceHeader == null)
            {
                throw new HandleException($"Invoice not found: {request.InvoiceNumber}");
            }

            // Get SO Number from invoice header
            var soNumber = invoiceHeader.SoRunning;

            if (string.IsNullOrEmpty(soNumber))
            {
                throw new HandleException($"Invoice {request.InvoiceNumber} does not have associated Sale Order.");
            }

            // Get Sale Order Header to get Data (JSON of all items)
            var saleOrderHeader = await _jewelryContext.TbtSaleOrder
                .FirstOrDefaultAsync(x => x.SoNumber == soNumber);

            if (saleOrderHeader == null)
            {
                throw new HandleException($"Sale Order {soNumber} not found.");
            }

            // Get confirmed items with invoice info
            var confirmedItems = await _jewelryContext.TbtSaleOrderProduct
                .Where(x => x.Invoice == request.InvoiceNumber)
                .Select(x => new jewelry.Model.Sale.Invoice.Get.Item
                {
                    Id = x.Id,
                    StockNumber = x.StockNumber,
                    IsConfirmed = true,
                    Invoice = x.Invoice,
                    InvoiceItem = x.InvoiceItem
                })
                .ToListAsync();

            return new jewelry.Model.Sale.Invoice.Get.Response
            {
                InvoiceNumber = invoiceHeader.Running,
                SoNumber = soNumber,

                CreateDate = invoiceHeader.CreateDate,
                CreateBy = invoiceHeader.CreateBy,
                UpdateBy = invoiceHeader.UpdateBy,
                UpdateDate = invoiceHeader.UpdateDate,

                CurrencyRate = invoiceHeader.CurrencyRate,
                CurrencyUnit = invoiceHeader.CurrencyUnit,
                
                CustomerCode = invoiceHeader.CustomerCode,
                CustomerName = invoiceHeader.CustomerName,
                CustomerAddress = invoiceHeader.CustomerAddress,
                CustomerEmail = invoiceHeader.CustomerEmail,
                CustomerTel = invoiceHeader.CustomerTel,
                CustomerRemark = invoiceHeader.CustomerRemark,

                // Pass Sale Order Data as-is (JSON string with all items)

                DeliveryDate = invoiceHeader.DeliveryDate,
                Deposit = invoiceHeader.Deposit,

                GoldRate = invoiceHeader.GoldRate,
                Markup = invoiceHeader.Markup,

                PaymentName = invoiceHeader.PaymantName,
                Payment = invoiceHeader.Payment,
                PaymentDay = invoiceHeader.PaymentDay,

                Priority = invoiceHeader.Priority,
                RefQuotation = invoiceHeader.RefQuotation,
                Remark = invoiceHeader.Remark,

                Status = invoiceHeader.Status,
                StatusName = invoiceHeader.StatusName,

                SpecialDiscount = invoiceHeader.SpecialDiscount,
                SpecialAddition = invoiceHeader.SpecialAddition,
                FreightAndInsurance = invoiceHeader.FreightAndInsurance,

                ConfirmedItems = confirmedItems
            };
        }

        public IQueryable<jewelry.Model.Sale.Invoice.List.Response> List(jewelry.Model.Sale.Invoice.List.Request request)
        {
            var query = from invoice in _jewelryContext.TbtSaleInvoiceHeader
                        select new jewelry.Model.Sale.Invoice.List.Response
                        {
                            InvoiceNumber = invoice.Running,

                            CreateBy = invoice.CreateBy,
                            CreateDate = invoice.CreateDate,

                            UpdateBy = invoice.UpdateBy,
                            UpdateDate = invoice.UpdateDate,

                            CurrencyRate = invoice.CurrencyRate,
                            CurrencyUnit = invoice.CurrencyUnit,

                            CustomerAddress = invoice.CustomerAddress,
                            CustomerCode = invoice.CustomerCode,
                            CustomerEmail = invoice.CustomerEmail,
                            CustomerName = invoice.CustomerName,
                            CustomerRemark = invoice.CustomerRemark,
                            CustomerTel = invoice.CustomerTel,
                            DeliveryDate = invoice.DeliveryDate,
                            //DepositPercent = invoice.DepositPercent,
                            GoldRate = invoice.GoldRate,
                            Markup = invoice.Markup,
                            PaymentName = invoice.PaymantName,
                            Payment = invoice.Payment,
                            Priority = invoice.Priority,
                            RefQuotation = invoice.RefQuotation,
                            Remark = invoice.Remark,

                            //Status = invoice.Status,
                            //StatusName = invoice.StatusName,

                            ItemCount = _jewelryContext.TbtSaleOrderProduct.Count(x => x.Invoice == invoice.Running),
                            //TotalAmount = _jewelryContext.TbtSaleOrderProduct
                            //    .Where(x => x.Invoice == invoice.Running)
                            //    .Sum(x => x.PriceAfterCurrecyRate * x.Qty)
                        };

            // Apply filters
            if (!string.IsNullOrEmpty(request.InvoiceNumber))
            {
                query = query.Where(x => x.InvoiceNumber.Contains(request.InvoiceNumber));
            }

            if (!string.IsNullOrEmpty(request.CustomerName))
            {
                query = query.Where(x => x.CustomerName.Contains(request.CustomerName));
            }

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                query = query.Where(x => x.CustomerCode.Contains(request.CustomerCode));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == request.Status.Value);
            }

            if (request.CreateDateFrom.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.CreateDateFrom.Value);
            }

            if (request.CreateDateTo.HasValue)
            {
                query = query.Where(x => x.CreateDate <= request.CreateDateTo.Value);
            }

            if (request.DeliveryDateFrom.HasValue)
            {
                query = query.Where(x => x.DeliveryDate >= request.DeliveryDateFrom.Value);
            }

            if (request.DeliveryDateTo.HasValue)
            {
                query = query.Where(x => x.DeliveryDate <= request.DeliveryDateTo.Value);
            }

            if (!string.IsNullOrEmpty(request.CreateBy))
            {
                query = query.Where(x => x.CreateBy.Contains(request.CreateBy));
            }

            // Apply ordering
            if (!string.IsNullOrEmpty(request.OrderBy))
            {
                if (request.OrderDirection?.ToUpper() == "ASC")
                {
                    switch (request.OrderBy.ToLower())
                    {
                        case "invoicenumber":
                            query = query.OrderBy(x => x.InvoiceNumber);
                            break;
                        case "customername":
                            query = query.OrderBy(x => x.CustomerName);
                            break;
                        case "createdate":
                            query = query.OrderBy(x => x.CreateDate);
                            break;
                        case "deliverydate":
                            query = query.OrderBy(x => x.DeliveryDate);
                            break;
                        case "totalamount":
                            query = query.OrderBy(x => x.TotalAmount);
                            break;
                        default:
                            query = query.OrderBy(x => x.CreateDate);
                            break;
                    }
                }
                else
                {
                    switch (request.OrderBy.ToLower())
                    {
                        case "invoicenumber":
                            query = query.OrderByDescending(x => x.InvoiceNumber);
                            break;
                        case "customername":
                            query = query.OrderByDescending(x => x.CustomerName);
                            break;
                        case "createdate":
                            query = query.OrderByDescending(x => x.CreateDate);
                            break;
                        case "deliverydate":
                            query = query.OrderByDescending(x => x.DeliveryDate);
                            break;
                        case "totalamount":
                            query = query.OrderByDescending(x => x.TotalAmount);
                            break;
                        default:
                            query = query.OrderByDescending(x => x.CreateDate);
                            break;
                    }
                }
            }
            else
            {
                query = query.OrderByDescending(x => x.CreateDate);
            }

            return query;
        }

        public async Task<string> Delete(jewelry.Model.Sale.Invoice.Delete.Request request)
        {
            if (string.IsNullOrEmpty(request.InvoiceNumber))
            {
                throw new HandleException("Invoice Number is Required.");
            }

            var invoiceHeader = await _jewelryContext.TbtSaleInvoiceHeader
                .FirstOrDefaultAsync(x => x.Running == request.InvoiceNumber);

            if (invoiceHeader == null)
            {
                throw new HandleException($"Invoice not found: {request.InvoiceNumber}");
            }

            // Update sale order products to remove invoice reference
            var saleOrderProducts = await _jewelryContext.TbtSaleOrderProduct
                .Where(x => x.Invoice == request.InvoiceNumber)
                .ToListAsync();

            foreach (var product in saleOrderProducts)
            {
                product.Invoice = null;
                product.InvoiceItem = null;
                product.UpdateBy = CurrentUsername;
                product.UpdateDate = DateTime.UtcNow;
            }

            // Delete invoice header
            _jewelryContext.TbtSaleInvoiceHeader.Remove(invoiceHeader);

            await _jewelryContext.SaveChangesAsync();

            return $"Invoice {request.InvoiceNumber} deleted successfully";
        }

        public async Task<string> GenerateInvoiceNumber()
        {
            var runningNumber = await _runningNumberService.GenerateRunningNumberForGold("INV");
            return runningNumber;
        }

        public async Task<jewelry.Model.Sale.InvoiceVersion.Upsert.Response> UpsertVersion(jewelry.Model.Sale.InvoiceVersion.Upsert.Request request)
        {
            // Validate required fields
            if (string.IsNullOrEmpty(request.InvoiceNumber))
            {
                throw new HandleException("Invoice Number is Required.");
            }

            if (string.IsNullOrEmpty(request.SoNumber))
            {
                throw new HandleException("Sale Order Number is Required.");
            }

            if (string.IsNullOrEmpty(request.Data))
            {
                throw new HandleException("Version Data is Required.");
            }

            // Check if invoice exists
            var invoiceExists = await _jewelryContext.TbtSaleInvoiceHeader
                .AnyAsync(x => x.Running == request.InvoiceNumber);

            if (!invoiceExists)
            {
                throw new HandleException($"Invoice {request.InvoiceNumber} not found.");
            }

            // Generate version number
            var versionNumber = await GenerateVersionNumber(request.InvoiceNumber);

            // Create new version
            var invoiceVersion = new TbtSaleInvoiceVersion
            {
                Running = versionNumber,
                InvoiceRunning = request.InvoiceNumber,
                SoRunning = request.SoNumber,
                Data = request.Data,
                IsActive = true,
                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow
            };

            _jewelryContext.TbtSaleInvoiceVersion.Add(invoiceVersion);
            await _jewelryContext.SaveChangesAsync();

            return new jewelry.Model.Sale.InvoiceVersion.Upsert.Response
            {
                VersionNumber = versionNumber,
                InvoiceNumber = request.InvoiceNumber,
                SoNumber = request.SoNumber
            };
        }

        public async Task<jewelry.Model.Sale.InvoiceVersion.Get.Response> GetVersion(jewelry.Model.Sale.InvoiceVersion.Get.Request request)
        {
            if (string.IsNullOrEmpty(request.VersionNumber))
            {
                throw new HandleException("Version Number is Required.");
            }

            var version = await _jewelryContext.TbtSaleInvoiceVersion
                .FirstOrDefaultAsync(x => x.Running == request.VersionNumber);

            if (version == null)
            {
                throw new HandleException($"Invoice Version not found: {request.VersionNumber}");
            }

            return new jewelry.Model.Sale.InvoiceVersion.Get.Response
            {
                VersionNumber = version.Running,
                InvoiceNumber = version.InvoiceRunning,
                SoNumber = version.SoRunning,
                Data = version.Data,
                CreateDate = version.CreateDate,
                CreateBy = version.CreateBy,
                UpdateDate = version.UpdateDate,
                UpdateBy = version.UpdateBy,
                IsActive = version.IsActive
            };
        }

        public IQueryable<jewelry.Model.Sale.InvoiceVersion.List.Response> ListVersions(jewelry.Model.Sale.InvoiceVersion.List.Request request)
        {
            var query = from version in _jewelryContext.TbtSaleInvoiceVersion
                        select new jewelry.Model.Sale.InvoiceVersion.List.Response
                        {
                            VersionNumber = version.Running,
                            InvoiceNumber = version.InvoiceRunning,
                            SoNumber = version.SoRunning,
                            CreateDate = version.CreateDate,
                            CreateBy = version.CreateBy,
                            UpdateDate = version.UpdateDate,
                            UpdateBy = version.UpdateBy,
                            IsActive = version.IsActive
                        };

            // Apply filters
            if (!string.IsNullOrEmpty(request.InvoiceNumber))
            {
                query = query.Where(x => x.InvoiceNumber == request.InvoiceNumber);
            }

            if (!string.IsNullOrEmpty(request.SoNumber))
            {
                query = query.Where(x => x.SoNumber == request.SoNumber);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            // Order by creation date descending (newest first)
            query = query.OrderByDescending(x => x.CreateDate);

            return query;
        }

        private async Task<string> GenerateVersionNumber(string invoiceNumber)
        {
            // Get the count of existing versions for this invoice
            var versionCount = await _jewelryContext.TbtSaleInvoiceVersion
                .CountAsync(x => x.InvoiceRunning == invoiceNumber);

            // Generate version number: INV-XXXXX-V001, INV-XXXXX-V002, etc.
            var versionNumber = $"{invoiceNumber}-V{(versionCount + 1):D3}";

            return versionNumber;
        }
    }
}