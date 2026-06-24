-- =============================================
-- Migration: Unified ticket comment thread (analysis | response | change)
-- Date: 2026-06-24
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_ticket_comment (
    id          BIGSERIAL NOT NULL,
    ticket_id   BIGINT NOT NULL,
    type        CHARACTER VARYING NOT NULL,
    author_role CHARACTER VARYING NOT NULL,
    message     CHARACTER VARYING NOT NULL,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    create_date TIMESTAMPTZ NOT NULL,
    create_by   CHARACTER VARYING NOT NULL,
    update_date TIMESTAMPTZ,
    update_by   CHARACTER VARYING,
    CONSTRAINT tbt_ticket_comment_pk PRIMARY KEY (id),
    CONSTRAINT tbt_ticket_comment_ticket_fk FOREIGN KEY (ticket_id) REFERENCES tbt_ticket(id)
);
CREATE INDEX IF NOT EXISTS idx_tbt_ticket_comment_ticket_id ON tbt_ticket_comment(ticket_id);

INSERT INTO tbt_ticket_comment (ticket_id,type,author_role,message,is_active,create_date,create_by)
SELECT id,'analysis','dev',dev_analysis,TRUE,COALESCE(update_date,create_date),COALESCE(update_by,create_by)
FROM tbt_ticket WHERE dev_analysis IS NOT NULL AND dev_analysis<>'';
INSERT INTO tbt_ticket_comment (ticket_id,type,author_role,message,is_active,create_date,create_by)
SELECT id,'response','dev',dev_response,TRUE,COALESCE(update_date,create_date),COALESCE(update_by,create_by)
FROM tbt_ticket WHERE dev_response IS NOT NULL AND dev_response<>'';
INSERT INTO tbt_ticket_comment (ticket_id,type,author_role,message,is_active,create_date,create_by)
SELECT ticket_id,'change',CASE WHEN action IN ('status','dev') THEN 'system' ELSE 'dev' END,
       CASE WHEN action='status' THEN 'เปลี่ยนสถานะ' ELSE COALESCE(detail,'') END,
       TRUE,create_date,create_by
FROM tbt_ticket_log;
