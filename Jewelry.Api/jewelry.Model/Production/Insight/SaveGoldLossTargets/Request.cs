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
        // 'SLIP' (default) | 'STAGE'
        public string Scope { get; set; } = "SLIP";
        public int WorkerType { get; set; }
        public string Metal { get; set; } = "GOLD";
        public decimal TargetPercent { get; set; }
    }
}
