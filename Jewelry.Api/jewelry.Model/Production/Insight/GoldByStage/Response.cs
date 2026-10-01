using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.GoldByStage
{
    public class Response
    {
        public List<DepartmentItem> Departments { get; set; } = new List<DepartmentItem>();
        public List<SeriesItem> Series { get; set; } = new List<SeriesItem>();
    }

    public class DepartmentItem
    {
        public string DeptKey { get; set; } = null!;

        // true เฉพาะ gemSort — ไม่ชั่งน้ำหนักเลย (send=check เสมอ) diffGram/diffPercent เป็น null เสมอ
        public bool NotWeighed { get; set; }

        // true เฉพาะ trim — ผลต่างรวมเศษทองหล่อ/ตะไบที่คืนเข้าหลอม (บันทึกราคาที่ cost ของการหล่อ ไม่ผูกกับ trim
        // โดยตรง) หักลบไม่ได้ — UI ควรแสดงหมายเหตุนี้
        public bool IncludesScrap { get; set; }

        // น้ำหนักส่งรวมทุกแถว (รวมที่ยังไม่คืน/pending ด้วย)
        public decimal SendGram { get; set; }

        // น้ำหนักรับคืนจริง (เฉพาะแถวที่คืนแล้ว)
        public decimal ReceivedGram { get; set; }

        // น้ำหนักส่งของเฉพาะแถวที่คืนแล้ว (ตัวหารของ diffPercent — ไม่รวม pending ต่างจาก SendGram)
        public decimal ReturnedSendGram { get; set; }

        // null ถ้า notWeighed
        public decimal? DiffGram { get; set; }
        public decimal? DiffPercent { get; set; }

        // เป้าหมาย % รายแผนก (scope=STAGE) — null สำหรับ trim/gemSort (ไม่มีเป้าหมาย)
        public decimal? TargetPercent { get; set; }

        // 'saved' | 'draft' — มีความหมายเฉพาะแผนกที่มี targetPercent (rawPolish/setting/plating)
        public string? TargetSource { get; set; }

        // % loss จากฝั่งใบ gold loss (scope=SLIP) ของแผนก/โลหะ/ช่วงเดียวกัน — มีเฉพาะ trim(50)/setting(80) —
        // null สำหรับ rawPolish/gemSort/plating (ไม่มีใบ gold loss คู่กัน)
        public decimal? SlipLossPercent { get; set; }

        // ทั้งหมด (รวม placeholder/ไม่มีช่าง)
        public int PendingCount { get; set; }
        public decimal PendingGram { get; set; }

        // ย่อยจาก pending: มีช่างจริงถือครองอยู่ (ทองออกไปกับคนแล้ว รอคืน)
        public int PendingWithWorkerCount { get; set; }
        public decimal PendingWithWorkerGram { get; set; }

        // ย่อยจาก pending: ยังอยู่ในคิว (รหัส placeholder เช่น "รอจ่ายขัดชุบ 9K" หรือไม่มีช่างเลย — ยังไม่ออกให้ใครจริง)
        public int PendingQueueCount { get; set; }
        public decimal PendingQueueGram { get; set; }

        // งานที่ diff% > 3×median ของแผนก (เฉพาะแถวคืนแล้ว) และ diffGram >= 0.20g — เฉพาะที่มีช่างจริงถือครอง
        // (ไม่คำนวณให้ trim/gemSort = 0 เสมอ)
        public int OutlierCount { get; set; }

        // outlier ที่ตรงเกณฑ์เดียวกันแต่ถือครองโดย placeholder/ไม่มีช่าง — ยัง loss จริงแต่หาตัวคนรับผิดชอบไม่ได้
        // แยกนับต่างหากไม่ปนกับ OutlierCount
        public int OutlierUnassignedCount { get; set; }

        public List<WorkerItem> Workers { get; set; } = new List<WorkerItem>();
    }

    // top 10 ตาม diffPercent มากสุด (เฉพาะช่างจริงที่มีงาน >= 5 แถวในช่วง — ไม่รวม placeholder/ไม่มีช่าง)
    public class WorkerItem
    {
        public string WorkerCode { get; set; } = null!;
        public int Rows { get; set; }
        public decimal ReturnedSendGram { get; set; }
        public decimal DiffGram { get; set; }
        public decimal? DiffPercent { get; set; }
    }

    public class SeriesItem
    {
        public DateTime BucketEnd { get; set; }
        public string DeptKey { get; set; } = null!;

        // null ถ้า notWeighed หรือไม่มีแถวคืนแล้วใน bucket นี้เลย
        public decimal? DiffPercent { get; set; }
        public decimal ReturnedSendGram { get; set; }
    }
}
