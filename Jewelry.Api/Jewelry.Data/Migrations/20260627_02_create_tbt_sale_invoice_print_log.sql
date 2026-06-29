-- =============================================
-- Migration: Sale Invoice Print Log
-- Date: 2026-06-27
-- Description: Create table to record every invoice print event
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_sale_invoice_print_log (
    running         CHARACTER VARYING NOT NULL,
    invoice_running CHARACTER VARYING,
    invoice_no      CHARACTER VARYING NOT NULL,
    paper_type      CHARACTER VARYING NOT NULL,
    copy_no         INT NOT NULL DEFAULT 1,
    data            JSONB,
    printed_by      CHARACTER VARYING NOT NULL,
    printed_at      TIMESTAMPTZ NOT NULL,
    CONSTRAINT tbt_sale_invoice_print_log_pk PRIMARY KEY (running)
);

CREATE INDEX IF NOT EXISTS idx_tbt_sale_invoice_print_log_invoice_no ON tbt_sale_invoice_print_log(invoice_no);
