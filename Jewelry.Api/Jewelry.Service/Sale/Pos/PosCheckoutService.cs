using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Sale.Invoice;
using Jewelry.Service.Sale.SaleOrder;
using Jewelry.Service.Stock.Product;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.Pos
{
    public class PosCheckoutService : BaseService, IPosCheckoutService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly ISaleOrderService _saleOrderService;
        private readonly IInvoiceService _invoiceService;
        private readonly IProductService _productService;

        public PosCheckoutService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor,
            ISaleOrderService saleOrderService,
            IInvoiceService invoiceService,
            IProductService productService) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _saleOrderService = saleOrderService;
            _invoiceService = invoiceService;
            _productService = productService;
        }

        public async Task<jewelry.Model.Sale.Pos.Checkout.Response> Checkout(jewelry.Model.Sale.Pos.Checkout.Request request)
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                throw new HandleException("Idempotency Key is required.");
            }

            if (string.IsNullOrWhiteSpace(request.CustomerCode))
            {
                throw new HandleException("Customer Code is required.");
            }

            if (request.Items == null || !request.Items.Any())
            {
                throw new HandleException("At least one item is required.");
            }

            var existingCheckout = await FindExistingCheckout(request.IdempotencyKey);
            if (existingCheckout != null)
            {
                return ToResponse(existingCheckout, isDuplicate: true);
            }

            var customerName = string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerCode : request.CustomerName;
            var now = DateTime.UtcNow;

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                // Guard: กันขายชิ้นเดียวกันซ้ำ (path นี้เท่านั้น — ไม่แตะ SaleOrderService.ConfirmStockItems เดิม)
                await GuardStockAvailability(request.Items);

                // 1) Create Sale Order (reuse SaleOrderService.Upsert — always creation branch, no SoNumber)
                var subTotal = request.Items.Sum(i =>
                    i.AppraisalPrice * (1 - i.DiscountPercent / 100m) * i.Qty / request.CurrencyRate);

                // ประกอบ Data (stockItems/copyItems/allItems/freight/copyFreight) เหมือนที่หน้าเว็บ (sale-order-view.vue)
                // ส่งให้ SaleOrder.Upsert เก็บลง TbtSaleOrder.Data — ให้ SaleOrder/Invoice-Detail ฝั่งเว็บเปิดบิล POS แล้วเห็นรายการสินค้าครบเหมือนบิลที่สร้างจากเว็บ
                var soData = await BuildSaleOrderData(request.Items, request.FreightAndInsurance);

                var soRequest = new jewelry.Model.Sale.SaleOrder.Create.Request
                {
                    SODate = DateTimeOffset.UtcNow,
                    CustomerName = customerName,
                    CustomerCode = request.CustomerCode,
                    CustomerAddress = request.CustomerAddress,
                    CustomerTel = request.CustomerTel,
                    CustomerEmail = request.CustomerEmail,
                    CustomerRemark = request.CustomerRemark,
                    CurrencyUnit = request.CurrencyUnit,
                    CurrencyRate = request.CurrencyRate,
                    SpecialDiscount = request.SpecialDiscount,
                    SpecialAddition = request.SpecialAddition,
                    Vat = request.Vat,
                    Freight = request.FreightAndInsurance,
                    SubTotal = subTotal,
                    Remark = request.Remark,
                    Data = soData
                };

                var soNumber = await _saleOrderService.Upsert(soRequest);

                // 2) Confirm stock items (reuse SaleOrderService core loop — guard already passed above)
                var stockItems = request.Items.Select(i => new jewelry.Model.Sale.SaleOrder.ConfirmStock.StockItemConfirmation
                {
                    StockNumber = i.StockNumber,
                    ProductNumber = i.ProductNumber,
                    Qty = i.Qty,
                    AppraisalPrice = i.AppraisalPrice,
                    Discount = i.DiscountPercent
                }).ToList();

                await _saleOrderService.ConfirmStockItemsForPos(soNumber, stockItems, now);
                await _jewelryContext.SaveChangesAsync();

                // 3) Create Invoice (reuse InvoiceService.Create as-is)
                var firstPayment = request.Payments?.FirstOrDefault();
                var paymentTypeCode = request.Payments != null && request.Payments.Count > 1 ? 0 : (firstPayment?.Payment ?? 0);
                var paymentTypeName = request.Payments == null || !request.Payments.Any()
                    ? "ค้างชำระ"
                    : (request.Payments.Count > 1 ? "ชำระหลายช่องทาง" : firstPayment!.PaymentName);

                var invoiceRequest = new jewelry.Model.Sale.Invoice.Create.Request
                {
                    DKInvoiceNumber = request.DkInvoiceNumber,
                    SoNumber = soNumber,
                    CustomerCode = request.CustomerCode,
                    CustomerName = customerName,
                    CustomerAddress = request.CustomerAddress,
                    CustomerTel = request.CustomerTel,
                    CustomerEmail = request.CustomerEmail,
                    CustomerRemark = request.CustomerRemark,
                    CurrencyUnit = request.CurrencyUnit,
                    CurrencyRate = request.CurrencyRate,
                    Deposit = request.Deposit ?? 0,
                    Payment = paymentTypeCode,
                    PaymentName = paymentTypeName,
                    PaymentDay = request.PaymentDay ?? 0,
                    Priority = "Normal",
                    Remark = request.Remark,
                    SpecialDiscount = request.SpecialDiscount,
                    SpecialAddition = request.SpecialAddition,
                    FreightAndInsurance = request.FreightAndInsurance,
                    Vat = request.Vat,
                    Items = stockItems.Select(s => new jewelry.Model.Sale.Invoice.Create.InvoiceItem
                    {
                        StockNumber = s.StockNumber
                    }).ToList()
                };

                var invoiceNumber = await _invoiceService.Create(invoiceRequest);

                // 4) Create payment items (reuse InvoiceService.CreatePayment as-is, one call per payment)
                decimal paidAmount = 0;
                if (request.Payments != null)
                {
                    foreach (var payment in request.Payments)
                    {
                        var paymentRequest = new jewelry.Model.Sale.InvoicePayment.Create.Request
                        {
                            InvoiceNumber = invoiceNumber,
                            PaymentDate = payment.PaymentDate,
                            Amount = payment.Amount,
                            Payment = payment.Payment,
                            PaymentName = payment.PaymentName,
                            ReferenceNumber = payment.ReferenceNumber,
                            BankCode = payment.BankCode,
                            BankBranch = payment.BankBranch,
                            Remark = payment.Remark,
                            ReceiptImage = null
                        };

                        await _invoiceService.CreatePayment(paymentRequest);
                        paidAmount += payment.Amount;
                    }
                }

                var invoiceHeader = await _jewelryContext.TbtSaleInvoiceHeader
                    .FirstOrDefaultAsync(x => x.Running == invoiceNumber);
                var grandTotal = invoiceHeader?.GrandTotalRounded ?? 0;

                // 5) Persist idempotency record
                var checkoutRecord = new TbtPosCheckout
                {
                    IdempotencyKey = request.IdempotencyKey,
                    SoNumber = soNumber,
                    InvoiceNumber = invoiceNumber,
                    GrandTotal = grandTotal,
                    PaidAmount = paidAmount,
                    CreateDate = now,
                    CreateBy = CurrentUsername
                };
                _jewelryContext.TbtPosCheckout.Add(checkoutRecord);
                await _jewelryContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return ToResponse(checkoutRecord, isDuplicate: false);
            }
            catch (DbUpdateException dbEx) when (IsIdempotencyKeyUniqueViolation(dbEx))
            {
                // Client retried the exact same request twice concurrently — the other
                // request already committed the real SO/Invoice/Payment. Roll back this
                // one (avoids a duplicate bill) and return the winning result instead.
                await transaction.RollbackAsync();

                var winner = await FindExistingCheckout(request.IdempotencyKey);
                if (winner != null)
                {
                    return ToResponse(winner, isDuplicate: true);
                }

                throw new HandleException("POS Checkout ล้มเหลวเนื่องจากมีการส่งคำขอซ้ำพร้อมกัน กรุณาลองใหม่อีกครั้ง");
            }
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"POS Checkout failed: {ex.Message}");
            }
        }

        private async Task<TbtPosCheckout?> FindExistingCheckout(string idempotencyKey)
        {
            return await _jewelryContext.TbtPosCheckout
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);
        }

        private static jewelry.Model.Sale.Pos.Checkout.Response ToResponse(TbtPosCheckout checkout, bool isDuplicate)
        {
            return new jewelry.Model.Sale.Pos.Checkout.Response
            {
                SoNumber = checkout.SoNumber,
                InvoiceNumber = checkout.InvoiceNumber,
                GrandTotal = checkout.GrandTotal,
                PaidAmount = checkout.PaidAmount,
                RemainingAmount = checkout.GrandTotal - checkout.PaidAmount,
                IsDuplicate = isDuplicate
            };
        }

        private static bool IsIdempotencyKeyUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is PostgresException pgEx
                && pgEx.SqlState == "23505"
                && (pgEx.ConstraintName == null || pgEx.ConstraintName == "tbt_pos_checkout_pk");
        }

        // กันขายชิ้นเดียวกันซ้ำ — ล็อกแถว TbtStockPiece ด้วย SELECT ... FOR UPDATE แล้วตรวจสถานะ
        // + ตรวจว่าไม่มี TbtSaleOrderProduct ค้างอยู่ของ StockNumber นั้น (path POS เท่านั้น)
        private async Task GuardStockAvailability(List<jewelry.Model.Sale.Pos.Checkout.CheckoutItem> items)
        {
            // ล็อกตามลำดับ StockNumber เดียวกันเสมอ กัน deadlock เมื่อ 2 บิลขายสินค้าชุดที่ทับกันพร้อมกัน
            var orderedItems = items.OrderBy(i => i.StockNumber, StringComparer.Ordinal).ToList();

            foreach (var item in orderedItems)
            {
                var stockNumber = item.StockNumber;

                var pieces = await _jewelryContext.TbtStockPiece
                    .FromSqlInterpolated($"SELECT * FROM tbt_stock_piece WHERE stock_number = {stockNumber} FOR UPDATE")
                    .ToListAsync();

                if (!pieces.Any())
                {
                    throw new HandleException($"ไม่พบสินค้า {stockNumber} ในระบบสต็อก");
                }

                var existingConfirmation = await _jewelryContext.TbtSaleOrderProduct
                    .Where(p => p.StockNumber == stockNumber)
                    .OrderByDescending(p => p.CreateDate)
                    .FirstOrDefaultAsync();

                var blockedPiece = pieces.FirstOrDefault(p => p.Status == "RESERVED" || p.Status == "SOLD");

                if (blockedPiece != null || existingConfirmation != null)
                {
                    var soLabel = existingConfirmation?.SoNumber ?? "ไม่ทราบเลขที่";
                    var sellerLabel = existingConfirmation?.CreateBy ?? "ไม่ทราบผู้ขาย";
                    throw new HandleException($"สินค้า {stockNumber} ถูกขายไปแล้วในบิล {soLabel} (โดย {sellerLabel})");
                }
            }
        }

        private static readonly JsonSerializerOptions SaleOrderDataJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // ประกอบ SaleOrder.Data ให้ตรง contract เดียวกับที่หน้าเว็บสร้าง (sale-order-view.vue fetchSaveSaleOrder)
        // reuse ProductService.Get (ตัวเดียวกับ StockProduct/Get) ต่อ 1 StockNumber — POS 1 บิลมีไม่กี่ชิ้น จึงไม่ทำ batch query เพิ่ม
        private async Task<string> BuildSaleOrderData(List<jewelry.Model.Sale.Pos.Checkout.CheckoutItem> items, decimal freight)
        {
            var stockItems = new List<object>();

            foreach (var item in items)
            {
                var product = await _productService.Get(new jewelry.Model.Stock.Product.Get.Request
                {
                    StockNumber = item.StockNumber
                });

                stockItems.Add(BuildStockItem(product, item));
            }

            var data = new
            {
                stockItems,
                copyItems = new List<object>(),
                allItems = stockItems,
                freight,
                copyFreight = 0m
            };

            return JsonSerializer.Serialize(data, SaleOrderDataJsonOptions);
        }

        // mapping เดียวกับที่ sale-order-view.vue ทำตอนเพิ่มสินค้าจาก StockProduct/Get (onSearchProduct)
        // + ทับด้วยค่าจริงจากการขาย (appraisalPrice/qty) + isConfirm/isInvoice = true (POS confirm และออก invoice ในคราวเดียว)
        private static object BuildStockItem(jewelry.Model.Stock.Product.Get.Response product, jewelry.Model.Sale.Pos.Checkout.CheckoutItem item)
        {
            var stockNumberOrigin = string.IsNullOrEmpty(product.StockNumberOrigin) ? product.StockNumber : product.StockNumberOrigin;

            return new
            {
                stockNumber = product.StockNumber,
                stockNumberOrigin,
                receiptNumber = product.ReceiptNumber,
                receiptType = product.ReceiptType,
                receiptDate = product.ReceiptDate,

                productNumber = product.ProductNumber,
                productNameEn = product.ProductNameEn,
                productNameTh = product.ProductNameTh,
                productTypeName = product.ProductTypeName,
                productType = product.ProductType,
                productPrice = product.ProductPrice,

                wo = product.Wo,
                woNumber = product.WoNumber,
                woText = product.WoText,

                productionType = product.ProductionType,
                productionTypeSize = product.ProductionTypeSize,
                productionDate = product.ProductionDate,
                mold = product.Mold,

                imageName = product.ImageName,
                imagePath = product.ImagePath,
                imageBlobPath = (string?)null,

                qty = item.Qty,
                location = product.Location,
                size = product.Size,
                remark = product.Remark,

                createDate = product.CreateDate,
                createBy = product.CreateBy,
                updateDate = product.UpdateDate,
                updateBy = product.UpdateBy,

                planQty = product.PlanQty ?? 1,
                tagPriceMultiplier = product.TagPriceMultiplier,

                materials = product.Materials,
                priceTransactions = product.PriceTransactions,
                planPriceItems = product.PlanPriceItems,

                price = product.ProductPrice,
                appraisalPrice = item.AppraisalPrice,
                description = product.ProductNameEn,
                group = "product",

                isRemainProduct = true,
                isConfirm = true,
                isInvoice = true
            };
        }
    }
}
