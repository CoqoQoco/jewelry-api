-- =============================================
-- Migration: AI Connector — tool get_customer_production_status — TK202611270006
-- Date: 2026-11-27
-- Description: เปิด scope module:production (allow_pat=TRUE, PAT ขอได้ — PAT ถือ data:* ไม่ได้จึงค้นลูกค้าได้เฉพาะรหัส)
--   + glossary customer_open_plan / melted_plan
--   ต้อง run ก่อน deploy API ที่มี tool นี้ (tool ต้องใช้ scope ที่ active)
-- Re-run safety: Idempotent (UPDATE ตามค่าที่กำหนด / ON CONFLICT DO NOTHING)
-- Rollback: 20261127_02_ai_customer_production_status_rollback.sql
-- =============================================

UPDATE tbm_ai_scope
SET name_th = 'ข้อมูลงานผลิตรายใบงาน (สถานะใบงานตามลูกค้า)',
    description_th = 'ดูข้อมูลงานผลิตรายใบงาน (สถานะใบงานตามลูกค้า) — ไม่รวมชื่อลูกค้า (ต้องอนุญาตชื่อลูกค้าแยกต่างหาก ไม่เช่นนั้นค้นได้เฉพาะรหัสลูกค้า)',
    allow_pat = TRUE,
    is_active = TRUE
WHERE code = 'module:production';

INSERT INTO tbm_ai_scope (code, name_th, description_th, kind, allow_pat, is_active, sort)
SELECT 'module:production', 'ข้อมูลงานผลิตรายใบงาน (สถานะใบงานตามลูกค้า)',
    'ดูข้อมูลงานผลิตรายใบงาน (สถานะใบงานตามลูกค้า) — ไม่รวมชื่อลูกค้า (ต้องอนุญาตชื่อลูกค้าแยกต่างหาก ไม่เช่นนั้นค้นได้เฉพาะรหัสลูกค้า)',
    'MODULE', TRUE, TRUE, 20
WHERE NOT EXISTS (SELECT 1 FROM tbm_ai_scope WHERE code = 'module:production');

INSERT INTO tbm_ai_glossary (metric_code, module, name_th, definition_th, caveat_th, update_date) VALUES
    ('customer_open_plan', 'production', 'ใบงานค้างของลูกค้า',
     'จำนวนใบงานผลิตที่ยังค้างของลูกค้า ใช้ใบเปิดจากแท็บ WIP แต่ไม่นับ รอ CVD/CVD ใบทดสอบ และใบปิดย้อนหลัง (ตรงกับตัวเลขใบค้างในหน้า Production Insight) · ใบที่หลอมนับแยกเป็น melted_plan',
     'ตัวเลขนี้ไม่รวมใบที่หลอม (ดู melted_plan) · วันที่ขยับล่าสุดมาจากการบันทึกสถานะล่าสุดของใบงาน', NOW()),
    ('melted_plan', 'production', 'ใบงานที่หลอม',
     'หลอม = ใบงานที่ยกเลิกแล้วนำทองกลับไปหลอม นับแยกเป็น meltedCount ไม่รวมในใบงานค้าง',
     'ไม่ใช่ใบที่ผลิตสำเร็จ และไม่ควรนำไปบวกกับใบงานค้าง', NOW())
ON CONFLICT (metric_code) DO NOTHING;
