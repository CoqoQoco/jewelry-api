using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtReleaseLog
{
    public long Id { get; set; }

    public long? AnnouncementId { get; set; }

    public string? UiCommit { get; set; }

    public string? ApiCommit { get; set; }

    public string? AppVersion { get; set; }

    public string TicketNos { get; set; } = "[]";

    public string ClosedTicketNos { get; set; } = "[]";

    public string ManualSteps { get; set; } = "[]";

    public string? Note { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;
}
