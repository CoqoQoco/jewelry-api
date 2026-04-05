-- Migration: สร้างตาราง Gold Loss แต่ง (Header + Item)
-- วันที่: 2026-04-06

-- Header ใบงาน Gold Loss
CREATE TABLE IF NOT EXISTS tbt_gold_loss_header (
    id SERIAL,
    document_no CHARACTER VARYING,
    start_date TIMESTAMPTZ,
    end_date TIMESTAMPTZ,
    status INT NOT NULL DEFAULT 50,
    remark CHARACTER VARYING,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    create_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    create_by CHARACTER VARYING NOT NULL,
    update_date TIMESTAMPTZ,
    update_by CHARACTER VARYING,
    CONSTRAINT tbt_gold_loss_header_pk PRIMARY KEY (id)
);

-- Item รายละเอียด Gold Loss แต่ง
CREATE TABLE IF NOT EXISTS tbt_gold_loss_item (
    id SERIAL,
    header_id INT NOT NULL,
    production_plan_id INT NOT NULL,
    item_no CHARACTER VARYING NOT NULL,
    wo CHARACTER VARYING,
    worker_code CHARACTER VARYING,
    worker_name CHARACTER VARYING,
    gold CHARACTER VARYING,
    gold_qty_send NUMERIC(18,4),
    gold_weight_send NUMERIC(18,4),
    gold_qty_check NUMERIC(18,4),
    gold_weight_check NUMERIC(18,4),
    loss_percent NUMERIC(10,4),
    gold_loss_price NUMERIC(18,4),
    weight_loss_allowed NUMERIC(18,4),
    weight_loss_actual NUMERIC(18,4),
    money_diff NUMERIC(18,4),
    loss_remark CHARACTER VARYING(500),
    request_date TIMESTAMPTZ,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    create_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    create_by CHARACTER VARYING NOT NULL,
    update_date TIMESTAMPTZ,
    update_by CHARACTER VARYING,
    CONSTRAINT tbt_gold_loss_item_pk PRIMARY KEY (id),
    CONSTRAINT fk_gold_loss_item_header FOREIGN KEY (header_id) REFERENCES tbt_gold_loss_header(id)
);
