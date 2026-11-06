using System;
using System.Collections.Generic;

namespace jewelry.Model.ReleaseLog;

public class ReleaseLogManualStep
{
    public string Desc { get; set; } = null!;
    public bool Done { get; set; }
}

public class CreateReleaseLogRequest
{
    public long? AnnouncementId { get; set; }
    public string? UiCommit { get; set; }
    public string? ApiCommit { get; set; }
    public string? AppVersion { get; set; }
    public List<string>? TicketNos { get; set; }
    public List<string>? ClosedTicketNos { get; set; }
    public List<ReleaseLogManualStep>? ManualSteps { get; set; }
    public string? Note { get; set; }
}

public class LatestReleaseLogRequest { }

public class ReleaseLogResponse
{
    public long Id { get; set; }
    public long? AnnouncementId { get; set; }
    public string? UiCommit { get; set; }
    public string? ApiCommit { get; set; }
    public string? AppVersion { get; set; }
    public List<string> TicketNos { get; set; } = new();
    public List<string> ClosedTicketNos { get; set; } = new();
    public List<ReleaseLogManualStep> ManualSteps { get; set; } = new();
    public string? Note { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreateBy { get; set; } = null!;
}

public class LatestReleaseLogResponse
{
    public ReleaseLogResponse? Data { get; set; }
}
