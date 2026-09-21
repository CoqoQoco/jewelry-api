using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.Master.SaleChannel;
using Jewelry.Service.Sale.SaleOrder;
using Jewelry.Service.Sale.SaleOrderDeposit;
using Jewelry.Service.Stock;
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
        private readonly IAzureBlobStorageService _azureBlobService;
        private readonly ISaleOrderService _saleOrderService;
        private readonly ISaleChannelService _saleChannelService;

        public InvoiceService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor,
            IHostEnvironment hostingEnvironment,
            IRunningNumber runningNumberService,
            IAzureBlobStorageService azureBlobService,
            ISaleOrderService saleOrderService,
            ISaleChannelService saleChannelService) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _hostingEnvironment = hostingEnvironment;
            _runningNumberService = runningNumberService;
            _azureBlobService = azureBlobService;
            _saleOrderService = saleOrderService;
            _saleChannelService = saleChannelService;
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

            var stockArray = request.Items.Select(i => i.StockNumber).ToArray();

            // Line-aware selection: ถ้าทุกบรรทัดส่ง SaleOrderProductId มา ใช้ id เลือกแถวแบบเจาะจง (รองรับ SO เดียวกันที่มี StockNumber ซ้ำจากล็อตเงิน re-scan)
            // ถ้าไม่มีบรรทัดไหนส่งมาเลย ใช้พฤติกรรมเดิม (match ด้วย StockNumber) เพื่อไม่กระทบ POS/client เก่า
            var itemsWithId = request.Items.Count(i => i.SaleOrderProductId.HasValue);
            var useIds = itemsWithId == request.Items.Count;

            if (itemsWithId > 0 && !useIds)
            {
                throw new HandleException("รายการใบแจ้งหนี้ต้องระบุ SaleOrderProductId ครบทุกบรรทัด หรือไม่ระบุเลย");
            }

            if (useIds)
            {
                var idList = request.Items.Select(i => i.SaleOrderProductId!.Value).ToList();
                if (idList.Distinct().Count() != idList.Count)
                {
                    throw new HandleException("มี SaleOrderProductId ซ้ำกันในรายการใบแจ้งหนี้");
                }
            }

            // ถ้าเรียกจาก POS transaction จะเปิดอยู่แล้ว (PosCheckoutService) — ใช้ transaction เดิม ไม่เปิดซ้อน
            var ownsTransaction = _jewelryContext.Database.CurrentTransaction == null;
            var transaction = ownsTransaction
                ? await _jewelryContext.Database.BeginTransactionAsync()
                : null;

            try
            {
            // ล็อกแถวมัดจำของ SO ก่อน (ถ้าจะหักมัดจำ) แล้วค่อยล็อก piece แล้วค่อยล็อก balance แล้วค่อยล็อกแถว SO product ตามลำดับ global: invoice header → SO deposit → piece → balance → SO product (กัน deadlock)
            if (request.DepositApplyAmount.HasValue && request.DepositApplyAmount.Value > 0)
            {
                await _jewelryContext.LockSaleOrderDepositsAsync(request.SoNumber);
            }

            await _jewelryContext.LockStockPiecesAsync(stockArray);
            await LockStockBalancesForStockNumbersAsync(stockArray);

            if (useIds)
            {
                var lockIds = request.Items.Select(i => i.SaleOrderProductId!.Value).ToList();
                await _jewelryContext.LockSaleOrderProductsByIdsAsync(lockIds);
            }
            else
            {
                await _jewelryContext.LockSaleOrderProductsAsync(request.SoNumber, stockArray);
            }

            //check duplicate DK Invoice Number
            if (!string.IsNullOrEmpty(request.DKInvoiceNumber))
            {
                var dkInvoiceExists = await _jewelryContext.TbtSaleInvoiceHeader
                    .AnyAsync(x => x.DkInvoiceNumber == request.DKInvoiceNumber && x.IsDelete == false);

                if (dkInvoiceExists)
                {
                    throw new HandleException($"DK Invoice Number {request.DKInvoiceNumber} already exists.");
                }
            }

            //check all stock not exist invoice — ต้องรันหลังล็อกแถวข้างบน กันสองคำขอออก invoice ซ้อนกันเห็น Invoice == null พร้อมกันทั้งคู่
            List<TbtSaleOrderProduct> getstockConfrim;

            if (useIds)
            {
                var ids = request.Items.Select(i => i.SaleOrderProductId!.Value).ToList();

                var rowsById = await _jewelryContext.TbtSaleOrderProduct
                    .Where(x => x.SoNumber == request.SoNumber && ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id);

                var badLines = new List<string>();
                foreach (var item in request.Items)
                {
                    var id = item.SaleOrderProductId!.Value;
                    if (!rowsById.TryGetValue(id, out var row))
                    {
                        badLines.Add($"SaleOrderProductId {id} (StockNumber {item.StockNumber}) ไม่พบในใบสั่งขาย {request.SoNumber}");
                        continue;
                    }
                    if (row.StockNumber != item.StockNumber)
                    {
                        badLines.Add($"SaleOrderProductId {id} เป็นสินค้า {row.StockNumber} ไม่ตรงกับ StockNumber {item.StockNumber} ที่ส่งมา");
                    }
                }

                if (badLines.Any())
                {
                    throw new HandleException($"รายการใบแจ้งหนี้ไม่ถูกต้อง: {string.Join("; ", badLines)}");
                }

                getstockConfrim = ids.Select(id => rowsById[id]).ToList();
            }
            else
            {
                getstockConfrim = await _jewelryContext.TbtSaleOrderProduct
                    .Where(x => x.SoNumber == request.SoNumber && stockArray.Contains(x.StockNumber))
                    .ToListAsync();

                if (!getstockConfrim.Any())
                {
                    throw new HandleException("No matching Sale Order Products found for the provided items.");
                }
            }

            // รายการรอของ (placeholder) ยังไม่มีชิ้นจริงในคลัง ห้ามออกใบแจ้งหนี้เด็ดขาดจนกว่าจะเติมของจริงแทนที่
            var placeholderRows = getstockConfrim.Where(x => x.IsPlaceholder).ToList();
            if (placeholderRows.Any())
            {
                var placeholderStockNumbers = string.Join(", ", placeholderRows.Select(x => x.StockNumber).Distinct());
                throw new HandleException($"บรรทัด {placeholderStockNumbers} ยังไม่มีของจริงในคลัง ต้องเติมของก่อนออกใบแจ้งหนี้");
            }

            if (getstockConfrim.Any(x => !string.IsNullOrEmpty(x.Invoice)))
            {
                throw new HandleException("One or more items have already been invoiced.");
            }

            // Generate invoice number
            var invoiceNumber = await GenerateInvoiceNumber();

            // Compute totals from confirmed items — ไม่ปัดเศษราคาต่อชิ้นหรือยอดต่อแถว คิดเต็มความละเอียดแล้วปัดครั้งเดียวที่ยอดสุดท้าย (ต้องตรงกับฝั่ง UI)
            var subTotal = getstockConfrim.Sum(x =>
                x.PriceOrigin * (1 - (x.Discount ?? 0) / 100m) / request.CurrencyRate * x.Qty);
            var t = MathHelper.ComputeTotals(subTotal, request.SpecialDiscount, request.SpecialAddition, request.FreightAndInsurance, request.Vat);

            // ดึงใบสั่งขายเพื่อสแนปช็อตผู้ขาย/ผู้ช่วยขายมาเก็บที่ invoice ณ ตอนสร้าง — ถ้าไม่พบ SO ก็ไม่ throw ให้ปล่อยเป็น null (เป็นแค่ข้อมูลประกอบใบพิมพ์)
            var saleOrder = await _jewelryContext.TbtSaleOrder
                .FirstOrDefaultAsync(x => x.SoNumber == request.SoNumber);

            // จุดขาย: SO เป็นตัวตั้ง ใบแจ้งหนี้ต้องจุดขายเดียวกับ SO เสมอ ถ้า SO มีจุดขายอยู่แล้วให้ใช้ค่านั้น ไม่สนใจค่าที่ request ส่งมา
            string? saleChannelCode;
            if (saleOrder != null && !string.IsNullOrEmpty(saleOrder.SaleChannelCode))
            {
                saleChannelCode = saleOrder.SaleChannelCode;
            }
            else
            {
                // ยังไม่เคยมีจุดขาย: ใช้ค่าที่ request ส่งมา ถ้าไม่ส่งมาให้เลือกอัตโนมัติจากช่องที่ active ในวันนี้
                saleChannelCode = request.SaleChannelCode;
                if (string.IsNullOrEmpty(saleChannelCode))
                {
                    saleChannelCode = (await _saleChannelService.Current())?.Code;
                }

                if (saleOrder != null && !string.IsNullOrEmpty(saleChannelCode))
                {
                    saleOrder.SaleChannelCode = saleChannelCode;
                    saleOrder.UpdateBy = CurrentUsername;
                    saleOrder.UpdateDate = DateTime.UtcNow;
                    _jewelryContext.TbtSaleOrder.Update(saleOrder);

                    // sync จุดขายไปยังใบแจ้งหนี้เดิมของ SO นี้ที่ยังจุดขายไม่ตรงกันด้วย — ไม่แตะ UpdateBy/UpdateDate เพราะฟิลด์นี้ใช้บันทึกว่าใครลบใบแจ้งหนี้เมื่อไหร่
                    var invoicesToSync = await _jewelryContext.TbtSaleInvoiceHeader
                        .Where(x => x.SoRunning == request.SoNumber && x.SaleChannelCode != saleChannelCode)
                        .ToListAsync();

                    if (invoicesToSync.Any())
                    {
                        foreach (var inv in invoicesToSync)
                        {
                            inv.SaleChannelCode = saleChannelCode;
                        }
                        _jewelryContext.TbtSaleInvoiceHeader.UpdateRange(invoicesToSync);
                    }
                }
            }

            var createDate = DateTime.UtcNow;

            // หักมัดจำของ SO เข้าใบแจ้งหนี้นี้ (ถ้าระบุ DepositApplyAmount มา) — allocate แบบ FIFO ตาม depositDate/createDate ของมัดจำแต่ละครั้ง
            var depositApplies = new List<TbtSaleOrderDepositApply>();
            decimal? depositApplyAmount = (request.DepositApplyAmount.HasValue && request.DepositApplyAmount.Value > 0)
                ? request.DepositApplyAmount.Value
                : null;

            if (depositApplyAmount.HasValue)
            {
                var depositBalance = await SaleOrderDepositHelper.GetBalanceAsync(_jewelryContext, request.SoNumber);
                if (depositApplyAmount.Value > depositBalance)
                {
                    throw new HandleException($"ยอดหักมัดจำ ({depositApplyAmount.Value:N2}) มากกว่ายอดมัดจำคงเหลือของใบสั่งขาย {request.SoNumber} ({depositBalance:N2})");
                }

                if (depositApplyAmount.Value > t.rounded)
                {
                    throw new HandleException("ยอดหักมัดจำต้องไม่มากกว่ายอดรวมใบแจ้งหนี้");
                }

                var remainingToApply = depositApplyAmount.Value;
                var availableDeposits = await SaleOrderDepositHelper.GetAvailableDepositsAsync(_jewelryContext, request.SoNumber);

                foreach (var (availableDeposit, remainingOnDeposit) in availableDeposits)
                {
                    if (remainingToApply <= 0) break;

                    var take = Math.Min(remainingToApply, remainingOnDeposit);
                    if (take <= 0) continue;

                    depositApplies.Add(new TbtSaleOrderDepositApply
                    {
                        DepositRunning = availableDeposit.Running,
                        SoNumber = request.SoNumber,
                        InvoiceRunning = invoiceNumber,
                        Amount = take,
                        IsDelete = false,
                        CreateBy = CurrentUsername,
                        CreateDate = createDate
                    });

                    remainingToApply -= take;
                }

                if (remainingToApply > 0)
                {
                    throw new HandleException("ยอดมัดจำคงเหลือไม่พอสำหรับหักตามจำนวนที่ระบุ");
                }
            }

            // Create invoice header
            var invoiceHeader = new TbtSaleInvoiceHeader
            {
                Running = invoiceNumber,
                DkInvoiceNumber = request.DKInvoiceNumber,
                SoRunning = request.SoNumber,

                CreateBy = CurrentUsername,
                CreateDate = createDate,

                CurrencyRate = request.CurrencyRate,
                CurrencyUnit = request.CurrencyUnit,
                CustomerAddress = request.CustomerAddress,
                CustomerCode = request.CustomerCode,
                CustomerEmail = request.CustomerEmail,
                CustomerName = request.CustomerName,
                CustomerRemark = request.CustomerRemark,
                CustomerTel = request.CustomerTel,

                DeliveryDate = request.DeliveryDate.HasValue ? request.DeliveryDate.Value.UtcDateTime : null,
                Deposit = depositApplyAmount ?? request.Deposit,

                GoldRate = request.GoldRate,
                Markup = request.Markup,

                PaymantName = request.PaymentName,
                Payment = request.Payment,
                PaymentDay = request.PaymentDay,

                Priority = request.Priority,
                RefQuotation = request.RefQuotation,
                Remark = request.Remark,

                // สแนปช็อตผู้ขาย/ผู้ช่วยขายจาก SO ณ ตอนสร้าง invoice เพื่อไม่ให้ใบพิมพ์เปลี่ยนตามเมื่อ SO ถูกแก้ไขภายหลัง
                SalePerson = saleOrder?.SalePerson,
                SaleSupport = saleOrder?.SaleSupport,
                SalePersonUsername = saleOrder?.SalePersonUsername,

                SaleChannelCode = saleChannelCode,
                DueDate = createDate.AddDays(request.PaymentDay),

                Status = 100,
                StatusName = "invoice",

                SpecialDiscount = request.SpecialDiscount,
                SpecialAddition = request.SpecialAddition,
                FreightAndInsurance = request.FreightAndInsurance,
                Vat = request.Vat,

                SubTotal = t.subTotal,
                SpecialDiscountAmt = request.SpecialDiscount,
                SpecialAdditionAmt = request.SpecialAddition,
                FreightAmt = request.FreightAndInsurance,
                VatAmount = t.vatAmount,
                GrandTotalRaw = t.raw,
                GrandTotalRounded = t.rounded,
                RoundingAdjustment = t.adjustment,
            };

            _jewelryContext.TbtSaleInvoiceHeader.Add(invoiceHeader);

            if (depositApplies.Any())
            {
                _jewelryContext.TbtSaleOrderDepositApply.AddRange(depositApplies);
            }

            // Update sale order products with invoice information
            foreach (var item in getstockConfrim)
            {
                //var saleOrderProduct = await _jewelryContext.TbtSaleOrderProduct
                //    .FirstOrDefaultAsync(x => x.SoNumber == request.SoNumber 
                //                            && x.StockNumber == item.StockNumber 
                //                            && x.Id == item.Id);
                item.Invoice = invoiceNumber;
                item.InvoiceItem = $"{invoiceNumber}-{item.Id}";
                item.DkInvoiceNumber = request.DKInvoiceNumber;

                item.UpdateBy = CurrentUsername;
                item.UpdateDate = DateTime.UtcNow;
            }
            _jewelryContext.TbtSaleOrderProduct.UpdateRange(getstockConfrim);

            foreach (var soProduct in getstockConfrim)
            {
                var piece = await _jewelryContext.TbtStockPiece
                    .FirstOrDefaultAsync(p => p.StockNumber == soProduct.StockNumber);

                // ผ่านการ block placeholder ข้างบนมาแล้ว แถวที่เหลือทุกแถวต้องเป็นของจริง ไม่พบ piece = ข้อมูลผิดปกติ
                // ต้อง throw ไม่ใช่ continue เงียบๆ (เดิมทำให้ตัดสต็อกไม่ครบแต่ invoice ออกไปแล้วโดยไม่มีใครรู้)
                if (piece == null)
                {
                    throw new HandleException($"ไม่พบเลขสินค้า {soProduct.StockNumber} ในคลัง ไม่สามารถออกใบแจ้งหนี้ได้");
                }

                var balance = await _jewelryContext.TbtStockBalance
                    .FirstOrDefaultAsync(b => b.SkuCode == piece.SkuCode && b.LocationCode == piece.LocationCode);

                if (balance == null) continue;

                piece.Qty -= soProduct.Qty;
                piece.QtyReserved -= soProduct.Qty;
                StockPieceQtyHelper.RecalcStatus(piece);
                piece.UpdateBy = CurrentUsername;
                piece.UpdateDate = DateTime.UtcNow;

                balance.QtyOnHand -= soProduct.Qty;
                balance.QtyReserved -= soProduct.Qty;
                balance.LastMovementAt = DateTime.UtcNow;
                balance.UpdateBy = CurrentUsername;
                balance.UpdateDate = DateTime.UtcNow;

                _jewelryContext.TbtStockPiece.Update(piece);
                _jewelryContext.TbtStockBalance.Update(balance);

                _jewelryContext.TbtStockMovement.Add(new TbtStockMovement
                {
                    MovementType = "SALE",
                    SkuCode = piece.SkuCode,
                    StockNumber = piece.StockNumber,
                    ProductCode = piece.ProductCode,
                    FromLocation = piece.LocationCode,
                    Qty = soProduct.Qty,
                    RefDocType = "INVOICE",
                    RefDocNo = invoiceNumber,
                    MovementDate = DateTime.UtcNow,
                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername
                });
            }

            await _jewelryContext.SaveChangesAsync();

            if (ownsTransaction)
            {
                await transaction!.CommitAsync();
            }

            return invoiceNumber;
            }
            catch
            {
                if (ownsTransaction)
                {
                    await transaction!.RollbackAsync();
                }
                throw;
            }
            finally
            {
                if (ownsTransaction)
                {
                    await transaction!.DisposeAsync();
                }
            }
        }

        public async Task<jewelry.Model.Sale.Invoice.Get.Response> Get(jewelry.Model.Sale.Invoice.Get.Request request)
        {
            if (string.IsNullOrEmpty(request.InvoiceNumber))
            {
                throw new HandleException("Invoice Number is Required.");
            }

            var invoiceHeader = await _jewelryContext.TbtSaleInvoiceHeader
                                                     .Include(x => x.TbtSaleInvoicePaymentItem)
                                                     .FirstOrDefaultAsync(x => x.Running == request.InvoiceNumber
                                                                            && x.IsDelete == false);

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
            var confirmedItems = await (
                from sop in _jewelryContext.TbtSaleOrderProduct
                join piece in _jewelryContext.TbtStockPiece on sop.StockNumber equals piece.StockNumber into pieceJoin
                from piece in pieceJoin.DefaultIfEmpty()
                join sku in _jewelryContext.TbtSku on piece.SkuCode equals sku.SkuCode into skuJoin
                from sku in skuJoin.DefaultIfEmpty()
                where sop.Invoice == request.InvoiceNumber
                select new jewelry.Model.Sale.Invoice.Get.Item
                {
                    Id = sop.Id,
                    StockNumber = sop.StockNumber,
                    LineKey = sop.LineKey,
                    IsConfirmed = true,
                    Invoice = sop.Invoice,
                    InvoiceItem = sop.InvoiceItem,
                    EarringStemSize = sku != null ? sku.EarringStemSize : null
                }
            ).ToListAsync();

            var response = new jewelry.Model.Sale.Invoice.Get.Response
            {
                InvoiceNumber = invoiceHeader.Running,
                DKInvoiceNumber = invoiceHeader.DkInvoiceNumber,
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

                // อ่านจาก invoiceHeader (สแนปช็อต ณ ตอนสร้าง) ไม่ใช่จาก saleOrderHeader เพื่อให้ใบพิมพ์ไม่เปลี่ยนตาม SO ที่แก้ไขภายหลัง
                SalePerson = invoiceHeader.SalePerson,
                SaleSupport = invoiceHeader.SaleSupport,

                Status = invoiceHeader.Status,
                StatusName = invoiceHeader.StatusName,

                SpecialDiscount = invoiceHeader.SpecialDiscount,
                SpecialAddition = invoiceHeader.SpecialAddition,
                FreightAndInsurance = invoiceHeader.FreightAndInsurance,
                Vat = invoiceHeader.Vat,

                SubTotal = invoiceHeader.SubTotal,
                SpecialDiscountAmt = invoiceHeader.SpecialDiscountAmt,
                SpecialAdditionAmt = invoiceHeader.SpecialAdditionAmt,
                FreightAmt = invoiceHeader.FreightAmt,
                VatAmount = invoiceHeader.VatAmount,
                GrandTotalRaw = invoiceHeader.GrandTotalRaw,
                GrandTotalRounded = invoiceHeader.GrandTotalRounded,
                RoundingAdjustment = invoiceHeader.RoundingAdjustment,

                ConfirmedItems = confirmedItems
            };

            if (invoiceHeader.TbtSaleInvoicePaymentItem.Any())
            {
                response.Payments = invoiceHeader.TbtSaleInvoicePaymentItem
                    .Where(x => x.IsDelete == false)
                    .Select(x => new jewelry.Model.Sale.Invoice.Get.InvoicePaymentItem
                    {
                        Running = x.Running,
                        PaymentDate = x.PaymentDate,

                        Amount = x.Amount,
                        CurrencyUnit = x.CurrencyUnit,

                        PaymentMethod = x.PaymantName,
                        Payment = x.Payment,
                        BankCode = x.BankCode,
                        ReferenceNumber = x.ReferenceNumber1,
                        Remark = x.Remark,
                        ImagePath = x.ImagePath,
                        CreateBy = x.CreateBy,
                        CreateDate = x.CreateDate,
                        UpdateBy = x.UpdateBy,
                        UpdateDate = x.UpdateDate
                    })
                    .ToList();
            }

            return response;
        }

        public IQueryable<jewelry.Model.Sale.Invoice.List.Response> List(jewelry.Model.Sale.Invoice.List.Request _request)
        {
            var request = _request.Search;

            var entityQuery = _jewelryContext.TbtSaleInvoiceHeader.Where(invoice => invoice.IsDelete == false);

            // Apply entity-level filters (before projection)
            if (!string.IsNullOrEmpty(request.InvoiceNumber))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(request.InvoiceNumber)}%";
                entityQuery = entityQuery.Where(x => EF.Functions.ILike(x.Running, pattern));
            }

            if (!string.IsNullOrEmpty(request.DKInvoiceNumber))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(request.DKInvoiceNumber)}%";
                entityQuery = entityQuery.Where(x => x.DkInvoiceNumber != null && EF.Functions.ILike(x.DkInvoiceNumber, pattern));
            }

            if (!string.IsNullOrEmpty(request.CustomerName))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(request.CustomerName)}%";
                entityQuery = entityQuery.Where(x => EF.Functions.ILike(x.CustomerName, pattern));
            }

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(request.CustomerCode)}%";
                entityQuery = entityQuery.Where(x => EF.Functions.ILike(x.CustomerCode, pattern));
            }

            if (request.Status.HasValue)
            {
                entityQuery = entityQuery.Where(x => x.Status == request.Status.Value);
            }

            if (!string.IsNullOrEmpty(request.CreateBy))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(request.CreateBy)}%";
                entityQuery = entityQuery.Where(x => EF.Functions.ILike(x.CreateBy, pattern));
            }

            if (!string.IsNullOrEmpty(request.SalePerson))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(request.SalePerson)}%";
                entityQuery = entityQuery.Where(x => x.SalePerson != null && EF.Functions.ILike(x.SalePerson, pattern));
            }

            if (!string.IsNullOrEmpty(request.SaleSupport))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(request.SaleSupport)}%";
                entityQuery = entityQuery.Where(x => x.SaleSupport != null && EF.Functions.ILike(x.SaleSupport, pattern));
            }

            if (request.CreateDateFrom.HasValue)
            {
                entityQuery = entityQuery.Where(x => x.CreateDate >= request.CreateDateFrom.Value.StartOfDayUtc());
            }

            if (request.CreateDateTo.HasValue)
            {
                entityQuery = entityQuery.Where(x => x.CreateDate <= request.CreateDateTo.Value.EndOfDayUtc());
            }

            if (request.DeliveryDateFrom.HasValue)
            {
                entityQuery = entityQuery.Where(x => x.DeliveryDate.HasValue && x.DeliveryDate.Value >= request.DeliveryDateFrom.Value.StartOfDayUtc());
            }

            if (request.DeliveryDateTo.HasValue)
            {
                entityQuery = entityQuery.Where(x => x.DeliveryDate.HasValue && x.DeliveryDate.Value <= request.DeliveryDateTo.Value.EndOfDayUtc());
            }

            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(request.StockNumber)}%";
                entityQuery = entityQuery.Where(x => _jewelryContext.TbtSaleOrderProduct
                    .Any(p => p.Invoice == x.Running
                        && (EF.Functions.ILike(p.StockNumber, pattern)
                            || _jewelryContext.TbtStockPiece.Any(piece => piece.StockNumber == p.StockNumber
                                && piece.StockNumberOrigin != null
                                && EF.Functions.ILike(piece.StockNumberOrigin, pattern)))));
            }

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(request.ProductNumber)}%";
                entityQuery = entityQuery.Where(x => _jewelryContext.TbtSaleOrderProduct
                    .Any(p => p.Invoice == x.Running
                        && _jewelryContext.TbtStockPiece
                            .Any(piece => piece.StockNumber == p.StockNumber && EF.Functions.ILike(piece.ProductCode, pattern))));
            }

            if (!string.IsNullOrEmpty(request.MoldNumber))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(request.MoldNumber)}%";
                entityQuery = entityQuery.Where(x => _jewelryContext.TbtSaleOrderProduct
                    .Any(p => p.Invoice == x.Running
                        && _jewelryContext.TbtStockPiece
                            .Any(piece => piece.StockNumber == p.StockNumber
                                && _jewelryContext.TbtSku.Any(sku => sku.SkuCode == piece.SkuCode && sku.MoldDesign != null && EF.Functions.ILike(sku.MoldDesign, pattern)))));
            }

            if (!string.IsNullOrEmpty(request.SaleChannelCode))
            {
                entityQuery = entityQuery.Where(x => x.SaleChannelCode == request.SaleChannelCode);
            }

            if (!string.IsNullOrEmpty(request.OwnerUsername))
            {
                entityQuery = entityQuery.Where(x =>
                    (string.IsNullOrEmpty(x.SalePersonUsername) ? x.CreateBy : x.SalePersonUsername) == request.OwnerUsername);
            }

            if (request.OverdueOnly == true)
            {
                var todayUtc = DateTime.UtcNow.Date;
                entityQuery = entityQuery.Where(x => (x.DueDate ?? x.CreateDate) < todayUtc);
            }

            if (!string.IsNullOrEmpty(request.PaymentStatus))
            {
                if (request.PaymentStatus == "paid")
                {
                    entityQuery = entityQuery.Where(x =>
                        (x.GrandTotalRounded ?? 0) - x.Deposit -
                        _jewelryContext.TbtSaleInvoicePaymentItem
                            .Where(p => p.InvoiceRunning == x.Running && p.IsDelete == false)
                            .Sum(p => p.Amount) <= 0);
                }
                else if (request.PaymentStatus == "unpaid")
                {
                    entityQuery = entityQuery.Where(x =>
                        (x.GrandTotalRounded ?? 0) - x.Deposit -
                        _jewelryContext.TbtSaleInvoicePaymentItem
                            .Where(p => p.InvoiceRunning == x.Running && p.IsDelete == false)
                            .Sum(p => p.Amount) > 0
                        && _jewelryContext.TbtSaleInvoicePaymentItem
                            .Where(p => p.InvoiceRunning == x.Running && p.IsDelete == false)
                            .Sum(p => p.Amount) == 0
                        && x.Deposit == 0);
                }
                else if (request.PaymentStatus == "partial")
                {
                    entityQuery = entityQuery.Where(x =>
                        (x.GrandTotalRounded ?? 0) - x.Deposit -
                        _jewelryContext.TbtSaleInvoicePaymentItem
                            .Where(p => p.InvoiceRunning == x.Running && p.IsDelete == false)
                            .Sum(p => p.Amount) > 0
                        && (_jewelryContext.TbtSaleInvoicePaymentItem
                                .Where(p => p.InvoiceRunning == x.Running && p.IsDelete == false)
                                .Sum(p => p.Amount) > 0
                            || x.Deposit > 0));
                }
            }

            var query = from invoice in entityQuery
                        select new jewelry.Model.Sale.Invoice.List.Response
                        {
                            InvoiceNumber = invoice.Running,
                            DKInvoiceNumber = invoice.DkInvoiceNumber,

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

                            Status = invoice.Status,
                            StatusName = invoice.StatusName,

                            SalePerson = invoice.SalePerson,
                            SaleSupport = invoice.SaleSupport,

                            GrandTotalRounded = invoice.GrandTotalRounded,
                            Deposit = invoice.Deposit,

                            ItemCount = _jewelryContext.TbtSaleOrderProduct.Count(x => x.Invoice == invoice.Running),
                            PaidAmount = _jewelryContext.TbtSaleInvoicePaymentItem
                                .Where(p => p.InvoiceRunning == invoice.Running && p.IsDelete == false)
                                .Sum(p => p.Amount),
                            //TotalAmount = _jewelryContext.TbtSaleOrderProduct
                            //    .Where(x => x.Invoice == invoice.Running)
                            //    .Sum(x => x.PriceAfterCurrecyRate * x.Qty)

                            SaleChannelCode = invoice.SaleChannelCode,
                            SaleChannelName = _jewelryContext.TbmSaleChannel
                                .Where(c => c.Code == invoice.SaleChannelCode)
                                .Select(c => c.NameTh)
                                .FirstOrDefault(),
                            DueDate = invoice.DueDate,
                            OwnerUsername = !string.IsNullOrEmpty(invoice.SalePersonUsername) ? invoice.SalePersonUsername : invoice.CreateBy,

                            OutstandingAmount = (invoice.GrandTotalRounded ?? 0) - invoice.Deposit -
                                _jewelryContext.TbtSaleInvoicePaymentItem
                                    .Where(p => p.InvoiceRunning == invoice.Running && p.IsDelete == false)
                                    .Sum(p => p.Amount),
                        };

            return query;
        }

        public async Task<List<jewelry.Model.Sale.Invoice.MoldSuggest.Response>> MoldSuggest(jewelry.Model.Sale.Invoice.MoldSuggest.Request request)
        {
            var query = from sop in _jewelryContext.TbtSaleOrderProduct
                        where sop.Invoice != null && sop.Invoice != ""
                        join piece in _jewelryContext.TbtStockPiece on sop.StockNumber equals piece.StockNumber
                        join sku in _jewelryContext.TbtSku on piece.SkuCode equals sku.SkuCode
                        where sku.MoldDesign != null
                        select new { Invoice = sop.Invoice!, MoldDesign = sku.MoldDesign! };

            if (!string.IsNullOrEmpty(request.Search?.Text))
            {
                var pattern = $"%{LikePattern.EscapeLikePattern(request.Search.Text)}%";
                query = query.Where(x => EF.Functions.ILike(x.MoldDesign, pattern));
            }

            var rows = await query.ToListAsync();

            var take = request.Take > 0 ? request.Take : 20;

            var result = rows
                .GroupBy(x => x.MoldDesign)
                .Select(g => new jewelry.Model.Sale.Invoice.MoldSuggest.Response
                {
                    MoldDesign = g.Key,
                    InvoiceCount = g.Select(x => x.Invoice).Distinct().Count(),
                    ItemCount = g.Count()
                })
                .OrderByDescending(x => x.InvoiceCount)
                .ThenBy(x => x.MoldDesign)
                .Take(take)
                .ToList();

            return result;
        }

        public async Task<jewelry.Model.Sale.Invoice.SaleTeamSuggest.Response> SaleTeamSuggest()
        {
            var invoices = await _jewelryContext.TbtSaleInvoiceHeader
                .Where(x => x.IsDelete == false)
                .Select(x => new { x.SalePerson, x.SaleSupport, x.SalePersonUsername, x.CreateBy })
                .ToListAsync();

            var salePersons = invoices
                .Where(x => !string.IsNullOrEmpty(x.SalePerson))
                .GroupBy(x => x.SalePerson!)
                .Select(g => new jewelry.Model.Sale.Invoice.SaleTeamSuggest.Item { Name = g.Key, InvoiceCount = g.Count() })
                .OrderByDescending(x => x.InvoiceCount)
                .ThenBy(x => x.Name)
                .ToList();

            var saleSupports = invoices
                .Where(x => !string.IsNullOrEmpty(x.SaleSupport))
                .GroupBy(x => x.SaleSupport!)
                .Select(g => new jewelry.Model.Sale.Invoice.SaleTeamSuggest.Item { Name = g.Key, InvoiceCount = g.Count() })
                .OrderByDescending(x => x.InvoiceCount)
                .ThenBy(x => x.Name)
                .ToList();

            var owners = invoices
                .GroupBy(x => string.IsNullOrEmpty(x.SalePersonUsername) ? x.CreateBy : x.SalePersonUsername!)
                .Select(g => new jewelry.Model.Sale.Invoice.SaleTeamSuggest.Item { Name = g.Key, InvoiceCount = g.Count() })
                .OrderByDescending(x => x.InvoiceCount)
                .ThenBy(x => x.Name)
                .ToList();

            return new jewelry.Model.Sale.Invoice.SaleTeamSuggest.Response
            {
                SalePersons = salePersons,
                SaleSupports = saleSupports,
                Owners = owners
            };
        }

        public async Task<string> Delete(jewelry.Model.Sale.Invoice.Delete.Request request)
        {
            if (string.IsNullOrEmpty(request.InvoiceNumber))
            {
                throw new HandleException("Invoice Number is Required.");
            }

            if (string.IsNullOrEmpty(request.DeleteReason))
            {
                throw new HandleException("Delete Reason is Required.");
            }

            var invoiceHeader = await _jewelryContext.TbtSaleInvoiceHeader
                .FirstOrDefaultAsync(x => x.Running == request.InvoiceNumber);

            if (invoiceHeader == null)
            {
                throw new HandleException($"Invoice not found: {request.InvoiceNumber}");
            }

            int cancelledPaymentCount;

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                // ล็อกแถว invoice header ก่อน แล้วอ่านสถานะล่าสุดมาเช็คซ้ำ กันสองคำขอยกเลิก invoice เดียวกันพร้อมกัน (T1)
                await _jewelryContext.LockInvoiceHeaderAsync(invoiceHeader.Running);
                await _jewelryContext.Entry(invoiceHeader).ReloadAsync();

                ValidateInvoiceCancellable(invoiceHeader);

                invoiceHeader.DeleteReason = request.DeleteReason;

                cancelledPaymentCount = await CancelInvoiceCore(invoiceHeader);

                await _jewelryContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"Error deleting invoice: {ex.Message}");
            }

            var message = $"Invoice {request.InvoiceNumber} deleted successfully";

            if (cancelledPaymentCount > 0)
            {
                message += $" (ยกเลิกรายการรับชำระเงิน {cancelledPaymentCount} รายการด้วย)";
            }

            return message;
        }

        public async Task<jewelry.Model.Sale.Invoice.CancelWithSaleOrder.Response> CancelWithSaleOrder(jewelry.Model.Sale.Invoice.CancelWithSaleOrder.Request request)
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

            string soNumber;
            var cancelledPaymentCount = 0;

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                // ล็อกแถว invoice header ก่อน แล้วอ่านสถานะล่าสุดมาเช็คซ้ำ กันสองคำขอยกเลิก invoice เดียวกันพร้อมกัน (T1)
                await _jewelryContext.LockInvoiceHeaderAsync(invoiceHeader.Running);
                await _jewelryContext.Entry(invoiceHeader).ReloadAsync();

                ValidateInvoiceCancellable(invoiceHeader);

                soNumber = invoiceHeader.SoRunning;

                cancelledPaymentCount = await CancelInvoiceCore(invoiceHeader);

                // ต้อง flush ก่อน เพราะ InactiveCore อ่านสถานะ Invoice ของสินค้าจากฐานข้อมูล — ยังอยู่ใน transaction เดียวกัน
                await _jewelryContext.SaveChangesAsync();

                await _saleOrderService.InactiveCore(soNumber);

                await _jewelryContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"เกิดข้อผิดพลาดในการยกเลิกใบแจ้งหนี้และใบสั่งขาย: {ex.Message}");
            }

            var message = $"ยกเลิกใบแจ้งหนี้ {request.InvoiceNumber} และใบสั่งขาย {soNumber} เรียบร้อยแล้ว";

            if (cancelledPaymentCount > 0)
            {
                message += $" (ยกเลิกรายการรับชำระเงิน {cancelledPaymentCount} รายการด้วย)";
            }

            return new jewelry.Model.Sale.Invoice.CancelWithSaleOrder.Response
            {
                InvoiceNumber = request.InvoiceNumber,
                SoNumber = soNumber,
                CancelledPaymentCount = cancelledPaymentCount,
                Message = message
            };
        }

        public async Task<jewelry.Model.Sale.Invoice.CancelAndUnconfirm.Response> CancelAndUnconfirm(jewelry.Model.Sale.Invoice.CancelAndUnconfirm.Request request)
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

            string soNumber;
            List<jewelry.Model.Sale.SaleOrder.UnconfirmStock.StockItemUnconfirmation> stockItemsToUnconfirm;
            var cancelledPaymentCount = 0;
            List<string> unconfirmedStockNumbers;

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                // ล็อกแถว invoice header ก่อน แล้วอ่านสถานะล่าสุดมาเช็คซ้ำ กันสองคำขอยกเลิก invoice เดียวกันพร้อมกัน (T1)
                await _jewelryContext.LockInvoiceHeaderAsync(invoiceHeader.Running);
                await _jewelryContext.Entry(invoiceHeader).ReloadAsync();

                ValidateInvoiceCancellable(invoiceHeader);

                soNumber = invoiceHeader.SoRunning;

                if (string.IsNullOrEmpty(soNumber))
                {
                    throw new HandleException($"ใบแจ้งหนี้ {request.InvoiceNumber} ไม่มีใบสั่งขายผูกอยู่");
                }

                // สแนปช็อตรายการสินค้าที่ผูกกับ invoice นี้ก่อน — ต้องทำก่อน CancelInvoiceCore เพราะ core จะ set Invoice = null ทำให้หาไม่เจอภายหลัง
                stockItemsToUnconfirm = await _jewelryContext.TbtSaleOrderProduct
                    .Where(x => x.Invoice == request.InvoiceNumber)
                    .Select(x => new jewelry.Model.Sale.SaleOrder.UnconfirmStock.StockItemUnconfirmation
                    {
                        Id = (int)x.Id,
                        StockNumber = x.StockNumber
                    })
                    .ToListAsync();

                cancelledPaymentCount = await CancelInvoiceCore(invoiceHeader);

                // ต้อง flush ก่อน เพราะ UnconfirmStockItemsCore อ่านสถานะสินค้าจากฐานข้อมูล — ยังอยู่ใน transaction เดียวกัน
                await _jewelryContext.SaveChangesAsync();

                unconfirmedStockNumbers = await _saleOrderService.UnconfirmStockItemsCore(soNumber, stockItemsToUnconfirm);

                await _jewelryContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"เกิดข้อผิดพลาดในการยกเลิกใบแจ้งหนี้และปลดยืนยันสินค้า: {ex.Message}");
            }

            var message = $"ยกเลิกใบแจ้งหนี้ {request.InvoiceNumber} และปลดยืนยันสินค้า {unconfirmedStockNumbers.Count} รายการออกจากใบสั่งขาย {soNumber} เรียบร้อยแล้ว";

            if (cancelledPaymentCount > 0)
            {
                message += $" (ยกเลิกรายการรับชำระเงิน {cancelledPaymentCount} รายการด้วย)";
            }

            return new jewelry.Model.Sale.Invoice.CancelAndUnconfirm.Response
            {
                InvoiceNumber = request.InvoiceNumber,
                SoNumber = soNumber,
                CancelledPaymentCount = cancelledPaymentCount,
                UnconfirmedItemCount = unconfirmedStockNumbers.Count,
                UnconfirmedStockNumbers = unconfirmedStockNumbers,
                Message = message
            };
        }

        private void ValidateInvoiceCancellable(TbtSaleInvoiceHeader invoiceHeader)
        {
            if (invoiceHeader.IsDelete)
            {
                throw new HandleException($"ใบแจ้งหนี้ {invoiceHeader.Running} ถูกยกเลิกไปแล้ว");
            }
        }

        // ล็อกแถว tbt_stock_balance ของ SKU ที่ผูกกับ stock number เหล่านี้ — ต้องเรียกหลังล็อก piece เสมอ (ลำดับ global: piece -> balance)
        private async Task LockStockBalancesForStockNumbersAsync(IEnumerable<string> stockNumbers)
        {
            var skuCodes = await _jewelryContext.TbtStockPiece
                .Where(p => stockNumbers.Contains(p.StockNumber))
                .Select(p => p.SkuCode)
                .Distinct()
                .ToListAsync();

            await _jewelryContext.LockStockBalancesAsync(skuCodes);
        }

        // ใช้ทั้ง Delete และ CancelWithSaleOrder — ไม่มี SaveChanges ภายใน ผู้เรียกเป็นคนควบคุม
        // คืนค่าจำนวนรายการรับชำระเงินที่ถูกยกเลิกไปพร้อมกับใบแจ้งหนี้
        private async Task<int> CancelInvoiceCore(TbtSaleInvoiceHeader invoiceHeader)
        {
            var invoiceNumber = invoiceHeader.Running;

            // คืนมัดจำที่หักเข้าใบแจ้งหนี้นี้กลับเข้ายอดคงเหลือของ SO — ล็อกแถวมัดจำหลังล็อก header (ผู้เรียกล็อกไว้แล้ว) ก่อนล็อก piece ตามลำดับ global
            var activeDepositApplies = await _jewelryContext.TbtSaleOrderDepositApply
                .Where(a => a.InvoiceRunning == invoiceNumber && !a.IsDelete)
                .ToListAsync();

            if (activeDepositApplies.Any())
            {
                await _jewelryContext.LockSaleOrderDepositsAsync(invoiceHeader.SoRunning);

                foreach (var apply in activeDepositApplies)
                {
                    apply.IsDelete = true;
                    apply.UpdateBy = CurrentUsername;
                    apply.UpdateDate = DateTime.UtcNow;
                }
                _jewelryContext.TbtSaleOrderDepositApply.UpdateRange(activeDepositApplies);
            }

            // Update sale order products to remove invoice reference
            var saleOrderProducts = await _jewelryContext.TbtSaleOrderProduct
                .Where(x => x.Invoice == invoiceNumber)
                .ToListAsync();

            // ล็อก piece ก่อนเพิ่ม qty กลับ กันคำขอ confirm ล็อตเงินเดียวกันมาเขียนทับพร้อมกัน (lost update)
            await _jewelryContext.LockStockPiecesAsync(saleOrderProducts.Select(p => p.StockNumber));
            await LockStockBalancesForStockNumbersAsync(saleOrderProducts.Select(p => p.StockNumber));

            foreach (var product in saleOrderProducts)
            {
                var piece = await _jewelryContext.TbtStockPiece
                    .FirstOrDefaultAsync(p => p.StockNumber == product.StockNumber);

                if (piece != null)
                {
                    var balance = await _jewelryContext.TbtStockBalance
                        .FirstOrDefaultAsync(b => b.SkuCode == piece.SkuCode && b.LocationCode == piece.LocationCode);

                    if (balance != null)
                    {
                        piece.Qty += product.Qty;
                        piece.QtyReserved += product.Qty;
                        StockPieceQtyHelper.RecalcStatus(piece);
                        piece.UpdateBy = CurrentUsername;
                        piece.UpdateDate = DateTime.UtcNow;

                        balance.QtyOnHand += product.Qty;
                        balance.QtyReserved += product.Qty;
                        balance.LastMovementAt = DateTime.UtcNow;
                        balance.UpdateBy = CurrentUsername;
                        balance.UpdateDate = DateTime.UtcNow;

                        _jewelryContext.TbtStockPiece.Update(piece);
                        _jewelryContext.TbtStockBalance.Update(balance);

                        _jewelryContext.TbtStockMovement.Add(new TbtStockMovement
                        {
                            MovementType = "RETURN",
                            SkuCode = piece.SkuCode,
                            StockNumber = piece.StockNumber,
                            ProductCode = piece.ProductCode,
                            ToLocation = piece.LocationCode,
                            Qty = product.Qty,
                            RefDocType = "INVOICE_DELETE",
                            RefDocNo = invoiceNumber,
                            MovementDate = DateTime.UtcNow,
                            CreateDate = DateTime.UtcNow,
                            CreateBy = CurrentUsername
                        });
                    }
                }

                product.Invoice = null;
                product.InvoiceItem = null;
                product.DkInvoiceNumber = null;

                product.UpdateBy = CurrentUsername;
                product.UpdateDate = DateTime.UtcNow;
            }

            // ยกเลิกรายการรับชำระเงินของใบแจ้งหนี้นี้ด้วย — ไม่ปล่อยให้เหลือรายการรับเงินที่ยังไม่ถูกลบค้างอยู่บนใบที่ยกเลิกแล้ว
            var payments = await _jewelryContext.TbtSaleInvoicePaymentItem
                .Where(x => x.InvoiceRunning == invoiceNumber && !x.IsDelete)
                .ToListAsync();

            foreach (var payment in payments)
            {
                payment.IsDelete = true;
                payment.UpdateBy = CurrentUsername;
                payment.UpdateDate = DateTime.UtcNow;

                _jewelryContext.TbtSaleInvoicePaymentItem.Update(payment);
            }

            invoiceHeader.UpdateDate = DateTime.UtcNow;
            invoiceHeader.UpdateBy = CurrentUsername;
            invoiceHeader.IsDelete = true;

            // Delete invoice header
            _jewelryContext.TbtSaleInvoiceHeader.Update(invoiceHeader);

            return payments.Count;
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

        // Payment Functions

        public async Task<string> CreatePayment(jewelry.Model.Sale.InvoicePayment.Create.Request request)
        {
            // Validate required fields
            if (string.IsNullOrEmpty(request.InvoiceNumber))
            {
                throw new HandleException("Invoice Number is Required.");
            }

            if (request.Amount <= 0)
            {
                throw new HandleException("Payment Amount must be greater than 0.");
            }

            if (string.IsNullOrEmpty(request.PaymentName))
            {
                throw new HandleException("Payment Method is Required.");
            }

            // Validate bank fields based on payment method
            if (request.Payment == 2) // Transfer
            {
                if (string.IsNullOrEmpty(request.BankCode))
                {
                    throw new HandleException("Bank is required for Transfer payment.");
                }
            }
            else if (request.Payment == 3) // Cheque
            {
                if (string.IsNullOrEmpty(request.BankCode))
                {
                    throw new HandleException("Bank is required for Cheque payment.");
                }
                if (string.IsNullOrEmpty(request.BankBranch))
                {
                    throw new HandleException("Bank Branch is required for Cheque payment.");
                }
            }

            // Validate BankCode exists in master
            if (!string.IsNullOrEmpty(request.BankCode))
            {
                var bankExists = await _jewelryContext.TbmBank
                    .AnyAsync(x => x.Code == request.BankCode && x.IsActive == true);
                if (!bankExists)
                {
                    throw new HandleException($"Bank code '{request.BankCode}' is invalid.");
                }
            }

            // Check if invoice exists
            var invoice = await _jewelryContext.TbtSaleInvoiceHeader
                .FirstOrDefaultAsync(x => x.Running == request.InvoiceNumber && x.IsDelete == false);

            if (invoice == null)
            {
                throw new HandleException($"Invoice {request.InvoiceNumber} not found.");
            }

            // Generate payment running number
            var paymentRunning = await _runningNumberService.GenerateRunningNumberForGold($"PAY-{request.InvoiceNumber}");

            // Handle image upload if provided
            string imagePath = string.Empty;
            if (request.ReceiptImage != null)
            {
                try
                {
                    // Generate unique filename with extension
                    string fileExtension = Path.GetExtension(request.ReceiptImage.FileName);
                    string fileName = $"{paymentRunning}{fileExtension}";

                    // Upload to Azure Blob Storage (Single Container Architecture)
                    using var stream = request.ReceiptImage.OpenReadStream();
                    var result = await _azureBlobService.UploadImageAsync(
                        stream,
                        "Payment",  // folder name in jewelry-images container
                        fileName
                    );

                    if (!result.Success)
                    {
                        throw new HandleException($"Failed to save receipt image: {result.ErrorMessage}");
                    }

                    // เก็บ blob path: "Payment/filename.jpg"
                    imagePath = result.BlobName;
                }
                catch (Exception ex)
                {
                    throw new HandleException($"Failed to save receipt image: {ex.Message}");
                }
            }

            // Create payment record
            var payment = new TbtSaleInvoicePaymentItem
            {
                Running = paymentRunning,
                InvoiceRunning = request.InvoiceNumber,
                SoRunning = invoice.SoRunning,

                PaymentDate = request.PaymentDate.UtcDateTime,
                
                Amount = request.Amount,
                CurrencyUnit = invoice.CurrencyUnit,

                PaymantName = request.PaymentName,
                Payment = request.Payment,

                ReferenceNumber1 = request.ReferenceNumber,
                Remark = request.Remark,
                ImagePath = imagePath,

                BankCode = request.BankCode,
                BankBranch = request.BankBranch,

                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow
            };

            _jewelryContext.TbtSaleInvoicePaymentItem.Add(payment);
            await _jewelryContext.SaveChangesAsync();

            return paymentRunning;
        }

        public IQueryable<jewelry.Model.Sale.InvoicePayment.List.Response> GetPaymentList(jewelry.Model.Sale.InvoicePayment.List.Request request)
        {
            if (string.IsNullOrEmpty(request.InvoiceNumber))
            {
                throw new HandleException("Invoice Number is Required.");
            }

            var query = from payment in _jewelryContext.TbtSaleInvoicePaymentItem
                        join bank in _jewelryContext.TbmBank
                            on payment.BankCode equals bank.Code into bankJoin
                        from bank in bankJoin.DefaultIfEmpty()
                        where payment.InvoiceRunning == request.InvoiceNumber
                           && payment.IsDelete == false
                        select new jewelry.Model.Sale.InvoicePayment.List.Response
                        {
                            Running = payment.Running,
                            InvoiceNumber = payment.InvoiceRunning,
                            SoNumber = payment.SoRunning,

                            PaymentDate = payment.PaymentDate,
                            Amount = payment.Amount,
                            CurrencyUnit = payment.CurrencyUnit,

                            PaymentMethod = payment.PaymantName,
                            ReferenceNumber = payment.ReferenceNumber1,
                            Remark = payment.Remark,
                            ImagePath = payment.ImagePath,

                            BankCode = payment.BankCode,
                            BankName = bank != null ? bank.NameTh : null,
                            BankBranch = payment.BankBranch,

                            CreateBy = payment.CreateBy,
                            CreateDate = payment.CreateDate,
                            UpdateBy = payment.UpdateBy,
                            UpdateDate = payment.UpdateDate
                        };

            // Order by payment date descending (newest first)
            query = query.OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.CreateDate);

            return query;
        }

        public async Task<string> DeletePayment(jewelry.Model.Sale.InvoicePayment.Delete.Request request)
        {
            if (string.IsNullOrEmpty(request.PaymentRunning))
            {
                throw new HandleException("Payment Running Number is Required.");
            }

            var payment = await _jewelryContext.TbtSaleInvoicePaymentItem
                .FirstOrDefaultAsync(x => x.Running == request.PaymentRunning && x.IsDelete == false);

            if (payment == null)
            {
                throw new HandleException($"Payment record {request.PaymentRunning} not found.");
            }

            // Soft delete
            payment.IsDelete = true;
            payment.UpdateBy = CurrentUsername;
            payment.UpdateDate = DateTime.UtcNow;

            _jewelryContext.TbtSaleInvoicePaymentItem.Update(payment);
            await _jewelryContext.SaveChangesAsync();

            return $"Payment {request.PaymentRunning} deleted successfully";
        }

        // Print Log Methods

        public async Task<string> CreatePrintLog(jewelry.Model.Sale.Invoice.PrintLog.Create.Request request)
        {
            if (string.IsNullOrEmpty(request.InvoiceNumber))
            {
                throw new HandleException("Invoice Number is Required.");
            }

            if (string.IsNullOrEmpty(request.PaperType))
            {
                throw new HandleException("Paper Type is Required.");
            }

            // Resolve invoice_running from invoice_no
            var invoiceHeader = await _jewelryContext.TbtSaleInvoiceHeader
                .FirstOrDefaultAsync(x => x.Running == request.InvoiceNumber && x.IsDelete == false);

            string? invoiceRunning = invoiceHeader?.Running;

            // Compute copy_no = max copy_no for this invoice_no + 1
            var maxCopyNo = await _jewelryContext.TbtSaleInvoicePrintLog
                .Where(x => x.InvoiceNo == request.InvoiceNumber)
                .Select(x => (int?)x.CopyNo)
                .MaxAsync();

            var copyNo = (maxCopyNo ?? 0) + 1;

            // Generate running using running number service
            var running = await _runningNumberService.GenerateRunningNumberForGold($"PRINT-{request.InvoiceNumber}");

            var printLog = new Jewelry.Data.Models.Jewelry.TbtSaleInvoicePrintLog
            {
                Running = running,
                InvoiceRunning = invoiceRunning,
                InvoiceNo = request.InvoiceNumber,
                PaperType = request.PaperType,
                CopyNo = copyNo,
                Data = request.Data,
                PrintedBy = CurrentUsername,
                PrintedAt = DateTime.UtcNow
            };

            _jewelryContext.TbtSaleInvoicePrintLog.Add(printLog);
            await _jewelryContext.SaveChangesAsync();

            return running;
        }

        public IQueryable<jewelry.Model.Sale.Invoice.PrintLog.List.Response> ListPrintLogs(jewelry.Model.Sale.Invoice.PrintLog.List.Request request)
        {
            if (string.IsNullOrEmpty(request.InvoiceNumber))
            {
                throw new HandleException("Invoice Number is Required.");
            }

            var query = from log in _jewelryContext.TbtSaleInvoicePrintLog
                        where log.InvoiceNo == request.InvoiceNumber
                        orderby log.PrintedAt descending
                        select new jewelry.Model.Sale.Invoice.PrintLog.List.Response
                        {
                            Running = log.Running,
                            InvoiceNo = log.InvoiceNo,
                            PaperType = log.PaperType,
                            CopyNo = log.CopyNo,
                            Data = log.Data,
                            PrintedBy = log.PrintedBy,
                            PrintedAt = log.PrintedAt
                        };

            return query;
        }
    }
}