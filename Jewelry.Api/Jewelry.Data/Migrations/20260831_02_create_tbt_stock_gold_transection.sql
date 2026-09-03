-- =============================================
-- Migration: Create tbt_stock_gold_transection
-- Date: 2026-08-31
-- Description: Ledger การเคลื่อนไหวคลังทอง — mirror tbt_stock_gem_transection
--              type: 1=รับเข้าคลัง[ซื้อ/รับใหม่](เข้า) 2=ตั้งยอดยกมา(เข้า)
--                    3=คืนเข้าคลัง[จากใบเบิกผสมทอง](เข้า) 4=เบิกออกคลัง[ใบเบิกผสมทอง](ออก)
--                    5=ปรับยอดเพิ่ม(เข้า) 6=ปรับยอดลด(ออก)
--                    7=กลับรายการเพิ่ม[แก้ไขรายการ](เข้า) 8=กลับรายการลด[แก้ไขรายการ](ออก)
--                    (7,8 = compensating entry ที่ ReverseByRefDoc สร้างขึ้น แยกจากหมวด 5,6
--                     ที่เป็นการปรับยอดด้วยมือของ user)
--              status: "completed"=รายการที่ยังมีผลจริง (รวมถึง compensating entry ที่โพสต์โดย ReverseByRefDoc)
--                      "reversed"=รายการต้นทางที่ถูกกลับรายการไปแล้ว (ReverseByRefDoc อัปเดตแถวต้นทางเป็นค่านี้)
-- Run order: หลัง 20260831_01_create_tbt_stock_gold.sql
-- Re-run safety: CREATE TABLE IF NOT EXISTS + CREATE INDEX IF NOT EXISTS — idempotent
-- =============================================

CREATE TABLE IF NOT EXISTS tbt_stock_gold_transection (
    id                          BIGSERIAL NOT NULL,
    running                     CHARACTER VARYING NOT NULL,
    gold_code                   CHARACTER VARYING NOT NULL,
    gold_size_code              CHARACTER VARYING NOT NULL,
    type                        INT NOT NULL,
    -- Type: 1=รับเข้าคลัง[ซื้อ/รับใหม่] 2=ตั้งยอดยกมา 3=คืนเข้าคลัง[จากใบเบิกผสมทอง]
    --       4=เบิกออกคลัง[ใบเบิกผสมทอง] 5=ปรับยอดเพิ่ม 6=ปรับยอดลด
    --       7=กลับรายการเพิ่ม[แก้ไขรายการ] 8=กลับรายการลด[แก้ไขรายการ]
    weight                      NUMERIC NOT NULL,
    -- น้ำหนักที่เคลื่อนไหว (บวกเสมอ ทิศทางดูจาก type)
    previous_remain_weight      NUMERIC,
    point_remain_weight         NUMERIC,
    ref_doc_type                CHARACTER VARYING,
    ref_doc_no                  CHARACTER VARYING,
    production_plan_wo          CHARACTER VARYING,
    production_plan_wo_number   INT,
    ref_running                 CHARACTER VARYING,
    -- จับคู่รายการคืนกับรายการเบิก (type 3 <-> 4) หรือ รายการกลับกับรายการต้นทาง (reverse)
    request_date                TIMESTAMPTZ,
    return_date                 TIMESTAMPTZ,
    status                      CHARACTER VARYING,
    remark                      CHARACTER VARYING,
    create_date                 TIMESTAMPTZ NOT NULL,
    create_by                   CHARACTER VARYING NOT NULL,
    update_date                 TIMESTAMPTZ,
    update_by                   CHARACTER VARYING,
    CONSTRAINT tbt_stock_gold_transection_pk PRIMARY KEY (id),
    CONSTRAINT tbt_stock_gold_transection_gold_code_fk
        FOREIGN KEY (gold_code) REFERENCES tbm_gold(code),
    CONSTRAINT tbt_stock_gold_transection_gold_size_code_fk
        FOREIGN KEY (gold_size_code) REFERENCES tbm_gold_size(code)
);

CREATE INDEX IF NOT EXISTS idx_tbt_stock_gold_transection_code_size
    ON tbt_stock_gold_transection(gold_code, gold_size_code);

CREATE INDEX IF NOT EXISTS idx_tbt_stock_gold_transection_ref_doc
    ON tbt_stock_gold_transection(ref_doc_type, ref_doc_no);

CREATE INDEX IF NOT EXISTS idx_tbt_stock_gold_transection_create_date
    ON tbt_stock_gold_transection(create_date);
