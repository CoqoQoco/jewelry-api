using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Jewelry.Service.AiConnector.Auth
{
    // กัน SSRF ตอน fetch client metadata (CIMD): ห้าม http / IP literal / localhost / IP ภายใน
    public static class SsrfGuard
    {
        private static readonly string[] BlockedSuffixes = { ".localhost", ".local", ".internal", ".lan", ".home", ".corp" };

        public static bool IsAllowedMetadataUrl(string? url, out Uri? uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(url) || url.Length > 2048) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return false;
            if (u.Scheme != Uri.UriSchemeHttps) return false;
            if (!string.IsNullOrEmpty(u.UserInfo) || !string.IsNullOrEmpty(u.Fragment)) return false;
            if (u.Port != 443) return false;
            if (u.AbsolutePath == "/" || u.AbsolutePath.Length < 2) return false;

            var host = u.IdnHost.ToLowerInvariant();
            if (host == "localhost" || !host.Contains('.')) return false;
            if (BlockedSuffixes.Any(s => host.EndsWith(s, StringComparison.Ordinal))) return false;
            if (IPAddress.TryParse(host.Trim('[', ']'), out _)) return false;

            uri = u;
            return true;
        }

        public static bool IsPublicAddress(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            if (ip.AddressFamily == AddressFamily.InterNetworkV6 && TryExtractEmbeddedIPv4(ip, out var embedded)) return IsPublicAddress(embedded);
            if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.None)) return false;

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var b = ip.GetAddressBytes();
                if (b[0] == 0 || b[0] == 10 || b[0] == 127) return false;
                if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return false;
                if (b[0] == 169 && b[1] == 254) return false;
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false;
                if (b[0] == 192 && b[1] == 168) return false;
                if (b[0] == 192 && b[1] == 0 && b[2] == 0) return false;
                if (b[0] == 198 && (b[1] == 18 || b[1] == 19)) return false;
                if (b[0] >= 224) return false;
                return true;
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast || ip.IsIPv6UniqueLocal) return false;
                return true;
            }

            return false;
        }

        // IPv6 ที่ฝัง IPv4: 64:ff9b::/96 (NAT64), 2002::/16 (6to4), ::a.b.c.d (IPv4-compatible) · Teredo 2001::/32 คืน 0.0.0.0 เพื่อให้ถูกปฏิเสธ
        private static bool TryExtractEmbeddedIPv4(IPAddress ip, out IPAddress embedded)
        {
            var b = ip.GetAddressBytes();
            embedded = IPAddress.None;
            var zero12 = b.Take(12).All(x => x == 0);
            if (zero12 && !ip.Equals(IPAddress.IPv6Any) && !ip.Equals(IPAddress.IPv6Loopback))
            {
                embedded = new IPAddress(b.Skip(12).ToArray());
                return true;
            }
            if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b && b.Skip(4).Take(8).All(x => x == 0))
            {
                embedded = new IPAddress(b.Skip(12).ToArray());
                return true;
            }
            if (b[0] == 0x20 && b[1] == 0x02)
            {
                embedded = new IPAddress(b.Skip(2).Take(4).ToArray());
                return true;
            }
            if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x00 && b[3] == 0x00)
            {
                embedded = IPAddress.Any;
                return true;
            }
            return false;
        }

        public static readonly string[] AppSchemes = { "cursor", "vscode", "vscode-insiders", "claude" };

        private static readonly string[] KnownHttpsRedirects =
        {
            "https://claude.ai/api/mcp/auth_callback",
            "https://chatgpt.com/connector_platform_oauth_redirect",
        };

        // redirect ที่ระบบรู้จัก: https ใน allowlist (exact) · http loopback · custom scheme ของแอปที่รู้จัก
        public static bool IsKnownRedirect(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var u)) return false;
            if (u.Scheme == Uri.UriSchemeHttps) return KnownHttpsRedirects.Contains(value, StringComparer.Ordinal) || IsChatGptConnectorRedirect(value, u);
            if (u.Scheme == Uri.UriSchemeHttp) return IsLoopbackHost(u.Host);
            return AppSchemes.Contains(u.Scheme);
        }

        private const string ChatGptConnectorPrefix = "https://chatgpt.com/connector/oauth/";

        private static bool IsChatGptConnectorRedirect(string value, Uri u)
        {
            if (u.Host != "chatgpt.com" || !u.IsDefaultPort || !string.IsNullOrEmpty(u.Query) || !string.IsNullOrEmpty(u.Fragment) || !string.IsNullOrEmpty(u.UserInfo)) return false;
            if (value.Contains('?') || value.Contains('#') || !value.StartsWith(ChatGptConnectorPrefix, StringComparison.Ordinal)) return false;
            var segment = value.Substring(ChatGptConnectorPrefix.Length);
            return segment.Length > 0 && segment.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-');
        }

        // redirect_uri ที่ยอมรับ: https · http เฉพาะ loopback · custom scheme ที่รู้จักหรือ reverse-DNS (allowlist)
        public static bool IsValidRedirectUri(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 2048) return false;
            if (!Uri.TryCreate(value, UriKind.Absolute, out var u)) return false;
            if (!string.IsNullOrEmpty(u.Fragment) || !string.IsNullOrEmpty(u.UserInfo)) return false;

            if (u.Scheme == Uri.UriSchemeHttps) return true;
            if (u.Scheme == Uri.UriSchemeHttp) return IsLoopbackHost(u.Host);

            // allowlist: app scheme ที่รู้จัก หรือ reverse-DNS scheme (มีจุด) เช่น com.example.app
            return AppSchemes.Contains(u.Scheme) || (u.Scheme.Contains('.') && u.Scheme.Length >= 5);
        }

        public static bool IsLoopbackHost(string host)
        {
            host = host.Trim('[', ']');
            return host == "localhost" || host == "127.0.0.1" || host == "::1";
        }

        // exact match · loopback ปล่อยให้ port ต่างได้ (RFC 8252 §7.3)
        public static bool RedirectMatches(string requested, System.Collections.Generic.IEnumerable<string> registered)
        {
            if (registered.Contains(requested, StringComparer.Ordinal)) return true;
            if (!Uri.TryCreate(requested, UriKind.Absolute, out var r) || r.Scheme != Uri.UriSchemeHttp || !IsLoopbackHost(r.Host)) return false;
            foreach (var reg in registered)
            {
                if (Uri.TryCreate(reg, UriKind.Absolute, out var g) && g.Scheme == Uri.UriSchemeHttp && IsLoopbackHost(g.Host)
                    && g.AbsolutePath == r.AbsolutePath && g.Query == r.Query) return true;
            }
            return false;
        }
    }
}
