-- Migration: Create tbt_sale_document table
-- Date: 2026-03-16

CREATE TABLE IF NOT EXISTS tbt_sale_document (
    id              SERIAL PRIMARY KEY,
    file_name       VARCHAR(500)  NOT NULL,
    blob_path       VARCHAR(1000) NOT NULL,
    document_month  INT           NOT NULL CHECK (document_month BETWEEN 1 AND 12),
    document_year   INT           NOT NULL,
    tags            VARCHAR(500),
    remark          VARCHAR(1000),
    is_active       BOOLEAN       NOT NULL DEFAULT TRUE,
    create_by       VARCHAR(100)  NOT NULL,
    create_date     TIMESTAMPTZ   NOT NULL,
    update_by       VARCHAR(100),
    update_date     TIMESTAMPTZ
);

CREATE INDEX idx_sale_document_month_year ON tbt_sale_document (document_year, document_month);
CREATE INDEX idx_sale_document_active     ON tbt_sale_document (is_active);
