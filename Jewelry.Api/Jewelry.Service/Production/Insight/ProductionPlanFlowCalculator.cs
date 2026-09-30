using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Production.Insight
{
    // Pure — ไม่แตะ DB, รับ header/plan rows ที่ query มาแล้วเข้ามาเป็น argument (unit-testable)
    public static class ProductionPlanFlowCalculator
    {
        // แผนก "ออกแบบ" ตาม ProductionPlanDepartments.Departments (status 10) — แผนใหม่ทุกแผนเริ่มที่นี่เสมอ
        private const int StatusDesign = 10;

        public class HeaderRow
        {
            public int ProductionPlanId { get; set; }
            public int Status { get; set; }
            public DateTime CreateDate { get; set; }
        }

        public class PlanCreationRow
        {
            public int ProductionPlanId { get; set; }
            public DateTime CreateDate { get; set; }
        }

        // inflow[d]  = curr.status ∈ d และ prev.status ∉ d
        // outflow[d] = prev.status ∈ d และ curr.status ∉ d
        // นับเฉพาะ transition ที่ curr.CreateDate อยู่ในช่วง [windowStartUtc, ...) — caller โหลด header ล่วงหน้า
        // มาเกินขอบเขตจริง (buffer) เพื่อให้ prev ของ transition แรกในหน้าต่างมักจะอยู่ในชุดข้อมูลนี้แล้ว
        //
        // planCreations = แผนที่ create_date ตกในช่วงนี้ (caller กรองมาแล้ว) — แผนที่ "ถือกำเนิด" ในช่วงถือเป็น
        // inflow เข้าแผนกออกแบบเสมอ (เดิมไม่เคยนับ ทำให้แผนกออกแบบมี outflow แต่ไม่มี inflow เลย ไม่ reconcile กับ
        // WIP จริง) — ถ้าพบทั้ง creation และ header แรกของแผนเดียวกันในช่วงเดียวกัน จะต่อกันเป็น transition เดียว
        // (ออกแบบ -> สถานะของ header แรก) แทนที่จะเป็น inflow ลอยๆ 2 ครั้งแยกกัน
        public static Dictionary<string, (int Inflow, int Outflow)> ComputeFlow(
            IEnumerable<HeaderRow> headers,
            IEnumerable<PlanCreationRow> planCreations,
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

            var headersByPlan = headers
                .GroupBy(h => h.ProductionPlanId)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.CreateDate).ToList());

            var creationByPlan = planCreations
                .GroupBy(c => c.ProductionPlanId)
                .ToDictionary(g => g.Key, g => g.First().CreateDate);

            var planIds = new HashSet<int>(headersByPlan.Keys);
            planIds.UnionWith(creationByPlan.Keys);

            var designDept = departmentKeyOf(StatusDesign);

            foreach (var planId in planIds)
            {
                string? prevDept = null;

                if (creationByPlan.TryGetValue(planId, out var createDate))
                {
                    // ถือว่า prev ก่อนถือกำเนิดคือ "ไม่มีอะไรเลย" แล้วเข้าแผนกออกแบบทันที — ต่อ prevDept ไว้ใช้กับ
                    // header แรกของแผนนี้ (ถ้ามี) แทนการปล่อยเป็น null (ซึ่งจะทำให้ header แรกดูเหมือน inflow ลอยๆ
                    // โดยไม่มี outflow จากออกแบบมาคู่กัน)
                    if (createDate >= windowStartUtc && designDept != null && designDept != prevDept)
                    {
                        AddInflow(designDept);
                    }

                    prevDept = designDept;
                }

                if (headersByPlan.TryGetValue(planId, out var planHeaders))
                {
                    foreach (var curr in planHeaders)
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
            }

            return result;
        }
    }
}
