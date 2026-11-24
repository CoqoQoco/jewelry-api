-- =============================================
-- Migration: AI Connector (MCP) สำหรับผู้บริหาร — TK202611240007
-- Date: 2026-11-24
-- Description: ตาราง scope / glossary / client / auth code / token / access log
--   + permission ai:connect (grant role Executive ด้วยชื่อ) + seed scope + glossary
--   ต้อง run "ก่อน" deploy API (API เริ่มต้นแล้วอ่านตารางเหล่านี้)
--   user ต้อง logout + login ใหม่หลังได้ permission ใหม่
-- Re-run safety: Idempotent (IF NOT EXISTS / ON CONFLICT DO NOTHING)
-- Rollback: 20261124_01_ai_connector_rollback.sql
-- =============================================

-- 1. tbm_ai_scope
--    kind: DATA = ข้อมูลอ่อนไหว (opt-in ตอน consent) | MODULE = ขอบเขตโมดูล | WRITE = เขียน (ยังไม่มี)
--    allow_pat: false = PAT ขอ scope นี้ไม่ได้
CREATE TABLE IF NOT EXISTS tbm_ai_scope (
    code            CHARACTER VARYING NOT NULL,
    name_th         CHARACTER VARYING NOT NULL,
    description_th  CHARACTER VARYING,
    kind            CHARACTER VARYING NOT NULL,
    allow_pat       BOOLEAN NOT NULL DEFAULT TRUE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    sort            INT NOT NULL DEFAULT 0,
    CONSTRAINT tbm_ai_scope_pk PRIMARY KEY (code)
);

-- 2. tbm_ai_glossary
CREATE TABLE IF NOT EXISTS tbm_ai_glossary (
    metric_code     CHARACTER VARYING NOT NULL,
    module          CHARACTER VARYING NOT NULL,
    name_th         CHARACTER VARYING NOT NULL,
    definition_th   TEXT NOT NULL,
    caveat_th       TEXT,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    update_date     TIMESTAMPTZ,
    CONSTRAINT tbm_ai_glossary_pk PRIMARY KEY (metric_code)
);

-- 3. tbt_ai_client (kind: CIMD = client_id เป็น URL | DCR = ลงทะเบียนเอง RFC 7591)
CREATE TABLE IF NOT EXISTS tbt_ai_client (
    id              BIGSERIAL NOT NULL,
    client_id       CHARACTER VARYING NOT NULL,
    client_name     CHARACTER VARYING NOT NULL,
    kind            CHARACTER VARYING NOT NULL,
    redirect_uris   JSONB NOT NULL DEFAULT '[]'::jsonb,
    metadata_url    CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    last_seen       TIMESTAMPTZ,
    CONSTRAINT tbt_ai_client_pk PRIMARY KEY (id),
    CONSTRAINT tbt_ai_client_client_id_uq UNIQUE (client_id)
);

-- 4. tbt_ai_auth_code (เก็บ hash ของ code เท่านั้น, ใช้ครั้งเดียว, อายุ 5 นาที)
CREATE TABLE IF NOT EXISTS tbt_ai_auth_code (
    code_hash       CHARACTER VARYING NOT NULL,
    client_id       CHARACTER VARYING NOT NULL,
    user_id         INT NOT NULL,
    redirect_uri    CHARACTER VARYING NOT NULL,
    code_challenge  CHARACTER VARYING NOT NULL,
    scopes          CHARACTER VARYING NOT NULL,
    resource        CHARACTER VARYING,
    expires_at      TIMESTAMPTZ NOT NULL,
    used_at         TIMESTAMPTZ,
    CONSTRAINT tbt_ai_auth_code_pk PRIMARY KEY (code_hash)
);

