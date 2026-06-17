-- =============================================
-- Revert: ลบ permission stock-product:edit (ยกเลิกแนวทาง DB-driven)
-- Date: 2026-06-17
-- =============================================

-- ลบ role mapping ก่อน (FK)
DELETE FROM tbt_role_permission
WHERE permission_id IN (SELECT id FROM tbm_permission WHERE code = 'stock-product:edit');

-- ลบ permission
DELETE FROM tbm_permission WHERE code = 'stock-product:edit';
