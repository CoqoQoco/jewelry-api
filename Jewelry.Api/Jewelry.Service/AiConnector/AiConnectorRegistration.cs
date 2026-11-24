using Jewelry.Service.AiConnector.Auth;
using Jewelry.Service.AiConnector.Glossary;
using Jewelry.Service.AiConnector.Pipeline;
using Jewelry.Service.AiConnector.Registry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Jewelry.Service.AiConnector
{
    public static class AiConnectorRegistration
    {
        public static IServiceCollection AddAiConnector(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AiConnectorOptions>(o => AiConnectorOptions.Load(configuration, o));

            services.AddSingleton<IClientMetadataFetcher, ClientMetadataFetcher>();
            services.AddSingleton<AiRateLimiter>();
            services.AddScoped<IAiTokenStore, AiTokenStore>();
            services.AddScoped<IAiUserDirectory, AiUserDirectory>();
            services.AddScoped<IAiScopeCatalog, AiScopeCatalog>();
            services.AddScoped<IAiClientRegistry, AiClientRegistry>();
            services.AddScoped<IAiTokenValidator, AiTokenValidator>();
            services.AddScoped<IAiOAuthService, OAuthService>();
            services.AddScoped<IAiConnectionService, AiConnectionService>();
            services.AddScoped<IAiGlossaryService, AiGlossaryService>();
            services.AddScoped<IAiToolPipeline, AiToolPipeline>();

            // convention ใหม่ (ต่างจากส่วนอื่นของ repo ที่ register ทีละตัว):
            // IAiTool ทุก class ใน assembly นี้ถูก scan เข้ามาเป็น singleton อัตโนมัติ — เพิ่ม tool ใหม่ = สร้าง class เดียว ไม่ต้องแก้ไฟล์นี้
            // tool ต้อง stateless; service แบบ scoped ให้ดึงผ่าน AiToolContext.Services
            // AiToolRegistry ตรวจกติกา (IsWrite / ชื่อซ้ำ / permission / AI DTO) ตอน startup — ไม่ผ่านแอปไม่ขึ้น
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IAiTool).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false }))
            {
                services.AddSingleton(typeof(IAiTool), type);
            }
            services.AddSingleton<AiToolRegistry>();

            return services;
        }
    }
}
