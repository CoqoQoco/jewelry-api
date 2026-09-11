-- =============================================
-- Migration: Create tbm_notification_type + tbt_notification
-- Date: 2026-09-10
-- Description: Notification Center — master ชนิด action item (tbm_notification_type) และ inbox รายบุคคล (tbt_notification)
-- Run order: ไม่มี dependency กับตารางอื่น รันได้ทันที (รันก่อนไฟล์ 02 ที่ seed tbm_notification_type)
-- Re-run safety: Idempotent — ใช้ CREATE TABLE/INDEX IF NOT EXISTS, รันซ้ำได้ไม่ error
-- =============================================

-- =============================================
-- 1. tbm_notification_type
-- =============================================
CREATE TABLE IF NOT EXISTS tbm_notification_type (
    code                VARCHAR(50)   NOT NULL,
    module              VARCHAR(50)   NOT NULL,
    name_th             VARCHAR(200)  NOT NULL,
    name_en             VARCHAR(200)  NULL,
    icon                VARCHAR(100)  NULL,
    default_severity    VARCHAR(10)   NOT NULL DEFAULT 'INFO',
    severity_warn_days  INT           NULL,
    severity_crit_days  INT           NULL,
    escalate_days       INT           NULL,
    action_route        VARCHAR(200)  NULL,
    source              VARCHAR(10)   NOT NULL,
    is_active           BOOLEAN       NOT NULL DEFAULT TRUE,
    sort_order          INT           NULL,
    create_date         TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    create_by           VARCHAR       NOT NULL,
    update_date         TIMESTAMPTZ   NULL,
    update_by           VARCHAR       NULL,
    CONSTRAINT tbm_notification_type_pk PRIMARY KEY (code)
);

COMMENT ON COLUMN tbm_notification_type.module IS 'โมดูลต้นทาง: SALE / TICKET / PRODUCTION / STOCK';
COMMENT ON COLUMN tbm_notification_type.default_severity IS 'ระดับความสำคัญเริ่มต้น: INFO / WARN / CRIT';
COMMENT ON COLUMN tbm_notification_type.severity_warn_days IS 'อายุ (วัน) ของ item ที่จะถูกยกระดับเป็น WARN — NULL = ไม่มีการยกระดับตามอายุ';
COMMENT ON COLUMN tbm_notification_type.severity_crit_days IS 'อายุ (วัน) ของ item ที่จะถูกยกระดับเป็น CRIT — NULL = ไม่มีการยกระดับตามอายุ';
COMMENT ON COLUMN tbm_notification_type.escalate_days IS 'อายุ (วัน) ที่เกินแล้วให้ส่งสำเนาแจ้งหัวหน้าด้วย — NULL = ไม่ escalate';
COMMENT ON COLUMN tbm_notification_type.source IS 'ที่มา: EVENT = โค้ดเรียก push ตอนเกิดเหตุ / RULE = ประเมินเงื่อนไขตอน MyCount ถูก poll แล้ว upsert (ระบบไม่มี scheduler)';

-- =============================================
-- 2. tbt_notification
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_notification (
    id                   BIGSERIAL     NOT NULL,
    type_code            VARCHAR(50)   NOT NULL,
    recipient_username   VARCHAR(100)  NOT NULL,
    title                VARCHAR(300)  NOT NULL,
    body                 VARCHAR(1000) NULL,
    ref_doc_type         VARCHAR(50)   NULL,
    ref_doc_no           VARCHAR(100)  NULL,
    action_url           VARCHAR(300)  NULL,
    severity             VARCHAR(10)   NOT NULL DEFAULT 'INFO',
    amount               NUMERIC(18,4) NULL,
    currency_unit        VARCHAR(10)   NULL,
    event_date           TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    due_date             TIMESTAMPTZ   NULL,
    state                VARCHAR(20)   NOT NULL DEFAULT 'NEW',
    snooze_until         TIMESTAMPTZ   NULL,
    source               VARCHAR(10)   NOT NULL,
    is_escalated         BOOLEAN       NOT NULL DEFAULT FALSE,
    read_date            TIMESTAMPTZ   NULL,
    done_date            TIMESTAMPTZ   NULL,
    create_date          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    create_by            VARCHAR       NOT NULL,
    update_date          TIMESTAMPTZ   NULL,
    update_by            VARCHAR       NULL,
    CONSTRAINT tbt_notification_pk PRIMARY KEY (id),
    CONSTRAINT tbt_notification_type_fk FOREIGN KEY (type_code)
        REFERENCES tbm_notification_type(code)
);

COMMENT ON COLUMN tbt_notification.recipient_username IS 'username ผู้รับ action item นี้';
COMMENT ON COLUMN tbt_notification.ref_doc_type IS 'ชนิดเอกสารอ้างอิง เช่น INVOICE, TICKET, PLAN';
COMMENT ON COLUMN tbt_notification.ref_doc_no IS 'เลขที่เอกสารอ้างอิง ใช้คู่กับ type_code + recipient_username กัน RULE สร้างซ้ำ';
COMMENT ON COLUMN tbt_notification.amount IS 'ยอดเงินที่เกี่ยวข้อง เช่น ยอดค้างชำระ';
COMMENT ON COLUMN tbt_notification.state IS 'สถานะ: NEW / READ / SNOOZED / DONE / AUTO_CLOSED';
COMMENT ON COLUMN tbt_notification.snooze_until IS 'เลื่อนแจ้งเตือนไปจนถึงวันเวลานี้ (ใช้กับ state = SNOOZED)';
COMMENT ON COLUMN tbt_notification.source IS 'ที่มา: EVENT = โค้ดเรียก push ตอนเกิดเหตุ / RULE = ประเมินเงื่อนไขตอน MyCount ถูก poll แล้ว upsert (ระบบไม่มี scheduler)';
COMMENT ON COLUMN tbt_notification.is_escalated IS 'TRUE = แถวนี้เป็นสำเนาที่ escalate ให้หัวหน้าเห็น';

CREATE UNIQUE INDEX IF NOT EXISTS uq_notification_dedupe
    ON tbt_notification (type_code, ref_doc_no, recipient_username)
    WHERE ref_doc_no IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_notification_inbox
    ON tbt_notification (recipient_username, state, due_date);

CREATE INDEX IF NOT EXISTS idx_notification_ref
    ON tbt_notification (ref_doc_type, ref_doc_no);
