using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Sale.StockBasket;

public class StockBasketService : BaseService, IStockBasketService
{
    private readonly JewelryContext _jewelryContext;

    public StockBasketService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
    }

    public async Task<string> GenerateNumber()
    {
        var count = await _jewelryContext.TbtStockBasket.CountAsync();
        return "BK-" + (count + 1).ToString("D4");
    }

    public async Task<string> Upsert(jewelry.Model.Sale.StockBasket.Create.Request request)
    {
        if (string.IsNullOrEmpty(request.Running))
        {
            var basket = new TbtStockBasket
            {
                Running = Guid.NewGuid().ToString(),
                BasketNumber = await GenerateNumber(),
                BasketName = request.BasketName,
                EventDate = request.EventDate?.UtcDateTime,
                Responsible = request.Responsible,
                Remark = request.Remark,
                Status = 0,
                StatusName = "Draft",
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername
            };

            await _jewelryContext.TbtStockBasket.AddAsync(basket);
            await _jewelryContext.SaveChangesAsync();

            return basket.Running;
        }
        else
        {
            var basket = await _jewelryContext.TbtStockBasket
                .FirstOrDefaultAsync(x => x.Running == request.Running);

            if (basket == null)
            {
                throw new HandleException($"ไม่พบตะกร้า Running: {request.Running}");
            }

            if (basket.Status != 0)
            {
                throw new HandleException("ไม่สามารถแก้ไขได้ เนื่องจากสถานะไม่ใช่ Draft");
            }

            basket.BasketName = request.BasketName;
            basket.EventDate = request.EventDate?.UtcDateTime;
            basket.Responsible = request.Responsible;
            basket.Remark = request.Remark;
            basket.UpdateDate = DateTime.UtcNow;
            basket.UpdateBy = CurrentUsername;

            _jewelryContext.TbtStockBasket.Update(basket);
            await _jewelryContext.SaveChangesAsync();

            return basket.Running;
        }
    }

    public async Task<jewelry.Model.Sale.StockBasket.Get.Response> Get(jewelry.Model.Sale.StockBasket.Get.Request request)
    {
        var basket = await _jewelryContext.TbtStockBasket
            .FirstOrDefaultAsync(x => x.Running == request.Running);

        if (basket == null)
        {
            throw new HandleException($"ไม่พบตะกร้า Running: {request.Running}");
        }

        var items = await (
            from item in _jewelryContext.TbtStockBasketItem
            where item.BasketRunning == request.Running
            join piece in _jewelryContext.TbtStockPiece
                on item.StockNumber equals piece.StockNumber into pieceGroup
            from piece in pieceGroup.DefaultIfEmpty()
            join sku in _jewelryContext.TbtSku
                on (piece != null ? piece.SkuCode : null) equals sku.SkuCode into skuGroup
            from sku in skuGroup.DefaultIfEmpty()
            select new jewelry.Model.Sale.StockBasket.Get.BasketItemResponse
            {
                Id = item.Id,
                StockNumber = item.StockNumber,
                ProductNumber = sku != null ? sku.ProductNumber : null,
                ProductNameTh = sku != null ? sku.ProductNameTh : null,
                ProductNameEn = sku != null ? sku.ProductNameEn : null,
                ProductType = sku != null ? sku.ProductType : null,
                ProductionTypeSize = sku != null ? sku.ProductionTypeSize : null,
                ImagePath = sku != null ? sku.ImagePath : null,
                Status = item.Status,
                StatusName = item.StatusName,
                CreateDate = item.CreateDate
            }
        ).ToListAsync();

        return new jewelry.Model.Sale.StockBasket.Get.Response
        {
            Running = basket.Running,
            BasketNumber = basket.BasketNumber,
            BasketName = basket.BasketName,
            EventDate = basket.EventDate,
            Responsible = basket.Responsible,
            Status = basket.Status,
            StatusName = basket.StatusName,
            Remark = basket.Remark,
            CheckoutDate = basket.CheckoutDate,
            CreateDate = basket.CreateDate,
            CreateBy = basket.CreateBy,
            Items = items
        };
    }

    public async Task<(IList<jewelry.Model.Sale.StockBasket.List.Response> Data, int Total)> List(jewelry.Model.Sale.StockBasket.List.Request request)
    {
        var query = _jewelryContext.TbtStockBasket.AsQueryable();

        if (!string.IsNullOrEmpty(request.BasketNumber))
        {
            query = query.Where(x => x.BasketNumber.Contains(request.BasketNumber));
        }

        if (!string.IsNullOrEmpty(request.BasketName))
        {
            query = query.Where(x => x.BasketName.Contains(request.BasketName));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (!string.IsNullOrEmpty(request.Responsible))
        {
            query = query.Where(x => x.Responsible != null && x.Responsible.Contains(request.Responsible));
        }

        if (request.EventDateStart.HasValue)
        {
            var startUtc = request.EventDateStart.Value.StartOfDayUtc().UtcDateTime;
            query = query.Where(x => x.EventDate >= startUtc);
        }

        if (request.EventDateEnd.HasValue)
        {
            var endUtc = request.EventDateEnd.Value.EndOfDayUtc().UtcDateTime;
            query = query.Where(x => x.EventDate <= endUtc);
        }

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(x => x.CreateDate)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(x => new jewelry.Model.Sale.StockBasket.List.Response
            {
                Running = x.Running,
                BasketNumber = x.BasketNumber,
                BasketName = x.BasketName,
                EventDate = x.EventDate,
                Responsible = x.Responsible,
                Status = x.Status,
                StatusName = x.StatusName,
                TotalItems = _jewelryContext.TbtStockBasketItem.Count(i => i.BasketRunning == x.Running),
                CreateDate = x.CreateDate,
                CreateBy = x.CreateBy
            })
            .ToListAsync();

        return (data, total);
    }

    public async Task<jewelry.Model.Sale.StockBasket.AddItems.Response> AddItems(jewelry.Model.Sale.StockBasket.AddItems.Request request)
    {
        var basket = await _jewelryContext.TbtStockBasket
            .FirstOrDefaultAsync(x => x.Running == request.BasketRunning);

        if (basket == null)
        {
            throw new HandleException($"ไม่พบตะกร้า Running: {request.BasketRunning}");
        }

        if (basket.Status != 0)
        {
            throw new HandleException("ไม่สามารถเพิ่มสินค้าได้ เนื่องจากสถานะไม่ใช่ Draft");
        }

        // Get all stock numbers in active baskets (status 0-3)
        var activeBasketStockNumbersList = await _jewelryContext.TbtStockBasketItem
            .Where(i => _jewelryContext.TbtStockBasket
                .Where(b => b.Status >= 0 && b.Status <= 3)
                .Select(b => b.Running)
                .Contains(i.BasketRunning))
            .Select(i => i.StockNumber)
            .ToListAsync();
        var activeBasketStockNumbers = new HashSet<string>(activeBasketStockNumbersList);

        var response = new jewelry.Model.Sale.StockBasket.AddItems.Response();
        var itemsToAdd = new List<TbtStockBasketItem>();

        if (request.StockNumbers != null && request.StockNumbers.Any())
        {
            foreach (var stockNumber in request.StockNumbers)
            {
                if (activeBasketStockNumbers.Contains(stockNumber))
                {
                    response.SkippedStockNumbers.Add(stockNumber);
                    continue;
                }

                var piece = await _jewelryContext.TbtStockPiece
                    .FirstOrDefaultAsync(p => p.StockNumber == stockNumber);

                if (piece == null || piece.Status != "IN_STOCK")
                {
                    response.SkippedStockNumbers.Add(stockNumber);
                    continue;
                }

                itemsToAdd.Add(new TbtStockBasketItem
                {
                    BasketRunning = request.BasketRunning,
                    BasketNumber = basket.BasketNumber,
                    StockNumber = stockNumber,
                    Status = "InBasket",
                    StatusName = "อยู่ในตะกร้า",
                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername
                });

                activeBasketStockNumbers.Add(stockNumber);
            }
        }

        if (request.CategoryFilter != null)
        {
            var pieceQuery = _jewelryContext.TbtStockPiece
                .Include(p => p.SkuCodeNavigation)
                .Where(p => p.Status == "IN_STOCK")
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.CategoryFilter.ProductType))
            {
                pieceQuery = pieceQuery.Where(p => p.SkuCodeNavigation.ProductType != null && p.SkuCodeNavigation.ProductType.Contains(request.CategoryFilter.ProductType));
            }

            if (!string.IsNullOrEmpty(request.CategoryFilter.ProductionType))
            {
                pieceQuery = pieceQuery.Where(p => p.SkuCodeNavigation.ProductionType != null && p.SkuCodeNavigation.ProductionType.Contains(request.CategoryFilter.ProductionType));
            }

            if (!string.IsNullOrEmpty(request.CategoryFilter.ProductionTypeSize))
            {
                pieceQuery = pieceQuery.Where(p => p.SkuCodeNavigation.ProductionTypeSize != null && p.SkuCodeNavigation.ProductionTypeSize.Contains(request.CategoryFilter.ProductionTypeSize));
            }

            if (!string.IsNullOrEmpty(request.CategoryFilter.ReceiptNumber))
            {
                pieceQuery = pieceQuery.Where(p => p.ReceiptNumber != null && p.ReceiptNumber.Contains(request.CategoryFilter.ReceiptNumber));
            }

            var categoryPieces = await pieceQuery
                .Where(p => !activeBasketStockNumbers.Contains(p.StockNumber))
                .ToListAsync();

            foreach (var piece in categoryPieces)
            {
                itemsToAdd.Add(new TbtStockBasketItem
                {
                    BasketRunning = request.BasketRunning,
                    BasketNumber = basket.BasketNumber,
                    StockNumber = piece.StockNumber,
                    Status = "InBasket",
                    StatusName = "อยู่ในตะกร้า",
                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername
                });
            }
        }

        if (itemsToAdd.Any())
        {
            await _jewelryContext.TbtStockBasketItem.AddRangeAsync(itemsToAdd);
            await _jewelryContext.SaveChangesAsync();
        }

        response.AddedCount = itemsToAdd.Count;
        response.Message = $"เพิ่มสินค้า {itemsToAdd.Count} รายการ, ข้าม {response.SkippedStockNumbers.Count} รายการ";

        return response;
    }

    public async Task RemoveItem(jewelry.Model.Sale.StockBasket.RemoveItem.Request request)
    {
        var item = await _jewelryContext.TbtStockBasketItem
            .FirstOrDefaultAsync(x => x.Id == request.ItemId);

        if (item == null)
        {
            throw new HandleException($"ไม่พบรายการ Id: {request.ItemId}");
        }

        if (item.BasketRunning != request.BasketRunning)
        {
            throw new HandleException("รายการนี้ไม่ได้อยู่ในตะกร้าที่ระบุ");
        }

        if (item.Status != "InBasket")
        {
            throw new HandleException("ไม่สามารถลบรายการที่มีสถานะ Sold หรือ Returned ได้");
        }

        _jewelryContext.TbtStockBasketItem.Remove(item);
        await _jewelryContext.SaveChangesAsync();
    }

    public async Task SubmitApproval(jewelry.Model.Sale.StockBasket.UpdateStatus.Request request)
    {
        var basket = await _jewelryContext.TbtStockBasket
            .FirstOrDefaultAsync(x => x.Running == request.BasketRunning);

        if (basket == null)
        {
            throw new HandleException($"ไม่พบตะกร้า Running: {request.BasketRunning}");
        }

        if (basket.Status != 0)
        {
            throw new HandleException("ไม่สามารถส่งอนุมัติได้ เนื่องจากสถานะไม่ใช่ Draft");
        }

        basket.Status = 1;
        basket.StatusName = "รออนุมัติ";
        basket.UpdateDate = DateTime.UtcNow;
        basket.UpdateBy = CurrentUsername;

        _jewelryContext.TbtStockBasket.Update(basket);
        await _jewelryContext.SaveChangesAsync();
    }

    public async Task Approve(jewelry.Model.Sale.StockBasket.UpdateStatus.Request request)
    {
        var basket = await _jewelryContext.TbtStockBasket
            .FirstOrDefaultAsync(x => x.Running == request.BasketRunning);

        if (basket == null)
        {
            throw new HandleException($"ไม่พบตะกร้า Running: {request.BasketRunning}");
        }

        if (basket.Status != 1)
        {
            throw new HandleException("ไม่สามารถอนุมัติได้ เนื่องจากสถานะไม่ใช่ รออนุมัติ");
        }

        basket.Status = 2;
        basket.StatusName = "อนุมัติแล้ว";
        basket.UpdateDate = DateTime.UtcNow;
        basket.UpdateBy = CurrentUsername;

        _jewelryContext.TbtStockBasket.Update(basket);
        await _jewelryContext.SaveChangesAsync();
    }

    public async Task Checkout(jewelry.Model.Sale.StockBasket.UpdateStatus.Request request)
    {
        var basket = await _jewelryContext.TbtStockBasket
            .FirstOrDefaultAsync(x => x.Running == request.BasketRunning);

        if (basket == null)
        {
            throw new HandleException($"ไม่พบตะกร้า Running: {request.BasketRunning}");
        }

        if (basket.Status != 2)
        {
            throw new HandleException("ไม่สามารถ Checkout ได้ เนื่องจากสถานะไม่ใช่ อนุมัติแล้ว");
        }

        basket.Status = 3;
        basket.StatusName = "CheckedOut";
        basket.CheckoutDate = DateTime.UtcNow;
        basket.UpdateDate = DateTime.UtcNow;
        basket.UpdateBy = CurrentUsername;

        _jewelryContext.TbtStockBasket.Update(basket);
        await _jewelryContext.SaveChangesAsync();
    }
}
