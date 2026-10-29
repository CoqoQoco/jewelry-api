using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Receipt.Lot
{
    public class ReceiptLotService : IReceiptLotService
    {
        private const int MaxResults = 20;

        private readonly JewelryContext _jewelryContext;

        public ReceiptLotService(JewelryContext jewelryContext)
        {
            _jewelryContext = jewelryContext;
        }

        public async Task<List<jewelry.Model.Receipt.Lot.Search.Response>> Search(jewelry.Model.Receipt.Lot.Search.Request request)
        {
            var keyword = (request?.Keyword ?? string.Empty).Trim().ToUpper();
            var normalized = StockNumberSearch.Normalize(request?.Keyword);
            if (keyword.Length == 0 || normalized.Length == 0)
            {
                return new List<jewelry.Model.Receipt.Lot.Search.Response>();
            }

            var rows = await (from piece in _jewelryContext.TbtStockPiece.AsNoTracking()
                              join sku in _jewelryContext.TbtSku.AsNoTracking()
                              on piece.SkuCode equals sku.SkuCode
                              where (piece.StockNumberOrigin != null && piece.StockNumberOrigin.Trim().ToUpper() == keyword)
                                 || piece.StockNumber.Replace("-", "").ToUpper() == normalized
                              orderby piece.StockNumber
                              select new jewelry.Model.Receipt.Lot.Search.Response
                              {
                                  StockNumber = piece.StockNumber,
                                  ProductCode = piece.ProductCode,
                                  StockNumberOrigin = piece.StockNumberOrigin,

                                  ProductNumber = sku.ProductNumber,
                                  ProductNameTh = sku.ProductNameTh,
                                  ProductNameEn = sku.ProductNameEn,

                                  ProductType = sku.ProductType,
                                  ProductTypeName = sku.ProductTypeName,
                                  ProductionType = sku.ProductionType,
                                  ProductionTypeSize = sku.ProductionTypeSize,
                                  Size = sku.Size,

                                  LocationCode = piece.LocationCode,

                                  Qty = piece.Qty,
                                  QtyReserved = piece.QtyReserved,
                                  Status = piece.Status,

                                  ReceiptDate = piece.ReceiptDate,
                                  ImagePath = sku.ImagePath
                              })
                              .Take(MaxResults)
                              .ToListAsync();

            foreach (var row in rows)
            {
                var available = row.Qty - row.QtyReserved;
                row.QtyAvailable = available > 0 ? available : 0;
            }

            return rows;
        }
    }
}
