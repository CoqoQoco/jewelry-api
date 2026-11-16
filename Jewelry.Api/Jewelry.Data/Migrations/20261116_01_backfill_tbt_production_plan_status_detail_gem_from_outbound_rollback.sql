-- TK202611160005 rollback : ลบแถวพลอยที่กู้ด้วย 20261116_01_backfill_tbt_production_plan_status_detail_gem_from_outbound.sql
BEGIN;

DELETE FROM tbt_production_plan_status_detail_gem
WHERE item_no LIKE 'G-%R20261116%';
-- ควรขึ้น DELETE 29

-- COMMIT;
