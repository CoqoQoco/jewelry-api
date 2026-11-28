using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;

namespace jewelry.Model.AiConnector
{
    public class AiAdminGrantState
    {
        public string Scope { get; set; } = "";
        public string NameTh { get; set; } = "";
        // Dev เปิดหมวดนี้ให้ user อยู่หรือไม่
        public bool Enabled { get; set; }
        // role ปัจจุบันของ user มี permission รองรับหมวดนี้ไหม (false = เปิดให้ไม่ได้)
        public bool CanGrant { get; set; }
        public string? GrantedBy { get; set; }
        public DateTime? GrantedAt { get; set; }
    }

    public class AiAdminUserItem
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public List<string> Roles { get; set; } = new List<string>();
        // มี permission ai:connect หรือไม่
        public bool CanConnect { get; set; }
        public int ActiveConnections { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public bool Suspended { get; set; }
        public string? SuspendedReason { get; set; }
        public DateTime? SuspendedAt { get; set; }
        // manual | auto
        public string? SuspendedSource { get; set; }
        public List<AiAdminGrantState> Grants { get; set; } = new List<AiAdminGrantState>();
    }

    public class AiAdminUserListResponse
    {
        public List<AiAdminUserItem> Items { get; set; } = new List<AiAdminUserItem>();
    }

    public class AiAdminConnectionsRequest
    {
        public int UserId { get; set; }
    }

    public class AiAdminRevokeRequest
    {
        public long TokenId { get; set; }
    }

    public class AiAdminSetGrantRequest
    {
        public int UserId { get; set; }
        public string? Scope { get; set; }
        public bool Enabled { get; set; }
    }

    public class AiAdminSuspendRequest
    {
        public int UserId { get; set; }
        public string? Reason { get; set; }
    }

    public class AiAdminLiftRequest
    {
        public int UserId { get; set; }
    }

    public class AiAdminAccessLogRequest : DataSourceRequest
    {
        public int? UserId { get; set; }
        public string? Tool { get; set; }
        public List<int>? UserIds { get; set; }
        public List<string>? Tools { get; set; }
        public DateTimeOffset? DateFrom { get; set; }
        public DateTimeOffset? DateTo { get; set; }
    }

    public class AiAdminAccessLogItem
    {
        public long Id { get; set; }
        public DateTime CreateDate { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public long? TokenId { get; set; }
        public string? ClientName { get; set; }
        public string Tool { get; set; } = "";
        public string ToolTitleTh { get; set; } = "";
        public List<string> Scopes { get; set; } = new List<string>();
        public int RowCount { get; set; }
        public int MaskedCount { get; set; }
        public int DurationMs { get; set; }
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessageTh { get; set; }
    }

    public class AiAdminSuspensionsRequest : DataSourceRequest
    {
        public int? UserId { get; set; }
        // true = เฉพาะที่ยังระงับอยู่ · false = เฉพาะที่ปลดแล้ว · null = ทั้งหมด
        public bool? Active { get; set; }
    }

    public class AiAdminSuspensionItem
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        // manual | auto
        public string Source { get; set; } = "";
        public string ReasonCode { get; set; } = "";
        // JSON ดิบของรายละเอียด (manual = {"reason":"..."})
        public string? ReasonDetail { get; set; }
        public string? ReasonText { get; set; }
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string? LiftedBy { get; set; }
        public DateTime? LiftedAt { get; set; }
        public bool Active { get; set; }
    }
}
