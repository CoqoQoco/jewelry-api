-- =============================================
-- Migration: Backfill tbt_ticket / tbt_ticket_image audit fields (user id -> username)
-- Date: 2026-06-24
-- Description: เดิม TicketService เก็บ create_by/update_by เป็น user id
--              แปลงให้เป็น username ให้ตรงมาตรฐานระบบ (เฉพาะค่าที่เป็นตัวเลขล้วน)
-- =============================================

UPDATE tbt_ticket t
SET create_by = u.username
FROM tbt_user u
WHERE t.create_by ~ '^[0-9]+$' AND u.id = t.create_by::int;

UPDATE tbt_ticket t
SET update_by = u.username
FROM tbt_user u
WHERE t.update_by ~ '^[0-9]+$' AND u.id = t.update_by::int;

UPDATE tbt_ticket_image ti
SET create_by = u.username
FROM tbt_user u
WHERE ti.create_by ~ '^[0-9]+$' AND u.id = ti.create_by::int;
