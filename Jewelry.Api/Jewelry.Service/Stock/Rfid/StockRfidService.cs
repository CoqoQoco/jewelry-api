using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Rfid;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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

        var pieceExists = await _jewelryContext.TbtStockPiece
            .AsNoTracking()
            .AnyAsync(p => p.StockNumber == stockNumber && p.ProductCode == productCode);

        if (!pieceExists)
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

        var pieceTag = await _jewelryContext.TbtStockPieceRfid
            .AsNoTracking()
            .Where(r => r.IsActive && r.StockNumber == stockNumber && r.ProductCode == productCode)
            .Select(r => r.Epc)
            .FirstOrDefaultAsync();

        if (pieceTag != null)
        {
            throw new HandleException($"สินค้านี้มีแท็กผูกอยู่แล้ว (EPC {pieceTag}) กรุณายกเลิกการผูกก่อน");
        }

        var entity = new TbtStockPieceRfid
        {
            Epc = epc,
            Tid = tid,
            StockNumber = stockNumber,
            ProductCode = productCode,
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

        return new jewelry.Model.Stock.Rfid.Bind.Response
        {
            Id = entity.Id,
            Epc = entity.Epc,
            Tid = entity.Tid,
            StockNumber = entity.StockNumber,
            ProductCode = entity.ProductCode
        };
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
        else if (!string.IsNullOrEmpty(stockNumber) && !string.IsNullOrEmpty(productCode))
        {
            targets = await _jewelryContext.TbtStockPieceRfid
                .Where(r => r.IsActive && r.StockNumber == stockNumber && r.ProductCode == productCode)
                .ToListAsync();
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

        var rows = await QueryPieceInfo(new List<string> { epc });
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
            ImagePath = info.ImagePath
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

        var rows = await QueryPieceInfo(epcs);
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

        var tag = await _jewelryContext.TbtStockPieceRfid
            .AsNoTracking()
            .Where(r => r.IsActive && r.StockNumber == stockNumber && r.ProductCode == productCode)
            .Select(r => new { r.Epc, r.Tid, r.CreateDate })
            .FirstOrDefaultAsync();

        if (tag == null)
        {
            return new jewelry.Model.Stock.Rfid.GetByStock.Response { Found = false };
        }

        return new jewelry.Model.Stock.Rfid.GetByStock.Response
        {
            Found = true,
            Epc = tag.Epc,
            Tid = tag.Tid,
            BoundDate = tag.CreateDate
        };
    }

    public async Task<jewelry.Model.Stock.Rfid.ListReceipts.Response> ListReceipts(jewelry.Model.Stock.Rfid.ListReceipts.Request request)
    {
        var (skip, take) = NormalizePaging(request.Skip, request.Take, 20, 100);
        var search = request.Search?.Trim();

        var pieces = _jewelryContext.TbtStockPiece.AsNoTracking()
            .Where(p => p.ReceiptNumber != null && p.ReceiptNumber != "");

        if (!string.IsNullOrEmpty(search))
        {
            var lowered = search.ToLowerInvariant();
            pieces = pieces.Where(p => p.ReceiptNumber!.ToLower().Contains(lowered));
        }
        else
        {
            var since = DateTime.UtcNow.AddDays(-365);
            pieces = pieces.Where(p => p.ReceiptDate >= since);
        }

        var activeTags = _jewelryContext.TbtStockPieceRfid.AsNoTracking().Where(r => r.IsActive);

        var flagged =
            from p in pieces
            join r in activeTags
                on new { p.StockNumber, p.ProductCode } equals new { r.StockNumber, r.ProductCode } into rj
            from r in rj.DefaultIfEmpty()
            select new
            {
                p.ReceiptNumber,
                p.ReceiptDate,
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
            from p in _jewelryContext.TbtStockPiece.AsNoTracking()
            join s in _jewelryContext.TbtSku.AsNoTracking() on p.SkuCode equals s.SkuCode
            join r in activeTags
                on new { p.StockNumber, p.ProductCode } equals new { r.StockNumber, r.ProductCode } into rj
            from r in rj.DefaultIfEmpty()
            where p.ReceiptNumber == receiptNumber
            orderby p.StockNumber, p.ProductCode
            select new
            {
                p.StockNumber,
                p.ProductCode,
                s.ProductNameTh,
                s.ProductNumber,
                p.Status,
                p.LocationCode,
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
            from p in _jewelryContext.TbtStockPiece.AsNoTracking()
            join s in _jewelryContext.TbtSku.AsNoTracking() on p.SkuCode equals s.SkuCode
            where (p.Status == "IN_STOCK" || p.Status == "RESERVED")
                && !_jewelryContext.TbtStockPieceRfid.Any(r =>
                    r.IsActive && r.StockNumber == p.StockNumber && r.ProductCode == p.ProductCode)
            select new { p, s };

        if (!string.IsNullOrEmpty(locationCode))
        {
            query = query.Where(x => x.p.LocationCode == locationCode);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var normalized = StockNumberSearch.Normalize(search);
            var lowered = search.ToLowerInvariant();
            query = query.Where(x =>
                (normalized != "" && x.p.StockNumber.Replace("-", "").ToUpper().Contains(normalized))
                || x.s.ProductNameTh.ToLower().Contains(lowered)
                || (x.s.ProductNumber != null && x.s.ProductNumber.ToLower().Contains(lowered)));
        }

        var total = await query.CountAsync();

        var rows = await query
            .OrderBy(x => x.p.StockNumber)
            .ThenBy(x => x.p.ProductCode)
            .Skip(skip)
            .Take(take)
            .Select(x => new
            {
                x.p.StockNumber,
                x.p.ProductCode,
                x.s.ProductNameTh,
                x.s.ProductNumber,
                x.p.Status,
                x.p.LocationCode,
                x.s.ImagePath,
                x.s.ImageName,
                x.p.ReceiptNumber
            })
            .ToListAsync();

        return new jewelry.Model.Stock.Rfid.ListUnbound.Response
        {
            Total = total,
            Items = rows.Select(x => new jewelry.Model.Stock.Rfid.ListUnbound.Item
            {
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
            select new
            {
                r.Id,
                r.Epc,
                r.StockNumber,
                r.ProductCode,
                ProductName = s != null ? s.ProductNameTh : null,
                ReceiptNumber = p != null ? p.ReceiptNumber : null,
                r.IsActive,
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

    private async Task<List<PieceInfo>> QueryPieceInfo(List<string> epcs)
    {
        var rows = await (
            from r in _jewelryContext.TbtStockPieceRfid.AsNoTracking()
            join p in _jewelryContext.TbtStockPiece.AsNoTracking()
                on new { r.StockNumber, r.ProductCode } equals new { p.StockNumber, p.ProductCode }
            join s in _jewelryContext.TbtSku.AsNoTracking() on p.SkuCode equals s.SkuCode
            where r.IsActive && epcs.Contains(r.Epc)
            select new
            {
                r.Epc,
                r.Tid,
                p.StockNumber,
                p.ProductCode,
                s.ProductNameTh,
                s.ProductNumber,
                p.Status,
                p.LocationCode,
                s.ImagePath,
                s.ImageName
            }
        ).ToListAsync();

        return rows.Select(x => new PieceInfo
        {
            Found = true,
            Epc = x.Epc,
            Tid = x.Tid,
            StockNumber = x.StockNumber,
            ProductCode = x.ProductCode,
            ProductName = x.ProductNameTh,
            ProductNumber = x.ProductNumber,
            Status = x.Status,
            LocationCode = x.LocationCode,
            ImagePath = StockImagePath.Build(x.ImagePath, x.ImageName)
        }).ToList();
    }
}
