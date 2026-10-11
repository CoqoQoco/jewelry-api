using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Catalog
{
    public class CatalogService : BaseService, ICatalogService
    {
        private readonly JewelryContext _jewelryContext;

        public CatalogService(JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor,
            IHostEnvironment hostingEnvironment) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public jewelry.Model.Catalog.List.Response List(jewelry.Model.Catalog.List.Request request)
        {
            var query = _jewelryContext.TbmProductCatalog
                .AsNoTracking()
                .Include(x => x.TbtCatalogProduct)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Code))
            {
                query = query.Where(x => x.Code.Contains(request.Code));
            }
            if (!string.IsNullOrEmpty(request.Name))
            {
                query = query.Where(x => x.NameTh.Contains(request.Name) || (x.NameEn != null && x.NameEn.Contains(request.Name)));
            }

            var total = query.Count();

            if (!string.IsNullOrEmpty(request.SortField))
            {
                query = request.SortDir?.ToLower() == "desc"
                    ? query.OrderByDescending(x => EF.Property<object>(x, request.SortField))
                    : query.OrderBy(x => EF.Property<object>(x, request.SortField));
            }
            else
            {
                query = query.OrderByDescending(x => x.CreateDate);
            }

            var rows = query
                .Skip(request.Skip)
                .Take(request.Take)
                .Select(x => new jewelry.Model.Catalog.List.ResponseRow
                {
                    Id = x.Id,
                    Code = x.Code,
                    NameTh = x.NameTh,
                    NameEn = x.NameEn,
                    CollectionTitle = x.CollectionTitle,
                    HeaderLabel = x.HeaderLabel,
                    IsActive = x.IsActive,
                    ProductCount = x.TbtCatalogProduct.Count(p => p.IsActive),
                    CreateDate = x.CreateDate,
                    CreateBy = x.CreateBy,
                    UpdateDate = x.UpdateDate,
                    UpdateBy = x.UpdateBy
                })
                .ToList();

            return new jewelry.Model.Catalog.List.Response
            {
                Total = total,
                Items = rows
            };
        }

        public jewelry.Model.Catalog.Get.Response Get(jewelry.Model.Catalog.Get.Request request)
        {
            var catalog = _jewelryContext.TbmProductCatalog
                .AsNoTracking()
                .Include(x => x.TbtCatalogProduct)
                .Where(x => x.Id == request.Id)
                .SingleOrDefault();

            if (catalog == null)
            {
                throw new HandleException($"ไม่พบ Catalog รหัส {request.Id}");
            }

            return new jewelry.Model.Catalog.Get.Response
            {
                Id = catalog.Id,
                Code = catalog.Code,
                NameTh = catalog.NameTh,
                NameEn = catalog.NameEn,
                CollectionTitle = catalog.CollectionTitle,
                HeaderLabel = catalog.HeaderLabel,
                IsActive = catalog.IsActive,
                CreateDate = catalog.CreateDate,
                CreateBy = catalog.CreateBy,
                UpdateDate = catalog.UpdateDate,
                UpdateBy = catalog.UpdateBy,
                Items = catalog.TbtCatalogProduct
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.SortOrder)
                    .Select(p => new jewelry.Model.Catalog.Get.ResponseItem
                    {
                        Id = p.Id,
                        ProductNumber = p.ProductNumber,
                        SortOrder = p.SortOrder,
                        Dimension1 = p.Dimension1,
                        Dimension2 = p.Dimension2,
                        Dimension3 = p.Dimension3,
                        PriceFull = p.PriceFull,
                        PriceSale = p.PriceSale,
                        PriceCurrency = p.PriceCurrency
                    })
                    .ToList()
            };
        }

        public async Task<string> Create(jewelry.Model.Catalog.Create.Request request)
        {
            var existing = _jewelryContext.TbmProductCatalog
                .Where(x => x.Code == request.Code.ToUpper())
                .SingleOrDefault();

            if (existing != null)
            {
                throw new HandleException($"มี Catalog รหัส {request.Code.ToUpper()} อยู่แล้ว ไม่สามารถสร้างรายการซ้ำได้");
            }

            var canWritePrice = CanWritePrice();
            var createPrices = request.Items?.Select(item => ResolvePrice(canWritePrice, item.PriceFull, item.PriceSale, item.PriceCurrency)).ToList();

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var catalog = new TbmProductCatalog
            {
                Code = request.Code.ToUpper(),
                NameTh = request.NameTh,
                NameEn = request.NameEn,
                CollectionTitle = request.CollectionTitle,
                HeaderLabel = request.HeaderLabel,
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername
            };

            _jewelryContext.TbmProductCatalog.Add(catalog);
            await _jewelryContext.SaveChangesAsync();

            if (request.Items != null && request.Items.Any())
            {
                var items = request.Items.Select((item, index) => new TbtCatalogProduct
                {
                    CatalogId = catalog.Id,
                    ProductNumber = item.ProductNumber,
                    SortOrder = item.SortOrder,
                    Dimension1 = item.Dimension1,
                    Dimension2 = item.Dimension2,
                    Dimension3 = item.Dimension3,
                    PriceFull = createPrices![index].Full,
                    PriceSale = createPrices[index].Sale,
                    PriceCurrency = createPrices[index].Currency,
                    IsActive = true,
                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername
                }).ToList();

                _jewelryContext.TbtCatalogProduct.AddRange(items);
                await _jewelryContext.SaveChangesAsync();
            }

            scope.Complete();
            return "success";
        }

        public async Task<string> Update(jewelry.Model.Catalog.Update.Request request)
        {
            var catalog = _jewelryContext.TbmProductCatalog
                .Include(x => x.TbtCatalogProduct)
                .Where(x => x.Id == request.Id)
                .SingleOrDefault();

            if (catalog == null)
            {
                throw new HandleException($"ไม่พบ Catalog รหัส {request.Id}");
            }

            var codeConflict = _jewelryContext.TbmProductCatalog
                .Where(x => x.Code == request.Code.ToUpper() && x.Id != request.Id)
                .SingleOrDefault();

            if (codeConflict != null)
            {
                throw new HandleException($"มี Catalog รหัส {request.Code.ToUpper()} อยู่แล้ว ไม่สามารถใช้รหัสซ้ำได้");
            }

            var canWritePrice = CanWritePrice();
            var updatePrices = request.Items?.Select(item => ResolvePrice(canWritePrice, item.PriceFull, item.PriceSale, item.PriceCurrency)).ToList();

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            catalog.Code = request.Code.ToUpper();
            catalog.NameTh = request.NameTh;
            catalog.NameEn = request.NameEn;
            catalog.CollectionTitle = request.CollectionTitle;
            catalog.HeaderLabel = request.HeaderLabel;
            catalog.UpdateDate = DateTime.UtcNow;
            catalog.UpdateBy = CurrentUsername;

            _jewelryContext.TbmProductCatalog.Update(catalog);

            var existingItems = catalog.TbtCatalogProduct.Where(p => p.IsActive).ToList();
            var existingByProduct = existingItems
                .OrderBy(p => p.SortOrder)
                .GroupBy(p => p.ProductNumber)
                .ToDictionary(g => g.Key, g => new Queue<TbtCatalogProduct>(g));
            foreach (var item in existingItems)
            {
                item.IsActive = false;
                item.UpdateDate = DateTime.UtcNow;
                item.UpdateBy = CurrentUsername;
            }
            _jewelryContext.TbtCatalogProduct.UpdateRange(existingItems);

            await _jewelryContext.SaveChangesAsync();

            if (request.Items != null && request.Items.Any())
            {
                var newItems = request.Items.Select((item, index) =>
                {
                    decimal? priceFull;
                    decimal? priceSale;
                    string priceCurrency;
                    if (canWritePrice)
                    {
                        (priceFull, priceSale, priceCurrency) = updatePrices![index];
                    }
                    else if (existingByProduct.TryGetValue(item.ProductNumber, out var queue) && queue.Count > 0)
                    {
                        var old = queue.Dequeue();
                        priceFull = old.PriceFull;
                        priceSale = old.PriceSale;
                        priceCurrency = old.PriceCurrency;
                    }
                    else
                    {
                        priceFull = null;
                        priceSale = null;
                        priceCurrency = "THB";
                    }

                    return new TbtCatalogProduct
                    {
                        CatalogId = catalog.Id,
                        ProductNumber = item.ProductNumber,
                        SortOrder = item.SortOrder,
                        Dimension1 = item.Dimension1,
                        Dimension2 = item.Dimension2,
                        Dimension3 = item.Dimension3,
                        PriceFull = priceFull,
                        PriceSale = priceSale,
                        PriceCurrency = priceCurrency,
                        IsActive = true,
                        CreateDate = DateTime.UtcNow,
                        CreateBy = CurrentUsername
                    };
                }).ToList();

                _jewelryContext.TbtCatalogProduct.AddRange(newItems);
                await _jewelryContext.SaveChangesAsync();
            }

            scope.Complete();
            return "success";
        }

        public async Task<string> Delete(jewelry.Model.Catalog.Delete.Request request)
        {
            var catalog = _jewelryContext.TbmProductCatalog
                .Where(x => x.Id == request.Id)
                .SingleOrDefault();

            if (catalog == null)
            {
                throw new HandleException($"ไม่พบ Catalog รหัส {request.Id}");
            }

            catalog.IsActive = false;
            catalog.UpdateDate = DateTime.UtcNow;
            catalog.UpdateBy = CurrentUsername;

            _jewelryContext.TbmProductCatalog.Update(catalog);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }

        public async Task<string> AddProducts(jewelry.Model.Catalog.AddProducts.Request request)
        {
            var catalog = _jewelryContext.TbmProductCatalog
                .Include(x => x.TbtCatalogProduct)
                .Where(x => x.Id == request.CatalogId)
                .SingleOrDefault();

            if (catalog == null)
            {
                throw new HandleException($"ไม่พบ Catalog รหัส {request.CatalogId}");
            }

            if (request.Items == null || !request.Items.Any())
            {
                throw new HandleException("กรุณาระบุสินค้าที่ต้องการเพิ่ม");
            }

            var canWritePrice = CanWritePrice();
            var addPrices = request.Items.Select(item => ResolvePrice(canWritePrice, item.PriceFull, item.PriceSale, item.PriceCurrency)).ToList();

            var maxSortOrder = catalog.TbtCatalogProduct
                .Where(p => p.IsActive)
                .Select(p => (int?)p.SortOrder)
                .Max() ?? 0;

            var newItems = request.Items.Select((item, index) => new TbtCatalogProduct
            {
                CatalogId = catalog.Id,
                ProductNumber = item.ProductNumber,
                SortOrder = maxSortOrder + index + 1,
                Dimension1 = item.Dimension1,
                Dimension2 = item.Dimension2,
                Dimension3 = item.Dimension3,
                PriceFull = addPrices[index].Full,
                PriceSale = addPrices[index].Sale,
                PriceCurrency = addPrices[index].Currency,
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername
            }).ToList();

            _jewelryContext.TbtCatalogProduct.AddRange(newItems);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }

        private bool CanWritePrice() =>
            CatalogPriceHelper.CanWritePrice(_jewelryContext, CurrentUserRoles);

        private static (decimal? Full, decimal? Sale, string Currency) ResolvePrice(bool canWrite, decimal? full, decimal? sale, string? currency) =>
            canWrite
                ? CatalogPriceHelper.Normalize(full, sale, currency)
                : (null, null, "THB");

        public async Task<string> RemoveProduct(jewelry.Model.Catalog.RemoveProduct.Request request)
        {
            if (request.ItemId == null && string.IsNullOrEmpty(request.ProductNumber))
            {
                throw new HandleException("กรุณาระบุ ItemId หรือ ProductNumber");
            }

            var query = _jewelryContext.TbtCatalogProduct
                .Where(p => p.CatalogId == request.CatalogId && p.IsActive);

            if (request.ItemId.HasValue)
            {
                query = query.Where(p => p.Id == request.ItemId.Value);
            }
            else
            {
                query = query.Where(p => p.ProductNumber == request.ProductNumber);
            }

            var items = query.ToList();

            if (!items.Any())
            {
                throw new HandleException("ไม่พบรายการสินค้าใน Catalog นี้");
            }

            foreach (var item in items)
            {
                item.IsActive = false;
                item.UpdateDate = DateTime.UtcNow;
                item.UpdateBy = CurrentUsername;
            }

            _jewelryContext.TbtCatalogProduct.UpdateRange(items);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }
    }
}
