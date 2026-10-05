using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Jewelry.Service.Production.Insight
{
    // Pure — ไม่แตะ DB, รับข้อมูลที่โหลดมาแล้วเข้ามาเป็น argument (unit-testable)
    //
    // จับคู่ requirement (tbt_production_plan_material, gem lines) กับ stock จริง (tbt_stock_gem) — เป็นการจับคู่
    // แบบประมาณ (approximate) ไม่รู้เกรด/คุณภาพเพชรพลอยละเอียด ไม่หัก reservation ของแผนอื่นออก (ดู FACTS ในสเปก) —
    // ใช้บอกแค่ "มีของแบบนี้อยู่ในคลังบ้างไหม พอไหม" ระดับภาพรวมเท่านั้น
    public static class ProductionMaterialGemEvaluator
    {
        public const string StatusReady = "ready";
        public const string StatusShort = "short";
        public const string StatusUnmatched = "unmatched";

        public class StockGemRow
        {
            public string Code { get; set; } = null!;
            public string GroupName { get; set; } = null!;
            public string Shape { get; set; } = null!;
            public string Grade { get; set; } = null!;
            public string Size { get; set; } = null!;
            public decimal Quantity { get; set; }
        }

        public class MatchResult
        {
            // 'ready' | 'short' | 'unmatched'
            public string Status { get; set; } = StatusUnmatched;

            // null เฉพาะ unmatched — ready/short มีค่าเสมอ (short คือเจอ spec ตรงแล้วแต่จำนวนไม่พอ ไม่ใช่ 0 อีกต่อไป)
            public decimal? AvailableQty { get; set; }

            // รหัส stock ที่ตรง shape+size+metal เป๊ะ (ใช้ดู usage ย้อนหลังต่อ — ว่างถ้า unmatched)
            public List<string> MatchedStockCodes { get; set; } = new List<string>();

            // เฉพาะ unmatched: 'gem' (ไม่รู้จักรหัสพลอยนี้เลย หรือ group_name+metal ไม่มี stock ชนิดนี้อยู่เลย) |
            // 'spec' (รู้จักชนิดพลอยนี้ มี stock อยู่ แต่ shape/size ที่เขียนไว้ไม่ตรงกับ stock แถวไหนเลย — ปัญหาข้อมูล/
            // การเขียน spec ไม่ตรงกัน เช่น "3.0" vs "3.0-3.5" คนละ format, รหัส shape ไม่ตรงกัน) — null ถ้า ready/short
            public string? UnmatchedReason { get; set; }
        }

        // metal: stock.grade ILIKE 'silver%' → SILVER else GOLD
        public static string ClassifyStockMetal(string? grade)
        {
            var trimmed = (grade ?? string.Empty).TrimStart();
            return trimmed.StartsWith("silver", StringComparison.OrdinalIgnoreCase) ? "SILVER" : "GOLD";
        }

        // upper, ตัดช่องว่างทั้งหมด — ใช้เทียบ shape ตรงๆ และเป็น input ของ SizeMatches
        public static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "");

        // เท่ากันพอดี (หลัง normalize) หรือ material size เป็นตัวเลขที่อยู่ในช่วง stock size แบบ "a-b"
        public static bool SizeMatches(string? materialSize, string? stockSize)
        {
            var matNorm = Normalize(materialSize);
            var stockNorm = Normalize(stockSize);
            if (matNorm.Length == 0 || stockNorm.Length == 0) return false;
            if (matNorm == stockNorm) return true;

            var dashIdx = stockNorm.IndexOf('-');
            if (dashIdx > 0 && dashIdx < stockNorm.Length - 1)
            {
                var loStr = stockNorm.Substring(0, dashIdx);
                var hiStr = stockNorm.Substring(dashIdx + 1);
                if (decimal.TryParse(loStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lo)
                    && decimal.TryParse(hiStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var hi)
                    && decimal.TryParse(matNorm, NumberStyles.Any, CultureInfo.InvariantCulture, out var matVal))
                {
                    var low = Math.Min(lo, hi);
                    var high = Math.Max(lo, hi);
                    return matVal >= low && matVal <= high;
                }
            }

            return false;
        }

        // gemCode: tbt_production_plan_material.gem (FK tbm_gem.code) — metal: ผลลัพธ์จาก
        // ProductionGoldLossEvaluator.ClassifyMetal(material.gold) — requiredQty: จำนวนที่ต้องใช้ (รวมแล้วถ้าเป็นระดับ spec)
        //
        // 2 ขั้น: (1) หา candidate ตาม group_name (ILIKE name_en||'%') + metal ก่อน — ถ้าไม่เจอเลย = unmatched
        // reason='gem' (ไม่รู้จักรหัสพลอยนี้ในคลังเลย ไม่ว่า shape/size ใด — ตรงกับ FACTS: DIA/RU(SO)/BT ไม่อยู่ใน
        // tbm_gem หรือชื่อไม่ตรง) (2) จาก candidate กรอง shape+size ต่อ — ถ้าไม่เหลือเลย = unmatched reason='spec'
        // (รู้จักชนิดพลอยนี้ มี stock อยู่ แต่เขียน shape/size ไม่ตรงกับ stock แถวไหนเลย — ปัญหาข้อมูล ไม่ใช่ของขาดจริง)
        // — ถ้าเหลือ sum quantity เทียบ requiredQty: พอ = ready, ไม่พอ = short (available ไม่ใช่ 0 แล้ว มีค่าจริง)
        public static MatchResult Match(
            string? gemCode, string? shape, string? size, string metal, decimal requiredQty,
            IReadOnlyList<StockGemRow> stock, IReadOnlyDictionary<string, string> gemNamesByCode)
        {
            if (string.IsNullOrWhiteSpace(gemCode)) return new MatchResult { Status = StatusUnmatched, AvailableQty = null, UnmatchedReason = "gem" };

            var code = gemCode.Trim().ToUpperInvariant();
            if (!gemNamesByCode.TryGetValue(code, out var nameEn) || string.IsNullOrWhiteSpace(nameEn))
            {
                return new MatchResult { Status = StatusUnmatched, AvailableQty = null, UnmatchedReason = "gem" };
            }

            var candidates = stock
                .Where(s => s.GroupName.StartsWith(nameEn, StringComparison.OrdinalIgnoreCase) && ClassifyStockMetal(s.Grade) == metal)
                .ToList();

            if (candidates.Count == 0)
            {
                return new MatchResult { Status = StatusUnmatched, AvailableQty = null, UnmatchedReason = "gem" };
            }

            var shapeNorm = Normalize(shape);
            var specific = candidates
                .Where(s => Normalize(s.Shape) == shapeNorm && SizeMatches(size, s.Size))
                .ToList();

            if (specific.Count == 0)
            {
                return new MatchResult { Status = StatusUnmatched, AvailableQty = null, UnmatchedReason = "spec" };
            }

            var available = specific.Sum(s => s.Quantity);
            return new MatchResult
            {
                Status = available >= requiredQty ? StatusReady : StatusShort,
                AvailableQty = available,
                MatchedStockCodes = specific.Select(s => s.Code).Distinct().ToList(),
                UnmatchedReason = null
            };
        }

        // worst ของหลาย status ในแผน/กลุ่มเดียวกัน — short > unmatched > ready
        public static string WorstStatus(IEnumerable<string> statuses)
        {
            var list = statuses.ToList();
            if (list.Contains(StatusShort)) return StatusShort;
            if (list.Contains(StatusUnmatched)) return StatusUnmatched;
            return StatusReady;
        }
    }
}
