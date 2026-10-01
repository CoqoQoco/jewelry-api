using System;

namespace Jewelry.Service.Production.Insight
{
    public class DeliveryTargetRow
    {
        public decimal TargetPercent { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public string CreateBy { get; set; } = null!;
        public string? Remark { get; set; }
    }
}
