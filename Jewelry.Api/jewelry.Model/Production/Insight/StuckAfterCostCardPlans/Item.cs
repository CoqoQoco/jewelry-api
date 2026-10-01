using System;

namespace jewelry.Model.Production.Insight.StuckAfterCostCardPlans
{
    // field ของ StalePlans.Item ทั้งหมด + เพิ่มด้านล่าง (ไม่เติม LastUpdateBy/LastAction/Workers — ดู comment
    // เดียวกับ DeliveryLatePlans.Item)
    public class Item : jewelry.Model.Report.Executive.StalePlans.Item
    {
        public DateTime CostCardDate { get; set; }
        public double DaysSinceCostCard { get; set; }
    }
}
