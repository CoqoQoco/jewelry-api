-- =============================================
-- Migration: Create tbm_sale_channel
-- Date: 2026-09-10
-- Description: master จุดขาย (Sale Channel) เช่น หน้าร้าน / งานแฟร์ / ออนไลน์ / ส่งออก — แยกจาก tbm_stock_location ที่เป็นล็อตสต๊อก
-- Run order: ไม่มี dependency กับตารางอื่น รันได้ทันที (รันก่อนไฟล์ 04 ที่ seed ข้อมูล และก่อนไฟล์ 05 ที่ FK มาอ้าง)
-- Re-run safety: Idempotent — ใช้ CREATE TABLE/INDEX IF NOT EXISTS, รันซ้ำได้ไม่ error
-- =============================================

CREATE TABLE IF NOT EXISTS tbm_sale_channel (
    code         VARCHAR(30)   NOT NULL,
    name_th      VARCHAR(200)  NOT NULL,
    name_en      VARCHAR(200)  NULL,
    type         VARCHAR(20)   NOT NULL,
    venue        VARCHAR(300)  NULL,
    start_date   DATE          NULL,
    end_date     DATE          NULL,
    is_default   BOOLEAN       NOT NULL DEFAULT FALSE,
    is_active    BOOLEAN       NOT NULL DEFAULT TRUE,
    sort_order   INT           NULL,
    create_date  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    create_by    VARCHAR       NOT NULL,
    update_date  TIMESTAMPTZ   NULL,
    update_by    VARCHAR       NULL,
    CONSTRAINT tbm_sale_channel_pk PRIMARY KEY (code)
);

COMMENT ON COLUMN tbm_sale_channel.type IS 'ชนิดจุดขาย: SHOP / FAIR / ONLINE / EXPORT / OTHER';
COMMENT ON COLUMN tbm_sale_channel.venue IS 'สถานที่จัดงาน (ใช้กับ type = FAIR)';
COMMENT ON COLUMN tbm_sale_channel.start_date IS 'วันเริ่มจัดงาน — ใช้ให้หน้าออกบิลเดา channel เริ่มต้นตามวันที่';
COMMENT ON COLUMN tbm_sale_channel.end_date IS 'วันสิ้นสุดจัดงาน — ใช้ให้หน้าออกบิลเดา channel เริ่มต้นตามวันที่';
COMMENT ON COLUMN tbm_sale_channel.is_default IS 'ใช้เป็น channel เริ่มต้นเมื่อวันที่ออกบิลไม่ตรงช่วงงานไหนเลย';

CREATE INDEX IF NOT EXISTS idx_sale_channel_active_range ON tbm_sale_channel (is_active, start_date, end_date);
