using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Production.Insight
{
    // Pure — ไม่แตะ DB, รับ header rows ที่ query มาแล้วเข้ามาเป็น argument (unit-testable)
    public static class ProductionPlanFlowCalculator
    {
        public class HeaderRow
        {
            public int ProductionPlanId { get; set; }
            public int Status { get; set; }
            public DateTime CreateDate { get; set; }
        }

        // inflow[d]  = curr.status ∈ d และ prev.status ∉ d (แถวแรกสุดของแผนที่โหลดมา นับเป็น inflow เข้าแผนกนั้น)
        // outflow[d] = prev.status ∈ d และ curr.status ∉ d
        // นับเฉพาะ transition ที่ curr.CreateDate อยู่ในช่วง [windowStartUtc, now) — caller โหลด header
        // ล่วงหน้ามาแค่ 180 วัน ดังนั้น prev ของ transition ในหน้าต่าง 90 วันมักจะอยู่ในชุดข้อมูลนี้แล้ว
        public static Dictionary<string, (int Inflow, int Outflow)> ComputeFlow(
            IEnumerable<HeaderRow> headers,
            Func<int, string?> departmentKeyOf,
            DateTime windowStartUtc)
        {
            var result = new Dictionary<string, (int Inflow, int Outflow)>();

            void AddInflow(string key)
            {
                result.TryGetValue(key, out var current);
                result[key] = (current.Inflow + 1, current.Outflow);
            }

            void AddOutflow(string key)
            {
                result.TryGetValue(key, out var current);
                result[key] = (current.Inflow, current.Outflow + 1);
            }

            var byPlan = headers.GroupBy(h => h.ProductionPlanId);
            foreach (var planGroup in byPlan)
            {
                var ordered = planGroup.OrderBy(h => h.CreateDate).ToList();
                string? prevDept = null;

                foreach (var curr in ordered)
                {
                    var currDept = departmentKeyOf(curr.Status);

                    if (curr.CreateDate >= windowStartUtc)
                    {
                        if (currDept != null && currDept != prevDept) AddInflow(currDept);
                        if (prevDept != null && prevDept != currDept) AddOutflow(prevDept);
                    }

                    prevDept = currDept;
                }
            }

            return result;
        }
    }
}
