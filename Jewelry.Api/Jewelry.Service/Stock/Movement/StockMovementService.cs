using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Movement.Move;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SearchModel = jewelry.Model.Stock.Movement.Search;

namespace Jewelry.Service.Stock.Movement
{
    public class StockMovementService : BaseService, IStockMovementService
    {
        private readonly JewelryContext _jewelryContext;

        public StockMovementService(JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public IQueryable<object> List()
        {
            return Enumerable.Empty<object>().AsQueryable();
        }

        public IQueryable<SearchModel.Response> Search(SearchModel.Request request)
        {
            var query = _jewelryContext.TbtStockMovement
                .Where(x => x.RefDocType == "MoveLocation")
                .AsQueryable();

            if (request.DateFrom.HasValue)
            {
                var from = request.DateFrom.Value.StartOfDayUtc();
                query = query.Where(x => x.MovementDate >= from);
            }

            if (request.DateTo.HasValue)
            {
                var to = request.DateTo.Value.EndOfDayUtc();
                query = query.Where(x => x.MovementDate <= to);
            }

            if (!string.IsNullOrWhiteSpace(request.FromLocation))
            {
                query = query.Where(x => x.FromLocation == request.FromLocation);
            }

            if (!string.IsNullOrWhiteSpace(request.ToLocation))
            {
                query = query.Where(x => x.ToLocation == request.ToLocation);
            }

            if (!string.IsNullOrWhiteSpace(request.StockNumber))
            {
                query = query.Where(x => x.StockNumber != null && x.StockNumber.Contains(request.StockNumber));
            }

            if (!string.IsNullOrWhiteSpace(request.CurrentLocation))
                query = query.Where(x => x.StockNumberNavigation != null && x.StockNumberNavigation.LocationCode == request.CurrentLocation);
            if (!string.IsNullOrWhiteSpace(request.MovedBy))
                query = query.Where(x => x.CreateBy != null && x.CreateBy.Contains(request.MovedBy));
            if (!string.IsNullOrWhiteSpace(request.StockNumberOrigin))
                query = query.Where(x => x.StockNumberNavigation != null && x.StockNumberNavigation.StockNumberOrigin != null && x.StockNumberNavigation.StockNumberOrigin.Contains(request.StockNumberOrigin));

            return query
                .OrderByDescending(x => x.MovementDate)
                .Select(x => new SearchModel.Response
                {
                    MovementDate = x.MovementDate,
                    StockNumber = x.StockNumber,
                    StockNumberOrigin = x.StockNumberNavigation != null ? x.StockNumberNavigation.StockNumberOrigin : null,
                    ProductCode = x.ProductCode,
                    FromLocation = x.FromLocation,
                    FromLocationName = x.FromLocationNavigation != null ? x.FromLocationNavigation.NameTh : null,
                    ToLocation = x.ToLocation,
                    ToLocationName = x.ToLocationNavigation != null ? x.ToLocationNavigation.NameTh : null,
                    CreateBy = x.CreateBy,
                    Remark = x.Remark
                });
        }

        public async Task<Response> MoveLocation(Request req)
        {
            if (req.StockNumbers == null || !req.StockNumbers.Any())
            {
                throw new HandleException("กรุณาระบุ StockNumbers อย่างน้อย 1 รายการ");
            }

            var target = req.TargetLocationCode.ToUpper();

            var targetLocation = _jewelryContext.TbmStockLocation
                .FirstOrDefault(x => x.Code == target);

            if (targetLocation == null)
            {
                throw new HandleException($"ไม่พบ Location {req.TargetLocationCode} ในระบบ");
            }

            var pieces = _jewelryContext.TbtStockPiece
                .Where(p => req.StockNumbers.Contains(p.StockNumber))
                .ToList();

            var now = DateTime.UtcNow;
            var username = CurrentUsername;

            var balancesToUpdate = new List<TbtStockBalance>();
            var balancesToAdd = new List<TbtStockBalance>();
            var movementsToAdd = new List<TbtStockMovement>();
            var piecesToUpdate = new List<TbtStockPiece>();

            int movedCount = 0;

            foreach (var piece in pieces)
            {
                var oldLocation = piece.LocationCode;

                if (oldLocation == target)
                    continue;

                // ล็อตที่มีการจองค้าง (QtyReserved > 0) ย้ายทั้งล็อตไม่ได้ — ย้ายบางส่วน (split ล็อต) เป็นเฟส 2
                if (piece.QtyReserved > 0)
                {
                    throw new HandleException($"ไม่สามารถย้ายคลังสินค้า {piece.StockNumber} ได้ เนื่องจากมีการจองค้างอยู่ {piece.QtyReserved} ชิ้น");
                }

                // ย้ายทั้งล็อตเสมอ — qty ที่ย้าย = จำนวนคงเหลือทั้งหมดของเลขนี้
                var moveQty = piece.Qty;

                // ปรับ balance ต้นทาง
                var srcBalance = _jewelryContext.TbtStockBalance
                    .FirstOrDefault(b => b.SkuCode == piece.SkuCode && b.LocationCode == oldLocation);

                if (srcBalance != null)
                {
                    srcBalance.QtyOnHand -= moveQty;
                    srcBalance.QtyAvailable = srcBalance.QtyOnHand - srcBalance.QtyReserved;
                    srcBalance.LastMovementAt = now;
                    srcBalance.UpdateDate = now;
                    srcBalance.UpdateBy = username;
                    balancesToUpdate.Add(srcBalance);
                }

                // ปรับ/สร้าง balance ปลายทาง
                var dstBalance = _jewelryContext.TbtStockBalance
                    .FirstOrDefault(b => b.SkuCode == piece.SkuCode && b.LocationCode == target);

                if (dstBalance == null)
                {
                    dstBalance = new TbtStockBalance
                    {
                        SkuCode = piece.SkuCode,
                        LocationCode = target,
                        QtyOnHand = moveQty,
                        QtyReserved = 0,
                        QtyAvailable = moveQty,
                        LastMovementAt = now,
                        CreateDate = now,
                        CreateBy = username
                    };
                    balancesToAdd.Add(dstBalance);
                }
                else
                {
                    dstBalance.QtyOnHand += moveQty;
                    dstBalance.QtyAvailable = dstBalance.QtyOnHand - dstBalance.QtyReserved;
                    dstBalance.LastMovementAt = now;
                    dstBalance.UpdateDate = now;
                    dstBalance.UpdateBy = username;
                    balancesToUpdate.Add(dstBalance);
                }

                // สร้าง movement record
                movementsToAdd.Add(new TbtStockMovement
                {
                    MovementDate = now,
                    MovementType = "Transfer",
                    SkuCode = piece.SkuCode,
                    StockNumber = piece.StockNumber,
                    ProductCode = piece.ProductCode,
                    FromLocation = oldLocation,
                    ToLocation = target,
                    Qty = moveQty,
                    RefDocType = "MoveLocation",
                    Remark = req.Remark,
                    CreateDate = now,
                    CreateBy = username
                });

                // อัปเดต piece
                piece.LocationCode = target;
                piece.UpdateDate = now;
                piece.UpdateBy = username;
                piecesToUpdate.Add(piece);

                movedCount++;
            }

            if (balancesToUpdate.Any())
                _jewelryContext.TbtStockBalance.UpdateRange(balancesToUpdate);

            if (balancesToAdd.Any())
                _jewelryContext.TbtStockBalance.AddRange(balancesToAdd);

            if (movementsToAdd.Any())
                _jewelryContext.TbtStockMovement.AddRange(movementsToAdd);

            if (piecesToUpdate.Any())
                _jewelryContext.TbtStockPiece.UpdateRange(piecesToUpdate);

            await _jewelryContext.SaveChangesAsync();

            return new Response { Message = "success", MovedCount = movedCount };
        }
    }
}
