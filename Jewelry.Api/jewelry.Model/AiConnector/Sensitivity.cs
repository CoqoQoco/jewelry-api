using System;

namespace jewelry.Model.AiConnector
{
    public enum SensitiveCategory
    {
        CustomerName = 1,
        WorkerName = 2,
        Cost = 3,
        Receivable = 4,
    }

    // property ของ AI DTO ที่เป็นข้อมูลอ่อนไหว — ถูก mask ถ้า caller ไม่มีทั้ง scope และ permission ของหมวดนั้น
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AiSensitiveAttribute : Attribute
    {
        public AiSensitiveAttribute(SensitiveCategory category) { Category = category; }
        public SensitiveCategory Category { get; }
    }

    // property ที่ตรวจแล้วว่าไม่อ่อนไหว
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AiNotSensitiveAttribute : Attribute
    {
    }
}