-- 5. tbt_ai_token (kind: OAUTH | PAT) — access token เป็น JWT อ้างแถวนี้ด้วย id (jti); refresh/PAT เก็บ SHA-256 hash
--    family_id = ตระกูล refresh token (reuse ของเก่า = revoke ทั้ง family)
CREATE TABLE IF NOT EXISTS tbt_ai_token (
    id                  BIGSERIAL NOT NULL,
    kind                CHARACTER VARYING NOT NULL,
    user_id             INT NOT NULL,
    client_id           CHARACTER VARYING,
    name                CHARACTER VARYING NOT NULL,
    scopes              CHARACTER VARYING NOT NULL,
    token_hash          CHARACTER VARYING,
    refresh_hash        CHARACTER VARYING,
    family_id           CHARACTER VARYING,
    expires_at          TIMESTAMPTZ NOT NULL,
    refresh_expires_at  TIMESTAMPTZ,
    revoked_at          TIMESTAMPTZ,
    last_used_at        TIMESTAMPTZ,
    create_date         TIMESTAMPTZ NOT NULL,
    CONSTRAINT tbt_ai_token_pk PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS idx_tbt_ai_token_user ON tbt_ai_token(user_id, create_date);
CREATE UNIQUE INDEX IF NOT EXISTS idx_tbt_ai_token_token_hash ON tbt_ai_token(token_hash) WHERE token_hash IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_tbt_ai_token_refresh_hash ON tbt_ai_token(refresh_hash);
CREATE INDEX IF NOT EXISTS idx_tbt_ai_token_family ON tbt_ai_token(family_id);

-- 6. tbt_ai_access_log (params ห้ามมี token/secret)
CREATE TABLE IF NOT EXISTS tbt_ai_access_log (
    id              BIGSERIAL NOT NULL,
    token_id        BIGINT,
    user_id         INT NOT NULL,
    tool            CHARACTER VARYING NOT NULL,
    scopes          CHARACTER VARYING,
    params          JSONB,
    row_count       INT NOT NULL DEFAULT 0,
    masked_count    INT NOT NULL DEFAULT 0,
    duration_ms     INT NOT NULL DEFAULT 0,
    error_code      CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    CONSTRAINT tbt_ai_access_log_pk PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS idx_tbt_ai_access_log_user_date ON tbt_ai_access_log(user_id, create_date);
CREATE INDEX IF NOT EXISTS idx_tbt_ai_access_log_token ON tbt_ai_access_log(token_id);

-- 7. permission ai:connect + grant role Executive (lookup ด้วยชื่อ ไม่ hardcode id)
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    ('ai:connect', 'เชื่อมต่อ AI (MCP) เข้ากับรายงานผู้บริหาร', 'Report', 'system')
ON CONFLICT (code) DO NOTHING;

INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Executive' AND r.is_active = TRUE
    AND p.code = 'ai:connect' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 7b. grant sale:view-margin ให้ role Executive (lookup ด้วยชื่อ) — จำเป็นต่อ scope data:cost
--     ผลข้างเคียง: Executive เห็นต้นทุน/markup บนหน้าจอขายด้วย · permission นี้มีอยู่แล้ว (Dev/SaleManager ใช้) ไม่สร้างใหม่
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Executive' AND r.is_active = TRUE
    AND p.code = 'sale:view-margin' AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- 8. seed scope
--    MODULE: รอบ 1 เปิดเฉพาะ module:executive (ตัวอื่น is_active=false รอรอบ 2)
--    DATA: allow_pat=false ทั้งหมด (PAT ขอไม่ได้)
INSERT INTO tbm_ai_scope (code, name_th, description_th, kind, allow_pat, is_active, sort) VALUES
    ('module:executive', 'รายงานผู้บริหาร', 'ดูภาพรวมผู้บริหาร เช่น จำนวนใบงานค้าง จำนวนบิลค้างชำระ จำนวนสินค้าในคลัง และประเด็นที่ควรจับตา (ไม่รวมชื่อลูกค้า ชื่อช่าง และตัวเลขต้นทุน)', 'MODULE', TRUE, TRUE, 10),
    ('module:production', 'งานผลิต', 'ดูข้อมูลงานผลิตเชิงลึก (ยังไม่เปิดใช้)', 'MODULE', TRUE, FALSE, 20),
    ('module:sale', 'งานขาย', 'ดูข้อมูลงานขายเชิงลึก (ยังไม่เปิดใช้)', 'MODULE', TRUE, FALSE, 30),
    ('module:stock', 'คลังสินค้า', 'ดูข้อมูลคลังสินค้าเชิงลึก (ยังไม่เปิดใช้)', 'MODULE', TRUE, FALSE, 40),
    ('module:material', 'วัตถุดิบ', 'ดูข้อมูลวัตถุดิบเชิงลึก (ยังไม่เปิดใช้)', 'MODULE', TRUE, FALSE, 50),
    ('data:customer-names', 'ชื่อลูกค้า', 'ให้ AI เห็นชื่อลูกค้าจริง เช่น "บริษัท ก. จำกัด" แทนที่จะเป็น "ลูกค้า #1". ข้อมูลจะถูกส่งไปประมวลผลที่ผู้ให้บริการ AI ภายนอก', 'DATA', FALSE, TRUE, 110),
    ('data:worker-names', 'ชื่อช่าง', 'ให้ AI เห็นชื่อช่างจริง เช่น "ช่างสมชาย" แทนที่จะเป็น "ช่าง #1". ข้อมูลจะถูกส่งไปประมวลผลที่ผู้ให้บริการ AI ภายนอก', 'DATA', FALSE, TRUE, 120),
    ('data:cost', 'ต้นทุนและกำไร', 'ให้ AI เห็นตัวเลขต้นทุนและกำไร (margin) เช่น มูลค่าต้นทุนสินค้าในคลัง — ต้องมีสิทธิ์ดูกำไรด้วย. ข้อมูลจะถูกส่งไปประมวลผลที่ผู้ให้บริการ AI ภายนอก', 'DATA', FALSE, TRUE, 130),
    ('data:receivables', 'ยอดลูกหนี้ค้างรับ', 'ให้ AI เห็นยอดเงินลูกหนี้ค้างรับ เช่น ยอดค้างชำระรวมเป็นบาท. ข้อมูลจะถูกส่งไปประมวลผลที่ผู้ให้บริการ AI ภายนอก', 'DATA', FALSE, TRUE, 140)
ON CONFLICT (code) DO NOTHING;

-- 9. seed glossary (ขั้นต่ำ)
INSERT INTO tbm_ai_glossary (metric_code, module, name_th, definition_th, caveat_th, update_date) VALUES
    ('stale_plan', 'executive', 'ใบงานนิ่ง', 'ใบงานผลิตที่ไม่มีการเคลื่อนไหวเลยเป็นเวลา 180 วันขึ้นไป', NULL, NOW()),
    ('unpaid_invoice', 'executive', 'บิลค้างชำระ', 'บิลที่ไม่มีรายการรับชำระเลย และมัดจำ <= 0 และยอดรวมสุทธิ > 0', 'บิลที่จ่ายบางส่วนไม่นับเป็น "ค้างชำระทั้งหมด" ให้ดูยอดจ่ายแยกต่างหาก', NOW()),
    ('so_gold_rate', 'executive', 'ราคาทองในใบสั่งขาย', 'ราคาทองที่บันทึกในใบสั่งขาย (SO)', 'ค่า 2000 คือค่าเริ่มต้นของระบบ ไม่ใช่ราคาทองจริง ห้ามใช้คำนวณมูลค่า', NOW()),
    ('diff_loss', 'executive', 'ส่วนต่างทองสูญเสีย', 'ทองที่อนุญาตให้สูญเสีย ลบ ทองที่สูญเสียจริง (allowed - raw)', 'ค่าบวก = ดี (สูญเสียน้อยกว่าที่อนุญาต) ค่าลบ = สูญเสียเกินกำหนด', NOW()),
    ('worker_slip_mixed_metal', 'executive', 'ใบจ่ายทองช่างฝัง/ช่างแต่ง', 'ใบจ่ายทองของช่างฝังและช่างแต่ง', 'ในใบเดียวกันอาจปนงานเงินกับงานทอง ต้องแยกโลหะก่อนสรุปตัวเลข', NOW()),
    ('weight_actual', 'executive', 'น้ำหนักจริงของชิ้นสินค้า', 'น้ำหนักจริงของสินค้าแต่ละชิ้นในคลัง (stock_piece.weight_actual)', 'ข้อมูลส่วนใหญ่ว่างเปล่า ห้ามสรุปน้ำหนักรวมจากฟิลด์นี้', NOW()),
    ('stock_gold_negative', 'executive', 'ยอดทองคงเหลือติดลบ', 'ยอดทองในคลังวัตถุดิบที่เป็นค่าลบ', 'เกิดจากไม่ได้บันทึกรับเข้า ไม่ใช่ทองหายจริงเสมอไป', NOW()),
    ('production_receipt_legacy', 'executive', 'การรับสินค้าจากผลิต', 'การรับสินค้าที่ผลิตเสร็จเข้าคลัง', 'บางส่วนยังทำในระบบเก่า ตัวเลขในระบบนี้อาจน้อยกว่าความเป็นจริง', NOW()),
    ('bottleneck', 'executive', 'คอขวด', 'ขั้นตอนที่มีคิวงานยาวที่สุด', 'ไม่ใช่ขั้นที่ออกใบงานน้อยที่สุด', NOW())
ON CONFLICT (metric_code) DO NOTHING;
