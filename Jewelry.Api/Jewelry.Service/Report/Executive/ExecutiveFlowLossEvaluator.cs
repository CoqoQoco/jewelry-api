using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Report.Executive
{
    // Pure — loss เทียบเป้าหมายรายแผนก x โลหะ (ไม่แตะ DB)
    // รับเฉพาะแถวที่ "คืนแล้ว" ในช่วงที่ต้องการ (ผู้เรียกกรองมาแล้ว)
    public static class ExecutiveFlowLossEvaluator
    {
        public const decimal MinWorkerSendG = 250m;
        public const decimal MinWorkerDeltaPp = 1.0m;

        public class LossCell
        {
            public string Dept { get; set; } = null!;
            public string Metal { get; set; } = null!;
            public string? Worker { get; set; }
            public int Rows { get; set; }
            public decimal Send { get; set; }
            public decimal Check { get; set; }
        }

        public class LossRowResult
        {
            public string Name { get; set; } = null!;
            public string? Code { get; set; }
            public bool IsTotal { get; set; }
            public int Rows { get; set; }
            public decimal SendG { get; set; }
            public decimal LossG { get; set; }
            public decimal? LossPercent { get; set; }
            public decimal? DeltaPp { get; set; }

            // delta ที่ยังไม่ปัดเศษ — ใช้ตัดสินเกณฑ์ "เกินเป้า"
            public decimal? RawDeltaPp { get; set; }
            public decimal ExcessG { get; set; }
        }

        public class DeptMetalResult
        {
            public string Dept { get; set; } = null!;
            public string Metal { get; set; } = null!;
            public decimal? TargetPercent { get; set; }
            public bool IsSystemDefault { get; set; }
            public List<LossRowResult> Rows { get; set; } = new List<LossRowResult>();
            public LossRowResult Total => Rows.First(r => r.IsTotal);
        }

        // loss% = (send - check) / send * 100 (ไม่ปัด)
        public static decimal? RawLossPercent(decimal send, decimal check)
        {
            return send > 0 ? (send - check) / send * 100m : (decimal?)null;
        }

        // ส่วนเกินเป้า (กรัม) = max(0, send * (loss% - target%) / 100) = max(0, loss - send * target / 100)
        public static decimal ExcessG(decimal send, decimal check, decimal? targetPercent)
        {
            if (!targetPercent.HasValue || send <= 0) return 0m;
            var excess = (send - check) - send * targetPercent.Value / 100m;
            return excess > 0 ? excess : 0m;
        }

        public static LossRowResult BuildRow(string name, string? code, bool isTotal, int rows, decimal send, decimal check, decimal? target)
        {
            var raw = RawLossPercent(send, check);
            decimal? rawDelta = raw.HasValue && target.HasValue ? raw.Value - target.Value : (decimal?)null;
            return new LossRowResult
            {
                Name = name,
                Code = code,
                IsTotal = isTotal,
                Rows = rows,
                SendG = ExecutiveFlowNormalizer.Round2(send),
                LossG = ExecutiveFlowNormalizer.Round2(send - check),
                LossPercent = raw.HasValue ? Math.Round(raw.Value, 2, MidpointRounding.AwayFromZero) : (decimal?)null,
                RawDeltaPp = rawDelta,
                DeltaPp = rawDelta.HasValue ? Math.Round(rawDelta.Value, 2, MidpointRounding.AwayFromZero) : (decimal?)null,
                ExcessG = ExecutiveFlowNormalizer.Round2(ExcessG(send, check, target))
            };
        }

        // targetFor(dept, metal) -> null = ไม่มีเป้าหมายระดับแผนก (เช่น trim)
        // isNotNamedWorker: true = คิว/ว่าง/test (ไม่นับเป็นช่างจริง); nameOf: รหัสช่าง -> ชื่อแสดง
        public static List<DeptMetalResult> Evaluate(
            IEnumerable<LossCell> cells,
            IEnumerable<string> depts,
            IEnumerable<string> metals,
            Func<string, string, (decimal Target, bool IsSystemDefault)?> targetFor,
            Func<string?, bool> isNotNamedWorker,
            Func<string, string> nameOf)
        {
            var list = cells.ToList();
            var metalList = metals.ToList();
            var results = new List<DeptMetalResult>();

            foreach (var dept in depts)
            {
                foreach (var metal in metalList)
                {
                    var scoped = list.Where(c => c.Dept == dept && c.Metal == metal).ToList();
                    var target = targetFor(dept, metal);
                    decimal? targetPercent = target?.Target;

                    var result = new DeptMetalResult
                    {
                        Dept = dept,
                        Metal = metal,
                        TargetPercent = targetPercent,
                        IsSystemDefault = target?.IsSystemDefault ?? false
                    };

                    result.Rows.Add(BuildRow("TOTAL", null, true, scoped.Sum(c => c.Rows), scoped.Sum(c => c.Send), scoped.Sum(c => c.Check), targetPercent));

                    var workers = scoped
                        .Where(c => !isNotNamedWorker(c.Worker))
                        .GroupBy(c => c.Worker!.Trim())
                        .Select(g => BuildRow(nameOf(g.Key), g.Key, false, g.Sum(c => c.Rows), g.Sum(c => c.Send), g.Sum(c => c.Check), targetPercent))
                        .Where(r => r.SendG >= MinWorkerSendG)
                        .OrderByDescending(r => r.ExcessG)
                        .ThenByDescending(r => r.SendG)
                        .ToList();

                    result.Rows.AddRange(workers);
                    results.Add(result);
                }
            }

            return results;
        }

        public static decimal TotalExcessG(IEnumerable<DeptMetalResult> results)
        {
            return ExecutiveFlowNormalizer.Round2(results.Sum(r => r.Total.ExcessG));
        }

        // ช่างจริงที่ส่ง >= 250 g และ loss% สูงกว่าเป้า >= 1.0 pp (นับต่อ แผนก x โลหะ)
        public static int CountWorkersOverTarget(IEnumerable<DeptMetalResult> results)
        {
            return results.Sum(r => r.Rows.Count(w =>
                !w.IsTotal && w.SendG >= MinWorkerSendG && w.RawDeltaPp.HasValue && w.RawDeltaPp.Value >= MinWorkerDeltaPp));
        }
    }
}
