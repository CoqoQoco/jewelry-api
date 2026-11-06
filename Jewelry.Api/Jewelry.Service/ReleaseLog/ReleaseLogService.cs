using jewelry.Model.ReleaseLog;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jewelry.Service.ReleaseLog;

public class ReleaseLogService : BaseService, IReleaseLogService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly JewelryContext _jewelryContext;

    public ReleaseLogService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
    }

    public async Task<long> Create(CreateReleaseLogRequest request)
    {
        var uiCommit = ReleaseLogRules.Clean(request.UiCommit);
        var apiCommit = ReleaseLogRules.Clean(request.ApiCommit);
        var appVersion = ReleaseLogRules.Clean(request.AppVersion);
        ReleaseLogRules.ValidateOptional(appVersion, uiCommit, apiCommit);

        var steps = (request.ManualSteps ?? new List<ReleaseLogManualStep>())
            .Select(s => new ReleaseLogManualStep { Desc = s.Desc?.Trim() ?? string.Empty, Done = s.Done })
            .ToList();
        var ticketNos = CleanList(request.TicketNos);
        var closedTicketNos = CleanList(request.ClosedTicketNos);
        var note = ReleaseLogRules.Clean(request.Note);
        ReleaseLogRules.ValidateLists(ticketNos, closedTicketNos, steps, note);

        var entity = new TbtReleaseLog
        {
            AnnouncementId = request.AnnouncementId,
            UiCommit = uiCommit,
            ApiCommit = apiCommit,
            AppVersion = appVersion,
            TicketNos = JsonSerializer.Serialize(ticketNos, Json),
            ClosedTicketNos = JsonSerializer.Serialize(closedTicketNos, Json),
            ManualSteps = JsonSerializer.Serialize(steps, Json),
            Note = note,
            CreateDate = DateTime.UtcNow,
            CreateBy = CurrentUsername
        };

        _jewelryContext.TbtReleaseLog.Add(entity);
        await _jewelryContext.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<LatestReleaseLogResponse> Latest()
    {
        var entity = await _jewelryContext.TbtReleaseLog
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (entity == null)
            return new LatestReleaseLogResponse { Data = null };

        return new LatestReleaseLogResponse
        {
            Data = new ReleaseLogResponse
            {
                Id = entity.Id,
                AnnouncementId = entity.AnnouncementId,
                UiCommit = entity.UiCommit,
                ApiCommit = entity.ApiCommit,
                AppVersion = entity.AppVersion,
                TicketNos = Parse<string>(entity.TicketNos),
                ClosedTicketNos = Parse<string>(entity.ClosedTicketNos),
                ManualSteps = Parse<ReleaseLogManualStep>(entity.ManualSteps),
                Note = entity.Note,
                CreateDate = entity.CreateDate,
                CreateBy = entity.CreateBy
            }
        };
    }

    private static List<string> CleanList(List<string>? list) =>
        (list ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

    private static List<T> Parse<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<T>();
        try { return JsonSerializer.Deserialize<List<T>>(json, Json) ?? new List<T>(); }
        catch (JsonException) { return new List<T>(); }
    }
}
