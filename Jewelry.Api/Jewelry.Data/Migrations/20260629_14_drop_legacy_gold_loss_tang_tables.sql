-- =============================================
-- Migration: Drop Legacy Gold Loss Tang Tables
-- Date: 2026-06-29
-- Description: ลบตารางเดิมที่ไม่มีข้อมูล (verified empty ใน jewelry_dev ก่อน run)
--              tbt_gold_loss_item, tbt_gold_loss_header, tbt_gold_loss_monthly_report
--              ถูกแทนที่ด้วย tbt_gold_loss_tang_slip (aggregate slip ต่อช่าง) และ
--              tbt_gold_loss_monthly_report ยังคงโครงสร้างใหม่ใน tbt_gold_loss_monthly_report
-- Note: tbt_gold_loss_monthly_report เป็นคนละตารางกับ GoldLossMonthlyReport feature
--       ซึ่งยังคงใช้งานอยู่ผ่าน tbt_gold_loss_monthly_report — ดู PlanService.GetGoldLossMonthlyReport
-- =============================================

-- Drop tbt_gold_loss_item ก่อน (FK → tbt_gold_loss_header)
DROP TABLE IF EXISTS tbt_gold_loss_item;

-- Drop tbt_gold_loss_header
DROP TABLE IF EXISTS tbt_gold_loss_header;

-- Note: tbt_gold_loss_monthly_report ยังคงใช้งาน (GoldLossMonthlyReport feature)
-- ไม่ drop ตารางนี้
