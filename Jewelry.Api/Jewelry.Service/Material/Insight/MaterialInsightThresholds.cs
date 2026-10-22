using System;
using jewelry.Model.Material.Insight;
using Jewelry.Data.Models.Jewelry;

namespace Jewelry.Service.Material.Insight
{
    // ค่า policy คลังวัตถุดิบ — default อยู่ที่นี่ที่เดียว (ตรงกับ seed 20261022_02)
    // ลำดับความสำคัญ: request (what-if) > ตาราง tbm_material_stock_policy > default; ทุกค่าถูก clamp
    public static class MaterialInsightThresholds
    {
        public const string SourceDefault = "default";
        public const string SourceDb = "db";
        public const string SourceRequest = "request";

        public const int WindowMonths = 3;
        public const int LeadTimeDays = 14;
        public const int SafetyDays = 7;
        public const decimal FloorQty = 0m;
        public const decimal NearPercent = 125m;
        public const int DeadStockDays = 180;
        public const int ForecastMonths = 3;
        public const int OnProcessStaleDays = 30;
        public const decimal PriceChangePercent = 10m;
        public const decimal ConcentrationPercent = 50m;
        public const int MinHistoryMonths = 3;

        public const int DefaultMonths = 12;
        public const int MinMonths = 3;
        public const int MaxMonths = 36;

        public static int ClampMonths(int months) => Math.Min(MaxMonths, Math.Max(MinMonths, months));

        public static int ClampLeadTimeDays(int days) => Math.Min(365, Math.Max(0, days));

        public static PolicyOverrides FromEntity(TbmMaterialStockPolicy e) => new PolicyOverrides
        {
            WindowMonths = e.WindowMonths,
            LeadTimeDays = e.LeadTimeDays,
            SafetyDays = e.SafetyDays,
            FloorQty = e.FloorQty,
            NearPercent = e.NearPercent,
            DeadStockDays = e.DeadStockDays,
            ForecastMonths = e.ForecastMonths,
            OnProcessStaleDays = e.OnProcessStaleDays,
            PriceChangePercent = e.PriceChangePercent,
            ConcentrationPercent = e.ConcentrationPercent,
            MinHistoryMonths = e.MinHistoryMonths
        };

        public static bool HasAny(PolicyOverrides? o) => o != null
            && (o.WindowMonths.HasValue || o.LeadTimeDays.HasValue || o.SafetyDays.HasValue || o.FloorQty.HasValue
                || o.NearPercent.HasValue || o.DeadStockDays.HasValue || o.ForecastMonths.HasValue
                || o.OnProcessStaleDays.HasValue || o.PriceChangePercent.HasValue
                || o.ConcentrationPercent.HasValue || o.MinHistoryMonths.HasValue);

        // db = แถวจากตาราง (null = ยังไม่มีแถว), request = override ชั่วคราว (null = ไม่มี)
        public static PolicyValues Resolve(PolicyOverrides? db, PolicyOverrides? request)
        {
            var source = HasAny(request) ? SourceRequest : db != null ? SourceDb : SourceDefault;

            return new PolicyValues
            {
                WindowMonths = Clamp(request?.WindowMonths ?? db?.WindowMonths ?? WindowMonths, 1, 24),
                LeadTimeDays = Clamp(request?.LeadTimeDays ?? db?.LeadTimeDays ?? LeadTimeDays, 0, 365),
                SafetyDays = Clamp(request?.SafetyDays ?? db?.SafetyDays ?? SafetyDays, 0, 365),
                FloorQty = Math.Max(0m, request?.FloorQty ?? db?.FloorQty ?? FloorQty),
                NearPercent = Math.Min(1000m, Math.Max(100m, request?.NearPercent ?? db?.NearPercent ?? NearPercent)),
                DeadStockDays = Clamp(request?.DeadStockDays ?? db?.DeadStockDays ?? DeadStockDays, 1, 3650),
                ForecastMonths = Clamp(request?.ForecastMonths ?? db?.ForecastMonths ?? ForecastMonths, 1, 36),
                OnProcessStaleDays = Clamp(request?.OnProcessStaleDays ?? db?.OnProcessStaleDays ?? OnProcessStaleDays, 1, 365),
                PriceChangePercent = Math.Min(100m, Math.Max(0m, request?.PriceChangePercent ?? db?.PriceChangePercent ?? PriceChangePercent)),
                ConcentrationPercent = Math.Min(100m, Math.Max(0m, request?.ConcentrationPercent ?? db?.ConcentrationPercent ?? ConcentrationPercent)),
                MinHistoryMonths = Clamp(request?.MinHistoryMonths ?? db?.MinHistoryMonths ?? MinHistoryMonths, 1, 12),
                Source = source
            };
        }

        private static int Clamp(int v, int min, int max) => Math.Min(max, Math.Max(min, v));
    }
}
