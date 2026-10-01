using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Production.Insight
{
    // Pure — ไม่แตะ DB, รับข้อมูลที่โหลดมาแล้วเข้ามาเป็น argument (unit-testable)
    //
    // Loss รายแผนก (จ่าย-รับ) — ต่างจากใบ gold loss (slip): ดึงตรงจาก status_detail ของ "งาน" ที่ส่งออกแผนกนั้นๆ
    // ไม่ผ่านใบสรุป — กฎข้อมูล match กับ Production/Plan GetGoldLossByStageReport เดิม (header active, detail
    // active, gold_weight_send>0 — เพิ่ม gold ไม่ว่างเป็นเงื่อนไขใหม่เฉพาะของหน้านี้) + เพิ่มมิติโลหะ/worker/
    // bucket รายเดือนที่ของเดิมไม่มี
    public static class ProductionGoldStageEvaluator
    {
        public const string DeptTrim = "trim";
        public const string DeptRawPolish = "rawPolish";
        public const string DeptGemSort = "gemSort";
        public const string DeptSetting = "setting";
        public const string DeptPlating = "plating";

        public static readonly string[] AllDepartments = { DeptTrim, DeptRawPolish, DeptGemSort, DeptSetting, DeptPlating };

        // gemSort ไม่เคยชั่งน้ำหนักจริง (ระบบบันทึก check = send เสมอ) — diff ที่ได้ไม่ใช่ loss จริง ต้องซ่อน
        public static readonly HashSet<string> NotWeighedDepts = new HashSet<string> { DeptGemSort };

        // trim ผลต่างปนเศษทองหล่อ/ตะไบที่คืนเข้าหลอม บันทึกที่ cost การหล่อ ไม่ได้ผูกกับ trim โดยตรง หักลบไม่ได้
        public static readonly HashSet<string> ScrapDepts = new HashSet<string> { DeptTrim };

        // outlier detection ไม่ทำกับ trim (ปนเศษ ทำให้ per-row diff ไม่มีความหมายเทียบกันตรงๆ) และ gemSort (ไม่ชั่งจริง)
        public static readonly HashSet<string> OutlierExcludedDepts = new HashSet<string> { DeptTrim, DeptGemSort };

        // รหัสช่างบางตัวใน tbm_worker เป็น "คิวรองาน" ไม่ใช่คนจริง (name_th ขึ้นต้นด้วย "รอ" เช่น CG9K="รอจ่าย
        // ขัดชุบ 9K", BG18K="รอจ่ายฝัง 18K") — ของที่อยู่กับรหัสพวกนี้ = ยังไม่ออกให้ใครจริง ไม่ใช่ loss ของช่างคนใด
        public static bool IsPlaceholderWorker(string? code, IReadOnlyDictionary<string, string> workerNames)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            return workerNames.TryGetValue(code.Trim(), out var name) && !string.IsNullOrEmpty(name) && name.StartsWith("รอ", StringComparison.Ordinal);
        }

        // รหัส/ชื่อช่างที่เป็นข้อมูลทดสอบ (เช่น code="TEST", name_th="TEST TEST TEST") — ไม่ใช่ช่างจริงและไม่ใช่
        // คิวรองานที่ถูกต้องตามกระบวนการ (ต่างจาก placeholder "รอจ่าย...") — ยืนยันจาก prod ว่าของ "ค้างที่ช่าง"
        // ทั้งหมดในบางแผนกเป็นรหัสนี้
        public static bool IsTestWorker(string? code, IReadOnlyDictionary<string, string> workerNames)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            var trimmed = code.Trim();
            if (trimmed.Contains("TEST", StringComparison.OrdinalIgnoreCase)) return true;
            return workerNames.TryGetValue(trimmed, out var name) && !string.IsNullOrEmpty(name) && name.Contains("TEST", StringComparison.OrdinalIgnoreCase);
        }

        // ว่าง/null (ไม่มีรหัสช่างเลย) หรือ placeholder หรือ test worker — รวมกันคือ "ไม่ใช่ช่างจริงที่โทษ/นับได้"
        // (placeholder/empty = ของค้างคิวจริงตามกระบวนการ, test = ข้อมูลทดสอบปนมา — ทั้งคู่ไม่นับเป็น "คน")
        public static bool IsQueueOrEmptyWorker(string? code, IReadOnlyDictionary<string, string> workerNames)
        {
            return string.IsNullOrWhiteSpace(code) || IsPlaceholderWorker(code, workerNames) || IsTestWorker(code, workerNames);
        }

        public class StageRow
        {
            public string DeptKey { get; set; } = null!;
            public int HeaderId { get; set; }
            public DateTime HeaderCreateDate { get; set; }
            public DateTime? DetailRequestDate { get; set; }
            public string? WorkerCode { get; set; }
            public decimal SendGram { get; set; }
            public decimal? CheckGram { get; set; }
            public bool IsReturned { get; set; }
            public int PlanId { get; set; }
            public string? Wo { get; set; }
            public int WoNumber { get; set; }
            public string? WoText { get; set; }
            public string? ProductName { get; set; }
            public string? ProductNumber { get; set; }
            public string? CustomerNumber { get; set; }
        }

        public class DeptStats
        {
            public string DeptKey { get; set; } = null!;
            public bool NotWeighed { get; set; }
            public bool IncludesScrap { get; set; }
            public decimal SendGram { get; set; }
            public decimal ReceivedGram { get; set; }
            public decimal ReturnedSendGram { get; set; }
            public decimal? DiffGram { get; set; }
            public decimal? DiffPercent { get; set; }
            public int PendingCount { get; set; }
            public decimal PendingGram { get; set; }

            // pending ที่มีช่างจริงถือครองอยู่ (ทองออกไปกับคนแล้ว รอแค่คืน)
            public int PendingWithWorkerCount { get; set; }
            public decimal PendingWithWorkerGram { get; set; }

            // pending ที่ยังอยู่ในคิว (placeholder หรือไม่มีช่างเลย — ยังไม่ออกให้ใครจริง)
            public int PendingQueueCount { get; set; }
            public decimal PendingQueueGram { get; set; }
        }

        public static DeptStats ComputeDeptStats(string deptKey, IReadOnlyList<StageRow> rows, IReadOnlyDictionary<string, string> workerNames)
        {
            var notWeighed = NotWeighedDepts.Contains(deptKey);
            var returned = rows.Where(r => r.IsReturned).ToList();
            var pending = rows.Where(r => !r.IsReturned).ToList();
            var pendingWithWorker = pending.Where(r => !IsQueueOrEmptyWorker(r.WorkerCode, workerNames)).ToList();
            var pendingQueue = pending.Where(r => IsQueueOrEmptyWorker(r.WorkerCode, workerNames)).ToList();

            var sendGram = rows.Sum(r => r.SendGram);
            var receivedGram = returned.Sum(r => r.CheckGram ?? 0);
            var returnedSendGram = returned.Sum(r => r.SendGram);
            var diffGram = returnedSendGram - receivedGram;

            decimal? diffPercentOut = null;
            decimal? diffGramOut = null;
            if (!notWeighed)
            {
                diffGramOut = Round(diffGram);
                diffPercentOut = returnedSendGram > 0 ? Math.Round(diffGram / returnedSendGram * 100, 2) : (decimal?)null;
            }

            return new DeptStats
            {
                DeptKey = deptKey,
                NotWeighed = notWeighed,
                IncludesScrap = ScrapDepts.Contains(deptKey),
                SendGram = Round(sendGram),
                ReceivedGram = Round(receivedGram),
                ReturnedSendGram = Round(returnedSendGram),
                DiffGram = diffGramOut,
                DiffPercent = diffPercentOut,
                PendingCount = pending.Count,
                PendingGram = Round(pending.Sum(r => r.SendGram)),
                PendingWithWorkerCount = pendingWithWorker.Count,
                PendingWithWorkerGram = Round(pendingWithWorker.Sum(r => r.SendGram)),
                PendingQueueCount = pendingQueue.Count,
                PendingQueueGram = Round(pendingQueue.Sum(r => r.SendGram))
            };
        }

        // median ของ diff% ต่อแถว (เฉพาะแถวคืนแล้ว, send>0) — ใช้เป็น baseline เทียบ outlier
        public static decimal? ComputeMedianDiffPercent(IReadOnlyList<StageRow> returnedRows)
        {
            var percents = returnedRows
                .Where(r => r.SendGram > 0)
                .Select(r => (double)((r.SendGram - (r.CheckGram ?? 0)) / r.SendGram * 100))
                .ToList();

            return percents.Count > 0 ? (decimal)ProductionStageLeadTimeEvaluator.Median(percents) : (decimal?)null;
        }

        public class OutlierRow
        {
            public StageRow Row { get; set; } = null!;
            public decimal DiffGram { get; set; }
            public decimal DiffPercent { get; set; }
            public decimal DeptMedianPercent { get; set; }
        }

        // row diff% > medianMultiplier × dept median diff% AND diffGram >= minDiffGram — แยกผลลัพธ์เป็น 2 กลุ่ม:
        // Attributed (มีช่างจริงถือครอง — โทษได้) กับ Unassigned (placeholder/ไม่มีช่าง — ยัง loss จริงแต่หาตัวคนไม่ได้
        // ไม่ควรเอาไปนับรวมกับของที่มีคนรับผิดชอบ)
        public static (List<OutlierRow> Attributed, List<OutlierRow> Unassigned) ComputeOutliers(
            IReadOnlyList<StageRow> returnedRows, IReadOnlyDictionary<string, string> workerNames, decimal medianMultiplier, decimal minDiffGram)
        {
            var medianPercent = ComputeMedianDiffPercent(returnedRows);
            if (!medianPercent.HasValue) return (new List<OutlierRow>(), new List<OutlierRow>());

            var threshold = medianPercent.Value * medianMultiplier;
            var attributed = new List<OutlierRow>();
            var unassigned = new List<OutlierRow>();

            foreach (var row in returnedRows)
            {
                if (row.SendGram <= 0) continue;
                var diffGram = row.SendGram - (row.CheckGram ?? 0);
                var diffPercent = diffGram / row.SendGram * 100;
                if (diffPercent > threshold && diffGram >= minDiffGram)
                {
                    var outlier = new OutlierRow
                    {
                        Row = row,
                        DiffGram = Round(diffGram),
                        DiffPercent = Math.Round(diffPercent, 2),
                        DeptMedianPercent = Math.Round(medianPercent.Value, 2)
                    };

                    if (IsQueueOrEmptyWorker(row.WorkerCode, workerNames)) unassigned.Add(outlier);
                    else attributed.Add(outlier);
                }
            }

            return (attributed, unassigned);
        }

        public class WorkerStats
        {
            public string WorkerCode { get; set; } = null!;
            public int Rows { get; set; }
            public decimal ReturnedSendGram { get; set; }
            public decimal DiffGram { get; set; }
            public decimal? DiffPercent { get; set; }
        }

        // top N ตาม diffPercent มากสุด (เฉพาะช่างที่มีงาน >= minRows แถวในช่วง, เฉพาะแถวคืนแล้ว) — ไม่รวม
        // placeholder/ไม่มีช่าง (ไม่ใช่คนจริง จัดอันดับไม่ได้)
        public static List<WorkerStats> ComputeTopWorkers(
            IReadOnlyList<StageRow> returnedRows, IReadOnlyDictionary<string, string> workerNames, int minRows, int topCount)
        {
            return returnedRows
                .Where(r => !IsQueueOrEmptyWorker(r.WorkerCode, workerNames))
                .GroupBy(r => r.WorkerCode!.Trim())
                .Select(g =>
                {
                    var sendSum = g.Sum(r => r.SendGram);
                    var checkSum = g.Sum(r => r.CheckGram ?? 0);
                    var diff = sendSum - checkSum;
                    return new WorkerStats
                    {
                        WorkerCode = g.Key,
                        Rows = g.Count(),
                        ReturnedSendGram = Round(sendSum),
                        DiffGram = Round(diff),
                        DiffPercent = sendSum > 0 ? Math.Round(diff / sendSum * 100, 2) : (decimal?)null
                    };
                })
                .Where(w => w.Rows >= minRows)
                .OrderByDescending(w => w.DiffPercent ?? 0)
                .Take(topCount)
                .ToList();
        }

        public class SeriesPoint
        {
            public DateTime BucketEnd { get; set; }
            public string DeptKey { get; set; } = null!;
            public decimal? DiffPercent { get; set; }
            public decimal ReturnedSendGram { get; set; }
        }

        // bucket ตาม header.create_date (เดียวกับขอบเขตช่วงทั้งหน้า) — เฉพาะแถวคืนแล้ว
        public static List<SeriesPoint> ComputeSeries(
            string deptKey, bool notWeighed, IReadOnlyList<StageRow> rows, DateTime start, IReadOnlyList<DateTime> bucketEnds)
        {
            var result = new List<SeriesPoint>();
            var prev = start;

            foreach (var bucketEnd in bucketEnds)
            {
                var bucketReturned = rows
                    .Where(r => r.IsReturned && r.HeaderCreateDate > prev && r.HeaderCreateDate <= bucketEnd)
                    .ToList();

                var sendSum = bucketReturned.Sum(r => r.SendGram);
                var checkSum = bucketReturned.Sum(r => r.CheckGram ?? 0);
                var diff = sendSum - checkSum;

                decimal? diffPercent = (notWeighed || sendSum <= 0) ? (decimal?)null : Math.Round(diff / sendSum * 100, 2);

                result.Add(new SeriesPoint
                {
                    BucketEnd = bucketEnd,
                    DeptKey = deptKey,
                    DiffPercent = diffPercent,
                    ReturnedSendGram = Round(sendSum)
                });

                prev = bucketEnd;
            }

            return result;
        }

        // แถว pending (ยังไม่คืน) ที่ค้างนานเกิน thresholdDays
        public static List<StageRow> ComputePendingOverDays(IReadOnlyList<StageRow> rows, int thresholdDays, DateTime now)
        {
            return rows.Where(r => !r.IsReturned && (now - r.HeaderCreateDate).TotalDays > thresholdDays).ToList();
        }

        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
