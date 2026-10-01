using System;

namespace jewelry.Model.Production.Insight.CostCardPendingPlans
{
    // field ของ StalePlans.Item ทั้งหมด (ไม่เติม LastUpdateBy/LastAction/Workers — ดู comment เดียวกับ
    // DeliveryLatePlans.Item) + costCardDate (เข้า 95 ครั้งแรก) / daysSinceCostCard — ไม่กรอง stale ออก (ตามสั่ง
    // "keep all") เรียงจาก daysSinceCostCard มากไปน้อยเป็นค่าเริ่มต้น
    public class Item : jewelry.Model.Report.Executive.StalePlans.Item
    {
        public DateTime CostCardDate { get; set; }
        public double DaysSinceCostCard { get; set; }
    }
}
