-- =============================================
-- Migration: ผูกประวัติคืนพลอย INB261127005 กับแถวซ้ำของแผน 6911008-13
-- Date: 2026-11-28
-- Ticket: TK202611280001
-- Description: แถวพลอย SD0109 / PIO261127022 ถูกลบออกจากแผนโดย 20261128_03 (สำรองใน bak_tk202611280001_plan_gem)
--              สต็อกถูกคืนไปแล้วด้วย INB261127005 — เพิ่มแถวประวัติ LINK_EXISTING ใน tbt_production_plan_gem_return
--              เพื่อให้รายการ INB นี้ไม่โผล่ในหน้า "รับเข้าที่ยังไม่ผูกใบงาน" และใบงานแสดงประวัติการคืน
--              ไม่แตะสต็อก · ข้ามด้วย NOTICE (ไม่ error) ถ้าไม่พบ INB ที่เป็น type 3 แค่ 1 รายการ หรือไม่มี backup
-- ลำดับ deploy: รันหลัง 20261128_03 และ 20261128_04 ทั้งคู่ · รันก่อน deploy API · รันซ้ำได้ (idempotent)
-- Rollback: 20261128_05_link_inb_6911008_13_rollback.sql
-- =============================================

DO $$
DECLARE
    v_lines  integer;
    v_line   RECORD;
    v_bak    RECORD;
BEGIN
    IF to_regclass('public.bak_tk202611280001_plan_gem') IS NULL
       OR to_regclass('public.tbt_production_plan_gem_return') IS NULL THEN
        RAISE NOTICE 'TK202611280001: run 20261128_03 and 20261128_04 first — skipped';
        RETURN;
    END IF;

    SELECT COUNT(*) INTO v_lines
    FROM tbt_stock_gem_transection
    WHERE running = 'INB261127005' AND code = 'SD0109' AND type = 3;

    IF v_lines <> 1 THEN
        RAISE NOTICE 'TK202611280001: expected exactly 1 type-3 line INB261127005/SD0109 but found % — skipped', v_lines;
        RETURN;
    END IF;

    SELECT id, running INTO v_line
    FROM tbt_stock_gem_transection
    WHERE running = 'INB261127005' AND code = 'SD0109' AND type = 3;

    IF EXISTS (SELECT 1 FROM tbt_production_plan_gem_return WHERE inbound_transection_id = v_line.id) THEN
        RAISE NOTICE 'TK202611280001: already linked — skipped';
        RETURN;
    END IF;

    SELECT production_plan_id, header_id, item_no, outbound_running, gem_code, gem_qty, gem_weight INTO v_bak
    FROM bak_tk202611280001_plan_gem
    WHERE production_plan_id = 11800 AND gem_code = 'SD0109' AND outbound_running = 'PIO261127022'
    LIMIT 1;

    IF NOT FOUND THEN
        RAISE NOTICE 'TK202611280001: backup row not found (run 20261128_03 first) — skipped';
        RETURN;
    END IF;

    INSERT INTO tbt_production_plan_gem_return
        (production_plan_id, header_id, item_no, outbound_running, gem_code, return_qty, return_weight, mode,
         inbound_transection_id, inbound_running, reason, remark, create_by, create_date)
    VALUES
        (v_bak.production_plan_id, v_bak.header_id, v_bak.item_no, v_bak.outbound_running, v_bak.gem_code,
         COALESCE(v_bak.gem_qty, 0), COALESCE(v_bak.gem_weight, 0), 'LINK_EXISTING',
         v_line.id, v_line.running, 'DUPLICATE', 'TK202611280001 data fix', 'system', now());
    RAISE NOTICE 'TK202611280001: linked INB261127005 to plan %', v_bak.production_plan_id;
END $$;
