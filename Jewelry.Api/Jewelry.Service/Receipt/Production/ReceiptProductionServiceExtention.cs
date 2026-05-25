using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jewelry.Service.Receipt.Production
{
    public static class ReceiptProductionServiceExtention
    {

        public static string MapToTbtStockProductReceiptPlanJson(this jewelry.Model.Receipt.Production.Draft.Create.Request request)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(request.Stocks, options);
        }
        public static string MapToTbtStockProductReceiptPlanBreakdownJson(this jewelry.Model.Receipt.Production.Draft.Create.Request request)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(request.BreakDown, options);
        }
        public static string MapToTbtStockProductReceiptPlanJson(this jewelry.Model.Receipt.Production.PlanGet.Response request)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(request.Stocks, options);
        }
        public static string MapToTbtStockProductReceiptPlanBreakdownJson(this jewelry.Model.Receipt.Production.PlanGet.Response request)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(request.BreakDown, options);
        }

        public static List<jewelry.Model.Receipt.Production.PlanGet.ReceiptStock> GetStocksFromJsonDraft(this TbtStockProductReceiptPlan plan)
        {
            if (string.IsNullOrEmpty(plan.JsonDraft))
                return new List<jewelry.Model.Receipt.Production.PlanGet.ReceiptStock>();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                return JsonSerializer.Deserialize<List<jewelry.Model.Receipt.Production.PlanGet.ReceiptStock>>(plan.JsonDraft, options) ?? new List<jewelry.Model.Receipt.Production.PlanGet.ReceiptStock>();
            }
            catch (Exception ex)
            {
                throw new HandleException($"ไม่สามารถแปลงข้อมูล Draft ได้ เนื่องจากรูปแบบข้อมูลไม่ถูกต้อง กรุณาตรวจสอบข้อมูลหรือลองใหม่อีกครั้ง");
            }
        }

        public static List<jewelry.Model.Receipt.Production.PlanGet.Material> GetStocksFromBreakdownJsonDraft(this TbtStockProductReceiptPlan plan)
        {
            if (string.IsNullOrEmpty(plan.JsonBreakdown))
                return new List<jewelry.Model.Receipt.Production.PlanGet.Material>();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                return JsonSerializer.Deserialize<List<jewelry.Model.Receipt.Production.PlanGet.Material>>(plan.JsonBreakdown, options) ?? new List<jewelry.Model.Receipt.Production.PlanGet.Material>();
            }
            catch (Exception ex)
            {
                throw new HandleException($"ไม่สามารถแปลงข้อมูล Draft ได้ เนื่องจากรูปแบบข้อมูลไม่ถูกต้อง กรุณาตรวจสอบข้อมูลหรือลองใหม่อีกครั้ง");
            }
        }

        public static TbtStockProduct MapNewStockProduction(this TbtStockProductReceiptItem stock,
            TbtStockProductReceiptPlan receipt,
            TbtProductionPlan plan,
            jewelry.Model.Receipt.Production.Confirm.ConfirmStock confirm,
            string stockRunning,
            string operatorBy)
        {

            var response = new TbtStockProduct
            {
                StockNumber = stockRunning,
                Status = StockProductStatus.Available,

                ReceiptNumber = stock.StockReceiptNumber,
                ReceiptDate = DateTime.UtcNow,
                ReceiptType = "production",

                Mold = plan.Mold,
                MoldDesign = confirm.MoldDesign,

                Qty = confirm.Qty,
                ProductPrice = confirm.Price,

                ProductNumber = confirm.ProductNumber.ToUpper(),
                ProductNameTh = confirm.ProductNameTH.ToUpper(),
                ProductNameEn = confirm.ProductNameEN.ToUpper(),

                ProductType = plan.ProductType,
                ProductTypeName = plan.ProductTypeNavigation.NameTh,

                ImageName = confirm.ImageName,
                ImagePath = confirm.ImagePath,

                Wo = plan.Wo,
                WoNumber = plan.WoNumber,

                ProductionDate = plan.CreateDate,
                ProductionType = plan.Type,
                ProductionTypeSize = plan.TypeSize,

                Size = confirm.Size,
                StudEarring = confirm.StudEarring,
                Location = confirm.Location,
                Remark = confirm.Remark,

                CreateBy = operatorBy,
                CreateDate = DateTime.UtcNow
            };

            return response;
        }

        public static jewelry.Model.Receipt.Production.Confirm.Stock MapResponseNewStockProduction(this TbtStockProduct stock)
        {

            var response = new jewelry.Model.Receipt.Production.Confirm.Stock()
            {
                StockNumber = stock.StockNumber,
                Status = stock.Status,

                ReceiptNumber = stock.ReceiptNumber,
                ReceiptDate = stock.ReceiptDate,
                ReceiptType = stock.ReceiptType,

                Mold = stock.Mold,
                MoldDesign = stock.MoldDesign,

                Qty = stock.Qty,
                ProductPrice = stock.ProductPrice,

                ProductNumber = stock.ProductNumber,
                ProductNameTh = stock.ProductNameTh,
                ProductNameEn = stock.ProductNameEn,

                ProductType = stock.ProductType,
                ProductTypeName = stock.ProductTypeName,

                ImageName = stock.ImageName,
                ImagePath = stock.ImagePath,

                Wo = stock.Wo,
                WoNumber = stock.WoNumber,

                ProductionDate = stock.CreateDate,
                ProductionType = stock.ProductionType,
                ProductionTypeSize = stock.ProductionTypeSize,

                Size = stock.Size,
                StudEarring = stock.StudEarring,
                Location = stock.Location,
                Remark = stock.Remark,

                CreateBy = stock.CreateBy,
                CreateDate = stock.CreateDate,

                Materials = new List<jewelry.Model.Receipt.Production.Confirm.Material>()
            };

            return response;
        }

        public static List<TbtStockProductMaterial> MapNewStockProductionMaterial(this jewelry.Model.Receipt.Production.Confirm.ConfirmStock confirm,
            string stockRunning,
            string operatorBy)
        {
            var response = new List<TbtStockProductMaterial>();

            foreach (var item in confirm.Materials)
            {
                response.Add(new TbtStockProductMaterial
                {
                    StockNumber = stockRunning,

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

                    CreateBy = operatorBy,
                    CreateDate = DateTime.UtcNow
                });
            }

            return response;
        }

        public static List<jewelry.Model.Receipt.Production.Confirm.Material> MapResponseNewStockMaterialProduction(this List<TbtStockProductMaterial> stocks)
        {
            var result = new List<jewelry.Model.Receipt.Production.Confirm.Material>();

            foreach (var stock in stocks)
            {
                result.Add(new jewelry.Model.Receipt.Production.Confirm.Material()
                {
                    Type = stock.Type,
                    TypeName = stock.TypeName,
                    TypeCode = stock.TypeCode,
                    TypeBarcode = stock.TypeBarcode,

                    Qty = stock.Qty,
                    QtyUnit = stock.QtyUnit,
                    Weight = stock.Weight,
                    WeightUnit = stock.WeightUnit,

                    Size = stock.Size,
                    Region = stock.Region,
                    Price = stock.Price,
                });
            }

            return result;
        }

        public static string DeriveSkuCode(this TbtStockProduct stock)
        {
            if (!string.IsNullOrWhiteSpace(stock.ProductNumber))
            {
                return $"SKU-{stock.ProductNumber.ToUpper()}";
            }

            var raw = string.Concat(
                stock.ProductNameTh ?? "",
                stock.Mold ?? "",
                stock.Size ?? "",
                stock.ProductionType ?? "",
                stock.ProductionTypeSize ?? "");

            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return $"SKU-{hashHex.Substring(0, 8)}";
        }

        public static TbtSku MapNewSku(this TbtStockProduct stock, string skuCode, string operatorBy)
        {
            return new TbtSku
            {
                SkuCode = skuCode,
                ProductNumber = stock.ProductNumber,
                ProductNameTh = stock.ProductNameTh ?? "",
                ProductNameEn = stock.ProductNameEn ?? "",
                ProductType = stock.ProductType,
                ProductTypeName = stock.ProductTypeName,
                Mold = stock.Mold,
                MoldDesign = stock.MoldDesign,
                StudEarring = stock.StudEarring,
                Size = stock.Size,
                ProductionType = stock.ProductionType,
                ProductionTypeSize = stock.ProductionTypeSize,
                ImageName = stock.ImageName,
                ImagePath = stock.ImagePath,
                DefaultPrice = stock.ProductPrice,
                TagPriceMultiplier = stock.TagPriceMultiplier,
                IsActive = true,
                IsSerialized = true,
                CreateBy = operatorBy,
                CreateDate = DateTime.UtcNow
            };
        }

        public static TbtStockPiece MapNewStockPiece(this TbtStockProduct stock, string skuCode, string locationCode, string operatorBy)
        {
            return new TbtStockPiece
            {
                StockNumber = stock.StockNumber,
                SkuCode = skuCode,
                LocationCode = locationCode,
                Status = "IN_STOCK",
                ReceiptNumber = stock.ReceiptNumber,
                ReceiptType = stock.ReceiptType,
                ReceiptDate = stock.ReceiptDate,
                ProductionDate = stock.ProductionDate,
                Wo = stock.Wo,
                WoNumber = stock.WoNumber,
                WoOrigin = stock.WoOrigin,
                ProductCost = stock.ProductCost,
                ProductCostDetail = stock.ProductCostDetail,
                WeightActual = null,
                SizeActual = null,
                Barcode = null,
                Remark = stock.Remark,
                CreateBy = operatorBy,
                CreateDate = DateTime.UtcNow
            };
        }

        public static TbtStockMovement MapNewReceiptMovement(string skuCode, string stockNumber, string locationCode, string receiptNumber, string operatorBy, string refDocType = "RECEIPT")
        {
            return new TbtStockMovement
            {
                MovementType = "RECEIPT",
                SkuCode = skuCode,
                StockNumber = stockNumber,
                ToLocation = locationCode,
                Qty = 1,
                RefDocType = refDocType,
                RefDocNo = receiptNumber,
                MovementDate = DateTime.UtcNow,
                CreateBy = operatorBy,
                CreateDate = DateTime.UtcNow
            };
        }

        public static async Task<string> ResolveLocationCodeAsync(JewelryContext jewelryContext, string? location, string operatorBy, Dictionary<string, string> cache)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return "MAIN";
            }

            var locationUpper = location.ToUpper();

            if (cache.ContainsKey(locationUpper))
            {
                return cache[locationUpper];
            }

            var exists = await jewelryContext.TbmStockLocation.AnyAsync(x => x.Code == locationUpper);
            if (exists)
            {
                cache[locationUpper] = locationUpper;
                return locationUpper;
            }

            var newLocation = new TbmStockLocation
            {
                Code = locationUpper,
                NameTh = location,
                Type = "WAREHOUSE",
                IsSalesPoint = false,
                IsActive = true,
                CreateBy = operatorBy,
                CreateDate = DateTime.UtcNow
            };

            jewelryContext.TbmStockLocation.Add(newLocation);
            await jewelryContext.SaveChangesAsync();

            cache[locationUpper] = locationUpper;
            return locationUpper;
        }

    }
}
