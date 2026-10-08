using System;

namespace Jewelry.Service.Helper
{
    // ตัวจัดประเภทโลหะกลางของทั้งระบบ (ตัวกรอง metal ของหน้า /executive และ ProductionInsight)
    //
    // - plan.Type = tbm_gold.name_en ("Silver", "Yellow Gold", ...) ไม่ใช่ "SV" — แต่ status_detail.gold / slip item /
    //   plan_material.gold ใช้รหัส "SV" — ทั้งสองแบบผ่าน IsSilver ได้ (SV หรือ Silver หรือขึ้นต้น SIL ไม่สนตัวพิมพ์)
    // - แผน/สต็อก: null/ว่าง = ทอง (GOLD) — บรรทัดขายที่ไม่มีชิ้นสินค้า (placeholder/ไม่พบ piece) = UNKNOWN
    public static class MetalClassifier
    {
        public const string All = "ALL";
        public const string Gold = "GOLD";
        public const string Silver = "SILVER";
        public const string Unknown = "UNKNOWN";

        public static bool IsSilver(string? value)
        {
            var t = value?.Trim();
            if (string.IsNullOrEmpty(t)) return false;
            return string.Equals(t, "SV", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, "Silver", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("SIL", StringComparison.OrdinalIgnoreCase);
        }

        // ค่าที่ไม่รู้จัก/ว่าง = ALL (พฤติกรรมเดิมทั้งหมด)
        public static string Normalize(string? metal)
        {
            var t = metal?.Trim().ToUpperInvariant();
            return t == Gold || t == Silver ? t : All;
        }

        // แผน/สต็อก: GOLD หรือ SILVER เท่านั้น (ว่าง = GOLD)
        public static string ForValue(string? value) => IsSilver(value) ? Silver : Gold;

        public static bool Matches(string requestedMetal, string? value)
            => requestedMetal == All || ForValue(value) == requestedMetal;
    }
}
