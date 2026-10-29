using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Rfid;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Jewelry.Service.Stock.Rfid;

public class StockRfidService : BaseService, IStockRfidService
{
    private readonly JewelryContext _jewelryContext;

    public StockRfidService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
    }

    public static string? NormalizeTag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        return normalized.Length == 0 ? null : normalized;
    }

    public async Task<jewelry.Model.Stock.Rfid.Bind.Response> Bind(jewelry.Model.Stock.Rfid.Bind.Request request)
    {
        var epc = NormalizeTag(request.Epc);
        if (epc == null)
        {
            throw new HandleException("กรุณาระบุ EPC ของแท็ก");
        }

        var tid = NormalizeTag(request.Tid);
        var stockNumber = request.StockNumber?.Trim();
        var productCode = request.ProductCode?.Trim();

        if (string.IsNullOrEmpty(stockNumber) || string.IsNullOrEmpty(productCode))
        {
            throw new HandleException("กรุณาระบุเลขสต็อกและรหัสสินค้า");
        }

        stockNumber = await _jewelryContext.ResolveAsync(stockNumber) ?? stockNumber;

        var transaction = await BeginLotLockAsync(new[] { stockNumber });
        try
        {
            var piece = await _jewelryContext.TbtStockPiece
                .FirstOrDefaultAsync(p => p.StockNumber == stockNumber && p.ProductCode == productCode);

            if (piece == null)
            {
                throw new HandleException("ไม่พบสินค้า");
            }

            var epcBound = await _jewelryContext.TbtStockPieceRfid
                .AsNoTracking()
                .Where(r => r.IsActive && r.Epc == epc)
                .Select(r => new { r.StockNumber, r.ProductCode })
                .FirstOrDefaultAsync();

            if (epcBound != null)
            {
                throw new HandleException($"แท็กนี้ถูกผูกกับสินค้า {epcBound.StockNumber} แล้ว");
            }

            var units = await EnsureUnitsAsync(piece);

            var lotTags = await _jewelryContext.TbtStockPieceRfid
                .AsNoTracking()
                .Where(r => r.IsActive && r.StockNumber == stockNumber && r.ProductCode == productCode)
                .Select(r => new { r.Epc, r.UnitId })
                .ToListAsync();

            var taggedUnitIds = lotTags.Where(t => t.UnitId.HasValue).Select(t => t.UnitId!.Value).ToHashSet();

            TbtStockUnit? unit;
            if (request.UnitId.HasValue)
            {
                unit = units.FirstOrDefault(u => u.Id == request.UnitId.Value);
                if (unit == null)
                {
                    throw new HandleException("ไม่พบชิ้นที่ระบุในสินค้านี้");
                }

                if (unit.Status == StockUnitSync.Sold)
                {
                    throw new HandleException("ชิ้นนี้ขายแล้ว ไม่สามารถผูกแท็กได้");
                }

                var unitTag = lotTags.FirstOrDefault(t => t.UnitId == unit.Id);
                if (unitTag != null)
                {
                    throw new HandleException($"ชิ้นนี้มีแท็กผูกอยู่แล้ว (EPC {unitTag.Epc}) กรุณายกเลิกการผูกก่อน");
                }
            }
            else
            {
                unit = units
                    .Where(u => u.Status != StockUnitSync.Sold && !taggedUnitIds.Contains(u.Id))
                    .OrderBy(u => u.UnitNo)
                    .FirstOrDefault();

                if (unit == null)
                {
                    if (lotTags.Count == 0)
                    {
                        throw new HandleException("สินค้านี้ไม่มีชิ้นคงเหลือที่ผูกแท็กได้");
                    }

                    var epcs = string.Join(", ", lotTags.Select(t => t.Epc).Take(3));
                    throw new HandleException($"สินค้านี้มีแท็กผูกครบทุกชิ้นแล้ว (EPC {epcs}) กรุณายกเลิกการผูกก่อน");
                }
            }

            var entity = new TbtStockPieceRfid
            {
                Epc = epc,
                Tid = tid,
                StockNumber = stockNumber,
                ProductCode = productCode,
                UnitId = unit.Id,
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername ?? "system"
            };

            _jewelryContext.TbtStockPieceRfid.Add(entity);

            try
            {
                await _jewelryContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new HandleException("ไม่สามารถผูกแท็กได้ แท็กหรือสินค้านี้อาจถูกผูกไปพร้อมกัน กรุณาลองใหม่");
            }

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            var locationNames = await LoadLocationNamesAsync(new[] { unit.LocationCode });

            return new jewelry.Model.Stock.Rfid.Bind.Response
            {
                Id = entity.Id,
                Epc = entity.Epc,
                Tid = entity.Tid,
                StockNumber = entity.StockNumber,
                ProductCode = entity.ProductCode,
                UnitId = unit.Id,
                UnitNo = unit.UnitNo,
                UnitStatus = unit.Status,
                UnitLocationCode = unit.LocationCode,
                UnitLocationName = locationNames.GetValueOrDefault(unit.LocationCode),
                LotQty = piece.Qty
            };
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<jewelry.Model.Stock.Rfid.Unbind.Response> Unbind(jewelry.Model.Stock.Rfid.Unbind.Request request)
    {
        var epc = NormalizeTag(request.Epc);
        var stockNumber = request.StockNumber?.Trim();
        var productCode = request.ProductCode?.Trim();

        List<TbtStockPieceRfid> targets;

        if (epc != null)
        {
            targets = await _jewelryContext.TbtStockPieceRfid
                .Where(r => r.IsActive && r.Epc == epc)
                .ToListAsync();
        }
        else if (request.UnitId.HasValue)
        {
            targets = await _jewelryContext.TbtStockPieceRfid
                .Where(r => r.IsActive && r.UnitId == request.UnitId.Value)
                .ToListAsync();
        }
        else if (!string.IsNullOrEmpty(stockNumber) && !string.IsNullOrEmpty(productCode))
        {
            stockNumber = await _jewelryContext.ResolveAsync(stockNumber) ?? stockNumber;

            targets = await _jewelryContext.TbtStockPieceRfid
                .Where(r => r.IsActive && r.StockNumber == stockNumber && r.ProductCode == productCode)
                .ToListAsync();

            if (targets.Count > 1)
            {
                throw new HandleException("สินค้านี้มีแท็กผูกหลายชิ้น กรุณาระบุ EPC หรือชิ้นที่ต้องการยกเลิกการผูก");
            }
        }
        else
        {
            throw new HandleException("กรุณาระบุ EPC หรือเลขสต็อกพร้อมรหัสสินค้า");
        }

        if (targets.Count == 0)
        {
            return new jewelry.Model.Stock.Rfid.Unbind.Response { Unbound = false };
        }

        var now = DateTime.UtcNow;
        var username = CurrentUsername ?? "system";

        foreach (var target in targets)
        {
            target.IsActive = false;
            target.UnbindReason = StockUnitSync.UnbindManual;
            target.UpdateDate = now;
            target.UpdateBy = username;
        }

        _jewelryContext.TbtStockPieceRfid.UpdateRange(targets);
        await _jewelryContext.SaveChangesAsync();

        return new jewelry.Model.Stock.Rfid.Unbind.Response { Unbound = true };
    }

    public async Task<jewelry.Model.Stock.Rfid.GetByEpc.Response> GetByEpc(jewelry.Model.Stock.Rfid.GetByEpc.Request request)
    {
        var epc = NormalizeTag(request.Epc);
        if (epc == null)
        {
            throw new HandleException("กรุณาระบุ EPC ของแท็ก");
        }

        var rows = await ResolveEpcsAsync(new List<string> { epc });
        var info = rows.FirstOrDefault();

        if (info == null)
        {
            return new jewelry.Model.Stock.Rfid.GetByEpc.Response { Found = false };
        }

        return new jewelry.Model.Stock.Rfid.GetByEpc.Response
        {
            Found = true,
            Epc = info.Epc,
            Tid = info.Tid,
            StockNumber = info.StockNumber,
            ProductCode = info.ProductCode,
            ProductName = info.ProductName,
            ProductNumber = info.ProductNumber,
            Status = info.Status,
            LocationCode = info.LocationCode,
            ImagePath = info.ImagePath,
            UnitId = info.UnitId,
            UnitNo = info.UnitNo,
            UnitStatus = info.UnitStatus,
            UnitLocationCode = info.UnitLocationCode,
            UnitLocationName = info.UnitLocationName,
            LotQty = info.LotQty,
            SoldMismatch = info.SoldMismatch
        };
    }

    public async Task<jewelry.Model.Stock.Rfid.GetByEpcs.Response> GetByEpcs(jewelry.Model.Stock.Rfid.GetByEpcs.Request request)
    {
        var epcs = (request.Epcs ?? new List<string>())
            .Select(NormalizeTag)
            .Where(e => e != null)
            .Select(e => e!)
            .Distinct()
            .ToList();

        var response = new jewelry.Model.Stock.Rfid.GetByEpcs.Response();

        if (epcs.Count == 0)
        {
            return response;
        }

        var rows = await ResolveEpcsAsync(epcs);
        response.Items = rows;

        var foundSet = rows.Select(r => r.Epc!).ToHashSet();
        response.NotFound = epcs.Where(e => !foundSet.Contains(e)).ToList();

        return response;
    }

    public async Task<jewelry.Model.Stock.Rfid.GetByStock.Response> GetByStock(jewelry.Model.Stock.Rfid.GetByStock.Request request)
    {
        var stockNumber = request.StockNumber?.Trim();
        var productCode = request.ProductCode?.Trim();

        if (string.IsNullOrEmpty(stockNumber) || string.IsNullOrEmpty(productCode))
        {
            throw new HandleException("กรุณาระบุเลขสต็อกและรหัสสินค้า");
        }

        stockNumber = await _jewelryContext.ResolveAsync(stockNumber) ?? stockNumber;

        var lotQty = await _jewelryContext.TbtStockPiece
            .AsNoTracking()
            .Where(p => p.StockNumber == stockNumber && p.ProductCode == productCode)
            .Select(p => (decimal?)p.Qty)
            .FirstOrDefaultAsync() ?? 0;

        var activeTags = _jewelryContext.TbtStockPieceRfid.AsNoTracking().Where(r => r.IsActive);

        var rows = await (
            from u in _jewelryContext.TbtStockUnit.AsNoTracking()
            join r in activeTags on (long?)u.Id equals r.UnitId into rj
            from r in rj.DefaultIfEmpty()
            where u.StockNumber == stockNumber && u.ProductCode == productCode
            orderby u.UnitNo
            select new
            {
                u.Id,
                u.UnitNo,
                u.Status,
                u.LocationCode,
                u.ReceiptNumber,
                u.ReceiptDate,
                u.SoldRef,
                u.SoldDate,
                u.ReservedRefType,
                u.ReservedRef,
                u.ReservedLineId,
                Epc = r != null ? r.Epc : null,
                Tid = r != null ? r.Tid : null,
                BoundDate = r != null ? (DateTime?)r.CreateDate : null,
                BoundBy = r != null ? r.CreateBy : null
            }
        ).ToListAsync();

        var locationNames = await LoadLocationNamesAsync(rows.Select(x => x.LocationCode));

        var units = rows.Select(x => new jewelry.Model.Stock.Rfid.GetByStock.Unit
        {
            ReservedRefType = x.ReservedRefType,
            ReservedRef = x.ReservedRef,
            ReservedLineId = x.ReservedLineId,
            UnitId = x.Id,
            UnitNo = x.UnitNo,
            Status = x.Status,
            LocationCode = x.LocationCode,
            LocationName = locationNames.GetValueOrDefault(x.LocationCode),
            ReceiptNumber = x.ReceiptNumber,
            ReceiptDate = x.ReceiptDate,
            SoldRef = x.SoldRef,
            SoldDate = x.SoldDate,
            Epc = x.Epc,
            Tid = x.Tid,
            BoundDate = x.BoundDate,
            BoundBy = x.BoundBy
        }).ToList();

        var firstTagged = units.FirstOrDefault(u => u.Epc != null);

        if (firstTagged == null)
        {
            return new jewelry.Model.Stock.Rfid.GetByStock.Response { Found = false, LotQty = lotQty, Units = units };
        }

        return new jewelry.Model.Stock.Rfid.GetByStock.Response
        {
            Found = true,
            Epc = firstTagged.Epc,
            Tid = firstTagged.Tid,
            BoundDate = firstTagged.BoundDate,
            LotQty = lotQty,
            Units = units
        };
    }

    public async Task<jewelry.Model.Stock.Rfid.ListReceipts.Response> ListReceipts(jewelry.Model.Stock.Rfid.ListReceipts.Request request)
    {
        var (skip, take) = NormalizePaging(request.Skip, request.Take, 20, 100);
        var search = request.Search?.Trim();

        var units = _jewelryContext.TbtStockUnit.AsNoTracking()
            .Where(u => u.ReceiptNumber != null && u.ReceiptNumber != "");

        if (!string.IsNullOrEmpty(search))
        {
            var lowered = search.ToLowerInvariant();
            units = units.Where(u => u.ReceiptNumber!.ToLower().Contains(lowered));
        }
        else
        {
            var since = DateTime.UtcNow.AddDays(-365);
            units = units.Where(u => u.ReceiptDate >= since);
        }

        var activeTags = _jewelryContext.TbtStockPieceRfid.AsNoTracking().Where(r => r.IsActive);

        var flagged =
            from u in units
            join r in activeTags on (long?)u.Id equals r.UnitId into rj
            from r in rj.DefaultIfEmpty()
            select new
            {
                u.ReceiptNumber,
                u.ReceiptDate,
                Bound = r != null ? 1 : 0
            };

        var grouped = flagged
            .GroupBy(x => x.ReceiptNumber)
            .Select(g => new
            {
                ReceiptNumber = g.Key,
                ReceiptDate = g.Max(x => x.ReceiptDate),
                Total = g.Count(),
                Bound = g.Sum(x => x.Bound)
            });

        if (request.OnlyIncomplete)
        {
            grouped = grouped.Where(g => g.Bound < g.Total);
        }

        var total = await grouped.CountAsync();

        var rows = await grouped
            .OrderByDescending(g => g.ReceiptDate)
            .ThenByDescending(g => g.ReceiptNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return new jewelry.Model.Stock.Rfid.ListReceipts.Response
        {
            Total = total,
            Items = rows.Select(g => new jewelry.Model.Stock.Rfid.ListReceipts.Item
            {
                ReceiptNumber = g.ReceiptNumber!,
                ReceiptDate = g.ReceiptDate,
                TotalPieces = g.Total,
                BoundPieces = g.Bound,
                RemainingPieces = g.Total - g.Bound
            }).ToList()
        };
    }

    public async Task<jewelry.Model.Stock.Rfid.ListByReceipt.Response> ListByReceipt(jewelry.Model.Stock.Rfid.ListByReceipt.Request request)
    {
        var receiptNumber = request.ReceiptNumber?.Trim();
        var response = new jewelry.Model.Stock.Rfid.ListByReceipt.Response { ReceiptNumber = receiptNumber };

        if (string.IsNullOrEmpty(receiptNumber))
        {
            return response;
        }

        var activeTags = _jewelryContext.TbtStockPieceRfid.AsNoTracking().Where(r => r.IsActive);

        var rows = await (
            from u in _jewelryContext.TbtStockUnit.AsNoTracking()
            join p in _jewelryContext.TbtStockPiece.AsNoTracking()
                on new { u.StockNumber, u.ProductCode } equals new { p.StockNumber, p.ProductCode }
            join s in _jewelryContext.TbtSku.AsNoTracking() on p.SkuCode equals s.SkuCode
            join r in activeTags on (long?)u.Id equals r.UnitId into rj
            from r in rj.DefaultIfEmpty()
            where u.ReceiptNumber == receiptNumber
            orderby u.StockNumber, u.ProductCode, u.UnitNo
            select new
            {
                u.Id,
                u.UnitNo,
                u.StockNumber,
                u.ProductCode,
                s.ProductNameTh,
                s.ProductNumber,
                u.Status,
                u.LocationCode,
                s.ImagePath,
                s.ImageName,
                Epc = r != null ? r.Epc : null,
                Tid = r != null ? r.Tid : null,
                BoundDate = r != null ? (DateTime?)r.CreateDate : null,
                BoundBy = r != null ? r.CreateBy : null
            }
        ).ToListAsync();

        response.Items = rows.Select(x => new jewelry.Model.Stock.Rfid.ListByReceipt.Item
        {
            UnitId = x.Id,
            UnitNo = x.UnitNo,
            UnitStatus = x.Status,
            StockNumber = x.StockNumber,
            ProductCode = x.ProductCode,
            ProductName = x.ProductNameTh,
            ProductNumber = x.ProductNumber,
            Status = x.Status,
            LocationCode = x.LocationCode,
            ImagePath = StockImagePath.Build(x.ImagePath, x.ImageName),
            Epc = x.Epc,
            Tid = x.Tid,
            BoundDate = x.BoundDate,
            BoundBy = x.BoundBy
        }).ToList();

        response.Total = response.Items.Count;
        response.Bound = response.Items.Count(i => i.Epc != null);

        return response;
    }

    public async Task<jewelry.Model.Stock.Rfid.ListUnbound.Response> ListUnbound(jewelry.Model.Stock.Rfid.ListUnbound.Request request)
    {
        var (skip, take) = NormalizePaging(request.Skip, request.Take, 20, 100);
        var search = request.Search?.Trim();
        var locationCode = request.LocationCode?.Trim();

        var query =
            from u in _jewelryContext.TbtStockUnit.AsNoTracking()
            join p in _jewelryContext.TbtStockPiece.AsNoTracking()
                on new { u.StockNumber, u.ProductCode } equals new { p.StockNumber, p.ProductCode }
            join s in _jewelryContext.TbtSku.AsNoTracking() on p.SkuCode equals s.SkuCode
            where (u.Status == StockUnitSync.InStock || u.Status == StockUnitSync.Reserved)
                && !_jewelryContext.TbtStockPieceRfid.Any(r => r.IsActive && r.UnitId == u.Id)
            select new { u, p, s };

        if (!string.IsNullOrEmpty(locationCode))
        {
            query = query.Where(x => x.u.LocationCode == locationCode);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var normalized = StockNumberSearch.Normalize(search);
            var lowered = search.ToLowerInvariant();
            query = query.Where(x =>
                (normalized != "" && x.u.StockNumber.Replace("-", "").ToUpper().Contains(normalized))
                || x.s.ProductNameTh.ToLower().Contains(lowered)
                || (x.s.ProductNumber != null && x.s.ProductNumber.ToLower().Contains(lowered)));
        }

        var total = await query.CountAsync();

        var rows = await query
            .OrderBy(x => x.u.StockNumber)
            .ThenBy(x => x.u.ProductCode)
            .ThenBy(x => x.u.UnitNo)
            .Skip(skip)
            .Take(take)
            .Select(x => new
            {
                UnitId = x.u.Id,
                x.u.UnitNo,
                x.u.StockNumber,
                x.u.ProductCode,
                x.s.ProductNameTh,
                x.s.ProductNumber,
                x.u.Status,
                x.u.LocationCode,
                x.s.ImagePath,
                x.s.ImageName,
                ReceiptNumber = x.u.ReceiptNumber ?? x.p.ReceiptNumber
            })
            .ToListAsync();

        return new jewelry.Model.Stock.Rfid.ListUnbound.Response
        {
            Total = total,
            Items = rows.Select(x => new jewelry.Model.Stock.Rfid.ListUnbound.Item
            {
                UnitId = x.UnitId,
                UnitNo = x.UnitNo,
                StockNumber = x.StockNumber,
                ProductCode = x.ProductCode,
                ProductName = x.ProductNameTh,
                ProductNumber = x.ProductNumber,
                Status = x.Status,
                LocationCode = x.LocationCode,
                ImagePath = StockImagePath.Build(x.ImagePath, x.ImageName),
                ReceiptNumber = x.ReceiptNumber
            }).ToList()
        };
    }

    public async Task<jewelry.Model.Stock.Rfid.ListRecent.Response> ListRecent(jewelry.Model.Stock.Rfid.ListRecent.Request request)
    {
        var take = request.Take <= 0 ? 50 : Math.Min(request.Take, 200);

        var binds = _jewelryContext.TbtStockPieceRfid.AsNoTracking().AsQueryable();
        binds = request.SinceId.HasValue
            ? binds.Where(r => r.Id > request.SinceId.Value).OrderBy(r => r.Id).Take(take)
            : binds.OrderByDescending(r => r.Id).Take(take);

        var rows = await (
            from r in binds
            join p in _jewelryContext.TbtStockPiece.AsNoTracking()
                on new { r.StockNumber, r.ProductCode } equals new { p.StockNumber, p.ProductCode } into pj
            from p in pj.DefaultIfEmpty()
            join s in _jewelryContext.TbtSku.AsNoTracking() on p.SkuCode equals s.SkuCode into sj
            from s in sj.DefaultIfEmpty()
            join un in _jewelryContext.TbtStockUnit.AsNoTracking() on r.UnitId equals (long?)un.Id into uj
            from un in uj.DefaultIfEmpty()
            select new
            {
                r.Id,
                r.Epc,
                r.StockNumber,
                r.ProductCode,
                ProductName = s != null ? s.ProductNameTh : null,
                ReceiptNumber = un != null ? (un.ReceiptNumber ?? (p != null ? p.ReceiptNumber : null)) : (p != null ? p.ReceiptNumber : null),
                r.IsActive,
                r.UnitId,
                UnitNo = un != null ? (int?)un.UnitNo : null,
                r.UnbindReason,
                r.CreateBy,
                r.CreateDate,
                r.UpdateDate
            }
        ).ToListAsync();

        var ordered = request.SinceId.HasValue
            ? rows.OrderBy(x => x.Id)
            : rows.OrderByDescending(x => x.Id);

        var items = ordered.Select(x => new jewelry.Model.Stock.Rfid.ListRecent.Item
        {
            Id = x.Id,
            Epc = x.Epc,
            StockNumber = x.StockNumber,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            ReceiptNumber = x.ReceiptNumber,
            IsActive = x.IsActive,
            UnitId = x.UnitId,
            UnitNo = x.UnitNo,
            UnbindReason = x.UnbindReason,
            BoundBy = x.CreateBy,
            BoundDate = x.CreateDate,
            UpdatedDate = x.UpdateDate
        }).ToList();

        return new jewelry.Model.Stock.Rfid.ListRecent.Response
        {
            LastId = items.Count > 0 ? items.Max(i => i.Id) : (request.SinceId ?? 0),
            Items = items
        };
    }

    private static (int skip, int take) NormalizePaging(int skip, int take, int defaultTake, int maxTake)
    {
        var safeSkip = Math.Max(skip, 0);
        var safeTake = take <= 0 ? defaultTake : Math.Min(take, maxTake);
        return (safeSkip, safeTake);
    }

    // ล็อก piece ของล็อตก่อนแตะ unit (ลำดับ lock เดียวกับ path ขาย: piece ก่อน) — provider ที่ไม่รองรับ transaction (in-memory test) ข้ามไป
    private async Task<IDbContextTransaction?> BeginLotLockAsync(IEnumerable<string> stockNumbers)
    {
        if (!_jewelryContext.Database.IsRelational())
        {
            return null;
        }

        if (_jewelryContext.Database.CurrentTransaction != null)
        {
            await _jewelryContext.LockStockPiecesAsync(stockNumbers);
            return null;
        }

        var transaction = await _jewelryContext.Database.BeginTransactionAsync();
        try
        {
            await _jewelryContext.LockStockPiecesAsync(stockNumbers);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }

        return transaction;
    }

    // ล็อตที่ยังไม่เคยมี unit เลย (เช่นก่อน backfill) แต่ยังมีของ → สร้าง unit ตาม qty ให้ก่อนผูกแท็ก
    private async Task<List<TbtStockUnit>> EnsureUnitsAsync(TbtStockPiece piece)
    {
        var units = await _jewelryContext.TbtStockUnit
            .Where(u => u.StockNumber == piece.StockNumber && u.ProductCode == piece.ProductCode)
            .ToListAsync();

        if (units.Count > 0 || piece.Status == StockUnitSync.Sold || piece.Qty <= 0)
        {
            return units;
        }

        await StockUnitSync.ReconcileAsync(_jewelryContext, piece, new StockUnitSyncInfo { By = CurrentUsername ?? "system" });
        await _jewelryContext.SaveChangesAsync();

        return await _jewelryContext.TbtStockUnit
            .Where(u => u.StockNumber == piece.StockNumber && u.ProductCode == piece.ProductCode)
            .ToListAsync();
    }

    private async Task<Dictionary<string, string>> LoadLocationNamesAsync(IEnumerable<string?> codes)
    {
        var distinct = codes.Where(c => !string.IsNullOrEmpty(c)).Select(c => c!).Distinct().ToList();
        if (distinct.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        return await _jewelryContext.TbmStockLocation
            .AsNoTracking()
            .Where(l => distinct.Contains(l.Code))
            .ToDictionaryAsync(l => l.Code, l => l.NameTh);
    }

    private async Task<List<PieceInfo>> ResolveEpcsAsync(List<string> epcs)
    {
        var tags = _jewelryContext.TbtStockPieceRfid.AsNoTracking().Where(r => r.IsActive && epcs.Contains(r.Epc));
        var rows = await QueryPieceInfo(tags, soldMismatch: false);

        var foundSet = rows.Select(r => r.Epc!).ToHashSet();
        var missing = epcs.Where(e => !foundSet.Contains(e)).ToList();

        if (missing.Count == 0)
        {
            return rows;
        }

        var (healedEpcs, mismatchBindingIds) = await HealSoldBindingsAsync(missing);

        if (healedEpcs.Count > 0)
        {
            var healedTags = _jewelryContext.TbtStockPieceRfid.AsNoTracking().Where(r => r.IsActive && healedEpcs.Contains(r.Epc));
            rows.AddRange(await QueryPieceInfo(healedTags, soldMismatch: false));
        }

        if (mismatchBindingIds.Count > 0)
        {
            var mismatchTags = _jewelryContext.TbtStockPieceRfid.AsNoTracking().Where(r => mismatchBindingIds.Contains(r.Id));
            rows.AddRange(await QueryPieceInfo(mismatchTags, soldMismatch: true));
        }

        return rows;
    }

    // แท็กที่ถูกปลดเพราะ unit ถูกขาย (SOLD) แต่ยิงเจอ — ถ้าล็อตยังมี unit ที่ยังไม่ขายและไม่มีแท็ก แปลว่าระบบตัดผิดชิ้น
    // → สลับ: unit ของแท็กนี้กลับมามีสถานะเดิม (ผูกแท็กใหม่) แล้ว unit ที่ไม่มีแท็กกลายเป็น SOLD แทน
    // ถ้าไม่มีชิ้นให้สลับ (กำกวม) ไม่แตะข้อมูล คืน mismatch ให้ผู้เรียกแสดงเตือน
    private async Task<(List<string> Healed, List<long> Mismatch)> HealSoldBindingsAsync(List<string> epcs)
    {
        var healed = new List<string>();
        var mismatch = new List<long>();

        var inactive = await _jewelryContext.TbtStockPieceRfid
            .AsNoTracking()
            .Where(r => !r.IsActive && epcs.Contains(r.Epc))
            .Select(r => new { r.Id, r.Epc, r.UnitId, r.UnbindReason, r.StockNumber, r.ProductCode })
            .ToListAsync();

        var latest = inactive
            .GroupBy(r => r.Epc)
            .Select(g => g.OrderByDescending(r => r.Id).First())
            .Where(r => r.UnbindReason == StockUnitSync.UnbindSold && r.UnitId.HasValue)
            .OrderBy(r => r.Id)
            .ToList();

        if (latest.Count == 0)
        {
            return (healed, mismatch);
        }

        var transaction = await BeginLotLockAsync(latest.Select(r => r.StockNumber));
        try
        {
            var now = DateTime.UtcNow;
            var username = CurrentUsername ?? "system";

            foreach (var item in latest)
            {
                var binding = await _jewelryContext.TbtStockPieceRfid.FirstAsync(r => r.Id == item.Id);
                if (binding.IsActive)
                {
                    continue;
                }

                var epcTaken = await _jewelryContext.TbtStockPieceRfid.AnyAsync(r => r.IsActive && r.Epc == binding.Epc);
                if (epcTaken)
                {
                    continue;
                }

                var units = await _jewelryContext.TbtStockUnit
                    .Where(u => u.StockNumber == binding.StockNumber && u.ProductCode == binding.ProductCode)
                    .ToListAsync();

                var scanned = units.FirstOrDefault(u => u.Id == binding.UnitId);
                if (scanned == null)
                {
                    mismatch.Add(binding.Id);
                    continue;
                }

                var taggedUnitIds = (await _jewelryContext.TbtStockPieceRfid
                    .Where(r => r.IsActive && r.StockNumber == binding.StockNumber && r.ProductCode == binding.ProductCode && r.UnitId != null)
                    .Select(r => r.UnitId!.Value)
                    .ToListAsync()).ToHashSet();

                if (scanned.Status != StockUnitSync.Sold)
                {
                    if (taggedUnitIds.Contains(scanned.Id))
                    {
                        mismatch.Add(binding.Id);
                        continue;
                    }
                }
                else
                {
                    // ล็อตเดียวหลายคลัง: สลับได้เฉพาะชิ้นที่อยู่คลังเดียวกับชิ้นที่ถูกขาย และสถานะ/บรรทัดจองเดียวกับตอนก่อนขาย
                    // (ชิ้นที่ขายไปจองให้บรรทัด L -> ต้องสลับกับชิ้นที่ยังจองบรรทัด L อยู่ ไม่มีบรรทัดจอง -> ต้องเป็นชิ้นว่าง)
                    // ไม่เข้าเงื่อนไข = ไม่แตะข้อมูล คืน mismatch ให้ผู้เรียกแสดงเตือน เพื่อไม่ให้ยอดรายคลัง/การจองเพี้ยน
                    var replacement = units
                        .Where(u => u.Status != StockUnitSync.Sold
                            && !taggedUnitIds.Contains(u.Id)
                            && u.LocationCode == scanned.LocationCode
                            && u.ReservedLineId == scanned.ReservedLineId
                            && u.Status == (scanned.ReservedLineId.HasValue ? StockUnitSync.Reserved : StockUnitSync.InStock))
                        .OrderByDescending(u => u.UnitNo)
                        .FirstOrDefault();

                    if (replacement == null)
                    {
                        mismatch.Add(binding.Id);
                        continue;
                    }

                    var soldRef = scanned.SoldRef;
                    var soldDate = scanned.SoldDate;

                    scanned.Status = replacement.Status;
                    scanned.SoldRef = null;
                    scanned.SoldDate = null;
                    scanned.ReservedRefType = replacement.ReservedRefType;
                    scanned.ReservedRef = replacement.ReservedRef;
                    scanned.ReservedLineId = replacement.ReservedLineId;
                    scanned.UpdateDate = now;
                    scanned.UpdateBy = username;

                    replacement.Status = StockUnitSync.Sold;
                    replacement.SoldRef = soldRef;
                    replacement.SoldDate = soldDate;
                    replacement.UpdateDate = now;
                    replacement.UpdateBy = username;

                    _jewelryContext.TbtStockUnit.UpdateRange(scanned, replacement);
                }

                binding.IsActive = true;
                binding.UnbindReason = null;
                binding.UpdateDate = now;
                binding.UpdateBy = username;
                _jewelryContext.TbtStockPieceRfid.Update(binding);

                await _jewelryContext.SaveChangesAsync();
                healed.Add(binding.Epc);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }

        return (healed, mismatch);
    }

    private async Task<List<PieceInfo>> QueryPieceInfo(IQueryable<TbtStockPieceRfid> tags, bool soldMismatch)
    {
        var rows = await (
            from r in tags
            join p in _jewelryContext.TbtStockPiece.AsNoTracking()
                on new { r.StockNumber, r.ProductCode } equals new { p.StockNumber, p.ProductCode }
            join s in _jewelryContext.TbtSku.AsNoTracking() on p.SkuCode equals s.SkuCode
            join u in _jewelryContext.TbtStockUnit.AsNoTracking() on r.UnitId equals (long?)u.Id into uj
            from u in uj.DefaultIfEmpty()
            select new
            {
                r.Epc,
                r.Tid,
                p.StockNumber,
                p.ProductCode,
                s.ProductNameTh,
                s.ProductNumber,
                PieceStatus = p.Status,
                PieceLocation = p.LocationCode,
                LotQty = p.Qty,
                s.ImagePath,
                s.ImageName,
                UnitId = u != null ? (long?)u.Id : null,
                UnitNo = u != null ? (int?)u.UnitNo : null,
                UnitStatus = u != null ? u.Status : null,
                UnitLocation = u != null ? u.LocationCode : null
            }
        ).ToListAsync();

        var locationNames = await LoadLocationNamesAsync(rows.Select(x => x.UnitLocation ?? x.PieceLocation));

        return rows.Select(x =>
        {
            var location = x.UnitLocation ?? x.PieceLocation;
            return new PieceInfo
            {
                Found = true,
                Epc = x.Epc,
                Tid = x.Tid,
                StockNumber = x.StockNumber,
                ProductCode = x.ProductCode,
                ProductName = x.ProductNameTh,
                ProductNumber = x.ProductNumber,
                Status = x.UnitStatus ?? x.PieceStatus,
                LocationCode = location,
                ImagePath = StockImagePath.Build(x.ImagePath, x.ImageName),
                UnitId = x.UnitId,
                UnitNo = x.UnitNo,
                UnitStatus = x.UnitStatus,
                UnitLocationCode = x.UnitLocation,
                UnitLocationName = location != null ? locationNames.GetValueOrDefault(location) : null,
                LotQty = x.LotQty,
                SoldMismatch = soldMismatch
            };
        }).ToList();
    }
}
