using jewelry.Model.Exceptions;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.ReleaseLog;
using System;
using System.Linq;

namespace Jewelry.Service.Ticket;

public static class TicketDeployRules
{
    public const string None = "none";
    public const string Pending = "pending";
    public const string Deployed = "deployed";

    public static readonly string[] Statuses = { None, Pending, Deployed };

    public static string? NormalizeStatus(string? value) => value?.Trim().ToLowerInvariant();

    public static void ValidateRequest(string? deployStatus, string? commitUi, string? commitApi)
    {
        if (deployStatus == null && commitUi == null && commitApi == null)
            throw new HandleException("ต้องส่ง deployStatus, commitUi หรือ commitApi อย่างน้อย 1 ช่อง");

        if (deployStatus != null && !Statuses.Contains(NormalizeStatus(deployStatus)))
            throw new HandleException($"deployStatus ต้องเป็น {string.Join(" / ", Statuses)}");

        ValidateCommitList("commitUi", commitUi);
        ValidateCommitList("commitApi", commitApi);
    }

    private static void ValidateCommitList(string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Any(p => !ReleaseLogRules.IsValidSha(p)))
            throw new HandleException($"{name} ต้องเป็น SHA (hex 7-40 ตัว) คั่นด้วย comma");
    }

    public static void CheckTransition(string? current, string requested)
    {
        var from = string.IsNullOrEmpty(current) ? None : current;
        if (from == Deployed && requested != Deployed)
            throw new HandleException("ticket ที่ deploy แล้ว (deployed) ไม่สามารถเปลี่ยนกลับเป็น pending หรือ none ได้");
    }

    private static string? NormalizeCommit(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>คืน true ถ้ามีการเปลี่ยนค่าจริง; commit "" = ล้างค่า</summary>
    public static bool Apply(TbtTicket ticket, string? deployStatus, string? commitUi, string? commitApi, DateTime utcNow)
    {
        ValidateRequest(deployStatus, commitUi, commitApi);

        var status = NormalizeStatus(deployStatus);
        if (status != null)
            CheckTransition(ticket.DeployStatus, status);

        var changed = false;

        if (status != null && status != (string.IsNullOrEmpty(ticket.DeployStatus) ? None : ticket.DeployStatus))
        {
            ticket.DeployStatus = status;
            if (status == Deployed)
                ticket.DeployDate = utcNow;
            changed = true;
        }

        if (commitUi != null)
        {
            var value = NormalizeCommit(commitUi);
            if (value != ticket.CommitUi) { ticket.CommitUi = value; changed = true; }
        }

        if (commitApi != null)
        {
            var value = NormalizeCommit(commitApi);
            if (value != ticket.CommitApi) { ticket.CommitApi = value; changed = true; }
        }

        return changed;
    }
}
