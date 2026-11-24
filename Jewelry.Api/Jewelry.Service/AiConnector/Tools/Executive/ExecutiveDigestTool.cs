using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Service.AiConnector.Registry;
using Jewelry.Service.Helper;
using Jewelry.Service.Report.Executive;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Digest = jewelry.Model.Report.Executive.Digest;

namespace Jewelry.Service.AiConnector.Tools.Executive
{
    public class ExecutiveDigestTool : AiToolBase<AiMetalInput, AiExecutiveDigestOutput>
    {
        public override string Name => "get_executive_digest";
        public override string TitleTh => "ประเด็นที่ควรจับตา (Digest)";
        public override string Description =>
            "สรุปสถานะ 4 ด้าน (งานผลิต งานขาย วัตถุดิบ คลังสินค้า) เป็น critical/warning/ok พร้อมรายการประเด็นสำคัญสุดที่ระบบตรวจพบ ณ ปัจจุบัน " +
            "ทุกประเด็นมีข้อความภาษาไทยอธิบายแล้ว · ตัวเลขประกอบเป็น metrics (ตัวเลขทั่วไป) ส่วนตัวเลขเงินจะอยู่ใน receivableMetrics/costMetrics ซึ่งอาจถูกซ่อน · " +
            "ชื่อช่างและชื่อลูกค้าไม่ถูกส่งออกในเครื่องมือนี้ · พารามิเตอร์ metal = ALL | GOLD | SILVER · ควรเรียก get_glossary ก่อนตีความ";
        public override string InputSchemaJson =>
            "{\"type\":\"object\",\"properties\":{\"metal\":{\"type\":\"string\",\"enum\":[\"ALL\",\"GOLD\",\"SILVER\"],\"description\":\"โลหะที่ต้องการ (ค่าเริ่มต้น ALL)\"}}}";
        public override IReadOnlyList<string> RequiredScopes => new[] { AiScopes.ModuleExecutive };
        public override IReadOnlyList<string> PermissionsAny => new[] { PermissionCodes.ExecutiveView };
        public override string WrapsEndpoint => "ExecutiveReportController.Digest";
        public override TimeSpan CacheTtl => TimeSpan.FromSeconds(60);
        public override IReadOnlyList<string> MetricCodes => new[] { "stale_plan", "unpaid_invoice", "diff_loss", "bottleneck", "stock_gold_negative" };

        protected override async Task<AiExecutiveDigestOutput> RunAsync(AiMetalInput input, AiToolContext context, CancellationToken ct)
        {
            var service = context.Services.GetRequiredService<IExecutiveDigestService>();
            var r = await service.Digest(new Digest.Request { Metal = MetalClassifier.Normalize(input.Metal) });
            return Map(r);
        }

        public static AiExecutiveDigestOutput Map(Digest.Response r) => new AiExecutiveDigestOutput
        {
            AsOf = r.AsOf,
            Metal = r.Metal,
            Domains = r.Domains.Select(d => new AiDigestDomain
            {
                Key = d.Key,
                NameTh = AiDigestMessages.Domains.TryGetValue(d.Key, out var n) ? n : d.Key,
                Status = d.Status,
                ProblemCount = d.ProblemCount,
                ForecastCount = d.ForecastCount,
                Available = d.Available,
            }).ToList(),
            Top = r.Top.Select(MapItem).ToList(),
        };

        // Params เป็น Dictionary<string, object> ที่อาจมีชื่อช่าง/ลูกค้า → เก็บเฉพาะค่าตัวเลข (แยกตัวเลขเงินไปหมวดอ่อนไหว) ตัดทุกอย่างที่เป็นข้อความทิ้ง
        public static AiDigestItem MapItem(Digest.TopItem t)
        {
            var item = new AiDigestItem
            {
                Domain = t.Domain,
                DomainTh = AiDigestMessages.Domains.TryGetValue(t.Domain, out var n) ? n : t.Domain,
                Severity = t.Severity,
                Tab = t.Tab,
                Code = t.Code,
                Message = AiDigestMessages.For(t.Code),
            };

            var receivable = t.Code.StartsWith("SAL_", StringComparison.Ordinal) || t.Code.StartsWith("FC_SAL_", StringComparison.Ordinal);
            foreach (var kv in t.Params ?? new Dictionary<string, object>())
            {
                if (!TryNumber(kv.Value, out var value)) continue;
                var metric = new AiMetric { Name = kv.Key, Value = value };
                if (!IsMoneyKey(kv.Key)) item.Metrics.Add(metric);
                else if (receivable) item.ReceivableMetrics.Add(metric);
                else item.CostMetrics.Add(metric);
            }
            return item;
        }

        // allowlist: key ที่ "ชัดเจนว่าไม่ใช่เงิน" เท่านั้นที่เข้า Metrics ทั่วไป — นอกนั้น (รวม key ใหม่ที่ยังไม่รู้จัก) ถือเป็นเงินและเข้าหมวดอ่อนไหวโดย default
        // key ที่มีคำเกี่ยวกับเงินชนะ allowlist เสมอ (เช่น costPerGram ไม่ถือว่าปลอดภัยเพราะมีคำว่า gram)
        private static readonly string[] MoneyWords = { "money", "thb", "baht", "amount", "wage", "cost", "price", "value", "outstanding", "revenue" };
        private static readonly string[] SafeWords =
        {
            "count", "percent", "pct", "ratio", "share", "days", "months", "gram", "qty", "wip", "jobs", "pieces", "plans", "workers", "lines",
            "overdue", "atrisk", "duesoon",
        };

        public static bool IsMoneyKey(string key)
        {
            var k = key.ToLowerInvariant();
            if (MoneyWords.Any(k.Contains)) return true;
            return !SafeWords.Any(k.Contains);
        }

        private static bool TryNumber(object? value, out decimal number)
        {
            number = 0;
            switch (value)
            {
                case null: return false;
                case JsonElement je when je.ValueKind == JsonValueKind.Number: return je.TryGetDecimal(out number);
                case Newtonsoft.Json.Linq.JValue jv when jv.Type == Newtonsoft.Json.Linq.JTokenType.Integer || jv.Type == Newtonsoft.Json.Linq.JTokenType.Float:
                    number = Convert.ToDecimal(jv.Value); return true;
                case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                    try { number = Convert.ToDecimal(value); return true; } catch (OverflowException) { return false; }
                default: return false;
            }
        }
    }
}
