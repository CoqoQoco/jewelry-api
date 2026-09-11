using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Notification;

public class NotificationService : BaseService, INotificationService
{
    private readonly JewelryContext _jewelryContext;
    private readonly NotificationRuleRunner _ruleRunner;

    public NotificationService(
        JewelryContext jewelryContext,
        IHttpContextAccessor httpContextAccessor,
        NotificationRuleRunner ruleRunner)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
        _ruleRunner = ruleRunner;
    }

    public async Task<jewelry.Model.Notification.MyCount.Response> MyCount()
    {
        await _ruleRunner.RunDueRules();

        var username = CurrentUsername;
        var now = DateTime.UtcNow;

        var modules = await (from n in _jewelryContext.TbtNotification.AsNoTracking()
                              join t in _jewelryContext.TbmNotificationType.AsNoTracking() on n.TypeCode equals t.Code
                              where n.RecipientUsername == username
                                 && (n.State == "NEW" || (n.State == "SNOOZED" && n.SnoozeUntil <= now))
                              select t.Module)
                              .ToListAsync();

        var grouped = modules
            .GroupBy(x => x)
            .Select(g => new jewelry.Model.Notification.MyCount.ModuleCount { Module = g.Key, Count = g.Count() })
            .ToList();

        return new jewelry.Model.Notification.MyCount.Response
        {
            Total = modules.Count,
            List = grouped
        };
    }

    public Task<jewelry.Model.Notification.MyList.Response> MyList(jewelry.Model.Notification.MyList.Request request)
    {
        var username = CurrentUsername;
        var notifications = _jewelryContext.TbtNotification.AsNoTracking()
            .Where(x => x.RecipientUsername == username);

        return QueryList(notifications, request.IncludeClosed, request.Module, request.TypeCode, request.Take, request.Skip);
    }

    public async Task<string> MarkRead(jewelry.Model.Notification.MarkRead.Request request)
    {
        var username = CurrentUsername;
        var now = DateTime.UtcNow;

        var query = _jewelryContext.TbtNotification.Where(x => x.RecipientUsername == username);

        if (request.All)
        {
            query = query.Where(x => x.State == "NEW");
        }
        else
        {
            var ids = request.Ids ?? Array.Empty<long>();
            query = query.Where(x => ids.Contains(x.Id));
        }

        var rows = await query.ToListAsync();

        foreach (var row in rows)
        {
            row.State = "READ";
            row.ReadDate = now;
            row.UpdateDate = now;
            row.UpdateBy = username;
        }

        _jewelryContext.TbtNotification.UpdateRange(rows);
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    public async Task<string> MarkDone(jewelry.Model.Notification.MarkDone.Request request)
    {
        var username = CurrentUsername;

        var row = await _jewelryContext.TbtNotification
            .Where(x => x.Id == request.Id && x.RecipientUsername == username)
            .SingleOrDefaultAsync();

        if (row == null)
            throw new HandleException("ไม่พบรายการแจ้งเตือน");

        var now = DateTime.UtcNow;
        row.State = "DONE";
        row.DoneDate = now;
        row.UpdateDate = now;
        row.UpdateBy = username;

        _jewelryContext.TbtNotification.Update(row);
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    public async Task<string> Snooze(jewelry.Model.Notification.Snooze.Request request)
    {
        var username = CurrentUsername;

        var row = await _jewelryContext.TbtNotification
            .Where(x => x.Id == request.Id && x.RecipientUsername == username)
            .SingleOrDefaultAsync();

        if (row == null)
            throw new HandleException("ไม่พบรายการแจ้งเตือน");

        var now = DateTime.UtcNow;
        row.State = "SNOOZED";
        row.SnoozeUntil = now.AddDays(request.Days);
        row.UpdateDate = now;
        row.UpdateBy = username;

        _jewelryContext.TbtNotification.Update(row);
        await _jewelryContext.SaveChangesAsync();

        return "success";
    }

    public Task<jewelry.Model.Notification.MyList.Response> TeamList(jewelry.Model.Notification.TeamList.Request request)
    {
        var notifications = _jewelryContext.TbtNotification.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(request.RecipientUsername))
            notifications = notifications.Where(x => x.RecipientUsername == request.RecipientUsername);

        return QueryList(notifications, request.IncludeClosed, request.Module, request.TypeCode, request.Take, request.Skip);
    }

    private async Task<jewelry.Model.Notification.MyList.Response> QueryList(
        IQueryable<TbtNotification> notifications,
        bool includeClosed,
        string? module,
        string? typeCode,
        int take,
        int skip)
    {
        var now = DateTime.UtcNow;

        var query = from n in notifications
                     join t in _jewelryContext.TbmNotificationType.AsNoTracking() on n.TypeCode equals t.Code
                     select new { n, t.NameTh, t.Module, t.Icon };

        if (!includeClosed)
            query = query.Where(x => x.n.State != "DONE" && x.n.State != "AUTO_CLOSED");

        if (!string.IsNullOrEmpty(module))
            query = query.Where(x => x.Module == module);

        if (!string.IsNullOrEmpty(typeCode))
            query = query.Where(x => x.n.TypeCode == typeCode);

        var total = await query.CountAsync();

        var matched = await query.ToListAsync();

        var clampedTake = ClampTake(take);
        var clampedSkip = skip < 0 ? 0 : skip;

        var items = matched
            .Select(x => MapToItem(x.n, x.NameTh, x.Module, x.Icon, now))
            .OrderByDescending(x => x.OverdueDays)
            .ThenByDescending(x => x.EventDate)
            .Skip(clampedSkip)
            .Take(clampedTake)
            .ToList();

        return new jewelry.Model.Notification.MyList.Response
        {
            Total = total,
            List = items
        };
    }

    private static int ClampTake(int take)
    {
        if (take < 1) return 1;
        if (take > 100) return 100;
        return take;
    }

    private static jewelry.Model.Notification.MyList.Item MapToItem(TbtNotification n, string typeName, string module, string? icon, DateTime now)
    {
        var overdueDays = n.DueDate.HasValue ? (now.Date - n.DueDate.Value.Date).Days : 0;

        return new jewelry.Model.Notification.MyList.Item
        {
            Id = n.Id,
            TypeCode = n.TypeCode,
            TypeName = typeName,
            Module = module,
            Icon = icon,
            Title = n.Title,
            Body = n.Body,
            RefDocType = n.RefDocType,
            RefDocNo = n.RefDocNo,
            ActionUrl = n.ActionUrl,
            Severity = n.Severity,
            Amount = n.Amount,
            CurrencyUnit = n.CurrencyUnit,
            EventDate = n.EventDate,
            DueDate = n.DueDate,
            OverdueDays = overdueDays,
            State = n.State,
            IsEscalated = n.IsEscalated,
            RecipientUsername = n.RecipientUsername
        };
    }
}
