using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Movement.Move;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.Stock;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MoveUnitsModel = jewelry.Model.Stock.Movement.MoveUnits;
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
                var normalized = StockNumberSearch.Normalize(request.StockNumber);
                if (!string.IsNullOrEmpty(normalized))
                {
                    query = query.Where(x => x.StockNumber != null && x.StockNumber.Replace("-", "").Contains(normalized));
                }
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

        // ย้ายทั้งล็อต (API เดิม) — ใช้ตัวเดียวกับ MoveUnits · MovedCount = จำนวนล็อตที่ย้ายจริง (ล็อตที่อยู่คลังปลายทางครบแล้วไม่นับ)
        public async Task<Response> MoveLocation(Request req)
        {
            if (req.StockNumbers == null || !req.StockNumbers.Any())
            {
                throw new HandleException("กรุณาระบุ StockNumbers อย่างน้อย 1 รายการ");
            }

            // เลขที่สแกน/พิมพ์อาจมีขีดหรือไม่มีขีด — แปลงเป็นเลขจริงใน DB ก่อน lock/lookup
            var resolvedNumbers = await _jewelryContext.ResolveAsync(req.StockNumbers);
            var realStockNumbers = resolvedNumbers.Values.Distinct().ToList();

            var response = await MoveUnits(new MoveUnitsModel.Request
            {
                TargetLocationCode = req.TargetLocationCode,
                Remark = req.Remark,
                Items = realStockNumbers.Select(n => new MoveUnitsModel.MoveUnitsItem { StockNumber = n }).ToList()
            });

            return new Response { Message = "success", MovedCount = response.Lots.Count(l => l.Moved > 0) };
        }

        // ย้ายคลังรายชิ้น: เลือกชิ้นด้วย UnitIds / (FromLocationCode + Qty) / ทั้งล็อต / EPC ของแท็ก
        // ชิ้นที่จองไว้ย้ายได้ (การจองย้ายไปด้วย) · ชิ้น SOLD ย้ายไม่ได้ · ชิ้นที่อยู่คลังปลายทางแล้วข้าม
        public async Task<MoveUnitsModel.Response> MoveUnits(MoveUnitsModel.Request req)
        {
            if (string.IsNullOrWhiteSpace(req.TargetLocationCode))
            {
                throw new HandleException("กรุณาระบุคลังปลายทาง");
            }

            var items = (req.Items ?? new List<MoveUnitsModel.MoveUnitsItem>())
                .Where(i => !string.IsNullOrWhiteSpace(i.StockNumber))
                .ToList();
            var epcs = (req.Epcs ?? new List<string>())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct()
                .ToList();

            if (items.Count == 0 && epcs.Count == 0)
            {
                throw new HandleException("กรุณาระบุสินค้าหรือแท็กที่ต้องการย้ายอย่างน้อย 1 รายการ");
            }

            foreach (var item in items)
            {
                var hasUnits = item.UnitIds != null && item.UnitIds.Count > 0;
                var hasFrom = !string.IsNullOrWhiteSpace(item.FromLocationCode);
                if (!hasUnits && hasFrom != item.Qty.HasValue)
                {
                    throw new HandleException($"สินค้า {item.StockNumber}: ต้องระบุ fromLocationCode และ qty ควบคู่กัน (หรือไม่ระบุทั้งคู่เพื่อย้ายทั้งล็อต)");
                }
            }

            var target = req.TargetLocationCode.Trim().ToUpper();

            var targetLocation = _jewelryContext.TbmStockLocation
                .FirstOrDefault(x => x.Code == target);

            if (targetLocation == null)
            {
                throw new HandleException($"ไม่พบ Location {req.TargetLocationCode} ในระบบ");
            }

            // เลขที่สแกน/พิมพ์อาจมีขีดหรือไม่มีขีด — แปลงเป็นเลขจริงใน DB ก่อน lock/lookup
            if (items.Count > 0)
            {
                var resolvedNumbers = await _jewelryContext.ResolveAsync(items.Select(i => i.StockNumber));
                foreach (var item in items)
                {
                    if (resolvedNumbers.TryGetValue(item.StockNumber, out var actual))
                    {
                        item.StockNumber = actual;
                    }
                }
            }

            // แท็ก -> ชิ้น: หาก่อนล็อต แล้วตรวจซ้ำอีกครั้งหลังล็อต (แท็กอาจถูกยกเลิกผูก/ชิ้นถูกขายระหว่างรอ)
            var epcUnits = await ResolveEpcUnitsAsync(epcs);
            foreach (var group in epcUnits.GroupBy(x => x.StockNumber))
            {
                items.Add(new MoveUnitsModel.MoveUnitsItem
                {
                    StockNumber = group.Key,
                    UnitIds = group.Select(x => x.UnitId).ToList()
                });
            }

            var lotNumbers = items.Select(i => i.StockNumber).Distinct().ToList();

            // เคารพ transaction เดิมถ้าถูกเรียกจากภายในอีก transaction ที่เปิดอยู่แล้ว เหมือน InvoiceService.Create
            var ownsTransaction = _jewelryContext.Database.CurrentTransaction == null;
            var transaction = ownsTransaction
                ? await _jewelryContext.Database.BeginTransactionAsync()
                : null;

            try
            {

            // ล็อก piece ก่อน แล้วค่อยล็อก balance ของ SKU ที่เกี่ยวข้อง ตามลำดับ global: piece -> balance
            await _jewelryContext.LockStockPiecesAsync(lotNumbers);

            var pieces = _jewelryContext.TbtStockPiece
                .Where(p => lotNumbers.Contains(p.StockNumber))
                .ToList();

            await _jewelryContext.LockStockBalancesAsync(pieces.Select(p => p.SkuCode));

            await VerifyEpcUnitsAsync(epcUnits);

            var now = DateTime.UtcNow;
            var username = CurrentUsername;
            var info = new StockUnitSyncInfo { By = username, Now = now };

            var response = new MoveUnitsModel.Response { Message = "success" };
            var lotRows = new Dictionary<string, MoveUnitsModel.MovedLot>();

            foreach (var item in items)
            {
                var matched = pieces.Where(p => p.StockNumber == item.StockNumber).ToList();
                if (matched.Count == 0)
                {
                    throw new HandleException($"ไม่พบสินค้า {item.StockNumber} ในคลัง");
                }
                if (matched.Count > 1)
                {
                    throw new HandleException($"พบสินค้าเลข {item.StockNumber} มากกว่า 1 รายการ ไม่สามารถย้ายคลังได้");
                }

                var piece = matched[0];
                var wholeLot = (item.UnitIds == null || item.UnitIds.Count == 0) && string.IsNullOrWhiteSpace(item.FromLocationCode);

                // ล็อตที่ขายหมดแล้วและอยู่คลังปลายทางอยู่แล้วข้ามได้ (เหมือนพฤติกรรมเดิม) · คลังอื่นให้ ops แจ้งว่าย้ายไม่ได้
                if (wholeLot && (piece.Status == "SOLD" || piece.Qty <= 0) && piece.LocationCode == target)
                {
                    GetLotRow(lotRows, response, piece);
                    continue;
                }

                var result = await StockUnitOps.MoveAsync(
                    _jewelryContext, piece, item.UnitIds, item.FromLocationCode, item.Qty, target, info);

                var row = GetLotRow(lotRows, response, piece);

                if (result.Count > 0)
                {
                    await StockBalanceApplier.ApplyAsync(_jewelryContext, result, new StockMovementTemplate
                    {
                        MovementType = "Transfer",
                        RefDocType = "MoveLocation",
                        Remark = req.Remark,
                        Date = now,
                        By = username
                    });

                    row.Moved += result.Count;
                    response.MovedCount += result.Count;
                }

                row.PrimaryLocation = piece.LocationCode;
            }

            await _jewelryContext.SaveChangesAsync();

            if (ownsTransaction)
            {
                await transaction!.CommitAsync();
            }

            return response;
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

        private static MoveUnitsModel.MovedLot GetLotRow(Dictionary<string, MoveUnitsModel.MovedLot> rows, MoveUnitsModel.Response response, TbtStockPiece piece)
        {
            if (!rows.TryGetValue(piece.StockNumber, out var row))
            {
                row = new MoveUnitsModel.MovedLot { StockNumber = piece.StockNumber, PrimaryLocation = piece.LocationCode };
                rows[piece.StockNumber] = row;
                response.Lots.Add(row);
            }

            return row;
        }

        private async Task<List<EpcUnit>> ResolveEpcUnitsAsync(List<string> epcs)
        {
            if (epcs.Count == 0)
            {
                return new List<EpcUnit>();
            }

            var tags = await _jewelryContext.TbtStockPieceRfid
                .AsNoTracking()
                .Where(r => r.IsActive && epcs.Contains(r.Epc))
                .Select(r => new { r.Epc, r.StockNumber, r.ProductCode, r.UnitId })
                .ToListAsync();

            var missing = epcs.Where(e => !tags.Any(t => t.Epc == e)).ToList();
            if (missing.Count > 0)
            {
                throw new HandleException($"ไม่พบแท็กที่ผูกกับสินค้า: {string.Join(", ", missing.Take(5))}{(missing.Count > 5 ? $" และอีก {missing.Count - 5} แท็ก" : string.Empty)}");
            }

            var noUnit = tags.Where(t => !t.UnitId.HasValue).Select(t => t.Epc).ToList();
            if (noUnit.Count > 0)
            {
                throw new HandleException($"แท็กยังไม่ได้ผูกกับชิ้นใดชิ้นหนึ่ง: {string.Join(", ", noUnit.Take(5))}");
            }

            return tags
                .Select(t => new EpcUnit { Epc = t.Epc, StockNumber = t.StockNumber, UnitId = t.UnitId!.Value })
                .ToList();
        }

        // หลังล็อต: แท็กต้องยังผูกกับชิ้นเดิมอยู่ (ไม่ถูกยกเลิกผูก/ปลดเพราะขายระหว่างรอ)
        private async Task VerifyEpcUnitsAsync(List<EpcUnit> epcUnits)
        {
            if (epcUnits.Count == 0)
            {
                return;
            }

            var epcs = epcUnits.Select(e => e.Epc).ToList();
            var current = await _jewelryContext.TbtStockPieceRfid
                .AsNoTracking()
                .Where(r => r.IsActive && epcs.Contains(r.Epc))
                .Select(r => new { r.Epc, r.UnitId })
                .ToListAsync();

            foreach (var epcUnit in epcUnits)
            {
                var tag = current.FirstOrDefault(c => c.Epc == epcUnit.Epc);
                if (tag == null || tag.UnitId != epcUnit.UnitId)
                {
                    throw new HandleException($"แท็ก {epcUnit.Epc} เปลี่ยนสถานะระหว่างทำรายการ กรุณาสแกนใหม่อีกครั้ง");
                }
            }
        }

        private sealed class EpcUnit
        {
            public string Epc { get; set; } = null!;
            public string StockNumber { get; set; } = null!;
            public long UnitId { get; set; }
        }
    }
}
