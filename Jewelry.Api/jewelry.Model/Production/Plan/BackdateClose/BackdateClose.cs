using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.BackdateClose
{
    public static class ReasonCode
    {
        public const string NotFound = "NOT_FOUND";
        public const string AlreadyCompleted = "ALREADY_COMPLETED";
        public const string Melted = "MELTED";
        public const string Cvd = "CVD";
        public const string Duplicate = "DUPLICATE";
        public const string InvalidFormat = "INVALID_FORMAT";
    }

    public class PreviewRequest
    {
        public List<string> Jobs { get; set; } = new List<string>();
    }

    public class PreviewItem
    {
        public string Input { get; set; } = string.Empty;
        public bool Found { get; set; }
        public int? Id { get; set; }
        public string? Wo { get; set; }
        public int? WoNumber { get; set; }
        public string? Mold { get; set; }
        public string? ProductNumber { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
        public DateTime? LastMoveDate { get; set; }
        public bool CanClose { get; set; }
        public string? ReasonCode { get; set; }
    }

    public class PreviewResponse
    {
        public List<PreviewItem> Items { get; set; } = new List<PreviewItem>();
    }

    public class CloseRequest
    {
        public List<int> PlanIds { get; set; } = new List<int>();
        public string Remark { get; set; } = string.Empty;
        public DateTimeOffset? CloseDate { get; set; }
    }

    public class ClosedItem
    {
        public int Id { get; set; }
        public string Wo { get; set; } = string.Empty;
        public int WoNumber { get; set; }
    }

    public class ErrorItem
    {
        public int Id { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
    }

    public class CloseResponse
    {
        public List<ClosedItem> Closed { get; set; } = new List<ClosedItem>();
        public List<ErrorItem> Errors { get; set; } = new List<ErrorItem>();
        public string? TransferNumber { get; set; }
    }
}
