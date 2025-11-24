using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.TransferStock
{

    public interface IOldStockService
    {
        Task<string> TransferStock9K(jewelry.Model.Stock.OldStock._9K.Request request);
        Task<string> TransferStock18K(jewelry.Model.Stock.OldStock._9K.Request request);
    }
    public class OldStockService : BaseService, IOldStockService
    {
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public OldStockService(JewelryContext JewelryContext, IHttpContextAccessor httpContextAccessor,
            IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }
      

        private TbmProductType GetProductType(List<TbmProductType> master, string check)
        {
            if (string.IsNullOrEmpty(check))
            {
                // ถ้าไม่มีข้อมูลประเภทสินค้า ให้ใช้ค่าเริ่มต้น (DK)
                return master.FirstOrDefault(x => x.Code == "N/A") ?? master.FirstOrDefault();
            }

            var checkText = check.ToUpper().Trim();

            // ตรวจสอบแต่ละประเภทตามลำดับความสำคัญ
            if (checkText.Contains("RING"))
            {
                return master.FirstOrDefault(x => x.Code == "R");
            }
            else if (checkText.Contains("PENDANT"))
            {
                return master.FirstOrDefault(x => x.Code == "P");
            }
            else if (checkText.Contains("EARRING") || checkText.Contains("LOCK") || checkText.Contains("STDU") || checkText.Contains("STUD"))
            {
                if (checkText.Contains("STUD") || checkText.Contains("STUD"))
                {
                    return master.FirstOrDefault(x => x.Code == "ES");
                }
                else if (checkText.Contains("HOOK"))
                {
                    return master.FirstOrDefault(x => x.Code == "EH");
                }
                else if (checkText.Contains("LOCK"))
                {
                    return master.FirstOrDefault(x => x.Code == "EL");
                }
                else
                {
                    return master.FirstOrDefault(x => x.Code == "E");
                }
            }
            else if (checkText.Contains("BRACELET"))
            {
                return master.FirstOrDefault(x => x.Code == "B");
            }
            else if (checkText.Contains("NECKLACE"))
            {
                return master.FirstOrDefault(x => x.Code == "N");
            }
            else if (checkText.Contains("BANGLE"))
            {
                return master.FirstOrDefault(x => x.Code == "G");
            }
            else if (checkText.Contains("BROOCH"))
            {
                return master.FirstOrDefault(x => x.Code == "T");
            }
            else if (checkText.Contains("BUTTON"))
            {
                return master.FirstOrDefault(x => x.Code == "V");
            }
            else if (checkText.Contains("CHAIN"))
            {
                return master.FirstOrDefault(x => x.Code == "CH");
            }

            // หากยังไม่พบ ให้ใช้ค่าเริ่มต้น
            return master.FirstOrDefault(x => x.Code == "N/A") ?? master.FirstOrDefault();
        }
        private DateTime GetProductionDate(string productionDateCode)
        {
            try
            {
                // ถ้าเป็นค่าว่างหรือ null ให้ใช้วันที่ปัจจุบัน
                if (string.IsNullOrEmpty(productionDateCode) || productionDateCode.ToUpper() == "[NULL]")
                {
                    return DateTime.UtcNow;
                }

                // ทำความสะอาดข้อมูล ตัดอักขระพิเศษหรือช่องว่างออก
                productionDateCode = productionDateCode.Trim();

                // ตรวจสอบว่าเป็นตัวเลขจำนวน 8 หลักหรือไม่
                if (productionDateCode.Length != 8 || !int.TryParse(productionDateCode, out _))
                {
                    return DateTime.UtcNow;
                }

                // แยกปี เดือน วัน
                int yearThai = int.Parse(productionDateCode.Substring(0, 4));  // พ.ศ.
                int month = int.Parse(productionDateCode.Substring(4, 2));      // เดือน
                int day = int.Parse(productionDateCode.Substring(6, 2));        // วัน

                // แปลงปี พ.ศ. เป็น ค.ศ.
                int yearGregorian = yearThai - 543;

                // ตรวจสอบความถูกต้องของวันที่
                if (month < 1 || month > 12 || day < 1 || day > 31 || yearGregorian < 1900 || yearGregorian > 2100)
                {
                    return DateTime.UtcNow;
                }

                // สร้าง DateTime ในเขตเวลาท้องถิ่น (ประเทศไทย - GMT+7)
                var localTime = new DateTime(yearGregorian, month, day, 0, 0, 0, DateTimeKind.Local);

                // แปลงเป็น UTC
                return localTime.ToUniversalTime();
            }
            catch (Exception)
            {
                // กรณีเกิดข้อผิดพลาดในการแปลงค่า ให้ใช้วันที่ปัจจุบัน
                return DateTime.UtcNow;
            }
        }
        private string ProducttionType(List<TbmGold> master, string check)
        {
            if (string.IsNullOrEmpty(check))
            {
                // ถ้าไม่มีข้อมูลประเภทสินค้า ให้ใช้ค่าเริ่มต้น (DK)
                return master.FirstOrDefault(x => x.Code == "WG").NameEn;
            }

            var checkText = check.ToUpper().Trim();

            if (checkText.Contains("PG"))
            {
                return master.FirstOrDefault(x => x.Code == "PG").NameEn;
            }
            else if (checkText.Contains("YG"))
            {
                return master.FirstOrDefault(x => x.Code == "YG").NameEn;
            }
            else if (checkText.Contains("SI"))
            {
                return master.FirstOrDefault(x => x.Code == "SV").NameEn;
            }
            else
            {
                return master.FirstOrDefault(x => x.Code == "WG").NameEn;
            }
        }
        private string ProducttionTypeSize(List<TbmGoldSize> master, string check)
        {
            if (string.IsNullOrEmpty(check))
            {
                // ถ้าไม่มีข้อมูลประเภทสินค้า ให้ใช้ค่าเริ่มต้น (DK)
                return master.FirstOrDefault(x => x.Code == "100").NameEn;
            }

            var checkText = check.ToUpper().Trim();

            if (checkText.Contains("14K"))
            {
                return master.FirstOrDefault(x => x.Code == "5").NameEn;
            }
            else if (checkText.Contains("18K"))
            {
                return master.FirstOrDefault(x => x.Code == "7").NameEn;
            }
            else if (checkText.Contains("22K"))
            {
                return master.FirstOrDefault(x => x.Code == "8").NameEn;
            }
            else if (checkText.Contains("10K"))
            {
                return master.FirstOrDefault(x => x.Code == "4").NameEn;
            }
            else if (checkText.Contains("9K"))
            {
                return master.FirstOrDefault(x => x.Code == "3").NameEn;
            }
            else if (checkText.Contains("SIL"))
            {
                return master.FirstOrDefault(x => x.Code == "9").NameEn;
            }
            else
            {
                return master.FirstOrDefault(x => x.Code == "100").NameEn;
            }
        }
        private string? GetWO(string check)
        {
            if (string.IsNullOrEmpty(check))
            {
                return null;
            }
            if (check.StartsWith("NO."))
            {
                check = check.Substring(3).Trim();
            }
            if (check.Contains("NO."))
            {
                // หาตำแหน่งเริ่มต้นของ "NO."
                int startPos = check.IndexOf("NO.");
                // ตัด "NO." ออกและคืนค่าข้อความที่เหลือ
                check = check.Substring(startPos + 3).Trim();
            }

            // ตรวจสอบกรณีที่มีทั้ง "-" และ "/"
            bool hasDash = check.Contains("-");
            bool hasSlash = check.Contains("/");

            if (hasSlash && hasDash)
            {
                // กรณีมีทั้ง "-" และ "/" ให้ตัดตาม "/" เท่านั้น
                string[] parts = check.Split('/');
                return parts.Length > 0 ? parts[0].Trim() : check;
            }
            else if (hasSlash)
            {
                // กรณีมีแค่ "/"
                string[] parts = check.Split('/');
                return parts.Length > 0 ? parts[0].Trim() : check;
            }
            else if (hasDash)
            {
                // กรณีมีแค่ "-"
                string[] parts = check.Split('-');
                return parts.Length > 0 ? parts[0].Trim() : check;
            }

            // กรณีไม่พบเครื่องหมายใ
            return check;
        }
        private int? GetWONumber(string check)
        {
            if (string.IsNullOrEmpty(check))
            {
                return null;
            }

            // ตัดช่องว่างหน้า-หลังออก
            check = check.Trim();

            // ถ้าข้อความขึ้นต้นด้วย "NO." ให้ตัดออก
            if (check.StartsWith("NO."))
            {
                check = check.Substring(3).Trim();
            }

            // ถ้าพบคำว่า "NO." ในตำแหน่งอื่น
            if (check.Contains("NO."))
            {
                // หาตำแหน่งเริ่มต้นของ "NO."
                int startPos = check.IndexOf("NO.");
                // ตัด "NO." ออกและคืนค่าข้อความที่เหลือ
                check = check.Substring(startPos + 3).Trim();
            }

            // ตรวจสอบกรณีที่มีทั้ง "-" และ "/"
            bool hasDash = check.Contains("-");
            bool hasSlash = check.Contains("/");

            string secondPart = null;

            if (hasSlash && hasDash)
            {
                // กรณีมีทั้ง "-" และ "/" ให้ตัดตาม "/" เท่านั้น
                string[] parts = check.Split('/');
                if (parts.Length >= 2)
                {
                    secondPart = parts[1].Trim();
                }
            }
            else if (hasSlash)
            {
                // กรณีมีแค่ "/"
                string[] parts = check.Split('/');
                if (parts.Length >= 2)
                {
                    secondPart = parts[1].Trim();
                }
            }
            else if (hasDash)
            {
                // กรณีมีแค่ "-"
                string[] parts = check.Split('-');
                if (parts.Length >= 2)
                {
                    secondPart = parts[1].Trim();
                }
            }

            // ถ้าไม่มีตำแหน่งที่ 2 หรือไม่พบเครื่องหมายใดๆ
            if (secondPart == null)
            {
                return null;
            }

            // พยายามแปลงเป็น int
            if (int.TryParse(secondPart, out int result))
            {
                return result;
            }

            return null;
        }

        private TbtStockProductMaterial? GetMaterial(string running, string check, string type, string typeCode, decimal? qty, decimal? weight, string weghtUnit, decimal? price, string size)
        {
            var newMaterial = new TbtStockProductMaterial();

            newMaterial.StockNumber = running;
            newMaterial.CreateDate = DateTime.UtcNow;
            newMaterial.CreateBy = CurrentUsername ?? "system";

            newMaterial.TypeOrigin = string.IsNullOrEmpty(type) || string.IsNullOrWhiteSpace(type) || type == "[NULL]" || type == " " ? null : type;
            newMaterial.Type = "Gem";
            newMaterial.TypeCode = typeCode;

            newMaterial.Qty = qty.HasValue ? qty.Value : 0;
            //QtyUnit = item.Unit2,

            newMaterial.Weight = weight.HasValue ? weight.Value : 0;
            newMaterial.WeightUnit = weghtUnit;
            newMaterial.Price = price.HasValue ? price.Value : 0;

            newMaterial.Size = size;

            if (check.Contains("DIA") || check == "D")
            {
                newMaterial.Type = "Diamond";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.Type}{newMaterial.Weight} {newMaterial.WeightUnit} {newMaterial.TypeCode}";
            }

            if (check.Contains("CZ"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Cubic Zirconia";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("RU"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Ruby";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("PINK SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Pink Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("PINK SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Yellow Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("FANCY SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Fancy Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("YELLOW SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Yellow Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("WHITE SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "White Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("AME"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Amethyst";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("EME") || check == "E")
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Emerald";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("BLUE TO") || check.Contains("BT"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Blue Topaz";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("GREEN TO"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Green Topaz";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }


            if (check.Contains("CIT") || check.Contains("CL"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Citrine";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("TANZ"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Tanzanite";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("MIX"))
            {
                newMaterial.Type = "GEM";
                newMaterial.TypeCode = "Mix Stone";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("AQU"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Aquamarine";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("LEMON"))
            {
                newMaterial.Type = "GEM";
                newMaterial.TypeCode = "Lemon Quart";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("ROSE QU"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Rose Quart";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("WHITE QU"))
            {
                newMaterial.Type = "GEM";
                newMaterial.TypeCode = "White Quart";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("TSA"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Tsavolite";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("GAR"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Garnet";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("WHITE PEA"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "WHITE Pearl";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("GOLD"))
            {
                newMaterial.Type = "Gold";
                newMaterial.TypeCode = check.Contains("WHITE") ? "WG" : "YG";
                newMaterial.TypeBarcode = $"{newMaterial.Weight} {newMaterial.WeightUnit} {newMaterial.Type} {newMaterial.Size ?? null}";
            }

            return newMaterial;
        }

        private bool CheckTypeOrigin(string check)
        {
            var response = false;

            if (!string.IsNullOrEmpty(check))
            {
                response = true;
            }
            if (!string.IsNullOrWhiteSpace(check))
            {
                response = true;
            }

            if (check == " ")
            {
                response = false;
            }
            if (check == "[NULL]")
            {
                response = false;
            }

            return response;
        }


        #region *** 2025-11-16 Trasfer Stock 9K ***

        public async Task<string> TransferStock9K(jewelry.Model.Stock.OldStock._9K.Request request)
        {
            var get9kStock = (from item in _jewelryContext.Stock9k
                              join stock in _jewelryContext.Stock
                              on item.NoProduct equals stock.Noproduct
                              //on new
                              //{
                              //    Product = item.NoProduct,
                              //    //Style = item.StyleNo
                              //} equals new
                              //{
                              //    Product = stock.Noproduct,
                              //    //Style = stock.Styleno
                              //}
                              into _stock
                              from stocks in _stock.DefaultIfEmpty()
                              where item.IsTransfer == false
                              select new { item, stocks }).Take(request.Take);

            if (!get9kStock.Any())
            {
                throw new HandleException("No 9K stock to transfer.");
            }

            var masterProductType = await _jewelryContext.TbmProductType.ToListAsync();
            var masterGold = await _jewelryContext.TbmGold.ToListAsync();
            var masterGoldSize = await _jewelryContext.TbmGoldSize.ToListAsync();

            // Pre-generate running numbers in batch to avoid multiple DB calls
            var _receiptRunning = await _runningNumberService.GenerateRunningNumber("DK9K");
            var stockRunningNumbers = new List<string>();
            for (int i = 0; i < get9kStock.Count(); i++)
            {
                stockRunningNumbers.Add(await _runningNumberService.GenerateRunningNumberForStockProductHash("DK-9K"));
            }


            var newProducts = new List<TbtStockProduct>();
            var newProductMaterials = new List<TbtStockProductMaterial>();
            var stock9kUpdates = new List<Stock9k>();

            int add = 0;
            int runningIndex = 0;

            foreach (var _stock in get9kStock)
            {
                var stock = _stock.stocks;
                var stock9kUpdate = _stock.item;

                if (stock == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(stock.NoCode))
                {
                    continue;
                }

                var _stockRunning = stockRunningNumbers[runningIndex++];
                add++;

                var newProduct = new TbtStockProduct
                {
                    StockNumber = _stockRunning,
                    Status = StockProductStatus.Available,

                    ReceiptNumber = _receiptRunning,
                    ReceiptDate = DateTime.UtcNow,
                    ReceiptType = "transfer",

                    Mold = stock.NoCode,
                    MoldDesign = stock.NoCode,

                    Qty = stock.Quantity ?? 1,
                    ProductPrice = stock.Pricesale ?? 0,

                    ProductCost = string.IsNullOrEmpty(stock.Pricecost) ? 0 :
                        decimal.TryParse(stock.Pricecost, out var cost) ? cost : 0,
                    ProductCode = stock.Noproduct,
                    ProductNumber = stock.Codeproduct,
                    ProductNameTh = stock.Productname ?? "DK",
                    ProductNameEn = stock.Productname ?? "DK",

                    ImageName = stock.NoCode,
                    ImagePath = $"{stock.NoCode}.jpg",

                    Size = stock.Ringsize,
                    Remark = stock.Remark,

                    CreateBy = CurrentUsername ?? "system",
                    CreateDate = DateTime.UtcNow
                };

                // Optimize product type assignment
                if (!string.IsNullOrEmpty(stock.Typep))
                {
                    var productType = GetProductType(masterProductType, stock.Typep);
                    newProduct.ProductType = productType.Code;
                    newProduct.ProductTypeName = productType.NameTh;
                }

                newProduct.ProductionDate = GetProductionDate(stock.Dateproduct);
                newProduct.ProductionType = ProducttionType(masterGold, stock.Typeg);
                newProduct.ProductionTypeSize = ProducttionTypeSize(masterGoldSize, stock.Productname);
                newProduct.WoOrigin = stock.Jobno;
                newProduct.Wo = GetWO(stock.Jobno);
                newProduct.WoNumber = GetWONumber(stock.Jobno);

                newProducts.Add(newProduct);

                // Process materials more efficiently
                var materialTypes = new[]
                {
                    //type gold
                    new { Type = stock.Typeg, TypeCode = stock.Typed1, Qty = stock.Qtyg, Weight = stock.Wg, Unit = stock.Unit1, Price = stock.Priceg, Size = (string)null },

                    new { Type = stock.Typed, TypeCode = stock.Typed1, Qty = stock.Qtyd, Weight = stock.Wd, Unit = stock.Unit2, Price = stock.Priced, Size = (string)null },
                    new { Type = stock.Typer, TypeCode = stock.Typed1, Qty = stock.Qtyr, Weight = stock.Wr, Unit = stock.Unit3, Price = stock.Pricer, Size = stock.Sizer },
                    new { Type = stock.TypeS, TypeCode = stock.Typed1, Qty = stock.Qtys, Weight = stock.Ws, Unit = stock.Unit4, Price = stock.Prices, Size = stock.Sizes },
                    new { Type = stock.Typee, TypeCode = stock.Typed1, Qty = stock.Qtye, Weight = stock.We, Unit = stock.Unit5, Price = stock.Pricee, Size = stock.Sizee },
                    new { Type = stock.Typem, TypeCode = stock.Typed1, Qty = stock.Qtym, Weight = stock.Wm, Unit = stock.Unit6, Price = stock.Pricem, Size = stock.Sizem }
                };

                foreach (var mat in materialTypes)
                {
                    if (!string.IsNullOrEmpty(mat.Type))
                    {
                        var newMaterial = GetMaterial(_stockRunning, mat.Type.ToUpper().Trim(),
                            mat.Type, mat.TypeCode, mat.Qty, mat.Weight, mat.Unit, mat.Price, mat.Size);

                        if (CheckTypeOrigin(newMaterial.TypeOrigin))
                        {
                            newProductMaterials.Add(newMaterial);
                        }
                    }
                }

                stock9kUpdate.IsTransfer = true;
                stock9kUpdates.Add(stock9kUpdate);
            }

            using var scope = new TransactionScope(
               TransactionScopeOption.Required,
               new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
               TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                // Batch insert operations
                if (newProducts.Any())
                {
                    _jewelryContext.TbtStockProduct.AddRange(newProducts);
                }

                if (newProductMaterials.Any())
                {
                    _jewelryContext.TbtStockProductMaterial.AddRange(newProductMaterials);
                }

                if (stock9kUpdates.Any())
                {
                    _jewelryContext.Stock9k.UpdateRange(stock9kUpdates);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }
            catch
            {
                scope.Dispose();
                throw;
            }

            return $"success {add} item";
        }
        #endregion
        #region *** 2025-11-25 TranferStock 18K ***
        public async Task<string> TransferStock18K(jewelry.Model.Stock.OldStock._9K.Request request)
        {
            var get9kStock = (from item in _jewelryContext.Stock18k
                              join stock in _jewelryContext.Stock
                              on item.NoProduct equals stock.Noproduct
                              //on new
                              //{
                              //    Product = item.NoProduct,
                              //    //Style = item.StyleNo
                              //} equals new
                              //{
                              //    Product = stock.Noproduct,
                              //    //Style = stock.Styleno
                              //}
                              into _stock
                              from stocks in _stock.DefaultIfEmpty()
                              where item.IsTransfer == false
                              select new { item, stocks }).Take(request.Take);

            if (!get9kStock.Any())
            {
                throw new HandleException("No 9K stock to transfer.");
            }

            var masterProductType = await _jewelryContext.TbmProductType.ToListAsync();
            var masterGold = await _jewelryContext.TbmGold.ToListAsync();
            var masterGoldSize = await _jewelryContext.TbmGoldSize.ToListAsync();

            // Pre-generate running numbers in batch to avoid multiple DB calls
            var _receiptRunning = await _runningNumberService.GenerateRunningNumber("DK18K");
            var stockRunningNumbers = new List<string>();
            for (int i = 0; i < get9kStock.Count(); i++)
            {
                stockRunningNumbers.Add(await _runningNumberService.GenerateRunningNumberForStockProductHash("DK-18K"));
            }


            var newProducts = new List<TbtStockProduct>();
            var newProductMaterials = new List<TbtStockProductMaterial>();
            var stock18kUpdates = new List<Stock18k>();

            int add = 0;
            int runningIndex = 0;

            foreach (var _stock in get9kStock)
            {
                var stock = _stock.stocks;
                var stock9kUpdate = _stock.item;

                if (stock == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(stock.NoCode))
                {
                    continue;
                }

                var _stockRunning = stockRunningNumbers[runningIndex++];
                add++;

                var newProduct = new TbtStockProduct
                {
                    StockNumber = _stockRunning,
                    Status = StockProductStatus.Available,

                    ReceiptNumber = _receiptRunning,
                    ReceiptDate = DateTime.UtcNow,
                    ReceiptType = "transfer",

                    Mold = stock.NoCode,
                    MoldDesign = stock.NoCode,

                    Qty = stock.Quantity ?? 1,
                    ProductPrice = stock.Pricesale ?? 0,

                    ProductCost = string.IsNullOrEmpty(stock.Pricecost) ? 0 :
                        decimal.TryParse(stock.Pricecost, out var cost) ? cost : 0,
                    ProductCode = stock.Noproduct,
                    ProductNumber = stock.Codeproduct,
                    ProductNameTh = stock.Productname ?? "DK",
                    ProductNameEn = stock.Productname ?? "DK",

                    ImageName = stock.NoCode,
                    ImagePath = $"{stock.NoCode}.jpg",

                    Size = stock.Ringsize,
                    Remark = stock.Remark,

                    CreateBy = CurrentUsername ?? "system",
                    CreateDate = DateTime.UtcNow
                };

                // Optimize product type assignment
                if (!string.IsNullOrEmpty(stock.Typep))
                {
                    var productType = GetProductType(masterProductType, stock.Typep);
                    newProduct.ProductType = productType.Code;
                    newProduct.ProductTypeName = productType.NameTh;
                }

                newProduct.ProductionDate = GetProductionDate(stock.Dateproduct);
                newProduct.ProductionType = ProducttionType(masterGold, stock.Typeg);
                newProduct.ProductionTypeSize = ProducttionTypeSize(masterGoldSize, stock.Productname);
                newProduct.WoOrigin = stock.Jobno;
                newProduct.Wo = GetWO(stock.Jobno);
                newProduct.WoNumber = GetWONumber(stock.Jobno);

                newProducts.Add(newProduct);

                // Process materials more efficiently
                var materialTypes = new[]
                {
                    //type gold
                    new { Type = stock.Typeg, TypeCode = stock.Typed1, Qty = stock.Qtyg, Weight = stock.Wg, Unit = stock.Unit1, Price = stock.Priceg, Size = (string)null },

                    new { Type = stock.Typed, TypeCode = stock.Typed1, Qty = stock.Qtyd, Weight = stock.Wd, Unit = stock.Unit2, Price = stock.Priced, Size = (string)null },
                    new { Type = stock.Typer, TypeCode = stock.Typed1, Qty = stock.Qtyr, Weight = stock.Wr, Unit = stock.Unit3, Price = stock.Pricer, Size = stock.Sizer },
                    new { Type = stock.TypeS, TypeCode = stock.Typed1, Qty = stock.Qtys, Weight = stock.Ws, Unit = stock.Unit4, Price = stock.Prices, Size = stock.Sizes },
                    new { Type = stock.Typee, TypeCode = stock.Typed1, Qty = stock.Qtye, Weight = stock.We, Unit = stock.Unit5, Price = stock.Pricee, Size = stock.Sizee },
                    new { Type = stock.Typem, TypeCode = stock.Typed1, Qty = stock.Qtym, Weight = stock.Wm, Unit = stock.Unit6, Price = stock.Pricem, Size = stock.Sizem }
                };

                foreach (var mat in materialTypes)
                {
                    if (!string.IsNullOrEmpty(mat.Type))
                    {
                        var newMaterial = GetMaterial(_stockRunning, mat.Type.ToUpper().Trim(),
                            mat.Type, mat.TypeCode, mat.Qty, mat.Weight, mat.Unit, mat.Price, mat.Size);

                        if (CheckTypeOrigin(newMaterial.TypeOrigin))
                        {
                            newProductMaterials.Add(newMaterial);
                        }
                    }
                }

                stock9kUpdate.IsTransfer = true;
                stock18kUpdates.Add(stock9kUpdate);
            }

            using var scope = new TransactionScope(
               TransactionScopeOption.Required,
               new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
               TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                // Batch insert operations
                if (newProducts.Any())
                {
                    _jewelryContext.TbtStockProduct.AddRange(newProducts);
                }

                if (newProductMaterials.Any())
                {
                    _jewelryContext.TbtStockProductMaterial.AddRange(newProductMaterials);
                }

                if (stock18kUpdates.Any())
                {
                    _jewelryContext.Stock18k.UpdateRange(stock18kUpdates);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }
            catch
            {
                scope.Dispose();
                throw;
            }

            return $"success {add} item";
        }
        #endregion

    }
}

