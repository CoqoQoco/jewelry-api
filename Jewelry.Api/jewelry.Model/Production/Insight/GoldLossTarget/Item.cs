using System;

namespace jewelry.Model.Production.Insight.GoldLossTarget
{
    public class Item
    {
        public int WorkerType { get; set; }
        public string Metal { get; set; } = "GOLD";
        public decimal TargetPercent { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public string CreateBy { get; set; } = null!;
        public string? Remark { get; set; }
    }
}
