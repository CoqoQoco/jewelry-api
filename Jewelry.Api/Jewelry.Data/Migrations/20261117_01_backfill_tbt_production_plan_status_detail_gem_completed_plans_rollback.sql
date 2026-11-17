-- TK202611160005 rollback : ลบแถวพลอยที่กู้ด้วย 20261117_01_backfill_tbt_production_plan_status_detail_gem_completed_plans.sql
BEGIN;

DELETE FROM tbt_production_plan_status_detail_gem
WHERE item_no LIKE 'G-%R20261117%';
-- ควรขึ้น DELETE 349

-- COMMIT;
