-- =============================================
-- Rollback: 20261128_03_fix_duplicate_gem_outbound_6911008_13.sql
-- Ticket: TK202611280001
-- Description: คืนแถวพลอย SD0109 / PIO261127022 ของแผน 11800 (6911008-13) จาก bak_tk202611280001_plan_gem
--              ตาราง backup ไม่ถูกลบ — ลบเองด้วยมือทีหลัง (DROP TABLE bak_tk202611280001_plan_gem) เมื่อมั่นใจแล้ว
-- =============================================

BEGIN;

INSERT INTO tbt_production_plan_status_detail_gem
SELECT *
FROM bak_tk202611280001_plan_gem
WHERE production_plan_id = 11800
  AND gem_code = 'SD0109'
  AND outbound_running = 'PIO261127022'
ON CONFLICT (production_plan_id, item_no) DO NOTHING;

SELECT item_no, header_id, gem_code, gem_qty, gem_weight, gem_price, outbound_running, is_active
FROM tbt_production_plan_status_detail_gem
WHERE production_plan_id = 11800
ORDER BY outbound_running, item_no;

COMMIT;
