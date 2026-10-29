-- Rollback: 20261030_02_tbm_gem_label_code.sql
ALTER TABLE tbm_gem
    DROP COLUMN IF EXISTS label_code,
    DROP COLUMN IF EXISTS label_sort;
