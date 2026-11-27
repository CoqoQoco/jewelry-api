-- =============================================
-- Migration: AI Connector — ทำให้ข้อความ scope module:production + glossary ตรงกับ fresh install — TK202611270006 (รอบ 3)
-- Date: 2026-11-27
-- Description: 20261127_02 รันบน prod ไปแล้ว (ข้อความที่รันอาจเป็นรุ่นเก่า) จึงตั้งทุกคอลัมน์ข้อความที่ 02 กำหนดซ้ำที่นี่
--   - tbm_ai_scope module:production: name_th, description_th (ป้ายที่หน้า consent แสดง)
--   - tbm_ai_glossary customer_open_plan / melted_plan: name_th, definition_th, caveat_th
-- Re-run safety: Idempotent (UPSERT ค่าคงที่)
-- Rollback: 20261127_03_ai_production_scope_label_glossary_rollback.sql
-- =============================================

INSERT INTO tbm_ai_scope (code, name_th, description_th, kind, allow_pat, is_active, sort) VALUES
    ('module:production', 'ข้อมูลงานผลิตรายใบงาน (สถานะใบงานตามลูกค้า)',
     'ดูข้อมูลงานผลิตรายใบงาน (สถานะใบงานตามลูกค้า) — ไม่รวมชื่อลูกค้า (ต้องอนุญาตชื่อลูกค้าแยกต่างหาก ไม่เช่นนั้นค้นได้เฉพาะรหัสลูกค้า)',
     'MODULE', TRUE, TRUE, 20)
ON CONFLICT (code) DO UPDATE
SET name_th = EXCLUDED.name_th,
    description_th = EXCLUDED.description_th;

INSERT INTO tbm_ai_glossary (metric_code, module, name_th, definition_th, caveat_th, update_date) VALUES
    ('customer_open_plan', 'production', 'ใบงานค้างของลูกค้า',
     'จำนวนใบงานผลิตที่ยังค้างของลูกค้า ใช้ใบเปิดจากแท็บ WIP แต่ไม่นับ รอ CVD/CVD ใบทดสอบ และใบปิดย้อนหลัง (ตรงกับตัวเลขใบค้างในหน้า Production Insight) · ใบที่หลอมนับแยกเป็น melted_plan',
     'ตัวเลขนี้ไม่รวมใบที่หลอม (ดู melted_plan) · วันที่ขยับล่าสุดมาจากการบันทึกสถานะล่าสุดของใบงาน', NOW()),
    ('melted_plan', 'production', 'ใบงานที่หลอม',
     'หลอม = ใบงานที่ยกเลิกแล้วนำทองกลับไปหลอม นับแยกเป็น meltedCount ไม่รวมในใบงานค้าง',
     'ไม่ใช่ใบที่ผลิตสำเร็จ และไม่ควรนำไปบวกกับใบงานค้าง', NOW())
ON CONFLICT (metric_code) DO UPDATE
SET module = EXCLUDED.module,
    name_th = EXCLUDED.name_th,
    definition_th = EXCLUDED.definition_th,
    caveat_th = EXCLUDED.caveat_th,
    update_date = NOW();
