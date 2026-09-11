-- =============================================
-- Migration: Seed tbm_sale_channel
-- Date: 2026-09-10
-- Description: seed จุดขายเริ่มต้น 4 รายการ (หน้าร้าน / งานแฟร์ BGJF ครั้งที่ 74 / ออนไลน์ / ส่งออก)
-- Run order: รันหลัง 20260910_03_create_tbm_sale_channel.sql
-- Re-run safety: Idempotent — ON CONFLICT (code) DO NOTHING, รันซ้ำได้ไม่ error
-- =============================================

INSERT INTO tbm_sale_channel
    (code, name_th, name_en, type, venue, start_date, end_date, is_default, sort_order, create_by)
VALUES
    ('SHOP',    'หน้าร้าน ดวงแก้ว',                          'Duangkeaw Shop',              'SHOP',   NULL,                                          NULL,         NULL,         TRUE,  1, 'system'),
    ('FAIR74',  'Bangkok Gems & Jewelry Fair ครั้งที่ 74',   'Bangkok Gems & Jewelry Fair #74', 'FAIR', 'ศูนย์การประชุมแห่งชาติสิริกิติ์ Hall 1-8', '2026-09-08', '2026-09-12', FALSE, 2, 'system'),
    ('ONLINE',  'ออนไลน์ / โซเชียล',                         'Online / Social',              'ONLINE', NULL,                                          NULL,         NULL,         FALSE, 3, 'system'),
    ('EXPORT',  'งานส่งออก',                                  'Export order',                 'EXPORT', NULL,                                          NULL,         NULL,         FALSE, 4, 'system')
ON CONFLICT (code) DO NOTHING;
