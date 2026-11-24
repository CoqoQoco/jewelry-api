using System;
using System.Collections.Generic;

namespace jewelry.Model.AiConnector
{
    // พารามิเตอร์ authorize (ตาม OAuth 2.1) — UI ส่งต่อจาก query ของหน้า /oauth/authorize
    public class AuthorizeParams
    {
        public string? ResponseType { get; set; }
        public string? ClientId { get; set; }
        public string? RedirectUri { get; set; }
        public string? Scope { get; set; }
        public string? State { get; set; }
        public string? CodeChallenge { get; set; }
        public string? CodeChallengeMethod { get; set; }
        public string? Resource { get; set; }
    }

    public class ConsentScope
    {
        public string Code { get; set; } = "";
        public string NameTh { get; set; } = "";
        // ตัวอย่างข้อมูลที่ AI จะเห็น (description_th ใน tbm_ai_scope)
        public string? DescriptionTh { get; set; }
        // MODULE | DATA
        public string Kind { get; set; } = "MODULE";
        // MODULE ที่ client ขอ = true (ต้องอนุญาต) ; DATA = false เสมอ
        public bool Required { get; set; }
        // ค่าเริ่มต้นของ checkbox — DATA เป็น false เสมอ
        public bool DefaultChecked { get; set; }
        // user ที่ล็อกอินมีสิทธิ์พอจะให้ scope นี้ได้ไหม (DATA ที่ผู้ใช้ไม่มี permission = false → UI ปิด checkbox)
        public bool Available { get; set; }
        public string? UnavailableReasonTh { get; set; }
    }

    public class ConsentResponse
    {
        // false = user ไม่มีสิทธิ์ ai:connect → UI แสดง error ไม่ให้กดอนุญาต
        public bool CanApprove { get; set; }
        public string? DenyReasonTh { get; set; }
        public string ClientId { get; set; } = "";
        public string ClientName { get; set; } = "";
        // CIMD | DCR
        public string ClientKind { get; set; } = "";
        // true = redirect_uris ทั้งหมดอยู่ใน allowlist ที่ระบบรู้จัก (Claude / ChatGPT / loopback / cursor / vscode ฯลฯ)
        // false = "แอปที่ระบบไม่รู้จัก" → UI ต้องแสดงป้ายเตือนพร้อม clientId และ redirect เต็ม
        public bool ClientVerified { get; set; }
        // redirect_uri ที่ request นี้จะส่ง code กลับไป (เต็ม)
        public string RedirectUri { get; set; } = "";
        // redirect_uris ทั้งหมดที่ client ลงทะเบียนไว้ (เต็ม)
        public List<string> RedirectUris { get; set; } = new List<string>();
        // host ของ redirect_uri ให้ user ตรวจว่าจะกลับไปที่ไหน
        public string RedirectHost { get; set; } = "";
        public string UserDisplayName { get; set; } = "";
        public List<ConsentScope> Scopes { get; set; } = new List<ConsentScope>();
    }

    public class ApproveRequest : AuthorizeParams
    {
        // scope ที่ user อนุญาต (MODULE ที่ขอ + DATA ที่ติ๊ก)
        public List<string> ApprovedScopes { get; set; } = new List<string>();
    }

    public class ApproveResponse
    {
        // URL ที่ UI ต้อง window.location ไป (redirect_uri?code=...&state=...)
        // Deny ของ client ที่ระบบไม่รู้จัก = null → UI แสดง "ปฏิเสธแล้ว" ในหน้าเอง ไม่ redirect
        public string? RedirectUrl { get; set; } = "";
    }

