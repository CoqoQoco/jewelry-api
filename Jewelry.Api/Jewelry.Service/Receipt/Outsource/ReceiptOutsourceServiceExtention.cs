using jewelry.Model.Constant;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Receipt.Production;
using System;
using System.Collections.Generic;

namespace Jewelry.Service.Receipt.Outsource
{
    public static class ReceiptOutsourceServiceExtention
    {
        public static StockProductDto MapNewStockOutsource(
            this jewelry.Model.Receipt.Outsource.Confirm.OutsourceStock stock,
            string vendor,
            string? poNumber,
            string productTypeName,
            string stockRunning,
            string operatorBy)
        {
            return new StockProductDto
            {
                StockNumber = stockRunning,
                Status = StockProductStatus.Available,

                ReceiptNumber = stockRunning,
                ReceiptDate = DateTime.UtcNow,
                ReceiptType = "outsource",

                Mold = null,
                MoldDesign = stock.MoldDesign,

                Qty = stock.Qty,
                ProductPrice = stock.Price,

                ProductNumber = stock.ProductNumber.ToUpper(),
                ProductNameTh = stock.ProductNameTH.ToUpper(),
                ProductNameEn = stock.ProductNameEN.ToUpper(),

                ProductType = stock.ProductType,
                ProductTypeName = productTypeName,

                ImageName = stock.ImageName,
                ImagePath = stock.ImagePath,

                Wo = null,
                WoNumber = null,

                ProductionDate = DateTime.UtcNow,
                ProductionType = stock.ProductionType,
                ProductionTypeSize = stock.ProductionTypeSize,

                Size = stock.Size,
                StudEarring = stock.StudEarring,
                Location = stock.Location,
                Remark = stock.Remark,

                Vendor = vendor,
                PoNumber = poNumber,

                CreateBy = operatorBy,
                CreateDate = DateTime.UtcNow
            };
        }

        public static jewelry.Model.Receipt.Outsource.Confirm.Stock MapResponseNewStockOutsource(this StockProductDto stock)
        {
            return new jewelry.Model.Receipt.Outsource.Confirm.Stock
            {
                StockNumber = stock.StockNumber,
                Status = stock.Status,

                ReceiptNumber = stock.ReceiptNumber,
                ReceiptDate = stock.ReceiptDate,
                ReceiptType = stock.ReceiptType,

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

                Vendor = stock.Vendor,
                PoNumber = stock.PoNumber,

                ProductionDate = stock.CreateDate,
                ProductionType = stock.ProductionType,
                ProductionTypeSize = stock.ProductionTypeSize,

                Size = stock.Size,
                StudEarring = stock.StudEarring,
                Location = stock.Location,
                Remark = stock.Remark,

                CreateBy = stock.CreateBy,
                CreateDate = stock.CreateDate,

                Materials = new List<jewelry.Model.Receipt.Outsource.Confirm.Material>()
            };
        }

        public static List<TbtStockPieceMaterial> MapNewOutsourcePieceMaterial(
            this jewelry.Model.Receipt.Outsource.Confirm.OutsourceStock stock,
            string stockRunning,
            string productCode,
            string operatorBy)
        {
            var response = new List<TbtStockPieceMaterial>();

            foreach (var item in stock.Materials)
            {
                response.Add(new TbtStockPieceMaterial
                {
                    StockNumber = stockRunning,
                    ProductCode = productCode,

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
    }
}
