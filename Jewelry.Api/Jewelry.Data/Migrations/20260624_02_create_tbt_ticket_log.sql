-- =============================================
-- Migration: Ticket work log (ประวัติการแก้ไข ticket)
-- Date: 2026-06-24
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_ticket_log (
    id          BIGSERIAL NOT NULL,
    ticket_id   BIGINT NOT NULL,
    action      CHARACTER VARYING NOT NULL,   -- status | dev | note
    detail      CHARACTER VARYING,
    old_value   CHARACTER VARYING,
    new_value   CHARACTER VARYING,
    create_date TIMESTAMPTZ NOT NULL,
    create_by   CHARACTER VARYING NOT NULL,
    CONSTRAINT tbt_ticket_log_pk PRIMARY KEY (id),
    CONSTRAINT tbt_ticket_log_ticket_fk FOREIGN KEY (ticket_id) REFERENCES tbt_ticket(id)
);

CREATE INDEX IF NOT EXISTS idx_tbt_ticket_log_ticket_id ON tbt_ticket_log(ticket_id);
