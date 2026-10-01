using System;

namespace jewelry.Model.Production.Insight.DeliveryAtRiskPlans
{
    // field ของ StalePlans.Item ทั้งหมด (PlanId, Wo, ..., LastUpdateBy/LastAction/Workers ฯลฯ) + เพิ่มด้านล่าง
    public class Item : jewelry.Model.Report.Executive.StalePlans.Item
    {
        public DateTime RequestDate { get; set; }
        public string? CurrentDeptKey { get; set; }
        public double DaysInCurrentDept { get; set; }

        // เวลาที่เหลือประมาณการ (วัน) จากตอนนี้ถึง projectedFinishDate
        public double RemainingDays { get; set; }
        public DateTime ProjectedFinishDate { get; set; }

        // จำนวนวันที่คาดว่าจะช้ากว่ากำหนด (เทียบวันที่ไทย) — แถวในรายการนี้ค่านี้ > 0 เสมอ (ตามนิยาม at-risk)
        public double ProjectedLateDays { get; set; }
    }
}
