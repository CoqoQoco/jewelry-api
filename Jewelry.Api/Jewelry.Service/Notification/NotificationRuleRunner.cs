using System.Collections.Concurrent;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Notification.Rules;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Notification;

// ระบบนี้ไม่มี background scheduler เลย (ไม่มี BackgroundService/Hangfire)
// runner ตัวนี้ถูกเรียกจาก NotificationService.MyCount() เท่านั้น — ประเมิน rule แบบ lazy
// ตอนมี request เข้ามา ไม่ใช่รันตามรอบเวลาแบบ cron
public class NotificationRuleRunner
{
    private static readonly ConcurrentDictionary<string, DateTime> _lastRunAt = new();
    private static readonly SemaphoreSlim _runLock = new(1, 1);
    private static readonly TimeSpan _minInterval = TimeSpan.FromMinutes(5);

    private readonly JewelryContext _jewelryContext;
    private readonly IEnumerable<INotificationRule> _rules;

    public NotificationRuleRunner(JewelryContext jewelryContext, IEnumerable<INotificationRule> rules)
    {
        _jewelryContext = jewelryContext;
        _rules = rules;
    }

    public async Task RunDueRules()
    {
        if (!await _runLock.WaitAsync(0))
            return;

        try
        {
            var activeTypes = await _jewelryContext.TbmNotificationType
                .Where(x => x.IsActive)
                .ToDictionaryAsync(x => x.Code);

            foreach (var rule in _rules)
            {
                if (!activeTypes.TryGetValue(rule.TypeCode, out var type))
                    continue;

                var now = DateTime.UtcNow;
                if (_lastRunAt.TryGetValue(rule.TypeCode, out var lastRun) && now - lastRun < _minInterval)
                    continue;

                await RunRule(rule, type);
                _lastRunAt[rule.TypeCode] = now;
            }
        }
        finally
        {
            _runLock.Release();
        }
    }

    private async Task RunRule(INotificationRule rule, TbmNotificationType type)
    {
        var candidates = await rule.Evaluate();

        var existingRows = await _jewelryContext.TbtNotification
            .Where(x => x.TypeCode == rule.TypeCode)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var seenRefDocNos = new HashSet<string>();

        List<string>? teamUsernames = null;
        if (type.EscalateDays.HasValue)
            teamUsernames = await GetTeamUsernames();

        foreach (var candidate in candidates)
        {
            seenRefDocNos.Add(candidate.RefDocNo);

            var overdueDays = GetOverdueDays(candidate.DueDate, now);
            var severity = ResolveSeverity(type, overdueDays);

            UpsertRow(existingRows, rule.TypeCode, candidate, candidate.RecipientUsername, severity, now, isEscalated: false);

            if (type.EscalateDays.HasValue && overdueDays >= type.EscalateDays.Value && teamUsernames != null)
            {
                foreach (var teamUsername in teamUsernames)
                {
                    if (string.Equals(teamUsername, candidate.RecipientUsername, StringComparison.OrdinalIgnoreCase))
                        continue;

                    UpsertRow(existingRows, rule.TypeCode, candidate, teamUsername, severity, now, isEscalated: true);
                }
            }
        }

        foreach (var row in existingRows)
        {
            if (row.RefDocNo != null && seenRefDocNos.Contains(row.RefDocNo))
                continue;
            if (row.State == "DONE" || row.State == "AUTO_CLOSED")
                continue;

            row.State = "AUTO_CLOSED";
            row.DoneDate = now;
            row.UpdateDate = now;
            row.UpdateBy = "system";
            _jewelryContext.TbtNotification.Update(row);
        }

        await _jewelryContext.SaveChangesAsync();
    }

    private void UpsertRow(List<TbtNotification> existingRows, string typeCode, NotificationCandidate candidate,
        string recipientUsername, string severity, DateTime now, bool isEscalated)
    {
        var row = existingRows.FirstOrDefault(x => x.RefDocNo == candidate.RefDocNo && x.RecipientUsername == recipientUsername);

        if (row == null)
        {
            row = new TbtNotification
            {
                TypeCode = typeCode,
                RecipientUsername = recipientUsername,
                Title = candidate.Title,
                Body = candidate.Body,
                RefDocType = candidate.RefDocType,
                RefDocNo = candidate.RefDocNo,
                ActionUrl = candidate.ActionUrl,
                Severity = severity,
                Amount = candidate.Amount,
                CurrencyUnit = candidate.CurrencyUnit,
                EventDate = now,
                DueDate = candidate.DueDate,
                State = "NEW",
                Source = "RULE",
                IsEscalated = isEscalated,
                CreateDate = now,
                CreateBy = "system"
            };

            _jewelryContext.TbtNotification.Add(row);
            existingRows.Add(row);
            return;
        }

        if (row.State == "DONE")
            return;

        row.Title = candidate.Title;
        row.Body = candidate.Body;
        row.Amount = candidate.Amount;
        row.DueDate = candidate.DueDate;
        row.Severity = severity;
        row.UpdateDate = now;
        row.UpdateBy = "system";

        _jewelryContext.TbtNotification.Update(row);
    }

    private async Task<List<string>> GetTeamUsernames()
    {
        return await (from ur in _jewelryContext.TbtUserRole
                       join rp in _jewelryContext.TbtRolePermission on ur.Role equals rp.RoleId
                       join perm in _jewelryContext.TbmPermission on rp.PermissionId equals perm.Id
                       join u in _jewelryContext.TbtUser on ur.Username equals u.Username
                       where perm.Code == "notification:team" && perm.IsActive && u.IsActive
                       select ur.Username)
                       .Distinct()
                       .ToListAsync();
    }

    private static int GetOverdueDays(DateTime? dueDate, DateTime now)
    {
        if (!dueDate.HasValue)
            return 0;

        return (now.Date - dueDate.Value.Date).Days;
    }

    private static string ResolveSeverity(TbmNotificationType type, int overdueDays)
    {
        if (type.SeverityCritDays.HasValue && overdueDays >= type.SeverityCritDays.Value)
            return "CRIT";
        if (type.SeverityWarnDays.HasValue && overdueDays >= type.SeverityWarnDays.Value)
            return "WARN";
        return type.DefaultSeverity;
    }
}
