-- =============================================
-- Migration: Ticket read status tracking per user
-- Date: 2026-06-29
-- Description: Track last read time per user per ticket for unread message badge
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_ticket_read_status (
    id             BIGSERIAL NOT NULL,
    ticket_id      BIGINT NOT NULL,
    username       CHARACTER VARYING NOT NULL,
    last_read_date TIMESTAMPTZ NOT NULL,
    CONSTRAINT tbt_ticket_read_status_pk PRIMARY KEY (id),
    CONSTRAINT tbt_ticket_read_status_ticket_fk FOREIGN KEY (ticket_id) REFERENCES tbt_ticket(id)
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_tbt_ticket_read_status_ticket_user ON tbt_ticket_read_status(ticket_id, username);
CREATE INDEX IF NOT EXISTS idx_tbt_ticket_read_status_username ON tbt_ticket_read_status(username);
