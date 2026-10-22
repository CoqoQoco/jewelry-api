-- Rollback: 20261022_03_seed_material_manage_min_permission.sql
-- ถอนสิทธิ์และลบ permission material:manage-min — รันหลัง rollback API/UI แล้วเท่านั้น
DELETE FROM tbt_role_permission
WHERE permission_id IN (SELECT id FROM tbm_permission WHERE code = 'material:manage-min');

DELETE FROM tbm_permission WHERE code = 'material:manage-min';
