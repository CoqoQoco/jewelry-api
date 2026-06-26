using jewelry.Model.Exceptions;
using jewelry.Model.Ticket;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Ticket;

public class TicketService : BaseService, ITicketService
{
    private readonly JewelryContext _jewelryContext;
    private readonly IRunningNumber _runningNumber;
    private readonly IAzureBlobStorageService _blobStorage;

    public TicketService(
        JewelryContext jewelryContext,
        IHttpContextAccessor httpContextAccessor,
        IRunningNumber runningNumber,
        IAzureBlobStorageService blobStorage)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
        _runningNumber = runningNumber;
        _blobStorage = blobStorage;
    }

    public async Task<string> CreateTicket(CreateTicketRequest request)
    {
        var ticketNo = await _runningNumber.GenerateRunningNumber("TK");

        string? screenshotUrl = null;
        if (request.Images != null && request.Images.Count > 0)
        {
            var firstImage = request.Images[0];
            var ext = System.IO.Path.GetExtension(firstImage.FileName);
            var fileName = $"{ticketNo}{ext}";
            using var stream = firstImage.OpenReadStream();
            var uploadResult = await _blobStorage.UploadImageAsync(stream, "Ticket", fileName);
            if (!uploadResult.Success)
                throw new HandleException(uploadResult.ErrorMessage ?? "อัปโหลดรูปภาพไม่สำเร็จ");
            screenshotUrl = uploadResult.Url;
        }

        var ticket = new TbtTicket
        {
            TicketNo = ticketNo,
            Type = request.Type,
            TopicRoute = request.TopicRoute,
            TopicName = request.TopicName,
            Title = request.Title,
            Description = request.Description,
            ScreenshotUrl = screenshotUrl,
            Status = 1,
            CreateDate = DateTime.UtcNow,
            CreateBy = CurrentUsername
        };

        _jewelryContext.TbtTicket.Add(ticket);
        await _jewelryContext.SaveChangesAsync();

        if (request.Images != null && request.Images.Count > 0)
        {
            var createImages = new List<TbtTicketImage>();
            int n = 1;

            foreach (var image in request.Images)
            {
                string fileName = $"{ticketNo}-{n}.png";
                using var stream = image.OpenReadStream();
                var result = await _blobStorage.UploadImageAsync(stream, "Ticket", fileName);

                if (!result.Success)
                    throw new HandleException(result.ErrorMessage ?? "อัปโหลดรูปภาพไม่สำเร็จ");

                createImages.Add(new TbtTicketImage
                {
                    TicketId = ticket.Id,
                    Number = n,
                    Path = result.Url,
                    IsActive = true,
                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername
                });

                n++;
            }

            _jewelryContext.TbtTicketImage.AddRange(createImages);
            await _jewelryContext.SaveChangesAsync();
        }

        return ticketNo;
    }

    public async Task<DataSourceResult> SearchTicket(SearchTicketRequest request)
    {
        var query = _jewelryContext.TbtTicket
            .AsNoTracking()
            .Include(x => x.StatusNavigation)
            .AsQueryable();

        if (request.TicketId.HasValue)
            query = query.Where(x => x.Id == request.TicketId.Value);

        if (request.Status != null && request.Status.Any())
            query = query.Where(x => request.Status.Contains(x.Status));

        if (request.Type != null && request.Type.Any())
            query = query.Where(x => request.Type.Contains(x.Type));

        if (!string.IsNullOrEmpty(request.TopicRoute))
            query = query.Where(x => x.TopicRoute == request.TopicRoute);

        if (!string.IsNullOrEmpty(request.Keyword))
        {
            var keyword = request.Keyword;
            query = query.Where(x =>
                x.Title.Contains(keyword) ||
                x.Description.Contains(keyword) ||
                x.TicketNo.Contains(keyword) ||
                x.TopicName.Contains(keyword));
        }

        var orderedQuery = query.OrderByDescending(x => x.CreateDate);

        var dataSource = orderedQuery.ToDataSourceResult(request);
        var pageEntities = dataSource.Data.Cast<TbtTicket>().ToList();

        var ticketIds = pageEntities.Select(x => x.Id).ToList();

        var images = await _jewelryContext.TbtTicketImage
            .AsNoTracking()
            .Where(x => ticketIds.Contains(x.TicketId) && x.IsActive)
            .OrderBy(x => x.Number)
            .ToListAsync();

        var imagesByTicket = images.GroupBy(x => x.TicketId)
            .ToDictionary(g => g.Key, g => g.Select(i => i.Path).ToList());

        var analysisComments = await _jewelryContext.TbtTicketComment
            .AsNoTracking()
            .Where(c => ticketIds.Contains(c.TicketId) && c.Type == "analysis" && c.IsActive)
            .OrderByDescending(c => c.CreateDate)
            .ToListAsync();

        var latestAnalysisByTicket = analysisComments
            .GroupBy(c => c.TicketId)
            .ToDictionary(g => g.Key, g => g.First().Message);

        Dictionary<long, List<TicketLogResponse>> logsByTicket = new();
        Dictionary<long, List<TicketCommentResponse>> commentsByTicket = new();
        if (request.TicketId.HasValue)
        {
            var logs = await _jewelryContext.TbtTicketLog
                .AsNoTracking()
                .Where(x => ticketIds.Contains(x.TicketId))
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();

            logsByTicket = logs.GroupBy(x => x.TicketId)
                .ToDictionary(g => g.Key, g => g.Select(l => new TicketLogResponse
                {
                    Id = l.Id,
                    Action = l.Action,
                    Detail = l.Detail,
                    OldValue = l.OldValue,
                    NewValue = l.NewValue,
                    CreateBy = l.CreateBy,
                    CreateDate = l.CreateDate
                }).ToList());

            var comments = await _jewelryContext.TbtTicketComment
                .AsNoTracking()
                .Where(x => ticketIds.Contains(x.TicketId) && x.IsActive)
                .OrderBy(x => x.CreateDate)
                .ToListAsync();

            commentsByTicket = comments.GroupBy(x => x.TicketId)
                .ToDictionary(g => g.Key, g => g.Select(c => new TicketCommentResponse
                {
                    Id = c.Id,
                    Type = c.Type,
                    AuthorRole = c.AuthorRole,
                    Message = c.Message,
                    CreateBy = c.CreateBy,
                    CreateDate = c.CreateDate
                }).ToList());
        }

        var pageItems = pageEntities.Select(x => new TicketListResponse
        {
            Id = x.Id,
            TicketNo = x.TicketNo,
            Type = x.Type,
            TopicRoute = x.TopicRoute,
            TopicName = x.TopicName,
            Title = x.Title,
            Description = x.Description,
            ScreenshotUrl = x.ScreenshotUrl,
            StatusId = x.Status,
            StatusNameTh = x.StatusNavigation.NameTh,
            StatusNameEn = x.StatusNavigation.NameEn,
            DevAnalysis = x.DevAnalysis,
            DevResponse = x.DevResponse,
            LatestAnalysis = latestAnalysisByTicket.TryGetValue(x.Id, out var latestAnalysis) ? latestAnalysis : x.DevAnalysis,
            CreateBy = x.CreateBy,
            CreateDate = x.CreateDate,
            UpdateDate = x.UpdateDate,
            UpdateBy = x.UpdateBy,
            ImageUrls = imagesByTicket.TryGetValue(x.Id, out var urls) ? urls : new List<string>(),
            Logs = logsByTicket.TryGetValue(x.Id, out var ticketLogs) ? ticketLogs : new List<TicketLogResponse>(),
            Comments = commentsByTicket.TryGetValue(x.Id, out var ticketComments) ? ticketComments : new List<TicketCommentResponse>()
        }).ToList();

        dataSource.Data = pageItems;
        return dataSource;
    }

    public async Task<DataSourceResult> GetMyTickets(SearchTicketRequest request)
    {
        var username = CurrentUsername;

        var query = _jewelryContext.TbtTicket
            .AsNoTracking()
            .Include(x => x.StatusNavigation)
            .Where(x => x.CreateBy == username)
            .AsQueryable();

        if (request.TicketId.HasValue)
            query = query.Where(x => x.Id == request.TicketId.Value);

        if (request.Status != null && request.Status.Any())
            query = query.Where(x => request.Status.Contains(x.Status));

        if (request.Type != null && request.Type.Any())
            query = query.Where(x => request.Type.Contains(x.Type));

        if (!string.IsNullOrEmpty(request.TopicRoute))
            query = query.Where(x => x.TopicRoute == request.TopicRoute);

        if (!string.IsNullOrEmpty(request.Keyword))
        {
            var keyword = request.Keyword;
            query = query.Where(x =>
                x.Title.Contains(keyword) ||
                x.Description.Contains(keyword) ||
                x.TicketNo.Contains(keyword) ||
                x.TopicName.Contains(keyword));
        }

        var orderedQuery = query.OrderByDescending(x => x.CreateDate);

        var dataSource = orderedQuery.ToDataSourceResult(request);
        var pageEntities = dataSource.Data.Cast<TbtTicket>().ToList();

        var ticketIds = pageEntities.Select(x => x.Id).ToList();

        var images2 = await _jewelryContext.TbtTicketImage
            .AsNoTracking()
            .Where(x => ticketIds.Contains(x.TicketId) && x.IsActive)
            .OrderBy(x => x.Number)
            .ToListAsync();

        var imagesByTicket2 = images2.GroupBy(x => x.TicketId)
            .ToDictionary(g => g.Key, g => g.Select(i => i.Path).ToList());

        var logs2 = await _jewelryContext.TbtTicketLog
            .AsNoTracking()
            .Where(x => ticketIds.Contains(x.TicketId))
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync();

        var logsByTicket2 = logs2.GroupBy(x => x.TicketId)
            .ToDictionary(g => g.Key, g => g.Select(l => new TicketLogResponse
            {
                Id = l.Id,
                Action = l.Action,
                Detail = l.Detail,
                OldValue = l.OldValue,
                NewValue = l.NewValue,
                CreateBy = l.CreateBy,
                CreateDate = l.CreateDate
            }).ToList());

        var myCommentTypes = new[] { "response", "change" };
        var myComments = await _jewelryContext.TbtTicketComment
            .AsNoTracking()
            .Where(x => ticketIds.Contains(x.TicketId) && x.IsActive && myCommentTypes.Contains(x.Type))
            .OrderBy(x => x.CreateDate)
            .ToListAsync();

        var myCommentsByTicket = myComments.GroupBy(x => x.TicketId)
            .ToDictionary(g => g.Key, g => g.Select(c => new TicketCommentResponse
            {
                Id = c.Id,
                Type = c.Type,
                AuthorRole = c.AuthorRole,
                Message = c.Message,
                CreateBy = c.CreateBy,
                CreateDate = c.CreateDate
            }).ToList());

        var pageItems2 = pageEntities.Select(x => new TicketListResponse
        {
            Id = x.Id,
            TicketNo = x.TicketNo,
            Type = x.Type,
            TopicRoute = x.TopicRoute,
            TopicName = x.TopicName,
            Title = x.Title,
            Description = x.Description,
            ScreenshotUrl = x.ScreenshotUrl,
            StatusId = x.Status,
            StatusNameTh = x.StatusNavigation.NameTh,
            StatusNameEn = x.StatusNavigation.NameEn,
            DevAnalysis = x.DevAnalysis,
            DevResponse = x.DevResponse,
            CreateBy = x.CreateBy,
            CreateDate = x.CreateDate,
            UpdateDate = x.UpdateDate,
            UpdateBy = x.UpdateBy,
            ImageUrls = imagesByTicket2.TryGetValue(x.Id, out var urls2) ? urls2 : new List<string>(),
            Logs = logsByTicket2.TryGetValue(x.Id, out var ticketLogs2) ? ticketLogs2 : new List<TicketLogResponse>(),
            Comments = myCommentsByTicket.TryGetValue(x.Id, out var myTicketComments) ? myTicketComments : new List<TicketCommentResponse>()
        }).ToList();

        dataSource.Data = pageItems2;
        return dataSource;
    }

    public async Task<string> UpdateTicketStatus(UpdateTicketStatusRequest request)
    {
        var ticket = _jewelryContext.TbtTicket
            .Where(x => x.Id == request.TicketId)
            .SingleOrDefault();

        if (ticket == null)
            throw new HandleException($"ไม่พบ Ticket รหัส {request.TicketId}");

        var oldStatus = ticket.Status;

        ticket.Status = request.Status;
        ticket.UpdateDate = DateTime.UtcNow;
        ticket.UpdateBy = CurrentUsername;

        _jewelryContext.TbtTicket.Update(ticket);
        _jewelryContext.TbtTicketLog.Add(BuildLog(ticket.Id, "status", null, oldStatus.ToString(), request.Status.ToString()));
        _jewelryContext.TbtTicketComment.Add(BuildComment(ticket.Id, "change", "system", "เปลี่ยนสถานะ"));
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    public async Task<string> UpdateTicketDev(UpdateTicketDevRequest request)
    {
        var ticket = _jewelryContext.TbtTicket
            .Where(x => x.Id == request.TicketId)
            .SingleOrDefault();

        if (ticket == null)
            throw new HandleException($"ไม่พบ Ticket รหัส {request.TicketId}");

        ticket.DevAnalysis = request.DevAnalysis;
        ticket.DevResponse = request.DevResponse;
        ticket.UpdateDate = DateTime.UtcNow;
        ticket.UpdateBy = CurrentUsername;

        _jewelryContext.TbtTicket.Update(ticket);
        _jewelryContext.TbtTicketLog.Add(BuildLog(ticket.Id, "dev", "อัปเดตผลวิเคราะห์ / คำตอบถึงผู้แจ้ง", null, null));
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    public async Task<string> AddTicketLog(AddTicketLogRequest request)
    {
        var ticket = _jewelryContext.TbtTicket
            .Where(x => x.Id == request.TicketId)
            .SingleOrDefault();

        if (ticket == null)
            throw new HandleException($"ไม่พบ Ticket รหัส {request.TicketId}");

        _jewelryContext.TbtTicketLog.Add(BuildLog(request.TicketId, "note", request.Detail, null, null));
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    public async Task<string> AddTicketComment(AddTicketCommentRequest request)
    {
        var validTypes = new[] { "analysis", "response", "change" };
        if (!validTypes.Contains(request.Type))
            throw new HandleException($"Type ไม่ถูกต้อง ต้องเป็น analysis, response หรือ change");

        var ticket = _jewelryContext.TbtTicket
            .Where(x => x.Id == request.TicketId)
            .SingleOrDefault();

        if (ticket == null)
            throw new HandleException($"ไม่พบ Ticket รหัส {request.TicketId}");

        _jewelryContext.TbtTicketComment.Add(BuildComment(request.TicketId, request.Type, "dev", request.Message));
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    public async Task<string> AddMyTicketComment(AddMyTicketCommentRequest request)
    {
        var ticket = _jewelryContext.TbtTicket
            .Where(x => x.Id == request.TicketId)
            .SingleOrDefault();

        if (ticket == null)
            throw new HandleException("ไม่พบ Ticket");

        if (ticket.CreateBy != CurrentUsername)
            throw new HandleException("ไม่มีสิทธิ์แสดงความคิดเห็นใน ticket นี้");

        _jewelryContext.TbtTicketComment.Add(BuildComment(request.TicketId, "response", "user", request.Message));
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    public async Task<string> DeleteTicketComment(DeleteTicketCommentRequest request)
    {
        var comment = await _jewelryContext.TbtTicketComment
            .Where(x => x.Id == request.CommentId && x.IsActive)
            .SingleOrDefaultAsync();

        if (comment == null)
            throw new HandleException("ไม่พบความคิดเห็น");

        if (comment.AuthorRole == "system")
            throw new HandleException("ไม่สามารถลบรายการที่ระบบสร้างได้");

        comment.IsActive = false;
        comment.UpdateDate = DateTime.UtcNow;
        comment.UpdateBy = CurrentUsername;

        _jewelryContext.TbtTicketComment.Update(comment);
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    public async Task<string> DeleteMyTicketComment(DeleteTicketCommentRequest request)
    {
        var comment = await _jewelryContext.TbtTicketComment
            .Where(x => x.Id == request.CommentId && x.IsActive)
            .SingleOrDefaultAsync();

        if (comment == null)
            throw new HandleException("ไม่พบความคิดเห็น");

        if (!(comment.AuthorRole == "user" && comment.CreateBy == CurrentUsername))
            throw new HandleException("ไม่มีสิทธิ์ลบความคิดเห็นนี้");

        comment.IsActive = false;
        comment.UpdateDate = DateTime.UtcNow;
        comment.UpdateBy = CurrentUsername;

        _jewelryContext.TbtTicketComment.Update(comment);
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    public async Task<int> CountOpen()
    {
        return await _jewelryContext.TbtTicket.CountAsync(x => x.Status == 1 || x.Status == 2);
    }

    private TbtTicketLog BuildLog(long ticketId, string action, string? detail, string? oldVal, string? newVal) => new TbtTicketLog
    {
        TicketId = ticketId,
        Action = action,
        Detail = detail,
        OldValue = oldVal,
        NewValue = newVal,
        CreateDate = DateTime.UtcNow,
        CreateBy = CurrentUsername
    };

    private TbtTicketComment BuildComment(long ticketId, string type, string authorRole, string message) => new TbtTicketComment
    {
        TicketId = ticketId,
        Type = type,
        AuthorRole = authorRole,
        Message = message,
        IsActive = true,
        CreateDate = DateTime.UtcNow,
        CreateBy = CurrentUsername
    };
}
