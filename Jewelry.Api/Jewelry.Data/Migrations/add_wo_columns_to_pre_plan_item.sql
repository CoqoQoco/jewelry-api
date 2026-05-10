BEGIN;

ALTER TABLE tbt_production_pre_plan_item
    ADD COLUMN IF NOT EXISTS wo         CHARACTER VARYING,
    ADD COLUMN IF NOT EXISTS wo_number  INT,
    ADD COLUMN IF NOT EXISTS wo_text    CHARACTER VARYING;

CREATE INDEX IF NOT EXISTS idx_tbt_production_pre_plan_item_wo_text
    ON tbt_production_pre_plan_item(wo_text);

COMMIT;
