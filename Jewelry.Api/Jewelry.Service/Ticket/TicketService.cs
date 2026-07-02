using jewelry.Model.Exceptions;
using jewelry.Model.Ticket;
using jewelry.Model.Ticket.Dashboard;
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

        var devUsername = CurrentUsername;

        var readStatuses = await _jewelryContext.TbtTicketReadStatus
            .AsNoTracking()
            .Where(x => ticketIds.Contains(x.TicketId) && x.Username == devUsername)
            .ToListAsync();

        var readByTicket = readStatuses.ToDictionary(x => x.TicketId, x => x.LastReadDate);

        var latestUserCommentDate = await _jewelryContext.TbtTicketComment
            .AsNoTracking()
            .Where(x => ticketIds.Contains(x.TicketId) && x.IsActive && x.AuthorRole == "user")
            .GroupBy(x => x.TicketId)
            .Select(g => new { TicketId = g.Key, MaxDate = g.Max(c => c.CreateDate) })
            .ToListAsync();

        var latestUserDateByTicket = latestUserCommentDate.ToDictionary(x => x.TicketId, x => x.MaxDate);

        var pageItems = pageEntities.Select(x =>
        {
            var lastRead = readByTicket.TryGetValue(x.Id, out var lr) ? lr : DateTime.MinValue;
            var hasNew = latestUserDateByTicket.TryGetValue(x.Id, out var maxUser) && maxUser > lastRead;
            return new TicketListResponse
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
                HasNewMessage = hasNew,
                ImageUrls = imagesByTicket.TryGetValue(x.Id, out var urls) ? urls : new List<string>(),
                Logs = logsByTicket.TryGetValue(x.Id, out var ticketLogs) ? ticketLogs : new List<TicketLogResponse>(),
                Comments = commentsByTicket.TryGetValue(x.Id, out var ticketComments) ? ticketComments : new List<TicketCommentResponse>()
            };
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

        var readStatuses2 = await _jewelryContext.TbtTicketReadStatus
            .AsNoTracking()
            .Where(x => ticketIds.Contains(x.TicketId) && x.Username == username)
            .ToListAsync();

        var readByTicket2 = readStatuses2.ToDictionary(x => x.TicketId, x => x.LastReadDate);

        var latestDevCommentDate2 = await _jewelryContext.TbtTicketComment
            .AsNoTracking()
            .Where(x => ticketIds.Contains(x.TicketId) && x.IsActive && x.AuthorRole != "user")
            .GroupBy(x => x.TicketId)
            .Select(g => new { TicketId = g.Key, MaxDate = g.Max(c => c.CreateDate) })
            .ToListAsync();

        var latestDevDateByTicket2 = latestDevCommentDate2.ToDictionary(x => x.TicketId, x => x.MaxDate);

        var pageItems2 = pageEntities.Select(x =>
        {
            var lastRead2 = readByTicket2.TryGetValue(x.Id, out var lr2) ? lr2 : DateTime.MinValue;
            var hasNew2 = latestDevDateByTicket2.TryGetValue(x.Id, out var maxDev2) && maxDev2 > lastRead2;
            return new TicketListResponse
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
                HasNewMessage = hasNew2,
                ImageUrls = imagesByTicket2.TryGetValue(x.Id, out var urls2) ? urls2 : new List<string>(),
                Logs = logsByTicket2.TryGetValue(x.Id, out var ticketLogs2) ? ticketLogs2 : new List<TicketLogResponse>(),
                Comments = myCommentsByTicket.TryGetValue(x.Id, out var myTicketComments) ? myTicketComments : new List<TicketCommentResponse>()
            };
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

    public async Task<TicketDashboardResponse> GetTicketDashboard(TicketDashboardRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var startDate = request.StartDate?.StartOfDayUtc() ?? now.AddDays(-30).StartOfDayUtc();
        var endDate = request.EndDate?.EndOfDayUtc() ?? now.EndOfDayUtc();

        var response = new TicketDashboardResponse();

        response.Summary = await GetTicketSummary();
        response.ByStatus = await GetTicketByStatus();
        response.ByTopic = await GetTicketByTopic();
        response.Trend = await GetTicketTrend(startDate, endDate);
        response.Aging = await GetTicketAging();

        return response;
    }

    private async Task<TicketSummary> GetTicketSummary()
    {
        var summary = await _jewelryContext.TbtTicket
            .GroupBy(x => 1)
            .Select(g => new TicketSummary
            {
                Total = g.Count(),
                Open = g.Count(x => x.Status == 1),
                InProgress = g.Count(x => x.Status == 2),
                Resolved = g.Count(x => x.Status == 3),
                Closed = g.Count(x => x.Status == 4),
                Cancelled = g.Count(x => x.Status == 5),
                Bug = g.Count(x => x.Type == 1),
                Feature = g.Count(x => x.Type == 2),
                Unanalyzed = g.Count(x => x.DevAnalysis == null && (x.Status == 1 || x.Status == 2))
            })
            .FirstOrDefaultAsync();

        if (summary == null)
            return new TicketSummary();

        summary.ResolvedRate = summary.Total == 0
            ? 0
            : Math.Round((decimal)summary.Resolved / summary.Total * 100, 2);

        return summary;
    }

    private async Task<List<TicketStatusBreakdown>> GetTicketByStatus()
    {
        return await (from t in _jewelryContext.TbtTicket
                      join s in _jewelryContext.TbmTicketStatus on t.Status equals s.Id
                      group new { t, s } by new { t.Status, s.NameTh, s.NameEn } into g
                      select new TicketStatusBreakdown
                      {
                          StatusId = g.Key.Status,
                          NameTh = g.Key.NameTh,
                          NameEn = g.Key.NameEn,
                          Count = g.Count()
                      })
            .Where(x => x.Count > 0)
            .OrderBy(x => x.StatusId)
            .ToListAsync();
    }

    private async Task<List<TicketTopicBreakdown>> GetTicketByTopic()
    {
        return await _jewelryContext.TbtTicket
            .GroupBy(x => new { x.TopicRoute, x.TopicName })
            .Select(g => new TicketTopicBreakdown
            {
                TopicRoute = g.Key.TopicRoute,
                TopicName = g.Key.TopicName,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
    }

    private async Task<List<TicketTrend>> GetTicketTrend(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        return await _jewelryContext.TbtTicket
            .Where(x => x.CreateDate >= startDate.UtcDateTime && x.CreateDate <= endDate.UtcDateTime)
            .GroupBy(x => x.CreateDate.Date)
            .Select(g => new TicketTrend
            {
                Date = g.Key,
                Created = g.Count(),
                Resolved = g.Count(x => x.Status == 3)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    private async Task<TicketAging> GetTicketAging()
    {
        var nowDate = DateTime.UtcNow.Date;
        var createDates = await _jewelryContext.TbtTicket
            .Where(x => x.Status == 1 || x.Status == 2)
            .Select(x => x.CreateDate)
            .ToListAsync();

        var aging = new TicketAging();
        foreach (var createDate in createDates)
        {
            var days = (nowDate - createDate.Date).Days;
            if (days == 0) aging.Today++;
            else if (days >= 1 && days <= 3) aging.Days1To3++;
            else if (days >= 4 && days <= 7) aging.Days4To7++;
            else aging.Over7++;
        }

        return aging;
    }

    public async Task MarkTicketAsRead(long ticketId)
    {
        var username = CurrentUsername;
        var existing = await _jewelryContext.TbtTicketReadStatus
            .Where(x => x.TicketId == ticketId && x.Username == username)
            .SingleOrDefaultAsync();

        if (existing != null)
        {
            existing.LastReadDate = DateTime.UtcNow;
            _jewelryContext.TbtTicketReadStatus.Update(existing);
        }
        else
        {
            _jewelryContext.TbtTicketReadStatus.Add(new TbtTicketReadStatus
            {
                TicketId = ticketId,
                Username = username,
                LastReadDate = DateTime.UtcNow
            });
        }

        await _jewelryContext.SaveChangesAsync();
    }

    public async Task<int> CountMyUnread()
    {
        var username = CurrentUsername;

        var ticketIds = await _jewelryContext.TbtTicket
            .AsNoTracking()
            .Where(x => x.CreateBy == username)
            .Select(x => x.Id)
            .ToListAsync();

        if (ticketIds.Count == 0) return 0;

        var readByTicket = (await _jewelryContext.TbtTicketReadStatus
            .AsNoTracking()
            .Where(x => ticketIds.Contains(x.TicketId) && x.Username == username)
            .ToListAsync())
            .ToDictionary(x => x.TicketId, x => x.LastReadDate);

        var latestDevByTicket = (await _jewelryContext.TbtTicketComment
            .AsNoTracking()
            .Where(x => ticketIds.Contains(x.TicketId) && x.IsActive && x.AuthorRole != "user")
            .GroupBy(x => x.TicketId)
            .Select(g => new { TicketId = g.Key, MaxDate = g.Max(c => c.CreateDate) })
            .ToListAsync())
            .ToDictionary(x => x.TicketId, x => x.MaxDate);

        return ticketIds.Count(id =>
        {
            var lastRead = readByTicket.TryGetValue(id, out var lr) ? lr : DateTime.MinValue;
            return latestDevByTicket.TryGetValue(id, out var maxDev) && maxDev > lastRead;
        });
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
