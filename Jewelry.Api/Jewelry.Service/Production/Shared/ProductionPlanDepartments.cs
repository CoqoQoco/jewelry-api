using System.Collections.Generic;
using System.Linq;
using System;

namespace Jewelry.Service.Production.Shared
{
    // Pure helper — แผนก <-> status ids ของ tbt_production_plan (ยกเว้น 100=เสร็จ, 500=หลอม)
    // ใช้ร่วมกันระหว่าง ExecutiveReportService และ ProductionInsightService — ห้ามแก้ mapping นี้
    // โดยไม่เช็คผลกระทบต่อทั้งสองฝั่ง
    public static class ProductionPlanDepartments
    {
        public static readonly (string Key, int[] StatusIds)[] Departments = new[]
        {
            ("design", new[] { 10 }),
            ("trim", new[] { 49, 50 }),
            ("rawPolish", new[] { 59, 60 }),
            ("gemSort", new[] { 69, 70 }),
            ("setting", new[] { 79, 80 }),
            ("plating", new[] { 89, 90 }),
            ("costCard", new[] { 94, 95 }),
        };

        public static string? DepartmentKeyOf(int status)
        {
            foreach (var department in Departments)
            {
                if (department.StatusIds.Contains(status))
                {
                    return department.Key;
                }
            }

            return null;
        }

        public static DateTime LastMoveOf(OpenPlanRow plan, IReadOnlyDictionary<int, DateTime> lastMoveDates)
        {
            return lastMoveDates.TryGetValue(plan.Id, out var lastMove) ? lastMove : (plan.UpdateDate ?? plan.CreateDate);
        }
    }
}
