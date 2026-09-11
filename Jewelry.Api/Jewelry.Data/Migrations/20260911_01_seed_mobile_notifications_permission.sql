-- =============================================
-- Migration: Seed mobile:notifications Permission for Additional Roles
-- Date: 2026-09-11
-- Description: ผูก permission mobile:notifications (มีอยู่แล้วใน tbm_permission) เพิ่มให้ role
--   ProductionOperator, Sale, StockOperator เนื่องจากศูนย์แจ้งเตือน (Notification Center) เป็นโมดูลกลาง
--   ที่จะรองรับงานผลิตและคลังในเฟสถัดไป และตอนนี้ role Sale ยังเข้าหน้าแจ้งเตือนบนมือถือไม่ได้
-- Run order: Standalone — ไม่ต้อง run ต่อจากไฟล์ไหน เพราะ permission mobile:notifications
--   มีอยู่แล้วใน tbm_permission (is_active = true) migration นี้แค่เพิ่มความสัมพันธ์ role
-- Re-run safety: Idempotent — ON CONFLICT DO NOTHING, รันซ้ำได้ไม่ error
-- ⚠️ user ต้อง logout + login ใหม่หลัง run migration นี้ ถึงจะได้ permission ชุดใหม่
--    เพราะ permission ถูก cache ไว้ที่ localStorage (permissions-dk) ตอน login เท่านั้น
-- =============================================

-- ผูก mobile:notifications เพิ่มให้ ProductionOperator, Sale, StockOperator
-- (ระบุชื่อ role ตรง ๆ แทนการ copy เงื่อนไขจาก mobile:dashboard เพื่อไม่ให้ผลเปลี่ยนตามถ้ามีคนแก้ mobile:dashboard ในอนาคต)
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name IN ('ProductionOperator', 'Sale', 'StockOperator') AND r.is_active = TRUE
  AND p.code = 'mobile:notifications'
  AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;
