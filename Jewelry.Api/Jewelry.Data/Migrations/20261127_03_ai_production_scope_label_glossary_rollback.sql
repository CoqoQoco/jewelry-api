-- Rollback: 20261127_03_ai_production_scope_label_glossary — TK202611270006
-- คืนข้อความเป็นรุ่นแรกของ 20261127_02 (ไม่ปิด scope / ไม่ลบ glossary — ใช้ 20261127_02 rollback สำหรับนั้น)
UPDATE tbm_ai_scope
SET name_th = 'งานผลิต',
    description_th = 'ดูข้อมูลงานผลิตรายใบงาน (สถานะใบงานตามลูกค้า) — ไม่รวมชื่อลูกค้า (ต้องอนุญาตชื่อลูกค้าแยกต่างหาก ไม่เช่นนั้นค้นได้เฉพาะรหัสลูกค้า)'
WHERE code = 'module:production';

UPDATE tbm_ai_glossary
SET name_th = 'ใบงานค้างของลูกค้า',
    definition_th = 'จำนวนใบงานผลิตที่ยังค้างของลูกค้า นับแบบเดียวกับแท็บ WIP: ไม่นับใบที่สำเร็จ ใบที่หลอม ใบที่อยู่ขั้นรอ CVD/CVD ใบที่ปิดย้อนหลัง และใบทดสอบ',
    caveat_th = 'ตัวเลขนี้ไม่รวมใบที่หลอม (ดู melted_plan) · วันที่ขยับล่าสุดมาจากการบันทึกสถานะล่าสุดของใบงาน',
    update_date = NOW()
WHERE metric_code = 'customer_open_plan';

UPDATE tbm_ai_glossary
SET name_th = 'ใบงานที่หลอม',
    definition_th = 'หลอม = ใบงานที่ยกเลิกแล้วนำทองกลับไปหลอม นับแยกเป็น meltedCount ไม่รวมในใบงานค้าง',
    caveat_th = 'ไม่ใช่ใบที่ผลิตสำเร็จ และไม่ควรนำไปบวกกับใบงานค้าง',
    update_date = NOW()
WHERE metric_code = 'melted_plan';
