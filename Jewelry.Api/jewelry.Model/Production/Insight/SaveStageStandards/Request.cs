using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.SaveStageStandards
{
    public class Request
    {
        public List<Item> Items { get; set; } = new List<Item>();
        public string? Remark { get; set; }
    }

    public class Item
    {
        public string DeptKey { get; set; } = null!;
        public decimal StandardDays { get; set; }
    }
}
