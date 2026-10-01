using System;

namespace jewelry.Model.Production.Insight.DeliveryLatePlans
{
    // field ของ StalePlans.Item ทั้งหมด (PlanId, Wo, ... ) + เพิ่มด้านล่าง — แผนเหล่านี้ "เสร็จแล้ว" (done) จึงไม่
    // เติม LastUpdateBy/LastAction/LastActionRemark/Workers (enrichment นั้นออกแบบไว้สำหรับแผนที่ยังเปิดอยู่) —
    // LastMoveDate/DaysSinceMove ยังคำนวณให้ตามปกติ (มาจาก max header create_date เดิม ใช้ร่วมกันได้)
    public class Item : jewelry.Model.Report.Executive.StalePlans.Item
    {
        public DateTime RequestDate { get; set; }
        public DateTime DoneDate { get; set; }

        // จำนวนวันที่ช้ากว่ากำหนด (เทียบวันที่ไทย) — แถวในรายการนี้ค่านี้ > 0 เสมอ
        public double LateDays { get; set; }
    }
}
