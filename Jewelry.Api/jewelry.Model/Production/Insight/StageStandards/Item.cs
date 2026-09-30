using System;

namespace jewelry.Model.Production.Insight.StageStandards
{
    public class Item
    {
        public string DeptKey { get; set; } = null!;
        public decimal StandardDays { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public string CreateBy { get; set; } = null!;
        public string? Remark { get; set; }
    }
}
