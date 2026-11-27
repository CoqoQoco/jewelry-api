using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Service.AiConnector.Registry;
using Jewelry.Service.AiConnector.Sensitivity;
using Jewelry.Service.Helper;
using Jewelry.Service.Production.Insight;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CustomerStatus = jewelry.Model.Production.Insight.CustomerStatus;

namespace Jewelry.Service.AiConnector.Tools.Production
{
    public class CustomerProductionStatusTool : AiToolBase<AiCustomerStatusInput, AiCustomerStatusOutput>
    {
        public const int MinNameLength = 3;
        public const int MaxQueryLength = 100;
        public const int MaxCustomers = 10;
        public const string NotFoundMessage =
            "ไม่พบลูกค้าที่ตรงกับคำค้น — ค้นด้วยรหัสลูกค้าให้ตรงทั้งคำ หรือค้นด้วยชื่อ (พิมพ์อย่างน้อย 3 ตัวอักษร) เมื่อได้รับอนุญาตให้ดูชื่อลูกค้า";

        public override string Name => "get_customer_production_status";
        public override string TitleTh => "สถานะใบงานผลิตตามลูกค้า";
        public override string Description =>
            "ใบงานผลิตที่ยังค้างของลูกค้า ณ ปัจจุบัน (as-of ตอนเรียก): จำนวนใบค้างทั้งหมด จำนวนตามขั้นตอน (stageCounts) และรายใบงาน (เรียงใบที่ไม่ขยับนานสุดก่อน สูงสุด 200 แถว) " +
            "พร้อมวันที่ขยับล่าสุดและจำนวนวันที่นิ่ง · ใบที่หลอมแยกเป็น meltedCount ไม่รวมในใบค้าง · พารามิเตอร์ customer = ชื่อหรือรหัสลูกค้า " +
            "(ค้นด้วยชื่อต้องได้รับอนุญาตให้ดูชื่อลูกค้า ไม่เช่นนั้นค้นได้เฉพาะรหัสที่ตรงทั้งคำ) · ไม่มีข้อความอิสระของใบงาน · ควรเรียก get_glossary ก่อนตีความตัวเลข";
        public override string InputSchemaJson =>
            "{\"type\":\"object\",\"properties\":{\"customer\":{\"type\":\"string\",\"description\":\"ชื่อหรือรหัสลูกค้า (ชื่อต้องยาวอย่างน้อย 3 ตัวอักษร)\"}},\"required\":[\"customer\"]}";
        public override IReadOnlyList<string> RequiredScopes => new[] { AiScopes.ModuleProduction };
        public override IReadOnlyList<string> PermissionsAny => new[] { PermissionCodes.ExecutiveView, PermissionCodes.ProductionView };
        public override string WrapsEndpoint => "ProductionInsightController.CustomerStatus";
        // ห้าม cache: key ของ pipeline ไม่ผูกสิทธิ์ดูชื่อ และเก็บผลดิบที่ยังไม่ mask — ผลค้นด้วยชื่อจะรั่วข้ามผู้เรียก
        public override TimeSpan CacheTtl => TimeSpan.Zero;
        public override IReadOnlyList<string> MetricCodes => new[] { "customer_open_plan", "melted_plan" };

        public static string BuildNamePattern(string query) => $"%{LikePattern.EscapeLikePattern(query)}%";

        protected override async Task<AiCustomerStatusOutput> RunAsync(AiCustomerStatusInput input, AiToolContext context, CancellationToken ct)
        {
            var query = (input.Customer ?? "").Trim();
            var output = new AiCustomerStatusOutput { AsOf = DateTime.UtcNow, Query = query.Length == 0 ? null : query };
            if (query.Length == 0 || query.Length > MaxQueryLength)
            {
                output.Message = NotFoundMessage;
                return output;
            }

            var lookup = context.Services.GetRequiredService<IAiCustomerLookup>();
            var codes = await lookup.FindByCode(query, ct);

            var nameAllowed = SensitiveCategoryPolicy.IsAllowed(SensitiveCategory.CustomerName, context.Scopes, context.Permissions);
            if (codes.Count == 0 && nameAllowed && query.Length >= MinNameLength)
                codes = await lookup.FindByNamePattern(BuildNamePattern(query), MaxCustomers, ct);

            if (codes.Count == 0)
            {
                output.Message = NotFoundMessage;
                return output;
            }

            var service = context.Services.GetRequiredService<IProductionInsightService>();
            var r = await service.CustomerStatus(new CustomerStatus.Request { CustomerCodes = codes.Take(MaxCustomers).ToList() });
            return Map(r, output);
        }

        public static AiCustomerStatusOutput Map(CustomerStatus.Response r, AiCustomerStatusOutput output)
        {
            output.AsOf = r.AsOf;
            output.Truncated = r.Truncated;
            output.Customers = r.Customers.Select(c => new AiCustomerProduction
            {
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                OpenPlanCount = c.OpenPlanCount,
                MeltedCount = c.MeltedCount,
                Truncated = c.Truncated,
                StageCounts = c.StageCounts.Select(s => new AiStageCount { Status = s.Status, StatusName = s.StatusName, Count = s.Count }).ToList(),
                Plans = c.Plans.Select(p => new AiCustomerPlan
                {
                    Wo = p.Wo,
                    WoNumber = p.WoNumber,
                    ProductNumber = p.ProductNumber,
                    Mold = p.Mold,
                    Status = p.Status,
                    StatusName = p.StatusName,
                    LastMoveDate = p.LastMoveDate,
                    DaysSinceMove = p.DaysSinceMove,
                }).ToList(),
            }).ToList();
            if (output.Customers.Count == 0) output.Message = NotFoundMessage;
            return output;
        }
    }
}
