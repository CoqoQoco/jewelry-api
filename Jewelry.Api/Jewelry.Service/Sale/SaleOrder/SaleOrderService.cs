using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.Sale.SaleOrderDeposit;
using Jewelry.Service.Stock;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

                var tCreate = MathHelper.ComputeTotals(
                    request.SubTotal ?? 0,
                    request.SpecialDiscount ?? 0,
                    request.SpecialAddition ?? 0,
                    request.Freight ?? 0,
                    request.Vat ?? 0);

                // Create new sale order
                saleOrder = new TbtSaleOrder
                {
                    SoNumber = soNumber,
                    Running = await _runningNumberService.GenerateRunningNumberForGold("RUNNING"),

                    SoDate = request.SODate.HasValue ? request.SODate.Value.UtcDateTime : null,
                    DeliveryDate = request.DeliveryDate.HasValue ? request.DeliveryDate.Value.UtcDateTime : null,

                    Status = 1,
                    StatusName = "DK-SO",

                    RefQuotation = request.RefQuotation,


                    Priority = request.Priority ?? "Normal",

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

                    MarkUp = request.Markup,
                    GoldRate = request.GoldRate,

                    SpecialDiscount = request.SpecialDiscount,
                    SpecialAddition = request.SpecialAddition,
                    Vat = request.Vat,
                    Freight = request.Freight,

                    Remark = request.Remark,

                    // Trim ชื่อผู้ขาย/ผู้ช่วยขาย เพราะข้อมูลชื่อใน user master บางรายมีช่องว่างหน้า-หลังติดมา และไม่ให้ string ว่างเข้าคอลัมน์
                    SalePerson = string.IsNullOrWhiteSpace(request.SalePerson) ? null : request.SalePerson.Trim(),
                    SaleSupport = string.IsNullOrWhiteSpace(request.SaleSupport) ? null : request.SaleSupport.Trim(),

                    SubTotal = tCreate.subTotal,
                    SpecialDiscountAmt = request.SpecialDiscount ?? 0,
                    SpecialAdditionAmt = request.SpecialAddition ?? 0,
                    FreightAmt = request.Freight ?? 0,
                    VatAmount = tCreate.vatAmount,
                    GrandTotalRaw = tCreate.raw,
                    GrandTotalRounded = tCreate.rounded,
                    RoundingAdjustment = tCreate.adjustment,

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
                //saleOrder.Status = request.Status;
                //saleOrder.StatusName = request.StatusName ?? saleOrder.StatusName;

                saleOrder.RefQuotation = request.RefQuotation;


                saleOrder.Priority = request.Priority ?? saleOrder.Priority;

                saleOrder.Data = request.Data;

                // Customer Information
                //saleOrder.CustomerName = request.CustomerName ?? saleOrder.CustomerName;
                //saleOrder.CustomerCode = request.CustomerCode ?? saleOrder.CustomerCode;
                saleOrder.CustomerAddress = request.CustomerAddress;
                saleOrder.CustomerTel = request.CustomerTel;
                saleOrder.CustomerEmail = request.CustomerEmail;
                saleOrder.CustomerRemark = request.CustomerRemark;

                // Currency and Pricing
                saleOrder.CurrencyUnit = request.CurrencyUnit ?? saleOrder.CurrencyUnit;
                saleOrder.CurrencyRate = request.CurrencyRate;

                saleOrder.MarkUp = request.Markup;
                saleOrder.GoldRate = request.GoldRate;

                saleOrder.SpecialDiscount = request.SpecialDiscount;
                saleOrder.SpecialAddition = request.SpecialAddition;
                saleOrder.Vat = request.Vat;
                saleOrder.Freight = request.Freight;

                saleOrder.Remark = request.Remark;

                // Trim ชื่อผู้ขาย/ผู้ช่วยขาย เพราะข้อมูลชื่อใน user master บางรายมีช่องว่างหน้า-หลังติดมา และไม่ให้ string ว่างเข้าคอลัมน์
                saleOrder.SalePerson = string.IsNullOrWhiteSpace(request.SalePerson) ? null : request.SalePerson.Trim();
                saleOrder.SaleSupport = string.IsNullOrWhiteSpace(request.SaleSupport) ? null : request.SaleSupport.Trim();

                var tUpdate = MathHelper.ComputeTotals(
                    request.SubTotal ?? 0,
                    request.SpecialDiscount ?? 0,
                    request.SpecialAddition ?? 0,
                    request.Freight ?? 0,
                    request.Vat ?? 0);

                saleOrder.SubTotal = tUpdate.subTotal;
                saleOrder.SpecialDiscountAmt = request.SpecialDiscount ?? 0;
                saleOrder.SpecialAdditionAmt = request.SpecialAddition ?? 0;
                saleOrder.FreightAmt = request.Freight ?? 0;
                saleOrder.VatAmount = tUpdate.vatAmount;
                saleOrder.GrandTotalRaw = tUpdate.raw;
                saleOrder.GrandTotalRounded = tUpdate.rounded;
                saleOrder.RoundingAdjustment = tUpdate.adjustment;

                saleOrder.UpdateDate = DateTime.UtcNow;
                saleOrder.UpdateBy = CurrentUsername;

                _jewelryContext.TbtSaleOrder.Update(saleOrder);
                await _jewelryContext.SaveChangesAsync();

                var changedInvoices = await ApplySaleTeamToInvoicesAsync(saleOrder, writeExact: false);
                if (changedInvoices.Any())
                {
                    _jewelryContext.TbtSaleInvoiceHeader.UpdateRange(changedInvoices);
                    await _jewelryContext.SaveChangesAsync();
                }
            }

            return soNumber;
        }

        // ที่ร้านมักกรอกชื่อผู้ขาย/ผู้ช่วยขายที่ใบสั่งขายทีหลังออกใบแจ้งหนี้ไปแล้ว
        // ถ้าไม่ sync ชื่อจะไม่มีวันขึ้นบนใบแจ้งหนี้ที่ออกไปแล้ว — กติกา: SO มีค่า -> เขียนทับใบแจ้งหนี้, SO ว่าง -> ไม่แตะค่าเดิมบนใบ
        // writeExact = true (แก้จากหน้า invoice detail ตรงๆ ผ่าน SO): เขียนค่า SalePerson/SaleSupport ของ SO ลงใบตรงๆ รวมถึง null
        // เพราะกรณีนี้ผู้ใช้ตั้งใจลบชื่อออกเอง ไม่ใช่ SO ที่ยังไม่เคยกรอก — ไม่แตะ SalePersonUsername ในโหมดนี้
        // ไม่เรียก SaveChangesAsync ในนี้ — ผู้เรียกเป็นคนบันทึก
        private async Task<List<TbtSaleInvoiceHeader>> ApplySaleTeamToInvoicesAsync(TbtSaleOrder saleOrder, bool writeExact)
        {
            var invoicesToSyncSalePerson = await _jewelryContext.TbtSaleInvoiceHeader
                .Where(x => x.SoRunning == saleOrder.SoNumber && x.IsDelete == false)
                .ToListAsync();

            var changedInvoices = new List<TbtSaleInvoiceHeader>();
            foreach (var inv in invoicesToSyncSalePerson)
            {
                var changed = false;

                if (writeExact)
                {
                    if (inv.SalePerson != saleOrder.SalePerson)
                    {
                        inv.SalePerson = saleOrder.SalePerson;
                        changed = true;
                    }

                    if (inv.SaleSupport != saleOrder.SaleSupport)
                    {
                        inv.SaleSupport = saleOrder.SaleSupport;
                        changed = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(saleOrder.SalePerson) && inv.SalePerson != saleOrder.SalePerson)
                    {
                        inv.SalePerson = saleOrder.SalePerson;
                        changed = true;
                    }

                    if (!string.IsNullOrEmpty(saleOrder.SaleSupport) && inv.SaleSupport != saleOrder.SaleSupport)
                    {
                        inv.SaleSupport = saleOrder.SaleSupport;
                        changed = true;
                    }

                    if (!string.IsNullOrEmpty(saleOrder.SalePersonUsername) && inv.SalePersonUsername != saleOrder.SalePersonUsername)
                    {
                        inv.SalePersonUsername = saleOrder.SalePersonUsername;
                        changed = true;
                    }
                }

                if (changed)
                {
                    changedInvoices.Add(inv);
                }
            }

            return changedInvoices;
        }

        public async Task<jewelry.Model.Sale.SaleOrder.UpdateSaleTeam.Response> UpdateSaleTeam(jewelry.Model.Sale.SaleOrder.UpdateSaleTeam.Request request)
        {
            if (string.IsNullOrEmpty(request.SoNumber))
            {
                throw new HandleException("กรุณาระบุเลขที่ใบสั่งขาย");
            }

            var saleOrder = await _jewelryContext.TbtSaleOrder
                .FirstOrDefaultAsync(x => x.SoNumber == request.SoNumber.ToUpper());

            if (saleOrder == null)
            {
                throw new HandleException($"ไม่พบใบสั่งขาย {request.SoNumber}");
            }

            saleOrder.SalePerson = string.IsNullOrWhiteSpace(request.SalePerson) ? null : request.SalePerson.Trim();
            saleOrder.SaleSupport = string.IsNullOrWhiteSpace(request.SaleSupport) ? null : request.SaleSupport.Trim();

            saleOrder.UpdateBy = CurrentUsername;
            saleOrder.UpdateDate = DateTime.UtcNow;

            // entity โหลดมาแบบ tracked อยู่แล้ว (ไม่ได้ AsNoTracking) — ห้ามเรียก Update()/UpdateRange() ซ้ำ
            // เพราะจะ mark ทุกคอลัมน์เป็น modified แล้วเขียนทับยอดเงิน/VAT ของ SO ทั้งแถวถ้ามีคนบันทึกพร้อมกัน
            var changedInvoices = await ApplySaleTeamToInvoicesAsync(saleOrder, writeExact: true);

            await _jewelryContext.SaveChangesAsync();

            return new jewelry.Model.Sale.SaleOrder.UpdateSaleTeam.Response
            {
                SoNumber = saleOrder.SoNumber,
                SalePerson = saleOrder.SalePerson,
                SaleSupport = saleOrder.SaleSupport,
                UpdatedInvoiceCount = changedInvoices.Count
            };
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

                Markup = saleOrder.MarkUp,
                GoldRate = saleOrder.GoldRate,

                SpecialDiscount = saleOrder.SpecialDiscount,
                SpecialAddition = saleOrder.SpecialAddition,
                Vat = saleOrder.Vat,
                Freight = saleOrder.Freight,

                SoDate = saleOrder.SoDate,

                Remark = saleOrder.Remark,

                SalePerson = saleOrder.SalePerson,
                SaleSupport = saleOrder.SaleSupport,

                SaleChannelCode = saleOrder.SaleChannelCode,

                SubTotal = saleOrder.SubTotal,
                SpecialDiscountAmt = saleOrder.SpecialDiscountAmt,
                SpecialAdditionAmt = saleOrder.SpecialAdditionAmt,
                FreightAmt = saleOrder.FreightAmt,
                VatAmount = saleOrder.VatAmount,
                GrandTotalRaw = saleOrder.GrandTotalRaw,
                GrandTotalRounded = saleOrder.GrandTotalRounded,
                RoundingAdjustment = saleOrder.RoundingAdjustment
            };

            if (!string.IsNullOrEmpty(response.SaleChannelCode))
            {
                var saleChannel = await _jewelryContext.TbmSaleChannel
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Code == response.SaleChannelCode);

                response.SaleChannelName = saleChannel?.NameTh ?? saleChannel?.NameEn ?? saleChannel?.Code;
            }

            if (!string.IsNullOrEmpty(response.Data))
            {
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(response.Data))
                    {
                        var root = doc.RootElement;
                        // เก็บทั้ง stockNumber และ lineKey ต่อ entry (คงลำดับ + คงรายการซ้ำไว้ทั้งหมด)
                        // ใบเก่าที่ไม่มี lineKey ใน JSON จะได้ lineKey = null
                        // อ่านทั้ง stockItems (ของจริงในคลัง) และ copyItems (รายการสำเนา/รอของ) — ไม่งั้นบรรทัด
                        // copyItems ที่ถูกยืนยันเป็น placeholder แล้วจะจับคู่กับ JSON ต้นทางไม่ได้ (ไปต่อท้ายเป็นแถวใหม่แทน)
                        var stockItemsFromJson = new List<(string? StockNumber, string? LineKey)>();

                        void CollectItemsFromJson(string propertyName)
                        {
                            if (root.TryGetProperty(propertyName, out JsonElement itemsElement))
                            {
                                foreach (var item in itemsElement.EnumerateArray())
                                {
                                    if (item.TryGetProperty("stockNumber", out JsonElement stockNumberElement))
                                    {
                                        string? lineKey = null;
                                        if (item.TryGetProperty("lineKey", out JsonElement lineKeyElement)
                                            && lineKeyElement.ValueKind == JsonValueKind.String)
                                        {
                                            lineKey = lineKeyElement.GetString();
                                        }

                                        stockItemsFromJson.Add((stockNumberElement.GetString(), lineKey));
                                    }
                                }
                            }
                        }

                        CollectItemsFromJson("stockItems");
                        CollectItemsFromJson("copyItems");

                        if (stockItemsFromJson.Any())
                        {
                            response.StockConfirm = stockItemsFromJson.Select(s => new jewelry.Model.Sale.SaleOrder.Get.StockConfirm
                            {
                                StockNumber = s.StockNumber,
                                LineKey = s.LineKey,
                                IsConfirm = false
                            }).ToList();
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"Error: {ex.Message}");
                }
            }

           
            #region *** get stock confirm ***
            var stockConfrim = (from item in _jewelryContext.TbtSaleOrderProduct
                                where item.SoNumber == response.SoNumber && item.Running == response.Running
                                select item).ToList();

            if (stockConfrim.Any())
            {
                if (response.StockConfirm.Any())
                {
                    // กันหลายแถว DB ที่เลขสินค้าเดียวกันไปจับคู่ placeholder ตัวเดียวกันซ้ำ (ต้อง 1 ต่อ 1)
                    var matchedPlaceholders = new HashSet<jewelry.Model.Sale.SaleOrder.Get.StockConfirm>();

                    foreach (var stock in stockConfrim)
                    {
                        // 1) จับคู่ด้วย lineKey ก่อน ถ้า DB แถวนี้มี lineKey
                        jewelry.Model.Sale.SaleOrder.Get.StockConfirm? matchStock = null;
                        if (!string.IsNullOrEmpty(stock.LineKey))
                        {
                            matchStock = response.StockConfirm.FirstOrDefault(s =>
                                !matchedPlaceholders.Contains(s) && s.LineKey == stock.LineKey);
                        }

                        // 2) ไม่เจอ (หรือไม่มี lineKey) → fallback จับคู่ด้วย StockNumber ตัวแรกที่ยังไม่ถูกใช้ (ข้อมูลเก่า)
                        if (matchStock == null)
                        {
                            matchStock = response.StockConfirm.FirstOrDefault(s =>
                                !matchedPlaceholders.Contains(s) && s.StockNumber == stock.StockNumber);
                        }

                        if (matchStock != null)
                        {
                            matchedPlaceholders.Add(matchStock);

                            matchStock.Id = stock.Id;
                            // ยืนยันแล้ว: ใช้เลขสต็อกจริงจาก DB เสมอ — สำคัญกับ copy line ที่ JSON ต้นทาง
                            // ยังไม่มี stockNumber (เป็น placeholder ที่พนักงานเพิ่งพิมพ์เลขเข้ามาตอนยืนยัน)
                            matchStock.StockNumber = stock.StockNumber;
                            matchStock.LineKey = stock.LineKey;
                            matchStock.PriceOrigin = stock.PriceOrigin;
                            matchStock.IsConfirm = true;
                            matchStock.IsPlaceholder = stock.IsPlaceholder;

                            matchStock.Qty = stock.Qty;
                            matchStock.Discount = stock.Discount;
                            matchStock.Remark = stock.Remark;
                            matchStock.NetPrice = stock.NetPrice;

                            matchStock.Invoice = stock.Invoice;
                            matchStock.InvoiceItem = stock.InvoiceItem;
                            matchStock.DKInvoiceNumber = stock.DkInvoiceNumber;
                        }
                        else
                        {
                            response.StockConfirm.Add(new jewelry.Model.Sale.SaleOrder.Get.StockConfirm
                            {
                                Id = stock.Id,
                                StockNumber = stock.StockNumber,
                                LineKey = stock.LineKey,
                                IsConfirm = true,
                                IsPlaceholder = stock.IsPlaceholder,

                                PriceOrigin = stock.PriceOrigin,
                                Qty = stock.Qty,
                                Discount = stock.Discount,
                                Remark = stock.Remark,
                                NetPrice = stock.NetPrice,

                                Invoice = stock.Invoice,
                                InvoiceItem = stock.InvoiceItem,
                                DKInvoiceNumber = stock.DkInvoiceNumber
                            });
                        }
                    }
                }
                else
                {
                    response.StockConfirm = stockConfrim.Select(s => new jewelry.Model.Sale.SaleOrder.Get.StockConfirm
                    {
                        Id = s.Id,
                        StockNumber = s.StockNumber,
                        LineKey = s.LineKey,
                        IsPlaceholder = s.IsPlaceholder,

                        PriceOrigin = s.PriceOrigin,
                        Qty = s.Qty,
                        Discount = s.Discount,
                        Remark = s.Remark,
                        NetPrice = s.NetPrice,

                        Invoice = s.Invoice,
                        InvoiceItem = s.InvoiceItem,
                        DKInvoiceNumber = s.DkInvoiceNumber,
                    }).ToList();
                }

            }
            #endregion
            #region *** get stock product ***
            if (response.StockConfirm.Any())
            {
                // copyItems ที่ยังไม่ยืนยันไม่มี stockNumber (null) — กรองออกก่อนสร้าง array ให้ query piece
                var stockArray = response.StockConfirm
                    .Where(s => !string.IsNullOrEmpty(s.StockNumber))
                    .Select(s => s.StockNumber)
                    .ToArray();
                var pieces = await _jewelryContext.TbtStockPiece
                    .Where(p => stockArray.Contains(p.StockNumber))
                    .ToListAsync();

                var skuLocationPairs = pieces
                    .Select(p => new { p.SkuCode, p.LocationCode })
                    .Distinct()
                    .ToList();

                var balances = await _jewelryContext.TbtStockBalance
                    .Where(b => skuLocationPairs.Select(x => x.SkuCode).Contains(b.SkuCode))
                    .ToListAsync();

                if (pieces.Any())
                {
                    foreach (var stock in response.StockConfirm)
                    {
                        var piece = pieces.FirstOrDefault(p => p.StockNumber == stock.StockNumber);
                        if (piece != null)
                        {
                            var balance = balances.FirstOrDefault(b => b.SkuCode == piece.SkuCode && b.LocationCode == piece.LocationCode);
                            var qtyRemaining = balance != null ? balance.QtyOnHand - balance.QtyReserved : 0;
                            if (qtyRemaining <= 0)
                            {
                                stock.IsRemainProduct = false;
                                stock.Message = stock.IsConfirm || stock.IsInvoice ? null : "สินค้าหมดสต็อก";
                            }
                        }
                    }
                }
            }
            #endregion

            return response;
        }

        public IQueryable<jewelry.Model.Sale.SaleOrder.List.Response> List(jewelry.Model.Sale.SaleOrder.List.Request _request)
        {
            var request = _request.Search;
            var query = from saleOrder in _jewelryContext.TbtSaleOrder
                        where saleOrder.Status > 0
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


                            Priority = saleOrder.Priority ?? string.Empty,

                            // Customer Information
                            CustomerName = saleOrder.CustomerName ?? string.Empty,
                            CustomerCode = saleOrder.CustomerCode ?? string.Empty,
                            CustomerTel = saleOrder.CustomerTel ?? string.Empty,
                            CustomerEmail = saleOrder.CustomerEmail ?? string.Empty,

                            // Currency and Pricing
                            CurrencyUnit = saleOrder.CurrencyUnit ?? string.Empty,
                            CurrencyRate = saleOrder.CurrencyRate,
                            Markup = saleOrder.MarkUp,
                            GoldRate = saleOrder.GoldRate,
                            Freight = saleOrder.Freight,
                            SoDate = saleOrder.SoDate
                        };

            // Apply filters
            if (!string.IsNullOrEmpty(request.SoNumber))
            {
                query = query.Where(x => x.SoNumber.Contains(request.SoNumber.ToUpper()));
            }

            if (!string.IsNullOrWhiteSpace(request.StockNumber))
            {
                var keyword = request.StockNumber.Trim();
                query = query.Where(x =>
                    _jewelryContext.TbtSaleOrderProduct
                        .Any(p => p.SoNumber == x.SoNumber
                               && EF.Functions.ILike(p.StockNumber, $"%{keyword}%")));
            }

            if (!string.IsNullOrWhiteSpace(request.ProductNumber))
            {
                var keyword = request.ProductNumber.Trim();
                query = query.Where(x =>
                    (from p in _jewelryContext.TbtSaleOrderProduct
                     join piece in _jewelryContext.TbtStockPiece on p.StockNumber equals piece.StockNumber
                     where p.SoNumber == x.SoNumber
                        && EF.Functions.ILike(piece.ProductCode, $"%{keyword}%")
                     select 1).Any());
            }

            if (!string.IsNullOrWhiteSpace(request.MoldNumber))
            {
                var keyword = request.MoldNumber.Trim();
                query = query.Where(x =>
                    (from p in _jewelryContext.TbtSaleOrderProduct
                     join piece in _jewelryContext.TbtStockPiece on p.StockNumber equals piece.StockNumber
                     join sku in _jewelryContext.TbtSku on piece.SkuCode equals sku.SkuCode
                     where p.SoNumber == x.SoNumber
                        && sku.MoldDesign != null
                        && EF.Functions.ILike(sku.MoldDesign, $"%{keyword}%")
                     select 1).Any());
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

            var confirmedDate = DateTime.UtcNow;
            // placeholder ไม่มีของจริงให้จอง ไม่ต้องล็อก piece/balance — ล็อกเฉพาะเลขสต็อกของรายการจริงเท่านั้น
            var requestStockNumbers = request.StockItems
                .Where(s => !string.IsNullOrEmpty(s.StockNumber) && !s.IsPlaceholder)
                .Select(s => s.StockNumber)
                .Distinct();

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                // ล็อก piece ก่อนตรวจสอบ/ยืนยัน กันสองคำขอจองชิ้นเดียวกันพร้อมกันแล้วจองเกินจำนวนพร้อมขาย
                await _jewelryContext.LockStockPiecesAsync(requestStockNumbers);
                await LockStockBalancesForStockNumbersAsync(requestStockNumbers);

                await ValidateStockItemConfirmations(saleOrder.SoNumber, request.StockItems);

                var confirmedStockNumbers = await ConfirmStockItemsCore(saleOrder, request.StockItems, confirmedDate);

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
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"Error confirming stock items: {ex.Message}");
            }
        }

        public async Task<List<string>> ConfirmStockItemsForPos(string soNumber, List<jewelry.Model.Sale.SaleOrder.ConfirmStock.StockItemConfirmation> stockItems, DateTime confirmedDate)
        {
            if (string.IsNullOrEmpty(soNumber))
            {
                throw new HandleException("Sale Order Number is required.");
            }

            if (stockItems == null || !stockItems.Any())
            {
                throw new HandleException("No stock items provided for confirmation.");
            }

            var saleOrder = await _jewelryContext.TbtSaleOrder
                .FirstOrDefaultAsync(so => so.SoNumber == soNumber.ToUpper());

            if (saleOrder == null)
            {
                throw new HandleException($"Sale Order {soNumber} not found.");
            }

            // เรียกจาก PosCheckoutService ซึ่งเปิด transaction ไว้แล้ว — ล็อกซ้ำในนี้ปลอดภัย (transaction เดียวกัน) ไม่เปิด transaction ใหม่
            // placeholder ไม่มีของจริงให้จอง ไม่ต้องล็อก piece/balance — ล็อกเฉพาะเลขสต็อกของรายการจริงเท่านั้น
            var requestStockNumbers = stockItems
                .Where(s => !string.IsNullOrEmpty(s.StockNumber) && !s.IsPlaceholder)
                .Select(s => s.StockNumber)
                .Distinct();
            await _jewelryContext.LockStockPiecesAsync(requestStockNumbers);
            await LockStockBalancesForStockNumbersAsync(requestStockNumbers);

            await ValidateStockItemConfirmations(saleOrder.SoNumber, stockItems);

            return await ConfirmStockItemsCore(saleOrder, stockItems, confirmedDate);
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

        private async Task ValidateStockItemConfirmations(string soNumber, List<jewelry.Model.Sale.SaleOrder.ConfirmStock.StockItemConfirmation> stockItems)
        {
            var errors = new List<string>();

            // Validate each stock item before processing
            foreach (var stockItem in stockItems)
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

            }

            // กันบรรทัดซ้ำ — เฉพาะรายการที่มี LineKey (ใบเก่า/POS ที่ไม่มี LineKey ข้าม check นี้)
            // 1) ซ้ำกันเองภายในคำขอเดียวกัน
            var duplicateLineKeyGroups = stockItems
                .Where(s => !string.IsNullOrEmpty(s.LineKey))
                .GroupBy(s => s.LineKey)
                .Where(g => g.Count() > 1);

            foreach (var group in duplicateLineKeyGroups)
            {
                var stockNumbersInGroup = string.Join(", ", group.Select(g => g.StockNumber));
                errors.Add($"พบ lineKey ซ้ำกันในคำขอเดียวกัน ({stockNumbersInGroup})");
            }

            // 2) ซ้ำกับบรรทัดที่ยืนยันไปแล้วในใบสั่งขายนี้ (กันกดยืนยันซ้ำ/ยิงคำขอซ้ำพร้อมกัน)
            var requestLineKeys = stockItems
                .Where(s => !string.IsNullOrEmpty(s.LineKey))
                .Select(s => s.LineKey)
                .Distinct()
                .ToList();

            if (requestLineKeys.Any())
            {
                var soNumberUpper = soNumber.ToUpper();
                var alreadyConfirmed = await _jewelryContext.TbtSaleOrderProduct
                    .Where(p => p.SoNumber == soNumberUpper && p.LineKey != null && requestLineKeys.Contains(p.LineKey))
                    .ToListAsync();

                foreach (var existing in alreadyConfirmed)
                {
                    errors.Add($"บรรทัด {existing.StockNumber} ยืนยันไปแล้ว กรุณารีเฟรชหน้าก่อนทำรายการ");
                }
            }

            // กันเลขสต็อกของ placeholder (พนักงานพิมพ์เอง) ซ้ำกัน — ไม่มีของจริงให้ยึดตามระบบเหมือนของจริง
            // 3) ซ้ำกันเองภายในคำขอเดียวกัน (เฉพาะรายการ placeholder)
            var duplicatePlaceholderGroups = stockItems
                .Where(s => s.IsPlaceholder && !string.IsNullOrEmpty(s.StockNumber))
                .GroupBy(s => s.StockNumber)
                .Where(g => g.Count() > 1);

            foreach (var group in duplicatePlaceholderGroups)
            {
                errors.Add($"เลข {group.Key} (รายการรอของ) ซ้ำกันในคำขอเดียวกัน");
            }

            // 4) ซ้ำกับบรรทัดที่มีอยู่แล้วในใบสั่งขายนี้ (ทั้งของจริงและ placeholder เดิม)
            var placeholderStockNumbers = stockItems
                .Where(s => s.IsPlaceholder && !string.IsNullOrEmpty(s.StockNumber))
                .Select(s => s.StockNumber)
                .Distinct()
                .ToList();

            if (placeholderStockNumbers.Any())
            {
                var soNumberUpper = soNumber.ToUpper();
                var existingStockNumbers = await _jewelryContext.TbtSaleOrderProduct
                    .Where(p => p.SoNumber == soNumberUpper && placeholderStockNumbers.Contains(p.StockNumber))
                    .Select(p => p.StockNumber)
                    .Distinct()
                    .ToListAsync();

                foreach (var stockNumber in existingStockNumbers)
                {
                    errors.Add($"เลข {stockNumber} (รายการรอของ) มีอยู่แล้วในใบสั่งขายนี้");
                }
            }

            // Silver lot: จองเกินจำนวนพร้อมขาย (qty - qtyReserved) ของ piece ไม่ได้ — ข้าม placeholder เพราะไม่มีของจริง
            // ให้ตรวจ ไม่ต้อง lookup piece เลย ต้องรวมจำนวนข้ามบรรทัดที่เลขสินค้าเดียวกันก่อนเทียบ (1 ใบสั่งขายอาจมีหลายบรรทัดเลขสินค้าเดียวกัน)
            var groupedByStockNumber = stockItems
                .Where(s => !string.IsNullOrEmpty(s.StockNumber) && !s.IsPlaceholder)
                .GroupBy(s => s.StockNumber)
                .Select(g => new { StockNumber = g.Key, TotalQty = g.Sum(x => x.Qty) });

            foreach (var group in groupedByStockNumber)
            {
                var piece = await _jewelryContext.TbtStockPiece
                    .FirstOrDefaultAsync(p => p.StockNumber == group.StockNumber);

                // ของจริงต้องมี piece อยู่ในคลังเสมอ ไม่งั้นจะกลายเป็นแถวยืนยันที่ไม่กระทบสต็อกแบบมองไม่เห็น (เคยเป็นบั๊กเงียบ)
                if (piece == null)
                {
                    errors.Add($"ไม่พบเลขสินค้า {group.StockNumber} ในคลัง กรุณาตรวจสอบเลขที่กรอก");
                    continue;
                }

                var available = StockPieceQtyHelper.Available(piece);
                if (group.TotalQty > available)
                {
                    var shortage = group.TotalQty - available;
                    var stockNumberLabel = string.IsNullOrEmpty(piece.StockNumberOrigin)
                        ? group.StockNumber
                        : $"{group.StockNumber} ({piece.StockNumberOrigin})";

                    errors.Add($"เลข {stockNumberLabel} พร้อมขาย {available:0.##} ชิ้น แต่ขอ {group.TotalQty:0.##} ชิ้น (ขาด {shortage:0.##})");
                }
            }

            // If there are validation errors, return them
            if (errors.Any())
            {
                throw new HandleException($"Validation errors: {string.Join("; ", errors)}");
            }
        }

        private async Task<List<string>> ConfirmStockItemsCore(TbtSaleOrder saleOrder, List<jewelry.Model.Sale.SaleOrder.ConfirmStock.StockItemConfirmation> stockItems, DateTime confirmedDate)
        {
            var confirmedStockNumbers = new List<string>();

            foreach (var stockItem in stockItems)
            {
                // Create new confirmed product entry
                var newProduct = new TbtSaleOrderProduct
                {
                    Running = saleOrder.Running,
                    SoNumber = saleOrder.SoNumber,
                    StockNumber = stockItem.StockNumber,
                    Stocknumberorigin = stockItem.ProductNumber ?? stockItem.StockNumber,

                    Qty = stockItem.Qty,
                    PriceOrigin = stockItem.AppraisalPrice,
                    Discount = stockItem.Discount,
                    NetPrice = stockItem.AppraisalPrice * (1 - (stockItem.Discount) / 100),
                    LineKey = stockItem.LineKey,
                    IsPlaceholder = stockItem.IsPlaceholder,

                    CreateDate = confirmedDate,
                    CreateBy = CurrentUsername
                };

                _jewelryContext.TbtSaleOrderProduct.Add(newProduct);

                // placeholder = รายการรอของ (เลขพนักงานพิมพ์เอง) — ไม่มีของจริงให้จอง ข้าม stock effect ทั้งหมด
                if (!stockItem.IsPlaceholder)
                {
                    var piece = await _jewelryContext.TbtStockPiece
                        .FirstOrDefaultAsync(p => p.StockNumber == stockItem.StockNumber);

                    if (piece == null)
                    {
                        throw new HandleException($"ไม่พบเลขสินค้า {stockItem.StockNumber} ในคลัง ไม่สามารถยืนยันรายการนี้ได้");
                    }

                    var balance = await _jewelryContext.TbtStockBalance
                        .FirstOrDefaultAsync(b => b.SkuCode == piece.SkuCode && b.LocationCode == piece.LocationCode);

                    if (balance != null)
                    {
                        balance.QtyReserved += stockItem.Qty;
                        balance.LastMovementAt = confirmedDate;
                        _jewelryContext.TbtStockBalance.Update(balance);
                    }

                    piece.QtyReserved += stockItem.Qty;
                    StockPieceQtyHelper.RecalcStatus(piece);
                    piece.UpdateDate = confirmedDate;
                    piece.UpdateBy = CurrentUsername;
                    _jewelryContext.TbtStockPiece.Update(piece);

                    _jewelryContext.TbtStockMovement.Add(new TbtStockMovement
                    {
                        MovementDate = confirmedDate,
                        MovementType = "RESERVE",
                        SkuCode = piece.SkuCode,
                        StockNumber = piece.StockNumber,
                        ProductCode = piece.ProductCode,
                        FromLocation = piece.LocationCode,
                        Qty = stockItem.Qty,
                        RefDocType = "SO",
                        RefDocNo = saleOrder.SoNumber,
                        CreateDate = confirmedDate,
                        CreateBy = CurrentUsername
                    });
                }

                confirmedStockNumbers.Add(stockItem.StockNumber);
            }

            return confirmedStockNumbers;
        }

        public async Task<bool> Inactive(jewelry.Model.Sale.SaleOrder.Inactive.Request request)
        {
            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                await InactiveCore(request.SoNumber);

                await _jewelryContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"Error inactivating sale order: {ex.Message}");
            }
        }

        // Used by Inactive and by Invoice/CancelWithSaleOrder — no internal transaction/SaveChanges, caller controls both.
        public async Task InactiveCore(string soNumber)
        {
            var saleOrder = await _jewelryContext.TbtSaleOrder
                .FirstOrDefaultAsync(x => x.SoNumber == soNumber);

            if (saleOrder == null)
                throw new HandleException($"Sale Order {soNumber} not found.");

            var hasInvoicedItems = await _jewelryContext.TbtSaleOrderProduct
                .AnyAsync(x => x.SoNumber == soNumber && !string.IsNullOrEmpty(x.Invoice));
            if (hasInvoicedItems)
                throw new HandleException($"ไม่สามารถยกเลิกใบสั่งขาย {soNumber} ได้ เนื่องจากมีสินค้าที่ออก Invoice แล้ว");

            var depositBalance = await SaleOrderDepositHelper.GetBalanceAsync(_jewelryContext, soNumber);
            if (depositBalance > 0)
                throw new HandleException($"ใบสั่งขาย {soNumber} มีมัดจำคงเหลือ {depositBalance:N2} ต้องลบรายการมัดจำ (คืนเงิน) ก่อนยกเลิกใบสั่งขาย");

            var now = DateTime.UtcNow;

            var confirmedProducts = await _jewelryContext.TbtSaleOrderProduct
                .Where(x => x.SoNumber == soNumber && string.IsNullOrEmpty(x.Invoice))
                .ToListAsync();

            // ล็อก piece ของแถวที่ยังไม่ออก invoice ก่อนปล่อยจอง กันสองคำขอ (เช่น ยกเลิก SO กับ ยืนยัน/ออก invoice) ชนกันบน qty_reserved
            await _jewelryContext.LockStockPiecesAsync(confirmedProducts.Select(p => p.StockNumber));
            await LockStockBalancesForStockNumbersAsync(confirmedProducts.Select(p => p.StockNumber));

            foreach (var product in confirmedProducts)
            {
                // placeholder ไม่เคยจองของจริง (ConfirmStockItemsCore ข้าม stock effect ให้แล้ว) — ลบแถวเฉยๆ ห้ามแตะ piece/balance
                // (เลขที่พนักงานพิมพ์เองอาจไปพ้องกับเลขสต็อกจริงในระบบโดยบังเอิญ ถ้าไม่กันไว้จะเผลอไปลด qty_reserved ของชิ้นอื่น)
                if (!product.IsPlaceholder)
                {
                    var piece = await _jewelryContext.TbtStockPiece
                        .FirstOrDefaultAsync(p => p.StockNumber == product.StockNumber);

                    if (piece != null)
                    {
                        var balance = await _jewelryContext.TbtStockBalance
                            .FirstOrDefaultAsync(b => b.SkuCode == piece.SkuCode && b.LocationCode == piece.LocationCode);

                        if (balance != null)
                        {
                            balance.QtyReserved -= product.Qty;
                            balance.LastMovementAt = now;
                            _jewelryContext.TbtStockBalance.Update(balance);
                        }

                        piece.QtyReserved = Math.Max(0, piece.QtyReserved - product.Qty);
                        StockPieceQtyHelper.RecalcStatus(piece);
                        piece.UpdateDate = now;
                        piece.UpdateBy = CurrentUsername;
                        _jewelryContext.TbtStockPiece.Update(piece);

                        _jewelryContext.TbtStockMovement.Add(new TbtStockMovement
                        {
                            MovementDate = now,
                            MovementType = "UNRESERVE",
                            SkuCode = piece.SkuCode,
                            StockNumber = piece.StockNumber,
                            ProductCode = piece.ProductCode,
                            ToLocation = piece.LocationCode,
                            Qty = product.Qty,
                            RefDocType = "SO",
                            RefDocNo = saleOrder.SoNumber,
                            CreateDate = now,
                            CreateBy = CurrentUsername
                        });
                    }
                }

                _jewelryContext.TbtSaleOrderProduct.Remove(product);
            }

            saleOrder.Status = 0;
            saleOrder.StatusName = "Inactive";
            saleOrder.UpdateDate = now;
            saleOrder.UpdateBy = CurrentUsername;
        }

        public async Task<jewelry.Model.Sale.SaleOrder.UnconfirmStock.Response> UnconfirmStockItems(jewelry.Model.Sale.SaleOrder.UnconfirmStock.Request request)
        {
            // Basic validation
            if (string.IsNullOrEmpty(request.SoNumber))
            {
                throw new HandleException("Sale Order Number is required.");
            }

            if (request.StockItems == null || !request.StockItems.Any())
            {
                throw new HandleException("No stock items provided for unconfirmation.");
            }

            // Validate Sale Order exists
            var saleOrder = await _jewelryContext.TbtSaleOrder
                .FirstOrDefaultAsync(so => so.SoNumber == request.SoNumber.ToUpper());

            if (saleOrder == null)
            {
                throw new HandleException($"Sale Order {request.SoNumber} not found.");
            }

            var unconfirmedStockNumbers = new List<string>();
            var errors = new List<string>();
            var unconfirmedDate = DateTime.UtcNow;

            // Required field validation เท่านั้น — เช็คสถานะยืนยัน/invoiced ย้ายไปทำหลังล็อกแถวใน UnconfirmStockItemsCore กันสองคำขอชนกัน
            foreach (var stockItem in request.StockItems)
            {
                if (string.IsNullOrEmpty(stockItem.StockNumber))
                {
                    errors.Add("Stock Number is required for all items.");
                }
            }

            if (errors.Any())
            {
                throw new HandleException($"Validation errors: {string.Join("; ", errors)}");
            }

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                unconfirmedStockNumbers = await UnconfirmStockItemsCore(request.SoNumber, request.StockItems);

                // Save all changes
                await _jewelryContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return new jewelry.Model.Sale.SaleOrder.UnconfirmStock.Response
                {
                    Success = true,
                    Message = $"Successfully unconfirmed {unconfirmedStockNumbers.Count} stock items.",
                    UnconfirmedItemsCount = unconfirmedStockNumbers.Count,
                    UnconfirmedStockNumbers = unconfirmedStockNumbers,
                    UnconfirmedDate = unconfirmedDate
                };
            }
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"Error unconfirming stock items: {ex.Message}");
            }
        }

        // Used by UnconfirmStockItems and by Invoice/CancelAndUnconfirm — caller ต้องเปิด transaction ไว้ก่อนเรียก (ล็อกแถวข้างในนี้), ไม่มี SaveChanges ภายใน ผู้เรียกเป็นคนควบคุม
        public async Task<List<string>> UnconfirmStockItemsCore(string soNumber, List<jewelry.Model.Sale.SaleOrder.UnconfirmStock.StockItemUnconfirmation> stockItems)
        {
            var soNumberUpper = soNumber.ToUpper();
            var unconfirmedDate = DateTime.UtcNow;
            var unconfirmedStockNumbers = new List<string>();

            var requestStockNumbers = stockItems
                .Where(s => !string.IsNullOrEmpty(s.StockNumber))
                .Select(s => s.StockNumber)
                .Distinct();

            // ล็อก piece + แถว SO product ก่อน แล้วค่อยเช็คว่ายังอยู่จริงและยังไม่ถูก invoice — กันสองคำขอ unconfirm/cancel ชนกัน
            await _jewelryContext.LockStockPiecesAsync(requestStockNumbers);
            await LockStockBalancesForStockNumbersAsync(requestStockNumbers);
            await _jewelryContext.LockSaleOrderProductsAsync(soNumberUpper, requestStockNumbers);

            foreach (var stockItem in stockItems)
            {
                // Get the confirmed product entry
                var confirmedProduct = await _jewelryContext.TbtSaleOrderProduct
                    .FirstOrDefaultAsync(p => p.SoNumber == soNumberUpper &&
                                              p.StockNumber == stockItem.StockNumber &&
                                              p.Id == stockItem.Id);

                if (confirmedProduct == null)
                {
                    throw new HandleException($"Stock item {stockItem.StockNumber} (ID: {stockItem.Id}) is not confirmed in this sale order.");
                }

                if (!string.IsNullOrEmpty(confirmedProduct.Invoice))
                {
                    throw new HandleException($"Cannot unconfirm stock item {stockItem.StockNumber} - already included in invoice {confirmedProduct.Invoice}.");
                }

                // placeholder ไม่เคยจองของจริง (ConfirmStockItemsCore ข้าม stock effect ให้แล้ว) — ลบแถวเฉยๆ ห้ามแตะ piece/balance
                if (!confirmedProduct.IsPlaceholder)
                {
                    var piece = await _jewelryContext.TbtStockPiece
                        .FirstOrDefaultAsync(p => p.StockNumber == stockItem.StockNumber);

                    if (piece != null)
                    {
                        var balance = await _jewelryContext.TbtStockBalance
                            .FirstOrDefaultAsync(b => b.SkuCode == piece.SkuCode && b.LocationCode == piece.LocationCode);

                        if (balance != null)
                        {
                            balance.QtyReserved -= confirmedProduct.Qty;
                            balance.LastMovementAt = unconfirmedDate;
                            _jewelryContext.TbtStockBalance.Update(balance);
                        }

                        piece.QtyReserved = Math.Max(0, piece.QtyReserved - confirmedProduct.Qty);
                        StockPieceQtyHelper.RecalcStatus(piece);
                        piece.UpdateDate = unconfirmedDate;
                        piece.UpdateBy = CurrentUsername;
                        _jewelryContext.TbtStockPiece.Update(piece);

                        _jewelryContext.TbtStockMovement.Add(new TbtStockMovement
                        {
                            MovementDate = unconfirmedDate,
                            MovementType = "UNRESERVE",
                            SkuCode = piece.SkuCode,
                            StockNumber = piece.StockNumber,
                            ProductCode = piece.ProductCode,
                            ToLocation = piece.LocationCode,
                            Qty = confirmedProduct.Qty,
                            RefDocType = "SO",
                            RefDocNo = soNumberUpper,
                            CreateDate = unconfirmedDate,
                            CreateBy = CurrentUsername
                        });
                    }
                }

                // Remove confirmed product entry
                _jewelryContext.TbtSaleOrderProduct.Remove(confirmedProduct);
                unconfirmedStockNumbers.Add(stockItem.StockNumber);
            }

            return unconfirmedStockNumbers;
        }

        public async Task<jewelry.Model.Sale.SaleOrder.ReplaceConfirmedStock.Response> ReplaceConfirmedStock(jewelry.Model.Sale.SaleOrder.ReplaceConfirmedStock.Request request)
        {
            if (string.IsNullOrEmpty(request.SoNumber))
            {
                throw new HandleException("Sale Order Number is required.");
            }

            if (string.IsNullOrEmpty(request.NewStockNumber))
            {
                throw new HandleException("New Stock Number is required.");
            }

            var soNumberUpper = request.SoNumber.ToUpper();
            var replacedDate = DateTime.UtcNow;

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                // Global lock order: stock piece -> stock balance -> sale order product
                await _jewelryContext.LockStockPiecesAsync(new[] { request.NewStockNumber });

                var piece = await _jewelryContext.TbtStockPiece
                    .FirstOrDefaultAsync(p => p.StockNumber == request.NewStockNumber);

                if (piece == null)
                {
                    throw new HandleException($"ไม่พบเลขสินค้า {request.NewStockNumber} ในคลัง");
                }

                if (piece.Status == "SOLD")
                {
                    throw new HandleException($"เลขสินค้า {request.NewStockNumber} ถูกขายไปแล้ว ไม่สามารถใช้เติมรายการรอของได้");
                }

                await _jewelryContext.LockStockBalancesAsync(new[] { piece.SkuCode });
                await _jewelryContext.LockSaleOrderProductsByIdsAsync(new[] { request.SaleOrderProductId });

                var soProduct = await _jewelryContext.TbtSaleOrderProduct
                    .FirstOrDefaultAsync(p => p.SoNumber == soNumberUpper && p.Id == request.SaleOrderProductId);

                if (soProduct == null)
                {
                    throw new HandleException($"ไม่พบรายการ (ID: {request.SaleOrderProductId}) ในใบสั่งขาย {request.SoNumber}");
                }

                if (!soProduct.IsPlaceholder)
                {
                    throw new HandleException("รายการนี้ไม่ใช่รายการรอของ (placeholder) ไม่สามารถเติมของแทนที่ได้");
                }

                if (!string.IsNullOrEmpty(soProduct.Invoice))
                {
                    throw new HandleException($"รายการนี้ออกใบแจ้งหนี้ {soProduct.Invoice} ไปแล้ว ไม่สามารถเติมของแทนที่ได้");
                }

                var qty = request.Qty ?? soProduct.Qty;
                if (qty <= 0)
                {
                    throw new HandleException("จำนวนต้องมากกว่า 0");
                }

                var available = StockPieceQtyHelper.Available(piece);
                if (qty > available)
                {
                    throw new HandleException($"เลข {request.NewStockNumber} พร้อมขาย {available:0.##} ชิ้น แต่ขอ {qty:0.##} ชิ้น");
                }

                var balance = await _jewelryContext.TbtStockBalance
                    .FirstOrDefaultAsync(b => b.SkuCode == piece.SkuCode && b.LocationCode == piece.LocationCode);

                // เก็บ identity เดิมของแถวไว้ — เปลี่ยนเฉพาะเลขสต็อก/สถานะ placeholder/จำนวน/audit fields
                // ราคาที่ตกลงกับลูกค้าไว้แล้ว (LineKey, Id, PriceOrigin, Discount, NetPrice) ห้ามแตะ
                soProduct.StockNumber = request.NewStockNumber;
                soProduct.Stocknumberorigin = piece.ProductCode;
                soProduct.IsPlaceholder = false;
                soProduct.Qty = qty;
                soProduct.UpdateBy = CurrentUsername;
                soProduct.UpdateDate = replacedDate;
                _jewelryContext.TbtSaleOrderProduct.Update(soProduct);

                if (balance != null)
                {
                    balance.QtyReserved += qty;
                    balance.LastMovementAt = replacedDate;
                    _jewelryContext.TbtStockBalance.Update(balance);
                }

                piece.QtyReserved += qty;
                StockPieceQtyHelper.RecalcStatus(piece);
                piece.UpdateDate = replacedDate;
                piece.UpdateBy = CurrentUsername;
                _jewelryContext.TbtStockPiece.Update(piece);

                _jewelryContext.TbtStockMovement.Add(new TbtStockMovement
                {
                    MovementDate = replacedDate,
                    MovementType = "RESERVE",
                    SkuCode = piece.SkuCode,
                    StockNumber = piece.StockNumber,
                    ProductCode = piece.ProductCode,
                    FromLocation = piece.LocationCode,
                    Qty = qty,
                    RefDocType = "SO",
                    RefDocNo = soNumberUpper,
                    CreateDate = replacedDate,
                    CreateBy = CurrentUsername
                });

                await _jewelryContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return new jewelry.Model.Sale.SaleOrder.ReplaceConfirmedStock.Response
                {
                    SaleOrderProductId = soProduct.Id,
                    StockNumber = soProduct.StockNumber,
                    StockNumberOrigin = soProduct.Stocknumberorigin,
                    Message = $"เติมของสำเร็จ เปลี่ยนเป็นเลขสินค้า {soProduct.StockNumber}"
                };
            }
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"Error replacing confirmed stock: {ex.Message}");
            }
        }
    }
}