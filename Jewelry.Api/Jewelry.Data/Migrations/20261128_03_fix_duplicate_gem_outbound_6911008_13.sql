-- =============================================
-- Migration: Fix duplicate gem outbound row in plan 6911008-13
-- Date: 2026-11-28
-- Ticket: TK202611280001
-- Description: แผน 6911008-13 (tbt_production_plan.id = 11800) เบิกพลอย SD0109 (6 pcs, 0.16) ซ้ำ 2 ครั้ง
--              PIO261124005 = ถูกต้อง · PIO261127022 = ซ้ำ (ร้านคืนสต็อกแล้วด้วย InboundGem INB261127005 "6911008-13 ตัดซ้ำ")
--              InboundGem เพิ่มสต็อกอย่างเดียว ไม่แตะแถวในแผน จึงต้องเอาแถวซ้ำออกจากแผน
--              ใช้ DELETE (ไม่ใช่ is_active = false) เพราะ data fix รันก่อน deploy API ใหม่ และ API เดิมยังแสดงแถวที่ is_active = false
--              การลบ (พร้อม backup) จึงมีผลทันที
--              แถวที่ลบสำรองไว้ใน bak_tk202611280001_plan_gem ก่อน
--              ไม่แตะ tbt_stock_gem / tbt_stock_gem_transection (สต็อกถูกต้องแล้ว)
--              ไม่แตะแถว PIO261124005 และ PIO261127032
-- ลำดับ deploy: รันเมื่อไหร่ก็ได้ (ไม่ผูกกับ deploy API) · รันซ้ำได้ผลเท่าเดิม
--              (ไม่มีในตารางจริง + มี 1 แถวใน backup = ข้ามด้วย NOTICE)
-- Guard: จำนวนแถวที่ตรงเงื่อนไข (plan 11800 + SD0109 + PIO261127022) ในตารางจริงต้องเป็น 1 (ทำงาน) หรือ 0 พร้อม backup 1 (ข้าม)
--        นอกนั้น raise exception และ rollback ทั้งหมด
-- Rollback: 20261128_03_fix_duplicate_gem_outbound_6911008_13_rollback.sql
-- =============================================

BEGIN;

-- Pre-check: แถวพลอยทั้งหมดของแผน
SELECT item_no, header_id, gem_code, gem_qty, gem_weight, gem_price, outbound_running, is_active
FROM tbt_production_plan_status_detail_gem
WHERE production_plan_id = 11800
ORDER BY outbound_running, item_no;

CREATE TABLE IF NOT EXISTS bak_tk202611280001_plan_gem AS
SELECT * FROM tbt_production_plan_status_detail_gem WHERE false;

DO $$
DECLARE
    v_live   integer;
    v_backup integer;
BEGIN
    SELECT COUNT(*) INTO v_live
    FROM tbt_production_plan_status_detail_gem
    WHERE production_plan_id = 11800
      AND gem_code = 'SD0109'
      AND outbound_running = 'PIO261127022';

    SELECT COUNT(*) INTO v_backup
    FROM bak_tk202611280001_plan_gem
    WHERE production_plan_id = 11800
      AND gem_code = 'SD0109'
      AND outbound_running = 'PIO261127022';

    IF v_live = 0 AND v_backup = 1 THEN
        RAISE NOTICE 'TK202611280001: already applied (row is in backup, not in live table) — skipped';
        RETURN;
    END IF;

    IF v_live <> 1 THEN
        RAISE EXCEPTION 'TK202611280001: expected exactly 1 live row (plan 11800, SD0109, PIO261127022) but found % (backup rows: %) — nothing changed', v_live, v_backup;
    END IF;

    INSERT INTO bak_tk202611280001_plan_gem
    SELECT g.*
    FROM tbt_production_plan_status_detail_gem g
    WHERE g.production_plan_id = 11800
      AND g.gem_code = 'SD0109'
      AND g.outbound_running = 'PIO261127022'
      AND NOT EXISTS (
          SELECT 1 FROM bak_tk202611280001_plan_gem b
          WHERE b.production_plan_id = g.production_plan_id AND b.item_no = g.item_no
      );

    DELETE FROM tbt_production_plan_status_detail_gem
    WHERE production_plan_id = 11800
      AND gem_code = 'SD0109'
      AND outbound_running = 'PIO261127022';
END $$;

-- Post-check: ตารางจริง (ต้องไม่เหลือ PIO261127022) และ backup (ต้องมี 1 แถว)
SELECT 'live' AS src, item_no, header_id, gem_code, gem_qty, gem_weight, gem_price, outbound_running, is_active
FROM tbt_production_plan_status_detail_gem
WHERE production_plan_id = 11800
UNION ALL
SELECT 'backup', item_no, header_id, gem_code, gem_qty, gem_weight, gem_price, outbound_running, is_active
FROM bak_tk202611280001_plan_gem
ORDER BY 1, 8, 2;

COMMIT;