    public class TokenRequest
    {
        public string? GrantType { get; set; }
        public string? Code { get; set; }
        public string? RedirectUri { get; set; }
        public string? ClientId { get; set; }
        public string? CodeVerifier { get; set; }
        public string? RefreshToken { get; set; }
        public string? Resource { get; set; }
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; } = "";
        public string Scope { get; set; } = "";
    }

    public class OAuthError
    {
        public string Error { get; set; } = "";
        public string? ErrorDescription { get; set; }
    }

    public class DcrRequest
    {
        public string? ClientName { get; set; }
        public List<string>? RedirectUris { get; set; }
        public string? TokenEndpointAuthMethod { get; set; }
        public List<string>? GrantTypes { get; set; }
        public List<string>? ResponseTypes { get; set; }
    }

    public class DcrResponse
    {
        public string ClientId { get; set; } = "";
        public string ClientName { get; set; } = "";
        public List<string> RedirectUris { get; set; } = new List<string>();
        public string TokenEndpointAuthMethod { get; set; } = "none";
        public List<string> GrantTypes { get; set; } = new List<string> { "authorization_code", "refresh_token" };
        public List<string> ResponseTypes { get; set; } = new List<string> { "code" };
    }

    public class ConnectionItem
    {
        // id ของแถวปัจจุบัน — ใช้กับ Revoke และเป็น tokenId ใน filter ของ AccessLog
        public long Id { get; set; }
        // OAUTH | PAT
        public string Kind { get; set; } = "";
        public string Name { get; set; } = "";
        public string? ClientId { get; set; }
        public List<string> Scopes { get; set; } = new List<string>();
        public DateTime CreateDate { get; set; }
        // null = ยังไม่เคยใช้
        public DateTime? LastUsedAt { get; set; }
        // วันหมดอายุของการเชื่อมต่อ (OAUTH = refresh หมดอายุ, PAT = วันหมดอายุของ PAT)
        public DateTime ExpiresAt { get; set; }
    }

    public class ConnectionListResponse
    {
        public List<ConnectionItem> Items { get; set; } = new List<ConnectionItem>();
    }

    public class RevokeRequest
    {
        public long Id { get; set; }
    }

    public class CreatePatRequest
    {
        public string? Name { get; set; }
        // 30 | 60 | 90
        public int ExpiresInDays { get; set; } = 30;
        // ว่าง = module:executive ; data:* ขอไม่ได้
        public List<string>? Scopes { get; set; }
    }

    public class CreatePatResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        // แสดงครั้งเดียว — ไม่เก็บ plain text
        public string Token { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public List<string> Scopes { get; set; } = new List<string>();
    }

    public class McpInfoResponse
    {
        public string McpUrl { get; set; } = "";
        public bool Configured { get; set; }
        public List<PatScopeOption> PatScopes { get; set; } = new List<PatScopeOption>();
    }

    public class PatScopeOption
    {
        public string Code { get; set; } = "";
        public string NameTh { get; set; } = "";
        public string? DescriptionTh { get; set; }
    }

    public class AccessLogRequest
    {
        public int Skip { get; set; }
        public int Take { get; set; } = 20;
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        // id ของ connection (จาก ConnectionItem.Id) — รวมทุกแถวใน family เดียวกันให้เอง
        public List<long>? TokenIds { get; set; }
        public List<string>? ClientIds { get; set; }
    }

    public class AccessLogItem
    {
        public long Id { get; set; }
        public DateTime CreateDate { get; set; }
        public long? TokenId { get; set; }
        public string? ConnectionName { get; set; }
        public string? ClientId { get; set; }
        public string Tool { get; set; } = "";
        // ชื่อเรื่องภาษาไทยของ tool (Title TH)
        public string ToolTitleTh { get; set; } = "";
        public List<string> Scopes { get; set; } = new List<string>();
        public int RowCount { get; set; }
        public int MaskedCount { get; set; }
        public int DurationMs { get; set; }
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        // เหตุผล error เป็นภาษาคน
        public string? ErrorMessageTh { get; set; }
    }

    public class AccessLogResponse
    {
        public List<AccessLogItem> Data { get; set; } = new List<AccessLogItem>();
        public int Total { get; set; }
    }
}
