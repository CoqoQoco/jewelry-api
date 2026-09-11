-- =============================================
-- Migration: Seed tbm_notification_type
-- Date: 2026-09-10
-- Description: seed ชนิด action item เริ่มต้น 5 รายการของ Notification Center
-- Run order: รันหลัง 20260910_01_create_tbt_notification.sql
-- Re-run safety: Idempotent — ON CONFLICT (code) DO NOTHING, รันซ้ำได้ไม่ error
-- =============================================

-- INVOICE_OUTSTANDING: เปิดใช้งานทันที ให้หน้า Notification Center แสดงงานค้างชำระของ Sale
-- อีก 4 รายการด้านล่าง (TICKET_ASSIGNED, TICKET_REPLY, PLAN_OVERDUE, STOCK_LOW) ตั้ง is_active = FALSE ไว้ก่อน
-- เป็น slot รอ implement provider ในเฟสถัดไป ไม่ให้ขึ้นในหน้าจอจนกว่าจะพร้อม
INSERT INTO tbm_notification_type
    (code, module, name_th, icon, default_severity, severity_warn_days, severity_crit_days, escalate_days, action_route, source, is_active, sort_order, create_by)
VALUES
    ('INVOICE_OUTSTANDING', 'SALE',       'ใบแจ้งหนี้ค้างชำระ',      'bi-receipt',           'INFO', 8,    31,   30,   '/invoice-detail', 'RULE',  TRUE,  1, 'system'),
    ('TICKET_ASSIGNED',     'TICKET',     'Ticket ที่ถูกมอบหมาย',     'bi-card-checklist',    'INFO', NULL, NULL, NULL, '/ticket-manage',  'EVENT', FALSE, 2, 'system'),
    ('TICKET_REPLY',        'TICKET',     'Ticket มีคนตอบกลับ',       'bi-chat-left-text',    'INFO', NULL, NULL, NULL, '/ticket-manage',  'EVENT', FALSE, 3, 'system'),
    ('PLAN_OVERDUE',        'PRODUCTION', 'แผนผลิตเลยกำหนด',          'bi-calendar-x',        'WARN', 3,    8,    14,   '/plan-update',    'RULE',  FALSE, 4, 'system'),
    ('STOCK_LOW',           'STOCK',      'สต๊อกต่ำกว่าเกณฑ์',        'bi-box-seam',          'INFO', NULL, NULL, NULL, '/stock/product',  'RULE',  FALSE, 5, 'system')
ON CONFLICT (code) DO NOTHING;
