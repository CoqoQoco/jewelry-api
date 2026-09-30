namespace jewelry.Model.Production.Insight.AbnormalDwellPlans
{
    // field ของ StalePlans.Item ทั้งหมด (PlanId, Wo, ..., LastUpdateBy/LastAction/Workers ฯลฯ) + เพิ่มด้านล่าง
    public class Item : jewelry.Model.Report.Executive.StalePlans.Item
    {
        public string DeptKey { get; set; } = null!;
        public double DaysInDept { get; set; }

        // null = แยกรอ/ทำงานไม่ได้ (ไม่มี receive_date และไม่ใช่กรณีที่รู้ชัดว่ายังรออยู่)
        public double? WaitDays { get; set; }
        public double? WorkDays { get; set; }
        public decimal StandardDays { get; set; }
    }
}
