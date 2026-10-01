using System;

namespace Jewelry.Service.Production.Insight
{
    public class GoldLossTargetRow
    {
        // 'SLIP' (เป้าหมายใบ gold loss เดิม) | 'STAGE' (เป้าหมายรายแผนก จ่าย-รับ ใหม่)
        public string Scope { get; set; } = null!;
        public int WorkerType { get; set; }

        // 'GOLD' | 'SILVER'
        public string Metal { get; set; } = null!;
        public decimal TargetPercent { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public string CreateBy { get; set; } = null!;
        public string? Remark { get; set; }
    }
}
