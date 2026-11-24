using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Service.AiConnector.Glossary;
using Jewelry.Service.AiConnector.Registry;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Tools.Executive
{
    public class GetGlossaryTool : AiToolBase<AiGlossaryInput, AiGlossaryOutput>
    {
        public override string Name => "get_glossary";
        public override string TitleTh => "ดูนิยามตัวเลขและข้อควรระวัง";
        public override string Description =>
            "คืนนิยามภาษาไทยของตัวชี้วัดในรายงานผู้บริหาร (เช่น ใบงานนิ่ง บิลค้างชำระ ส่วนต่างทองสูญเสีย) พร้อมข้อควรระวังของข้อมูล " +
            "ควรเรียกเครื่องมือนี้ก่อนตีความตัวเลขจาก get_executive_summary หรือ get_executive_digest · ระบุ module (เช่น executive) หรือ metric (รหัสตัวชี้วัด) เพื่อกรอง ไม่ระบุ = ทั้งหมด";
        public override string InputSchemaJson =>
            "{\"type\":\"object\",\"properties\":{\"module\":{\"type\":\"string\",\"description\":\"ชื่อโมดูล เช่น executive\"},\"metric\":{\"type\":\"string\",\"description\":\"รหัสตัวชี้วัด เช่น stale_plan\"}}}";
        public override IReadOnlyList<string> RequiredScopes => new[] { AiScopes.ModuleExecutive };
        public override IReadOnlyList<string> PermissionsAny => new[] { PermissionCodes.ExecutiveView };
        public override string WrapsEndpoint => "AiGlossaryService (ไม่มี controller เดิม)";

        protected override async Task<AiGlossaryOutput> RunAsync(AiGlossaryInput input, AiToolContext context, CancellationToken ct)
        {
            var service = context.Services.GetRequiredService<IAiGlossaryService>();
            return new AiGlossaryOutput { Items = await service.Get(input.Module, input.Metric) };
        }
    }
}
