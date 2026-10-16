using jewelry.Model.Report.Executive.Flow;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Jewelry.Service.Report.Executive
{
    // Pure — กฎ "ความน่าเชื่อถือของข้อมูล" ของหน้า Flow: คืนเฉพาะกฎที่ถูกกระตุ้น (ไม่แตะ DB)
    public static class ExecutiveFlowTrustEvaluator
    {
        // threshold ของกฎแบบสัดส่วน (กฎแบบ "มีอย่างน้อย 1" ใช้ > 0)
        public const decimal StockCostMissingFailShare = 50m;
        public const decimal OverdueShareWarn = 90m;
        public const decimal IdleShareWarn = 25m;
        public const decimal TypeMissingShareWarn = 10m;
        public const decimal MoldStaleShareWarn = 50m;
        public const decimal StockOldShareInfo = 25m;

        public class NegativeGoldRow
        {
            public string Code { get; set; } = null!;
            public string SizeCode { get; set; } = null!;
            public decimal Weight { get; set; }
        }

        public class UsdRateOneInvoice
        {
            public string CustomerName { get; set; } = null!;
            public string Date { get; set; } = null!;
            public int Items { get; set; }
        }

        public class Input
        {
            public List<NegativeGoldRow> NegativeGoldRows { get; set; } = new List<NegativeGoldRow>();

            public int InStockPieces { get; set; }
            public int InStockCostMissing { get; set; }

            public List<UsdRateOneInvoice> UsdRateOneInvoices { get; set; } = new List<UsdRateOneInvoice>();

            public int OpenPcs { get; set; }
            public int OpenPlans { get; set; }
            public int IdlePcs { get; set; }
            public int IdlePlans { get; set; }
            public int TypeMissingPcs { get; set; }
            public int TestWorkerPlans { get; set; }
            public int OverduePcs { get; set; }

            public int MoldInProgressPlans { get; set; }
            public int MoldInProgressStalePlans { get; set; }

            public int NotReceivedPcs { get; set; }
            public int NotReceivedJobs { get; set; }

            public int InvoicesTotal { get; set; }
            public int InvoicesNoTotal { get; set; }
            public int InvoicesNoSalePerson { get; set; }
            public int InvoicesNoChannel { get; set; }

            public int StockOldPieces { get; set; }
            public int? OldestProductionYear { get; set; }
        }

        public static List<FlowTrustItem> Evaluate(Input i)
        {
            var list = new List<FlowTrustItem>();

            // ---- fail ----
            if (i.NegativeGoldRows.Count > 0)
            {
                var evidence = string.Join(", ", i.NegativeGoldRows.Take(12).Select(r => $"{r.Code}/{r.SizeCode} {Num(r.Weight, 2)} g"));
                list.Add(Item("fail", "material", "gold-stock-negative", "สต็อกทองติดลบ",
                    $"{i.NegativeGoldRows.Count} แถว: {evidence}", "คลังวัตถุดิบ"));
            }

            var costShare = ExecutiveFlowNormalizer.Percent(i.InStockCostMissing, i.InStockPieces);
            if (costShare.HasValue && costShare.Value > StockCostMissingFailShare)
            {
                list.Add(Item("fail", "stock", "stock-cost-missing", "สินค้าในสต็อกไม่มีต้นทุน",
                    $"{Num(i.InStockCostMissing)} จาก {Num(i.InStockPieces)} ชิ้น ({Pct(costShare)})", "คลังสินค้า/บัญชีต้นทุน"));
            }

            if (i.UsdRateOneInvoices.Count > 0)
            {
                var evidence = string.Join("; ", i.UsdRateOneInvoices.Take(5).Select(r => $"{r.CustomerName} {r.Date}, {Num(r.Items)} รายการ"));
                list.Add(Item("fail", "sales", "usd-invoice-rate-1", "บิล US$ ที่อัตราแลกเปลี่ยน = 1",
                    $"{i.UsdRateOneInvoices.Count} บิล: {evidence}", "ฝ่ายขาย/บัญชี"));
            }

            // ---- warn ----
            var idleShare = ExecutiveFlowNormalizer.Percent(i.IdlePcs, i.OpenPcs);
            if (idleShare.HasValue && idleShare.Value >= IdleShareWarn)
            {
                list.Add(Item("warn", "production", "open-idle-180d", "งานค้างไม่ขยับ >= 180 วัน",
                    $"{Num(i.IdlePcs)} จาก {Num(i.OpenPcs)} ชิ้น ({Pct(idleShare)}), {Num(i.IdlePlans)} จาก {Num(i.OpenPlans)} ใบงาน", "ฝ่ายผลิต"));
            }

            var typeShare = ExecutiveFlowNormalizer.Percent(i.TypeMissingPcs, i.OpenPcs);
            if (typeShare.HasValue && typeShare.Value >= TypeMissingShareWarn)
            {
                list.Add(Item("warn", "production", "open-type-missing", "ใบงานไม่ระบุชนิดโลหะ (plan.type)",
                    $"{Num(i.TypeMissingPcs)} จาก {Num(i.OpenPcs)} ชิ้น ({Pct(typeShare)})", "ฝ่ายผลิต/วางแผน"));
            }

            if (i.TestWorkerPlans > 0)
            {
                list.Add(Item("warn", "production", "open-test-worker", "งานค้างอยู่กับช่างทดสอบ (TEST)",
                    $"{Num(i.TestWorkerPlans)} ใบงาน", "ฝ่ายผลิต"));
            }

            var overdueShare = ExecutiveFlowNormalizer.Percent(i.OverduePcs, i.OpenPcs);
            if (overdueShare.HasValue && overdueShare.Value > OverdueShareWarn)
            {
                list.Add(Item("warn", "production", "open-overdue", "งานค้างเกินกำหนดส่งเกือบทั้งหมด",
                    $"{Num(i.OverduePcs)} จาก {Num(i.OpenPcs)} ชิ้น ({Pct(overdueShare)}) — วันกำหนดส่งอาจไม่ถูกอัปเดต", "ฝ่ายผลิต/วางแผน"));
            }

            var moldShare = ExecutiveFlowNormalizer.Percent(i.MoldInProgressStalePlans, i.MoldInProgressPlans);
            if (moldShare.HasValue && moldShare.Value >= MoldStaleShareWarn)
            {
                list.Add(Item("warn", "mold", "mold-in-progress-stale", "แม่พิมพ์ระหว่างทำที่ไม่ขยับ >= 180 วัน",
                    $"{Num(i.MoldInProgressStalePlans)} จาก {Num(i.MoldInProgressPlans)} แผน ({Pct(moldShare)})", "ฝ่ายแม่พิมพ์"));
            }

            if (i.NotReceivedPcs > 0)
            {
                list.Add(Item("warn", "receive", "done-not-received", "งานเสร็จแต่ยังไม่รับเข้าสต็อก",
                    $"{Num(i.NotReceivedPcs)} ชิ้น / {Num(i.NotReceivedJobs)} ใบงาน", "คลังสินค้า"));
            }

            if (i.InvoicesNoTotal > 0)
            {
                list.Add(Item("warn", "sales", "invoice-no-total", "บิลไม่มียอดรวม",
                    $"{Num(i.InvoicesNoTotal)} จาก {Num(i.InvoicesTotal)} บิล", "บัญชี"));
            }

            if (i.InvoicesNoSalePerson > 0)
            {
                list.Add(Item("warn", "sales", "invoice-no-sale-person", "บิลไม่ระบุผู้ขาย",
                    $"{Num(i.InvoicesNoSalePerson)} จาก {Num(i.InvoicesTotal)} บิล", "ฝ่ายขาย"));
            }

            if (i.InvoicesNoChannel > 0)
            {
                list.Add(Item("warn", "sales", "invoice-no-channel", "บิลไม่ระบุช่องทางขาย",
                    $"{Num(i.InvoicesNoChannel)} จาก {Num(i.InvoicesTotal)} บิล", "ฝ่ายขาย"));
            }

            // ---- info ----
            var oldShare = ExecutiveFlowNormalizer.Percent(i.StockOldPieces, i.InStockPieces);
            if (oldShare.HasValue && oldShare.Value >= StockOldShareInfo)
            {
                list.Add(Item("info", "stock", "stock-old-365d", "สต็อกที่ผลิตมานานกว่า 365 วัน",
                    $"{Num(i.StockOldPieces)} จาก {Num(i.InStockPieces)} ชิ้น ({Pct(oldShare)})", "คลังสินค้า"));
            }

            if (i.OldestProductionYear.HasValue)
            {
                list.Add(Item("info", "stock", "stock-oldest-year", "ปีที่ผลิตเก่าสุดในสต็อก",
                    i.OldestProductionYear.Value.ToString(CultureInfo.InvariantCulture), "คลังสินค้า"));
            }

            return list;
        }

        private static FlowTrustItem Item(string level, string zone, string key, string title, string evidence, string owner)
        {
            return new FlowTrustItem { Level = level, Zone = zone, Key = key, Title = title, Evidence = evidence, Owner = owner };
        }

        private static string Num(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
        private static string Num(decimal value, int decimals) => value.ToString("N" + decimals, CultureInfo.InvariantCulture);
        private static string Pct(decimal? value) => value.HasValue ? value.Value.ToString("0.0", CultureInfo.InvariantCulture) + "%" : "-";
    }
}
