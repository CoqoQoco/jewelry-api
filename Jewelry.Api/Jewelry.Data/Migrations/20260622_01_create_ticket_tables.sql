-- =============================================
-- Migration: Create ticket tables
-- Date: 2026-06-22
-- Description: สร้างตาราง tbm_ticket_status และ tbt_ticket
--              สำหรับระบบแจ้งปัญหา/ขอฟีเจอร์ (Ticket system)
-- =============================================

-- =============================================
-- 1. tbm_ticket_status (master)
-- =============================================
CREATE TABLE IF NOT EXISTS tbm_ticket_status (
    id          SERIAL NOT NULL,
    name_th     CHARACTER VARYING NOT NULL,
    name_en     CHARACTER VARYING NOT NULL,
    CONSTRAINT tbm_ticket_status_pk PRIMARY KEY (id)
);

INSERT INTO tbm_ticket_status (id, name_th, name_en) VALUES
    (1, 'เปิด',              'Open'),
    (2, 'กำลังดำเนินการ',   'In Progress'),
    (3, 'แก้เสร็จ',          'Resolved'),
    (4, 'ปิด',               'Closed')
ON CONFLICT DO NOTHING;

-- =============================================
-- 2. tbt_ticket (transaction)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_ticket (
    id              BIGSERIAL NOT NULL,
    ticket_no       CHARACTER VARYING NOT NULL,
    -- type: 1=Bug/แจ้งปัญหา, 2=Feature/ขอฟีเจอร์
    type            INT NOT NULL,
    topic_route     CHARACTER VARYING NOT NULL,
    topic_name      CHARACTER VARYING NOT NULL,
    title           CHARACTER VARYING NOT NULL,
    description     CHARACTER VARYING NOT NULL,
    screenshot_url  CHARACTER VARYING,
    status          INT NOT NULL DEFAULT 1,
    dev_analysis    CHARACTER VARYING,
    dev_response    CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbt_ticket_pk PRIMARY KEY (id),
    CONSTRAINT tbt_ticket_status_fk
        FOREIGN KEY (status)
        REFERENCES tbm_ticket_status(id)
);

CREATE UNIQUE INDEX IF NOT EXISTS tbt_ticket_no_uq
    ON tbt_ticket(ticket_no);

CREATE INDEX IF NOT EXISTS idx_tbt_ticket_status
    ON tbt_ticket(status);

CREATE INDEX IF NOT EXISTS idx_tbt_ticket_create_by
    ON tbt_ticket(create_by);

CREATE INDEX IF NOT EXISTS idx_tbt_ticket_type
    ON tbt_ticket(type);
