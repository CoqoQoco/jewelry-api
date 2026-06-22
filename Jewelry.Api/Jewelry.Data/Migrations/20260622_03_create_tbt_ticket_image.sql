-- =============================================
-- Migration: Ticket Multi-Image Support
-- Date: 2026-06-22
-- Description: สร้างตาราง tbt_ticket_image สำหรับเก็บหลายรูปต่อ ticket
--              คง tbt_ticket.screenshot_url ไว้ (legacy, nullable)
-- =============================================

-- =============================================
-- 1. tbt_ticket_image
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_ticket_image (
    id              BIGSERIAL NOT NULL,
    ticket_id       BIGINT NOT NULL,
    number          INT NOT NULL,
    path            CHARACTER VARYING NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbt_ticket_image_pk PRIMARY KEY (id),
    CONSTRAINT tbt_ticket_image_ticket_fk FOREIGN KEY (ticket_id)
        REFERENCES tbt_ticket (id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_ticket_image_ticket_id ON tbt_ticket_image (ticket_id);
