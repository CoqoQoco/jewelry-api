using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.AiConnector.Sensitivity
{
    // หมวดข้อมูลอ่อนไหว → scope + permission ที่ต้องมี "ทั้งคู่" ถึงเปิดให้ AI เห็น
    public static class SensitiveCategoryPolicy
    {
        public sealed record Rule(SensitiveCategory Category, string Scope, string Permission, string NameTh);

        public static readonly IReadOnlyList<Rule> Rules = new List<Rule>
        {
            new Rule(SensitiveCategory.CustomerName, AiScopes.DataCustomerNames, PermissionCodes.ExecutiveView, "ชื่อลูกค้า"),
            new Rule(SensitiveCategory.WorkerName, AiScopes.DataWorkerNames, PermissionCodes.ExecutiveView, "ชื่อช่าง"),
            new Rule(SensitiveCategory.Cost, AiScopes.DataCost, PermissionCodes.SaleViewMargin, "ต้นทุนและกำไร"),
            new Rule(SensitiveCategory.Receivable, AiScopes.DataReceivables, PermissionCodes.ExecutiveView, "ยอดลูกหนี้ค้างรับ"),
        };

        public static Rule For(SensitiveCategory category) => Rules.First(r => r.Category == category);

        public static Rule? ForScope(string scope) => Rules.FirstOrDefault(r => r.Scope == scope);

        public static bool IsAllowed(SensitiveCategory category, ISet<string> scopes, ISet<string> permissions)
        {
            var rule = For(category);
            return scopes.Contains(rule.Scope) && permissions.Contains(rule.Permission);
        }
    }
}
