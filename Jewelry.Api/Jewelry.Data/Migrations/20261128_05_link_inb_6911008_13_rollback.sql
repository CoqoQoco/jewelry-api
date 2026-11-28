-- Rollback: 20261128_05_link_inb_6911008_13.sql
-- Ticket: TK202611280001
-- ข้ามด้วย NOTICE ถ้ายังไม่มีตาราง tbt_production_plan_gem_return

DO $$
BEGIN
    IF to_regclass('public.tbt_production_plan_gem_return') IS NULL THEN
        RAISE NOTICE 'TK202611280001: tbt_production_plan_gem_return not found — skipped';
        RETURN;
    END IF;

    DELETE FROM tbt_production_plan_gem_return
    WHERE inbound_running = 'INB261127005'
      AND production_plan_id = 11800
      AND gem_code = 'SD0109'
      AND mode = 'LINK_EXISTING'
      AND remark = 'TK202611280001 data fix';
END $$;
