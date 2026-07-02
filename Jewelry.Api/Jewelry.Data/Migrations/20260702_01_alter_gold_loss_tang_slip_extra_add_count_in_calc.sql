-- Migration: Add count_in_calc to tbt_gold_loss_tang_slip_extra
-- Date: 2026-07-02
-- Description: flag ต่อบรรทัดรายการเพิ่มเติม — false = ไม่นำน้ำหนักมาคำนวณค่าเสียทอง (เช่น ลูกยาง)
ALTER TABLE tbt_gold_loss_tang_slip_extra
    ADD COLUMN IF NOT EXISTS count_in_calc BOOLEAN NOT NULL DEFAULT true;
