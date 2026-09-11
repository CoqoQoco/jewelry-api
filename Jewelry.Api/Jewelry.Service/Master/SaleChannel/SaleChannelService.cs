using jewelry.Model.Exceptions;
using jewelry.Model.Master.SaleChannel;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Master.SaleChannel
{
    public class SaleChannelService : BaseService, ISaleChannelService
    {
        private readonly JewelryContext _jewelryContext;

        public SaleChannelService(JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public async Task<List<SaleChannelResponse>> List(SaleChannelListRequest req)
        {
            var query = _jewelryContext.TbmSaleChannel.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(req.NameOrCode))
            {
                var pattern = $"%{req.NameOrCode}%";
                query = query.Where(x =>
                    EF.Functions.ILike(x.Code, pattern) ||
                    EF.Functions.ILike(x.NameTh, pattern) ||
                    (x.NameEn != null && EF.Functions.ILike(x.NameEn, pattern)));
            }

            if (!string.IsNullOrWhiteSpace(req.Type))
            {
                query = query.Where(x => x.Type == req.Type);
            }

            if (req.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == req.IsActive.Value);
            }

            var entities = await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .ToListAsync();

            return entities.Select(ToResponse).ToList();
        }

        public async Task<List<SaleChannelResponse>> Active()
        {
            var entities = await _jewelryContext.TbmSaleChannel
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .ToListAsync();

            return entities.Select(ToResponse).ToList();
        }

        public async Task<SaleChannelResponse?> Current()
        {
            var today = DateTime.UtcNow.AddHours(7).Date;

            var entity = await _jewelryContext.TbmSaleChannel
                .AsNoTracking()
                .Where(x => x.IsActive
                    && (x.StartDate == null || x.StartDate <= today)
                    && (x.EndDate == null || x.EndDate >= today))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .FirstOrDefaultAsync();

            if (entity == null)
            {
                entity = await _jewelryContext.TbmSaleChannel
                    .AsNoTracking()
                    .Where(x => x.IsActive && x.IsDefault)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Code)
                    .FirstOrDefaultAsync();
            }

            return entity == null ? null : ToResponse(entity);
        }

        public async Task<SaleChannelResponse> Get(string code)
        {
            var entity = await _jewelryContext.TbmSaleChannel
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code.ToUpper());

            if (entity == null)
            {
                throw new HandleException($"ไม่พบจุดขาย {code} ในระบบ");
            }

            return ToResponse(entity);
        }

        public async Task<string> Create(CreateSaleChannelRequest req)
        {
            var code = req.Code.ToUpper();

            var existing = await _jewelryContext.TbmSaleChannel
                .FirstOrDefaultAsync(x => x.Code == code);

            if (existing != null)
            {
                throw new HandleException($"พบรหัสจุดขาย {code} ซ้ำในระบบ กรุณาสร้างรหัสใหม่");
            }

            var entity = new TbmSaleChannel
            {
                Code = code,
                NameTh = req.NameTh,
                NameEn = req.NameEn,
                Type = req.Type,
                Venue = req.Venue,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                IsDefault = req.IsDefault,
                IsActive = req.IsActive,
                SortOrder = req.SortOrder,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername
            };

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                if (req.IsDefault)
                {
                    await ClearOtherDefaults(null);
                }

                _jewelryContext.TbmSaleChannel.Add(entity);
                await _jewelryContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"เกิดข้อผิดพลาดในการสร้างจุดขาย: {ex.Message}");
            }

            return $"{entity.Code} - {entity.NameTh}";
        }

        public async Task<string> Update(UpdateSaleChannelRequest req)
        {
            var code = req.Code.ToUpper();

            var entity = await _jewelryContext.TbmSaleChannel
                .FirstOrDefaultAsync(x => x.Code == code);

            if (entity == null)
            {
                throw new HandleException($"ไม่พบจุดขาย {code} ในระบบ");
            }

            if (req.NameTh != null) entity.NameTh = req.NameTh;
            if (req.NameEn != null) entity.NameEn = req.NameEn;
            if (req.Type != null) entity.Type = req.Type;
            if (req.Venue != null) entity.Venue = req.Venue;
            if (req.StartDate.HasValue) entity.StartDate = req.StartDate.Value;
            if (req.EndDate.HasValue) entity.EndDate = req.EndDate.Value;
            if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
            if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;

            entity.UpdateDate = DateTime.UtcNow;
            entity.UpdateBy = CurrentUsername;

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                if (req.IsDefault.HasValue)
                {
                    entity.IsDefault = req.IsDefault.Value;

                    if (req.IsDefault.Value)
                    {
                        await ClearOtherDefaults(code);
                    }
                }

                _jewelryContext.TbmSaleChannel.Update(entity);
                await _jewelryContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"เกิดข้อผิดพลาดในการแก้ไขจุดขาย: {ex.Message}");
            }

            return $"{entity.Code} - {entity.NameTh}";
        }

        public async Task Delete(string code)
        {
            var normalizedCode = code.ToUpper();

            var entity = await _jewelryContext.TbmSaleChannel
                .FirstOrDefaultAsync(x => x.Code == normalizedCode);

            if (entity == null)
            {
                throw new HandleException($"ไม่พบจุดขาย {normalizedCode} ในระบบ");
            }

            var saleOrderCount = await _jewelryContext.TbtSaleOrder
                .CountAsync(x => x.SaleChannelCode == normalizedCode);
            var invoiceCount = await _jewelryContext.TbtSaleInvoiceHeader
                .CountAsync(x => x.SaleChannelCode == normalizedCode);

            if (saleOrderCount + invoiceCount > 0)
            {
                throw new HandleException(
                    $"ไม่สามารถลบได้ มีใบสั่งขาย {saleOrderCount} รายการ และใบแจ้งหนี้ {invoiceCount} รายการอ้างอิงจุดขายนี้อยู่");
            }

            _jewelryContext.TbmSaleChannel.Remove(entity);
            await _jewelryContext.SaveChangesAsync();
        }

        private async Task ClearOtherDefaults(string? exceptCode)
        {
            var others = await _jewelryContext.TbmSaleChannel
                .Where(x => x.IsDefault && (exceptCode == null || x.Code != exceptCode))
                .ToListAsync();

            if (others.Count == 0)
            {
                return;
            }

            foreach (var other in others)
            {
                other.IsDefault = false;
                other.UpdateDate = DateTime.UtcNow;
                other.UpdateBy = CurrentUsername;
            }

            _jewelryContext.TbmSaleChannel.UpdateRange(others);
        }

        private static SaleChannelResponse ToResponse(TbmSaleChannel x) => new SaleChannelResponse
        {
            Code = x.Code,
            NameTh = x.NameTh,
            NameEn = x.NameEn,
            Type = x.Type,
            Venue = x.Venue,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            IsDefault = x.IsDefault,
            IsActive = x.IsActive,
            SortOrder = x.SortOrder,
            CreateDate = x.CreateDate,
            CreateBy = x.CreateBy,
            UpdateDate = x.UpdateDate,
            UpdateBy = x.UpdateBy
        };
    }
}
