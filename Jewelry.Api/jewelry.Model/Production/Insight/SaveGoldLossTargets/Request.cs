using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.SaveGoldLossTargets
{
    public class Request
    {
        public List<Item> Items { get; set; } = new List<Item>();
        public string? Remark { get; set; }
    }

    public class Item
    {
        public int WorkerType { get; set; }
        public string Metal { get; set; } = "GOLD";
        public decimal TargetPercent { get; set; }
    }
}
