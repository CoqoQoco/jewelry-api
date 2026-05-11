-- =============================================
-- Migration: Print Layout Settings (Bill + VAT)
-- Description: Shop-wide key/value table for storing print layout JSON config
-- =============================================

CREATE TABLE IF NOT EXISTS tbm_print_layout (
    layout_key      CHARACTER VARYING NOT NULL,
    layout_json     TEXT NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    create_date     TIMESTAMPTZ NOT NULL,
    update_by       CHARACTER VARYING,
    update_date     TIMESTAMPTZ,
    CONSTRAINT tbm_print_layout_pk PRIMARY KEY (layout_key)
);
