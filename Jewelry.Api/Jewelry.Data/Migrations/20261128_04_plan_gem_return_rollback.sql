-- Rollback: 20261128_04_plan_gem_return.sql
-- Ticket: TK202611280001
-- ลบประวัติการคืนพลอยทั้งหมด (ไม่ย้อนสต็อก/แถวพลอยในใบงาน)

DELETE FROM tbt_role_permission
WHERE permission_id IN (SELECT id FROM tbm_permission WHERE code = 'stock-gem:return-from-plan');
DELETE FROM tbm_permission WHERE code = 'stock-gem:return-from-plan';
DROP TABLE IF EXISTS tbt_production_plan_gem_return;
