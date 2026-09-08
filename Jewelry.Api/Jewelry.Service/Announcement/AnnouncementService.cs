using jewelry.Model.Announcement;
using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Announcement;

public class AnnouncementService : BaseService, IAnnouncementService
{
    private readonly JewelryContext _jewelryContext;
    private readonly IAzureBlobStorageService _blobStorage;

    public AnnouncementService(
        JewelryContext jewelryContext,
        IHttpContextAccessor httpContextAccessor,
        IAzureBlobStorageService blobStorage)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
        _blobStorage = blobStorage;
    }

    public async Task<FeedAnnouncementResponse> Feed(FeedAnnouncementRequest request)
    {
        var now = DateTime.UtcNow;

        var take = request.Take;
        if (take < 1) take = 1;
        if (take > 50) take = 50;

        var skip = request.Skip < 0 ? 0 : request.Skip;

        var query = _jewelryContext.TbtAnnouncement
            .AsNoTracking()
            .Where(x => x.IsActive && x.IsPublished
                && x.PublishStart <= now
                && (x.PublishEnd == null || x.PublishEnd >= now));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.PublishStart)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return new FeedAnnouncementResponse
        {
            Data = items.Select(x => MapToItem(x, now)).ToList(),
            Total = total
        };
    }

    public async Task<AnnouncementItemResponse> Get(GetAnnouncementRequest request)
    {
        var entity = await _jewelryContext.TbtAnnouncement
            .AsNoTracking()
            .Where(x => x.Id == request.Id && x.IsActive)
            .SingleOrDefaultAsync();

        if (entity == null)
            throw new HandleException("ไม่พบประกาศ");

        return MapToItem(entity, DateTime.UtcNow);
    }

    public async Task<DataSourceResult> Search(SearchAnnouncementRequest request)
    {
        var query = _jewelryContext.TbtAnnouncement
            .AsNoTracking()
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Keyword))
        {
            var keywordPattern = $"%{request.Keyword}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Title, keywordPattern) ||
                EF.Functions.ILike(x.Body, keywordPattern));
        }

        if (request.IsPublished.HasValue)
            query = query.Where(x => x.IsPublished == request.IsPublished.Value);

        if (request.IsPinned.HasValue)
            query = query.Where(x => x.IsPinned == request.IsPinned.Value);

        if (request.Sort == null || !request.Sort.Any())
            query = query.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.CreateDate);

        var dataSource = query.ToDataSourceResult(request);
        var pageEntities = dataSource.Data.Cast<TbtAnnouncement>().ToList();

        var now = DateTime.UtcNow;
        var items = pageEntities.Select(x => MapToItem(x, now)).ToList();

        dataSource.Data = items;
        return dataSource;
    }

    public async Task<CreateAnnouncementResponse> Create(CreateAnnouncementRequest request)
    {
        var title = ValidateTitle(request.Title);

        var publishStart = request.PublishStart?.UtcDateTime ?? DateTime.UtcNow;
        var publishEnd = request.PublishEnd?.UtcDateTime;

        if (publishEnd.HasValue && publishEnd.Value < publishStart)
            throw new HandleException("วันสิ้นสุดต้องไม่ก่อนวันเริ่มแสดง");

        string? imagePath = null;
        if (request.Image != null)
            imagePath = await UploadImage(request.Image);

        var entity = new TbtAnnouncement
        {
            Title = title,
            Body = request.Body,
            ImagePath = imagePath,
            IsPinned = request.IsPinned,
            PublishStart = publishStart,
            PublishEnd = publishEnd,
            IsPublished = request.IsPublished,
            IsActive = true,
            CreateDate = DateTime.UtcNow,
            CreateBy = CurrentUsername
        };

        _jewelryContext.TbtAnnouncement.Add(entity);
        await _jewelryContext.SaveChangesAsync();

        return new CreateAnnouncementResponse { Id = entity.Id };
    }

    public async Task<string> Update(UpdateAnnouncementRequest request)
    {
        var entity = await _jewelryContext.TbtAnnouncement
            .Where(x => x.Id == request.Id && x.IsActive)
            .SingleOrDefaultAsync();

        if (entity == null)
            throw new HandleException("ไม่พบประกาศ");

        var title = ValidateTitle(request.Title);

        var publishStart = request.PublishStart?.UtcDateTime ?? entity.PublishStart;
        var publishEnd = request.PublishEnd?.UtcDateTime;

        if (publishEnd.HasValue && publishEnd.Value < publishStart)
            throw new HandleException("วันสิ้นสุดต้องไม่ก่อนวันเริ่มแสดง");

        var oldImagePath = entity.ImagePath;

        entity.Title = title;
        entity.Body = request.Body;
        entity.IsPinned = request.IsPinned;
        entity.PublishStart = publishStart;
        entity.PublishEnd = publishEnd;
        entity.IsPublished = request.IsPublished;

        if (request.RemoveImage)
            entity.ImagePath = null;

        if (request.Image != null)
            entity.ImagePath = await UploadImage(request.Image);

        entity.UpdateDate = DateTime.UtcNow;
        entity.UpdateBy = CurrentUsername;

        _jewelryContext.TbtAnnouncement.Update(entity);
        await _jewelryContext.SaveChangesAsync();

        if (!string.IsNullOrEmpty(oldImagePath) && oldImagePath != entity.ImagePath)
            await TryDeleteBlob(oldImagePath);

        return "success";
    }

    public async Task<string> TogglePublish(TogglePublishAnnouncementRequest request)
    {
        var entity = await _jewelryContext.TbtAnnouncement
            .Where(x => x.Id == request.Id && x.IsActive)
            .SingleOrDefaultAsync();

        if (entity == null)
            throw new HandleException("ไม่พบประกาศ");

        entity.IsPublished = request.IsPublished;
        entity.UpdateDate = DateTime.UtcNow;
        entity.UpdateBy = CurrentUsername;

        _jewelryContext.TbtAnnouncement.Update(entity);
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    public async Task<string> Delete(DeleteAnnouncementRequest request)
    {
        var entity = await _jewelryContext.TbtAnnouncement
            .Where(x => x.Id == request.Id && x.IsActive)
            .SingleOrDefaultAsync();

        if (entity == null)
            throw new HandleException("ไม่พบประกาศ");

        entity.IsActive = false;
        entity.UpdateDate = DateTime.UtcNow;
        entity.UpdateBy = CurrentUsername;

        _jewelryContext.TbtAnnouncement.Update(entity);
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    private static string ValidateTitle(string? title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
            throw new HandleException("กรุณาระบุหัวข้อประกาศ");
        if (trimmed.Length > 200)
            throw new HandleException("หัวข้อประกาศต้องไม่เกิน 200 ตัวอักษร");
        return trimmed;
    }

    private async Task<string> UploadImage(IFormFile image)
    {
        if (string.IsNullOrEmpty(image.ContentType) || !image.ContentType.StartsWith("image/"))
            throw new HandleException("ไฟล์ต้องเป็นรูปภาพเท่านั้น");

        if (image.Length > 5 * 1024 * 1024)
            throw new HandleException("ขนาดรูปภาพต้องไม่เกิน 5 MB");

        var ext = System.IO.Path.GetExtension(image.FileName);
        var fileName = $"{Guid.NewGuid():N}{ext}";

        using var stream = image.OpenReadStream();
        var result = await _blobStorage.UploadImageAsync(stream, "Announcement", fileName);

        if (!result.Success)
            throw new HandleException(result.ErrorMessage ?? "อัปโหลดรูปภาพไม่สำเร็จ");

        return result.Url;
    }

    private async Task TryDeleteBlob(string imagePath)
    {
        try
        {
            var uri = new Uri(imagePath);
            var segments = uri.AbsolutePath.TrimStart('/').Split('/');
            if (segments.Length >= 3)
            {
                var fileName = segments[^1];
                var folderName = segments[^2];
                await _blobStorage.DeleteImageAsync(folderName, fileName);
            }
        }
        catch
        {
            // best-effort; ignore deletion failure
        }
    }

    private static AnnouncementItemResponse MapToItem(TbtAnnouncement x, DateTime now)
    {
        return new AnnouncementItemResponse
        {
            Id = x.Id,
            Title = x.Title,
            Body = x.Body,
            ImageUrl = x.ImagePath,
            IsPinned = x.IsPinned,
            PublishStart = x.PublishStart,
            PublishEnd = x.PublishEnd,
            IsPublished = x.IsPublished,
            DisplayStatus = GetDisplayStatus(x, now),
            CreateDate = x.CreateDate,
            CreateBy = x.CreateBy,
            UpdateDate = x.UpdateDate,
            UpdateBy = x.UpdateBy
        };
    }

    private static string GetDisplayStatus(TbtAnnouncement x, DateTime now)
    {
        if (!x.IsPublished) return "Hidden";
        if (x.PublishStart > now) return "Scheduled";
        if (x.PublishEnd != null && x.PublishEnd < now) return "Expired";
        return "Visible";
    }
}
