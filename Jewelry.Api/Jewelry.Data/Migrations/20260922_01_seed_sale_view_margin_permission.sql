-- =============================================
-- Migration: Seed sale:view-margin Permission + SaleManager Role
-- Date: 2026-09-22
-- Description: เพิ่ม permission ให้เห็น Markup / ส่วนลด % / ต้นทุน ในหน้าใบเสนอราคา/ใบสั่งขาย (UI-only permission)
--   ต้อง run migration นี้ "ก่อน" deploy UI ที่เช็ค permission sale:view-margin
--   ⚠️ user ต้อง logout + login ใหม่หลัง run migration นี้ ถึงจะได้ permission ชุดใหม่
--      (permission cache ที่ localStorage permissions-dk, roles ฝังอยู่ใน JWT ตอน login)
--   SaleManager role มีแค่ permission เดียว (sale:view-margin) — user ที่จะได้สิทธินี้
--      ต้องมี role "Sale" ควบคู่ไปด้วย ไม่งั้นจะเข้าหน้าขายไม่ได้เลย (SaleManager ไม่มี sale:view/sale:create)
-- Re-run safety: Idempotent — ON CONFLICT DO NOTHING, รันซ้ำได้ไม่ error
-- =============================================

-- 1. Insert permission
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('sale:view-margin', 'ดู Markup / ส่วนลด % / ต้นทุน (ใบเสนอราคา/ใบสั่งขาย)', 'Sale', 'system')
ON CONFLICT (code) DO NOTHING;

-- 2. Create role SaleManager
--    tbm_user_role: id ไม่มี default (ต้องระบุเอง), create_date ไม่มี default (ต้องระบุเอง), is_active default FALSE (ต้องระบุ TRUE เอง)
--    prod ปัจจุบันมี id 1-6 และ 999 อยู่แล้ว จึงใช้ id = 7
--    level = 100 เท่ากับ role Sale เดิม — ห้ามให้สิทธิเพิ่มเกินกว่านี้
INSERT INTO tbm_user_role (id, name, description, level, is_active, create_date, create_by)
SELECT 7, 'SaleManager', 'หัวหน้าฝ่ายขาย — เห็น Markup / ส่วนลด % / ต้นทุน ในใบเสนอราคาและใบสั่งขาย', 100, TRUE, NOW(), 'system'
WHERE NOT EXISTS (SELECT 1 FROM tbm_user_role WHERE name = 'SaleManager')
ON CONFLICT (id) DO NOTHING;

-- 3. Grant sale:view-margin เฉพาะ role Dev และ SaleManager เท่านั้น (lookup id จากชื่อ ไม่ hardcode id)
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name IN ('Dev', 'SaleManager') AND r.is_active = TRUE
    AND p.code = 'sale:view-margin' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT r.id, r.name, r.level, r.is_active
-- FROM tbm_user_role r
-- JOIN tbt_role_permission rp ON rp.role_id = r.id
-- JOIN tbm_permission p ON p.id = rp.permission_id
-- WHERE p.code = 'sale:view-margin'
-- ORDER BY r.id;
