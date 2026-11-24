using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Registry
{
    // 1 class ต่อ 1 tool — ลงทะเบียนอัตโนมัติด้วย assembly scan (ดู AiConnectorRegistration)
    public interface IAiTool
    {
        string Name { get; }
        string TitleTh { get; }
        // บอกชัดว่าตัวเลขคืออะไร ช่วงเวลาไหน และให้เรียก get_glossary ก่อนตีความ
        string Description { get; }
        string InputSchemaJson { get; }
        Type InputType { get; }
        Type OutputType { get; }

        // scope ที่ต้องมีครบทุกตัว
        IReadOnlyList<string> RequiredScopes { get; }
        // permission ที่ต้องมีอย่างน้อย 1 ตัว (ANY) — ใช้ค่าจาก PermissionCodes
        IReadOnlyList<string> PermissionsAny { get; }
        // controller action เดิมที่ tool ห่ออยู่ เช่น "ExecutiveReportController.Summary" (เทสเทียบสิทธิ์กับ [RequirePermission])
        string WrapsEndpoint { get; }

        bool IsWrite { get; }
        // TimeSpan.Zero = ไม่ cache
        TimeSpan CacheTtl { get; }
        IReadOnlyList<string> MetricCodes { get; }
        IReadOnlyList<string> CacheInvalidationPrefixes { get; }

        // ตัวกรองระดับแถวของ user (ถ้ามี) — ใช้เป็นส่วนของ cache key; ไม่มี = "shared"
        string RowFilterId(AiToolContext context);

        Task<object> ExecuteAsync(object input, AiToolContext context, CancellationToken cancellationToken);
    }

    // เผื่อรอบถัดไป (prompt template) — รอบ 1 ยังไม่มีตัว implement
    public interface IAiPrompt
    {
        string Name { get; }
        string TitleTh { get; }
        string Description { get; }
    }

    // input ที่มีช่วงวัน — pipeline จำกัดไม่เกิน 1 ปี
    public interface IAiRangedInput
    {
        DateTime? From { get; set; }
        DateTime? To { get; set; }
    }

    // input ที่มี row cap — pipeline ตั้ง default 50 และ clamp ไม่เกิน 200
    public interface IAiRowCappedInput
    {
        int? Limit { get; set; }
    }

    public sealed class AiToolContext
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public long TokenId { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
        // DI scope ของ request — tool เป็น singleton ต้องดึง service แบบ scoped จากตรงนี้
        public IServiceProvider Services { get; set; } = null!;
        public ISet<string> Scopes { get; set; } = new HashSet<string>();
        public ISet<string> Permissions { get; set; } = new HashSet<string>();
    }

    public abstract class AiToolBase<TInput, TOutput> : IAiTool
        where TInput : class, new()
        where TOutput : class
    {
        public abstract string Name { get; }
        public abstract string TitleTh { get; }
        public abstract string Description { get; }
        public virtual string InputSchemaJson => "{\"type\":\"object\",\"properties\":{}}";
        public Type InputType => typeof(TInput);
        public Type OutputType => typeof(TOutput);
        public abstract IReadOnlyList<string> RequiredScopes { get; }
        public abstract IReadOnlyList<string> PermissionsAny { get; }
        public abstract string WrapsEndpoint { get; }
        public virtual bool IsWrite => false;
        public virtual TimeSpan CacheTtl => TimeSpan.Zero;
        public virtual IReadOnlyList<string> MetricCodes => Array.Empty<string>();
        public virtual IReadOnlyList<string> CacheInvalidationPrefixes => Array.Empty<string>();
        public virtual string RowFilterId(AiToolContext context) => "shared";

        protected abstract Task<TOutput> RunAsync(TInput input, AiToolContext context, CancellationToken cancellationToken);

        public async Task<object> ExecuteAsync(object input, AiToolContext context, CancellationToken cancellationToken)
            => await RunAsync((TInput)input, context, cancellationToken);
    }
}
