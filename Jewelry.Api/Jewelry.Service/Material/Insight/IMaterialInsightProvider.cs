using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Material.Insight
{
    public enum MaterialMovementKind
    {
        // รับเข้าจากการซื้อ/รับใหม่
        Inflow,
        // เบิกใช้ (ที่ยังมีผล)
        Consumed,
        // คืนจากการผลิต (ที่ยังมีผล)
        Returned,
        // ยอดยกมา/ปรับยอด/กลับรายการ/รายการที่ถูกกลับรายการแล้ว
        Other
    }

    // Qty เป็นบวกเสมอ, Sign = ผลต่อยอดคงเหลือ (+1 เข้า / -1 ออก) — ใช้ย้อนยอดคงเหลือรายเดือน
    public record MaterialMovement(string ItemKey, DateTime DateUtc, MaterialMovementKind Kind, decimal Qty, int Sign);

    public class MaterialItemInput
    {
        public string ItemKey { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? Name { get; set; }
        public string? SizeCode { get; set; }
        public string? SizeName { get; set; }

        // หน่วยของ OnHand/OnProcess/movement ของ item นี้ (ทอง = g; พลอยตัดสินรายตัว pcs|ct)
        public string Unit { get; set; } = "g";

        // กลุ่มสำหรับ "ตามกลุ่ม" (null = ใช้ Code เป็นกลุ่ม แบบทอง)
        public string? GroupCode { get; set; }
        public string? GroupName { get; set; }

        // ราคาต่อ 1 Unit (บาท); null = provider ไม่มีราคา (ทอง) -> ไม่คำนวณมูลค่า
        public decimal? UnitValue { get; set; }

        // ในคลัง (พร้อมเบิก)
        public decimal OnHand { get; set; }

        // ถืออยู่ในกระบวนการ/ที่ช่าง
        public decimal OnProcess { get; set; }

        // วันที่ item เริ่มมีในคลัง — ใช้เป็นฐานนับอายุของที่ไม่เคยถูกเบิก
        public DateTime? FirstSeenUtc { get; set; }

        // อายุ (วัน) ของของที่ค้างในกระบวนการ; null = provider ไม่มีข้อมูล (ทอง)
        public int? OnProcessAgeDays { get; set; }
    }

    public class MaterialDataset
    {
        public List<MaterialItemInput> Items { get; set; } = new List<MaterialItemInput>();
        public List<MaterialMovement> Movements { get; set; } = new List<MaterialMovement>();

        // true เมื่อ Items[].OnProcessAgeDays มีความหมาย (เปิดใช้กฎ MAT_ON_PROCESS_STALE)
        public bool OnProcessAgeSupported { get; set; }

        // true เมื่อมี movement รับเข้า/ยอดยกมาที่ยังมีผลอย่างน้อย 1 แถว
        public bool InboundRecorded { get; set; }
    }

    // ตัวโหลดข้อมูลเฉพาะประเภทวัตถุดิบ (GOLD ตอนนี้, GEM ภายหลัง) — engine ที่เหลือไม่รู้จักตารางต้นทาง
    public interface IMaterialInsightProvider
    {
        string Kind { get; }
        // unit ระดับ provider (ทอง = g; พลอย = "mixed" เพราะแต่ละ item มี unit ของตัวเอง)
        string Unit { get; }
        Task<MaterialDataset> LoadAsync();
    }
}
