using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Location;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.Location
{
    public class StockLocationService : BaseService, IStockLocationService
    {
        private readonly JewelryContext _jewelryContext;

        public StockLocationService(JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public IQueryable<object> List()
        {
            return _jewelryContext.TbmStockLocation
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .Select(x => new
                {
                    x.Code,
                    x.NameTh,
                    x.NameEn,
                    x.Type,
                    x.ParentCode,
                    x.IsSalesPoint,
                    x.IsTemporary,
                    x.IsActive,
                    x.SortOrder,
                    x.CreateDate,
                    x.CreateBy
                });
        }

        public async Task<string> Create(CreateStockLocationRequest req)
        {
            var existing = _jewelryContext.TbmStockLocation
                .FirstOrDefault(x => x.Code == req.Code.ToUpper());

            if (existing != null)
            {
                throw new HandleException($"พบรหัส Location {req.Code} ซ้ำในระบบ กรุณาสร้างรหัสใหม่");
            }

            var entity = new TbmStockLocation
            {
                Code = req.Code.ToUpper(),
                NameTh = req.NameTh,
                NameEn = req.NameEn,
                Type = req.Type,
                ParentCode = req.ParentCode,
                IsSalesPoint = req.IsSalesPoint,
                IsTemporary = req.IsTemporary,
                IsActive = req.IsActive,
                SortOrder = req.SortOrder,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername
            };

            _jewelryContext.TbmStockLocation.Add(entity);
            await _jewelryContext.SaveChangesAsync();

            return $"{entity.Code} - {entity.NameTh}";
        }

        public async Task<string> Update(UpdateStockLocationRequest req)
        {
            var entity = _jewelryContext.TbmStockLocation
                .FirstOrDefault(x => x.Code == req.Code.ToUpper());

            if (entity == null)
            {
                throw new HandleException($"ไม่พบ Location {req.Code} ในระบบ");
            }

            if (req.NameTh != null) entity.NameTh = req.NameTh;
            if (req.NameEn != null) entity.NameEn = req.NameEn;
            if (req.Type != null) entity.Type = req.Type;
            if (req.ParentCode != null) entity.ParentCode = req.ParentCode;
            if (req.IsSalesPoint.HasValue) entity.IsSalesPoint = req.IsSalesPoint.Value;
            if (req.IsTemporary.HasValue) entity.IsTemporary = req.IsTemporary.Value;
            if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
            if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;

            entity.UpdateDate = DateTime.UtcNow;
            entity.UpdateBy = CurrentUsername;

            _jewelryContext.TbmStockLocation.Update(entity);
            await _jewelryContext.SaveChangesAsync();

            return $"{entity.Code} - {entity.NameTh}";
        }

        public async Task Delete(string code)
        {
            var entity = _jewelryContext.TbmStockLocation
                .FirstOrDefault(x => x.Code == code.ToUpper());

            if (entity == null)
            {
                throw new HandleException($"ไม่พบ Location {code} ในระบบ");
            }

            var hasPiece = _jewelryContext.TbtStockPiece.Any(p => p.LocationCode == code);
            var hasBalance = _jewelryContext.TbtStockBalance.Any(b => b.LocationCode == code);

            if (hasPiece || hasBalance)
            {
                throw new HandleException("ไม่สามารถลบได้ มีสินค้าอยู่ใน location นี้");
            }

            _jewelryContext.TbmStockLocation.Remove(entity);
            await _jewelryContext.SaveChangesAsync();
        }
    }
}
