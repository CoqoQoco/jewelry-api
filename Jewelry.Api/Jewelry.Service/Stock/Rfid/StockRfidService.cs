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
