-- =============================================
-- Migration: สิทธิ์ข้อมูล AI รายคน (grant) + การระงับการใช้ AI (suspension) + permission ai:admin — TK202611280006
-- Date: 2026-11-28
-- Description:
--   1. tbt_ai_user_grant      — Dev เปิดหมวดข้อมูลอ่อนไหว (data:*) ให้ user รายคน (แทนการติ๊กตอน consent)
--   2. tbt_ai_user_suspension — ระงับ/ปลดระงับการใช้ AI รายคน (manual จากหน้าควบคุม, auto จากใบ TK202611280007)
--   3. permission ai:admin → role Dev เท่านั้น (lookup ด้วยชื่อ)
--   4. backfill grant จาก token OAUTH ที่ยังใช้งานอยู่ (revoked_at IS NULL AND refresh_expires_at > now())
--      เฉพาะ user ที่ role ปัจจุบันยังมี permission รองรับหมวดนั้น:
--        data:customer-names / data:worker-names / data:receivables → executive:view, data:cost → sale:view-margin
--   ต้องรัน "ก่อน" deploy API (API อ่านตารางเหล่านี้) · ต้องรัน 20261124_01_ai_connector.sql ก่อน
--   Dev ต้อง logout + login ใหม่หลังได้ permission ai:admin
-- Re-run safety: Idempotent (IF NOT EXISTS / ON CONFLICT DO NOTHING)
-- Rollback: 20261128_06_create_ai_user_grant_suspension_rollback.sql
-- =============================================

BEGIN;

-- 1. tbt_ai_user_grant (scope = data:* ที่ Dev เปิดให้ user — revoked_at IS NULL = เปิดอยู่)
CREATE TABLE IF NOT EXISTS tbt_ai_user_grant (
    id          BIGSERIAL NOT NULL,
    user_id     INT NOT NULL,
    scope       CHARACTER VARYING NOT NULL,
    granted_by  CHARACTER VARYING NOT NULL,
    granted_at  TIMESTAMPTZ NOT NULL,
    revoked_by  CHARACTER VARYING,
    revoked_at  TIMESTAMPTZ,
    CONSTRAINT tbt_ai_user_grant_pk PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_tbt_ai_user_grant_active
    ON tbt_ai_user_grant(user_id, scope) WHERE revoked_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_tbt_ai_user_grant_user ON tbt_ai_user_grant(user_id);

-- 2. tbt_ai_user_suspension (source: manual | auto — lifted_at IS NULL = ถูกระงับอยู่)
CREATE TABLE IF NOT EXISTS tbt_ai_user_suspension (
    id             BIGSERIAL NOT NULL,
    user_id        INT NOT NULL,
    source         CHARACTER VARYING NOT NULL,
    reason_code    CHARACTER VARYING NOT NULL,
    reason_detail  JSONB,
    created_by     CHARACTER VARYING NOT NULL,
    created_at     TIMESTAMPTZ NOT NULL,
    lifted_by      CHARACTER VARYING,
    lifted_at      TIMESTAMPTZ,
    CONSTRAINT tbt_ai_user_suspension_pk PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_tbt_ai_user_suspension_active
    ON tbt_ai_user_suspension(user_id) WHERE lifted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_tbt_ai_user_suspension_user ON tbt_ai_user_suspension(user_id, created_at);

-- 3. permission ai:admin → role Dev เท่านั้น
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('ai:admin', 'ควบคุมการใช้ AI ของผู้ใช้ทุกคน (เปิดสิทธิ์ข้อมูล / ระงับ / ดูประวัติ)', 'Setting', 'system')
ON CONFLICT (code) DO NOTHING;

INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Dev' AND r.is_active = TRUE
    AND p.code = 'ai:admin' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 4. backfill grant จาก token OAUTH ที่ยังใช้งานอยู่ (นิยามเดียวกับ AiConnectionService.List)
--    scopes เก็บเป็น string คั่นด้วยช่องว่าง → แตกด้วย string_to_array
INSERT INTO tbt_ai_user_grant (user_id, scope, granted_by, granted_at)
SELECT DISTINCT t.user_id, s.scope, 'system:backfill', NOW()
FROM tbt_ai_token t
CROSS JOIN LATERAL unnest(string_to_array(t.scopes, ' ')) AS s(scope)
WHERE t.kind = 'OAUTH'
    AND t.revoked_at IS NULL
    AND t.refresh_expires_at > NOW()
    AND s.scope IN ('data:customer-names', 'data:worker-names', 'data:cost', 'data:receivables')
    AND EXISTS (
        SELECT 1
        FROM tbt_user_role ur
        JOIN tbm_user_role r ON r.id = ur.role AND r.is_active = TRUE
        JOIN tbt_role_permission rp ON rp.role_id = r.id
        JOIN tbm_permission p ON p.id = rp.permission_id AND p.is_active = TRUE
        WHERE ur.user_id = t.user_id
            AND p.code = CASE s.scope WHEN 'data:cost' THEN 'sale:view-margin' ELSE 'executive:view' END
    )
ON CONFLICT (user_id, scope) WHERE revoked_at IS NULL DO NOTHING;

COMMIT;

-- =============================================
-- Verification (uncomment to run manually)
-- =============================================
-- SELECT user_id, scope, granted_by, granted_at FROM tbt_ai_user_grant WHERE revoked_at IS NULL ORDER BY user_id, scope;
-- SELECT r.name, p.code FROM tbm_user_role r JOIN tbt_role_permission rp ON rp.role_id = r.id JOIN tbm_permission p ON p.id = rp.permission_id WHERE p.code = 'ai:admin';
