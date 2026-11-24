using Jewelry.Api.Extension;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Pipeline;
using Jewelry.Service.AiConnector.Registry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using ModelContextProtocol.Protocol;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace Jewelry.Api.AiConnector
{
    public static class AiMcpSetup
    {
        public static void AddAiConnectorWeb(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthentication()
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, AiConnectorAuthenticationHandler>(
                    AiConnectorDefaults.AuthenticationScheme, null);

            var isProduction = builder.Environment.IsProduction();
            builder.Services.AddCors(c => c.AddPolicy(AiConnectorDefaults.CorsPolicy, policy =>
            {
                var origins = new List<string>(AiOptions(builder).EffectiveAllowedOrigins);
                if (!isProduction)
                {
                    origins.AddRange(new[] { "http://localhost:6274", "http://127.0.0.1:6274", "http://localhost:2002" });
                }
                policy.WithOrigins(origins.ToArray())
                    .WithMethods("GET", "POST", "DELETE", "OPTIONS")
                    .WithHeaders("Authorization", "Content-Type", "Accept", "Mcp-Session-Id", "Mcp-Protocol-Version", "Last-Event-ID")
                    .WithExposedHeaders("WWW-Authenticate", "Mcp-Session-Id");
            }));

            builder.Services.AddRateLimiter(o =>
            {
                // /mcp จำกัดต่อ IP ก่อนถึงขั้น auth (กันยิง token มั่ว) — IP ที่เห็นอาจเป็น IP ของ proxy เพราะยังไม่ตั้ง ForwardedHeaders
                o.AddPolicy("ai-mcp-ip", ctx => RateLimitPartition.GetFixedWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
                // DCR เปิดสาธารณะ (ไม่มี token) → จำกัดเข้มต่อ IP กันถมตาราง tbt_ai_client
                o.AddPolicy("ai-register", ctx => RateLimitPartition.GetFixedWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMcpServer(o =>
                {
                    o.ServerInfo = new Implementation { Name = "dk-jewelry", Version = "1.0.0" };
                })
                .WithHttpTransport(o => o.Stateless = true)
                .WithListToolsHandler(ListTools)
                .WithCallToolHandler(CallTool);
        }

        private static AiConnectorOptions AiOptions(WebApplicationBuilder builder)
        {
            var o = new AiConnectorOptions();
            AiConnectorOptions.Load(builder.Configuration, o);
            return o;
        }

        public static void UseAiConnectorWeb(this WebApplication app)
        {
            // 503 เมื่อไม่มี SigningKey — ไม่มี key สำรอง ไม่ว่ากรณีใด
            app.Use(async (ctx, next) =>
            {
                var path = ctx.Request.Path;
                if (path.StartsWithSegments(AiConnectorDefaults.McpPath))
                {
                    var options = ctx.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiConnectorOptions>>().Value;
                    if (!options.IsConfigured)
                    {
                        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                        await ctx.Response.WriteAsync("AI connector is not configured");
                        return;
                    }

                    // MCP spec: ตรวจ Origin กัน DNS rebinding — client ที่ไม่ใช่เบราว์เซอร์ไม่ส่ง Origin
                    if (ctx.Request.Headers.TryGetValue("Origin", out var origin) && !string.IsNullOrEmpty(origin.ToString()))
                    {
                        var allowed = new List<string>(options.EffectiveAllowedOrigins);
                        var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
                        if (!env.IsProduction()) allowed.AddRange(new[] { "http://localhost:6274", "http://127.0.0.1:6274", "http://localhost:2002" });
                        if (!allowed.Contains(origin.ToString().TrimEnd('/'), StringComparer.OrdinalIgnoreCase))
                        {
                            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return;
                        }
                    }
                }
                await next();
            });
        }

        public static void MapAiConnectorWeb(this WebApplication app)
        {
            // ตรวจกติกา tool ตอน startup (IsWrite / ชื่อซ้ำ / permission / AI DTO) — ไม่ผ่านแอปไม่ขึ้น
            app.Services.GetRequiredService<AiToolRegistry>();

            app.MapMcp(AiConnectorDefaults.McpPath)
                .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = AiConnectorDefaults.AuthenticationScheme })
                .RequireCors(AiConnectorDefaults.CorsPolicy)
                .RequireRateLimiting("ai-mcp-ip");
        }

        private static ValueTask<ListToolsResult> ListTools(ModelContextProtocol.Server.RequestContext<ListToolsRequestParams> ctx, CancellationToken ct)
        {
            var http = ctx.Services!.GetRequiredService<IHttpContextAccessor>().HttpContext!;
            var registry = http.RequestServices.GetRequiredService<AiToolRegistry>();
            var scopes = http.User.FindAll("ai_scope").Select(c => c.Value).ToHashSet();

            var tools = registry.Tools
                .Where(t => t.RequiredScopes.All(scopes.Contains))
                .Select(t => new Tool
                {
                    Name = t.Name,
                    Title = t.TitleTh,
                    Description = t.Description,
                    InputSchema = JsonSerializer.Deserialize<JsonElement>(t.InputSchemaJson),
                    Annotations = new ToolAnnotations
                    {
                        Title = t.TitleTh,
                        ReadOnlyHint = true,
                        DestructiveHint = false,
                        IdempotentHint = true,
                        OpenWorldHint = false,
                    },
                })
                .ToList();
            return ValueTask.FromResult(new ListToolsResult { Tools = tools });
        }

        private static async ValueTask<CallToolResult> CallTool(ModelContextProtocol.Server.RequestContext<CallToolRequestParams> ctx, CancellationToken ct)
        {
            var http = ctx.Services!.GetRequiredService<IHttpContextAccessor>().HttpContext!;
            var pipeline = http.RequestServices.GetRequiredService<IAiToolPipeline>();
            var args = ctx.Params?.Arguments == null ? null : JsonSerializer.Serialize(ctx.Params.Arguments);

            var result = await pipeline.Run(http.User, ctx.Params?.Name ?? "", args, ct);
            return new CallToolResult
            {
                IsError = result.IsError,
                Content = new List<ContentBlock> { new TextContentBlock { Text = result.Json } },
            };
        }
    }
}
