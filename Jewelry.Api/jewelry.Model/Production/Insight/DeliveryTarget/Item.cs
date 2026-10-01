using System;

namespace jewelry.Model.Production.Insight.DeliveryTarget
{
    public class Item
    {
        public decimal TargetPercent { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public string CreateBy { get; set; } = null!;
        public string? Remark { get; set; }
    }
}
