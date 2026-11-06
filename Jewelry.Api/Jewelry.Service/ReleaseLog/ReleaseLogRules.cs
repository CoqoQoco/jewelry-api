using jewelry.Model.Exceptions;
using jewelry.Model.ReleaseLog;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jewelry.Service.ReleaseLog;

public static class ReleaseLogRules
{
    private static readonly Regex VersionPattern = new(@"^\d{4}\.\d{2}\.\d{2}\.\d{4}$", RegexOptions.Compiled);
    private static readonly Regex TicketNoPattern = new(@"^TK\d{12}$", RegexOptions.Compiled);
    private static readonly Regex ShaPattern = new(@"^[0-9a-fA-F]{7,40}$", RegexOptions.Compiled);

    public static bool IsValidAppVersion(string? value) => value != null && VersionPattern.IsMatch(value);

    public static bool IsValidSha(string? value) => value != null && ShaPattern.IsMatch(value);

    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static void ValidateOptional(string? appVersion, string? uiCommit, string? apiCommit)
    {
        if (appVersion != null && !IsValidAppVersion(appVersion))
            throw new HandleException("appVersion ต้องอยู่ในรูปแบบ yyyy.MM.dd.HHmm เช่น 2026.11.06.1530");
        if (uiCommit != null && !IsValidSha(uiCommit))
            throw new HandleException("uiCommit ต้องเป็น SHA (hex 7-40 ตัว)");
        if (apiCommit != null && !IsValidSha(apiCommit))
            throw new HandleException("apiCommit ต้องเป็น SHA (hex 7-40 ตัว)");
    }

    public const int MaxTicketNos = 200;
    public const int MaxManualSteps = 50;
    public const int MaxStepDescLength = 500;
    public const int MaxNoteLength = 1000;

    public static bool IsValidTicketNo(string? value) => value != null && TicketNoPattern.IsMatch(value);

    public static void ValidateLists(
        IReadOnlyCollection<string> ticketNos,
        IReadOnlyCollection<string> closedTicketNos,
        IReadOnlyCollection<ReleaseLogManualStep> manualSteps,
        string? note)
    {
        ValidateTicketNos(ticketNos, "ticketNos");
        ValidateTicketNos(closedTicketNos, "closedTicketNos");

        if (manualSteps.Count > MaxManualSteps)
            throw new HandleException($"manualSteps ต้องไม่เกิน {MaxManualSteps} รายการ");
        if (manualSteps.Any(s => string.IsNullOrWhiteSpace(s.Desc)))
            throw new HandleException("manualSteps ทุกรายการต้องมี desc (ห้ามว่าง)");
        if (manualSteps.Any(s => s.Desc.Trim().Length > MaxStepDescLength))
            throw new HandleException($"desc ของ manualSteps ต้องไม่เกิน {MaxStepDescLength} ตัวอักษร");

        if (note != null && note.Length > MaxNoteLength)
            throw new HandleException($"note ต้องไม่เกิน {MaxNoteLength} ตัวอักษร");
    }

    private static void ValidateTicketNos(IReadOnlyCollection<string> list, string field)
    {
        if (list.Count > MaxTicketNos)
            throw new HandleException($"{field} ต้องไม่เกิน {MaxTicketNos} รายการ");
        var bad = list.FirstOrDefault(x => !IsValidTicketNo(x));
        if (bad != null)
            throw new HandleException($"{field} มีเลข ticket ไม่ถูกต้อง ({bad}) ต้องเป็นรูปแบบ TK ตามด้วยตัวเลข 12 หลัก");
    }
}
