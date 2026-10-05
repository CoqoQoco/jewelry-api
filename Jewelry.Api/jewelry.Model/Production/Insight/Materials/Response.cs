using System;
using System.Collections.Generic;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace jewelry.Model.Production.Insight.Materials
{
    public class Response
    {
        public DateTime AsOf { get; set; }

        // 'critical' | 'warning' | 'ok'
        public string Status { get; set; } = "ok";

        public List<Wip.Finding> Problems { get; set; } = new List<Wip.Finding>();
        public List<Wip.Finding> Forecasts { get; set; } = new List<Wip.Finding>();
        public List<Wip.ActionItem> Actions { get; set; } = new List<Wip.ActionItem>();

        public KpiData Kpi { get; set; } = new KpiData();
        public List<SeriesItem> Series { get; set; } = new List<SeriesItem>();
    }

    public class KpiData
    {
        // แผนที่สถานะ 69/70 (gemSort), ไม่ stale, ยังไม่มีการเบิกพลอยจริง (type 7) เลย — snapshot ตอนนี้ ไม่ขึ้นกับช่วง
        public int WaitingPlans { get; set; }

        // median ของ (ตอนนี้ - วันที่เข้าแผนกตัดพลอยครั้งแรก [สถานะ 69 หรือ 70]) ของ waitingPlans ด้านบน
        public double? WaitingMedianDays { get; set; }

        // เข้าสถานะ 70 (ทำงานจริง) ครั้งแรก → เบิกพลอยจริง (type 7) ครั้งแรก — เฉพาะแผนที่เข้า 70 ในช่วง [start,end]
        // และมีการเบิกจริงแล้ว (ไม่รวมที่ยังไม่เบิก) — null ถ้าไม่มีคู่ไหนเลยในช่วง
        public double? IssueMedianDays { get; set; }
        public double? IssueP90Days { get; set; }

        // แผนที่ยังไม่ผ่านแผนกตัดพลอย (สถานะ 10,49,50,59,60,69,70) ไม่ stale — ทุกบรรทัดวัตถุดิบพลอยจับคู่ของได้และพอ
        // (รวมแผนที่ยังไม่ถึงแผนกตัดพลอยด้วย — กว้างกว่า ReadyWaitingPlans ด้านล่าง)
        public int ReadyPlans { get; set; }

        // ย่อยจาก ReadyPlans: เฉพาะที่อยู่แผนกตัดพลอยแล้ว (สถานะ 69/70) และยังไม่เบิกพลอยจริง (type 7) เลย —
        // ตัวเลขที่ขับ MAT_READY_NOT_ISSUED/ACT_ISSUE_READY ("พร้อมแล้ว แต่ยังไม่มีใครไปเบิก")
        public int ReadyWaitingPlans { get; set; }

        // อย่างน้อย 1 บรรทัดวัตถุดิบพลอยมี spec ตรงกับ stock แต่จำนวนไม่พอ (short) — นับแผน (ไม่ซ้ำ) ที่ worst status = short
        public int ShortPlans { get; set; }

        // บรรทัดวัตถุดิบพลอย (ไม่ใช่แผน) ที่มี spec ตรงกับ stock แต่จำนวนไม่พอ
        public int ShortLines { get; set; }

        // บรรทัดที่หา stock จับคู่ไม่ได้เลย (ไม่รู้จักรหัสพลอยนี้ ไม่มี stock ชนิดนี้เลย หรือมีแต่ shape/size ไม่ตรงกับ
        // stock แถวไหนเลย) = UnmatchedByGem + UnmatchedBySpec
        public int UnmatchedLines { get; set; }

        // ย่อยจาก UnmatchedLines: ไม่รู้จักรหัสพลอยนี้เลย หรือไม่มี stock ชนิดนี้อยู่เลย (ไม่ว่า shape/size ใด)
        public int UnmatchedByGem { get; set; }

        // ย่อยจาก UnmatchedLines: รู้จักชนิดพลอยนี้ มี stock อยู่ แต่เขียน shape/size ไม่ตรงกับ stock แถวไหนเลย
        // (ปัญหาข้อมูล/การเขียน spec ไม่ตรงกัน เช่น "3.0" vs "3.0-3.5")
        public int UnmatchedBySpec { get; set; }

        // = TotalLines - UnmatchedLines (มี spec ตรงกับ stock จริง ไม่ว่าจะพอหรือไม่พอ)
        public int MatchedLines { get; set; }
        public int TotalLines { get; set; }

        // รหัส stock พลอยที่ quantity>0, มีการเบิก (type 7) ใน 90 วันล่าสุด, และ cover < 30 วัน ที่ rate การเบิกปัจจุบัน
        public int LowCoverCount { get; set; }
    }

    public class SeriesItem
    {
        public DateTime BucketEnd { get; set; }
        public int Entered { get; set; }

        // null ถ้าไม่มี entry ที่มีคู่ issue ใน bucket นี้
        public double? IssueMedianDays { get; set; }
        public double? IssueP90Days { get; set; }
    }
}
