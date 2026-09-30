using System;

namespace Jewelry.Service.Production.Insight
{
    public class StageStandardRow
    {
        public string DeptKey { get; set; } = null!;
        public decimal StandardDays { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public string CreateBy { get; set; } = null!;
        public string? Remark { get; set; }
    }
}
