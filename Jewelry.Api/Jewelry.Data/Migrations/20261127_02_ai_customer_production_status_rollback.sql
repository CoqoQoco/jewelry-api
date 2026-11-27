-- Rollback: AI tool get_customer_production_status — TK202611270006
-- ถอด API รุ่นที่มี tool นี้ก่อน แล้วค่อยรัน
-- ปิด scope กลับเป็นสถานะเดิมของรอบ 1 (is_active=FALSE) — token ที่เคยได้ module:production ยังเก็บค่าไว้แต่ไม่มี tool ใช้
UPDATE tbm_ai_scope
SET is_active = FALSE,
    description_th = 'ดูข้อมูลงานผลิตเชิงลึก (ยังไม่เปิดใช้)'
WHERE code = 'module:production';

DELETE FROM tbm_ai_glossary WHERE metric_code IN ('customer_open_plan', 'melted_plan');
