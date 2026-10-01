using System;

namespace Jewelry.Service.Production.Insight
{
    public class GoldLossTargetRow
    {
        public int WorkerType { get; set; }

        // 'GOLD' | 'SILVER'
        public string Metal { get; set; } = null!;
        public decimal TargetPercent { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public string CreateBy { get; set; } = null!;
        public string? Remark { get; set; }
    }
}
