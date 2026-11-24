using System;
using System.Collections.Generic;

namespace Jewelry.Service.AiConnector
{
    // bind จาก config section "AiConnector" — ค่า default ปลอดภัย: ไม่มี SigningKey = connector ปิดตัวเอง (/mcp ตอบ 503)
    public class AiConnectorOptions
    {
        public const string SectionName = "AiConnector";
        public const string Audience = "dk-jewelry-mcp";
        public const string DefaultIssuer = "dk-jewelry-ai-connector";

        // key ของ access token MCP — ต้องไม่ซ้ำกับ JwtSettings:Key (อย่างน้อย 32 ตัวอักษร)
        public string? SigningKey { get; set; }
        public string? Issuer { get; set; }
        // base URL สาธารณะของ API เช่น https://api.example.com (ว่าง = อนุมานจาก request)
        public string? BaseUrl { get; set; }
        // base URL ของ UI ที่มีหน้า /oauth/authorize เช่น https://app.example.com
        public string? UiBaseUrl { get; set; }
        // Origin ที่อนุญาตเรียก /mcp จากเบราว์เซอร์ (ตรวจ Origin ตาม MCP spec) — ว่าง = ใช้ค่า default ด้านล่าง
        public List<string> AllowedOrigins { get; set; } = new List<string>();

        public int AccessTokenMinutes { get; set; } = 60;
        public int RefreshTokenDays { get; set; } = 30;
        public int AuthCodeMinutes { get; set; } = 5;
        public int RateLimitPerMinute { get; set; } = 30;

        public static readonly string[] DefaultAllowedOrigins =
        {
            "https://claude.ai",
            "https://chatgpt.com",
            "https://chat.openai.com",
        };

        // อ่านทีละ key (Service ไม่ได้อ้าง Configuration.Binder) — env var บน Azure ใช้รูป AiConnector__SigningKey
        public static void Load(Microsoft.Extensions.Configuration.IConfiguration config, AiConnectorOptions o)
        {
            var s = config.GetSection(SectionName);
            o.SigningKey = s["SigningKey"];
            o.Issuer = s["Issuer"];
            o.BaseUrl = s["BaseUrl"];
            o.UiBaseUrl = s["UiBaseUrl"];
            var env = config["ASPNETCORE_ENVIRONMENT"] ?? config["DOTNET_ENVIRONMENT"] ?? "Production";
            o.RequireExplicitUrls = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(s["AccessTokenMinutes"], out var a) && a > 0) o.AccessTokenMinutes = a;
            if (int.TryParse(s["RefreshTokenDays"], out var r) && r > 0) o.RefreshTokenDays = r;
            if (int.TryParse(s["AuthCodeMinutes"], out var c) && c > 0) o.AuthCodeMinutes = c;
            if (int.TryParse(s["RateLimitPerMinute"], out var l) && l > 0) o.RateLimitPerMinute = l;
            // key เดียวกับ JWT ของเว็บ = ไม่ปลอดภัย → ถือว่ายังไม่ตั้งค่า (connector ปิดตัวเอง)
            if (!string.IsNullOrEmpty(o.SigningKey) && o.SigningKey == config["JwtSettings:Key"]) o.SigningKey = null;
            foreach (var origin in s.GetSection("AllowedOrigins").GetChildren())
            {
                if (!string.IsNullOrWhiteSpace(origin.Value)) o.AllowedOrigins.Add(origin.Value!.TrimEnd('/'));
            }
        }

        // production ต้องตั้ง BaseUrl + UiBaseUrl เอง (ไม่อนุมานจาก request / ไม่ fallback localhost) ไม่งั้น connector ปิด (503)
        public bool RequireExplicitUrls { get; set; }

        public bool IsConfigured =>
            !string.IsNullOrEmpty(SigningKey) && SigningKey.Length >= 32
            && (!RequireExplicitUrls || (!string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(UiBaseUrl)));
        public string EffectiveIssuer => string.IsNullOrWhiteSpace(Issuer) ? DefaultIssuer : Issuer!;

        public IReadOnlyList<string> EffectiveAllowedOrigins =>
            AllowedOrigins.Count > 0 ? AllowedOrigins : new List<string>(DefaultAllowedOrigins);
    }

    public static class AiScopes
    {
        public const string ModuleExecutive = "module:executive";
        public const string DataCustomerNames = "data:customer-names";
        public const string DataWorkerNames = "data:worker-names";
        public const string DataCost = "data:cost";
        public const string DataReceivables = "data:receivables";

        public const string DataPrefix = "data:";

        public static string[] Split(string? scopes) =>
            string.IsNullOrWhiteSpace(scopes)
                ? Array.Empty<string>()
                : scopes!.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public static string Join(IEnumerable<string> scopes) => string.Join(' ', scopes);
    }

    public static class AiTokenKinds
    {
        public const string OAuth = "OAUTH";
        public const string Pat = "PAT";
        public const string PatPrefix = "dkm_";
    }
}
