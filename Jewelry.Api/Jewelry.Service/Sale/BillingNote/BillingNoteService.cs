using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.BillingNote
{
    public class BillingNoteService : BaseService, IBillingNoteService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IRunningNumber _runningNumberService;

        public BillingNoteService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor,
            IRunningNumber runningNumberService) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _runningNumberService = runningNumberService;
        }

        private IQueryable<TbtSaleInvoiceHeader> AvailableInvoiceBaseQuery()
        {
            var billedInvoiceRunnings = _jewelryContext.TbtSaleBillingNoteItem
                .Where(i => _jewelryContext.TbtSaleBillingNoteHeader
                    .Any(h => h.Running == i.BillingNoteRunning && h.IsDelete == false))
                .Select(i => i.InvoiceRunning);

            return _jewelryContext.TbtSaleInvoiceHeader
                .Where(x => x.IsDelete == false && !billedInvoiceRunnings.Contains(x.Running));
        }

        public async Task<List<jewelry.Model.Sale.BillingNote.AvailableInvoices.Response>> AvailableInvoices(jewelry.Model.Sale.BillingNote.AvailableInvoices.Request request)
        {
            if (string.IsNullOrEmpty(request.CustomerCode))
            {
                throw new HandleException("Customer Code is Required.");
            }

            var query = AvailableInvoiceBaseQuery()
                .Where(x => x.CustomerCode == request.CustomerCode)
                .OrderByDescending(x => x.CreateDate)
                .Select(x => new jewelry.Model.Sale.BillingNote.AvailableInvoices.Response
                {
                    InvoiceRunning = x.Running,
                    InvoiceDate = x.CreateDate,
                    SubTotal = x.SubTotal ?? 0,
                    CustomerName = x.CustomerName
                });

            return await query.ToListAsync();
        }

        public async Task<List<jewelry.Model.Sale.BillingNote.AvailableCustomers.Response>> AvailableCustomers()
        {
            var invoices = await AvailableInvoiceBaseQuery()
                .Select(x => new
                {
                    x.CustomerCode,
                    x.CustomerName,
                    x.CustomerAddress,
                    x.CustomerTel,
                    x.CreateDate
                })
                .ToListAsync();

            var result = invoices
                .GroupBy(x => x.CustomerCode)
                .Select(g =>
                {
                    var latest = g.OrderByDescending(x => x.CreateDate).First();
                    return new jewelry.Model.Sale.BillingNote.AvailableCustomers.Response
                    {
                        CustomerCode = g.Key,
                        CustomerName = latest.CustomerName,
                        CustomerAddress = latest.CustomerAddress,
                        CustomerTel = latest.CustomerTel,
                        InvoiceCount = g.Count()
                    };
                })
                .OrderBy(x => x.CustomerName)
                .ToList();

            return result;
        }

        public async Task<List<jewelry.Model.Sale.BillingNote.PreviewProducts.Response>> PreviewProducts(jewelry.Model.Sale.BillingNote.PreviewProducts.Request request)
        {
            if (request.InvoiceRunnings == null || !request.InvoiceRunnings.Any())
            {
                throw new HandleException("Invoice Runnings are Required.");
            }

            var query = from sop in _jewelryContext.TbtSaleOrderProduct
                        join piece in _jewelryContext.TbtStockPiece on sop.StockNumber equals piece.StockNumber into pieceJoin
                        from piece in pieceJoin.DefaultIfEmpty()
                        join sku in _jewelryContext.TbtSku on piece.SkuCode equals sku.SkuCode into skuJoin
                        from sku in skuJoin.DefaultIfEmpty()
                        where sop.Invoice != null && request.InvoiceRunnings.Contains(sop.Invoice)
                        select new jewelry.Model.Sale.BillingNote.PreviewProducts.Response
                        {
                            InvoiceRunning = sop.Invoice!,
                            ProductNumber = sku != null ? sku.ProductNumber : piece != null ? piece.ProductCode : null,
                            ProductType = sku != null ? sku.ProductType : null,
                            ProductTypeName = sku != null ? sku.ProductTypeName : null,
                            ProductionType = sku != null ? sku.ProductionType : null,
                            Qty = sop.Qty,
                            Amount = sop.NetPrice ?? 0
                        };

            return await query.ToListAsync();
        }

        public async Task<string> Create(jewelry.Model.Sale.BillingNote.Create.Request request)
        {
            if (string.IsNullOrEmpty(request.CustomerCode))
            {
                throw new HandleException("Customer Code is Required.");
            }

            if (request.InvoiceRunnings == null || !request.InvoiceRunnings.Any())
            {
                throw new HandleException("Invoice Runnings are Required.");
            }

            var invoices = await _jewelryContext.TbtSaleInvoiceHeader
                .Where(x => request.InvoiceRunnings.Contains(x.Running)
                         && x.CustomerCode == request.CustomerCode
                         && x.IsDelete == false)
                .ToListAsync();

            if (invoices.Count != request.InvoiceRunnings.Distinct().Count())
            {
                throw new HandleException("Some invoices were not found for this customer.");
            }

            var alreadyBilled = await _jewelryContext.TbtSaleBillingNoteItem
                .Where(i => request.InvoiceRunnings.Contains(i.InvoiceRunning)
                    && _jewelryContext.TbtSaleBillingNoteHeader.Any(h => h.Running == i.BillingNoteRunning && h.IsDelete == false))
                .Select(i => i.InvoiceRunning)
                .ToListAsync();

            if (alreadyBilled.Any())
            {
                throw new HandleException($"Invoice(s) already billed: {string.Join(", ", alreadyBilled)}");
            }

            var running = await _runningNumberService.GenerateBillingNoteNumber();

            var subTotal = invoices.Sum(x => x.SubTotal ?? 0);
            var vatAmount = subTotal * request.VatPercent / 100m;
            var grandTotal = subTotal + vatAmount;

            var header = new TbtSaleBillingNoteHeader
            {
                Running = running,
                DocumentDate = request.DocumentDate.UtcDateTime,

                CustomerCode = request.CustomerCode,
                CustomerName = invoices.First().CustomerName,
                CustomerAddress = invoices.First().CustomerAddress,
                CustomerTel = invoices.First().CustomerTel,

                BillCount = invoices.Count,

                GoldResizeQty = request.GoldResizeQty,
                GoldResizeAmount = request.GoldResizeAmount,
                SilverResizeQty = request.SilverResizeQty,
                SilverResizeAmount = request.SilverResizeAmount,

                SubTotal = subTotal,
                VatPercent = request.VatPercent,
                VatAmount = vatAmount,
                GrandTotal = grandTotal,

                Remark = request.Remark,

                Status = 0,
                StatusName = "Active",

                IsDelete = false,

                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow
            };

            _jewelryContext.TbtSaleBillingNoteHeader.Add(header);

            var seq = 1;
            foreach (var invoiceRunning in request.InvoiceRunnings)
            {
                var invoice = invoices.First(x => x.Running == invoiceRunning);
                _jewelryContext.TbtSaleBillingNoteItem.Add(new TbtSaleBillingNoteItem
                {
                    BillingNoteRunning = running,
                    Seq = seq,
                    InvoiceRunning = invoiceRunning,
                    InvoiceDate = invoice.CreateDate,
                    AmountBeforeVat = invoice.SubTotal ?? 0,

                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow
                });
                seq++;
            }

            if (request.Products != null)
            {
                foreach (var product in request.Products)
                {
                    _jewelryContext.TbtSaleBillingNoteProduct.Add(new TbtSaleBillingNoteProduct
                    {
                        BillingNoteRunning = running,
                        InvoiceRunning = product.InvoiceRunning,
                        ProductNumber = product.ProductNumber,
                        ProductType = product.ProductType,
                        ProductTypeName = product.ProductTypeName,
                        ProductionType = product.ProductionType,
                        Qty = product.Qty,
                        Amount = product.Amount,
                        Remark = product.Remark,

                        CreateBy = CurrentUsername,
                        CreateDate = DateTime.UtcNow
                    });
                }
            }

            await _jewelryContext.SaveChangesAsync();

            return running;
        }

        public async Task<jewelry.Model.Sale.BillingNote.Get.Response> Get(jewelry.Model.Sale.BillingNote.Get.Request request)
        {
            if (string.IsNullOrEmpty(request.Running))
            {
                throw new HandleException("Running is Required.");
            }

            var header = await _jewelryContext.TbtSaleBillingNoteHeader
                .Include(x => x.TbtSaleBillingNoteItem)
                .Include(x => x.TbtSaleBillingNoteProduct)
                .FirstOrDefaultAsync(x => x.Running == request.Running && x.IsDelete == false);

            if (header == null)
            {
                throw new HandleException($"Billing Note not found: {request.Running}");
            }

            return new jewelry.Model.Sale.BillingNote.Get.Response
            {
                Running = header.Running,
                DocumentDate = header.DocumentDate,

                CustomerCode = header.CustomerCode,
                CustomerName = header.CustomerName,
                CustomerAddress = header.CustomerAddress,
                CustomerTel = header.CustomerTel,

                BillCount = header.BillCount,

                GoldResizeQty = header.GoldResizeQty,
                GoldResizeAmount = header.GoldResizeAmount,
                SilverResizeQty = header.SilverResizeQty,
                SilverResizeAmount = header.SilverResizeAmount,

                SubTotal = header.SubTotal,
                VatPercent = header.VatPercent,
                VatAmount = header.VatAmount,
                GrandTotal = header.GrandTotal,

                Remark = header.Remark,

                Status = header.Status,
                StatusName = header.StatusName,

                CreateBy = header.CreateBy,
                CreateDate = header.CreateDate,
                UpdateBy = header.UpdateBy,
                UpdateDate = header.UpdateDate,

                Items = header.TbtSaleBillingNoteItem
                    .OrderBy(x => x.Seq)
                    .Select(x => new jewelry.Model.Sale.BillingNote.Get.Item
                    {
                        Seq = x.Seq,
                        InvoiceRunning = x.InvoiceRunning,
                        InvoiceDate = x.InvoiceDate,
                        AmountBeforeVat = x.AmountBeforeVat,
                        Remark = x.Remark
                    }).ToList(),

                Products = header.TbtSaleBillingNoteProduct
                    .Select(x => new jewelry.Model.Sale.BillingNote.Get.Product
                    {
                        InvoiceRunning = x.InvoiceRunning,
                        ProductNumber = x.ProductNumber,
                        ProductType = x.ProductType,
                        ProductTypeName = x.ProductTypeName,
                        ProductionType = x.ProductionType,
                        Qty = x.Qty,
                        Amount = x.Amount,
                        Remark = x.Remark
                    }).ToList()
            };
        }

        public IQueryable<jewelry.Model.Sale.BillingNote.List.Response> List(jewelry.Model.Sale.BillingNote.List.Request _request)
        {
            var request = _request.Search;

            var entityQuery = _jewelryContext.TbtSaleBillingNoteHeader.Where(x => x.IsDelete == false);

            if (!string.IsNullOrEmpty(request.Running))
            {
                entityQuery = entityQuery.Where(x => x.Running.Contains(request.Running));
            }

            if (!string.IsNullOrEmpty(request.CustomerName))
            {
                entityQuery = entityQuery.Where(x => x.CustomerName.Contains(request.CustomerName));
            }

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                entityQuery = entityQuery.Where(x => x.CustomerCode.Contains(request.CustomerCode));
            }

            if (request.Status.HasValue)
            {
                entityQuery = entityQuery.Where(x => x.Status == request.Status.Value);
            }

            if (!string.IsNullOrEmpty(request.CreateBy))
            {
                entityQuery = entityQuery.Where(x => x.CreateBy.Contains(request.CreateBy));
            }

            if (request.DocumentDateFrom.HasValue)
            {
                entityQuery = entityQuery.Where(x => x.DocumentDate >= request.DocumentDateFrom.Value.StartOfDayUtc());
            }

            if (request.DocumentDateTo.HasValue)
            {
                entityQuery = entityQuery.Where(x => x.DocumentDate <= request.DocumentDateTo.Value.EndOfDayUtc());
            }

            var query = from header in entityQuery
                        select new jewelry.Model.Sale.BillingNote.List.Response
                        {
                            Running = header.Running,
                            DocumentDate = header.DocumentDate,

                            CustomerCode = header.CustomerCode,
                            CustomerName = header.CustomerName,

                            BillCount = header.BillCount,

                            SubTotal = header.SubTotal,
                            VatPercent = header.VatPercent,
                            VatAmount = header.VatAmount,
                            GrandTotal = header.GrandTotal,

                            Status = header.Status,
                            StatusName = header.StatusName,

                            CreateBy = header.CreateBy,
                            CreateDate = header.CreateDate,
                            UpdateBy = header.UpdateBy,
                            UpdateDate = header.UpdateDate
                        };

            return query;
        }

        public async Task<string> Delete(jewelry.Model.Sale.BillingNote.Delete.Request request)
        {
            if (string.IsNullOrEmpty(request.Running))
            {
                throw new HandleException("Running is Required.");
            }

            var header = await _jewelryContext.TbtSaleBillingNoteHeader
                .FirstOrDefaultAsync(x => x.Running == request.Running && x.IsDelete == false);

            if (header == null)
            {
                throw new HandleException($"Billing Note not found: {request.Running}");
            }

            header.IsDelete = true;
            header.DeleteReason = request.DeleteReason;
            header.UpdateBy = CurrentUsername;
            header.UpdateDate = DateTime.UtcNow;

            _jewelryContext.TbtSaleBillingNoteHeader.Update(header);
            await _jewelryContext.SaveChangesAsync();

            return $"Billing Note {request.Running} deleted successfully";
        }
    }
}
