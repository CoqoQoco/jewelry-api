-- =============================================
-- 1. สร้างตาราง tbm_permission (master สิทธิ์)
-- =============================================
CREATE TABLE IF NOT EXISTS tbm_permission (
    id SERIAL,
    code CHARACTER VARYING(100) NOT NULL,
    name CHARACTER VARYING(200) NOT NULL,
    group_name CHARACTER VARYING(100),
    description CHARACTER VARYING(500),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    create_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    create_by CHARACTER VARYING(100) NOT NULL DEFAULT 'system',
    update_date TIMESTAMPTZ,
    update_by CHARACTER VARYING(100),
    CONSTRAINT tbm_permission_pk PRIMARY KEY (id),
    CONSTRAINT tbm_permission_code_uq UNIQUE (code)
);

-- =============================================
-- 2. สร้างตาราง tbt_role_permission (junction role ↔ permission)
-- =============================================
CREATE TABLE IF NOT EXISTS tbt_role_permission (
    id SERIAL,
    role_id INT NOT NULL,
    permission_id INT NOT NULL,
    create_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    create_by CHARACTER VARYING(100) NOT NULL DEFAULT 'system',
    CONSTRAINT tbt_role_permission_pk PRIMARY KEY (id),
    CONSTRAINT tbt_role_permission_role_fk FOREIGN KEY (role_id) REFERENCES tbm_user_role(id),
    CONSTRAINT tbt_role_permission_permission_fk FOREIGN KEY (permission_id) REFERENCES tbm_permission(id),
    CONSTRAINT tbt_role_permission_uq UNIQUE (role_id, permission_id)
);

-- =============================================
-- 3. Seed: Insert 36 permissions
-- =============================================
INSERT INTO tbm_permission (code, name, group_name, create_by) VALUES
    -- Dashboard
    ('dashboard', 'แดชบอร์ด', 'Dashboard', 'system'),
    -- User Management
    ('user:view', 'ดูข้อมูลผู้ใช้', 'User Management', 'system'),
    ('user:create', 'สร้างผู้ใช้', 'User Management', 'system'),
    ('user:edit', 'แก้ไขผู้ใช้', 'User Management', 'system'),
    ('user:delete', 'ลบผู้ใช้', 'User Management', 'system'),
    ('user:dev', 'ผู้พัฒนาระบบ', 'User Management', 'system'),
    -- Production
    ('production:view', 'ดูงานผลิต', 'Production', 'system'),
    ('production:create', 'สร้างงานผลิต', 'Production', 'system'),
    ('production:edit', 'แก้ไขงานผลิต', 'Production', 'system'),
    -- Mold
    ('mold:view', 'ดูแม่พิมพ์', 'Mold', 'system'),
    ('mold:create', 'สร้างแม่พิมพ์', 'Mold', 'system'),
    ('mold:edit', 'แก้ไขแม่พิมพ์', 'Mold', 'system'),
    -- Stock Gem
    ('stock-gem:view', 'ดูสต็อกพลอย', 'Stock Gem', 'system'),
    ('stock-gem:create', 'สร้างสต็อกพลอย', 'Stock Gem', 'system'),
    ('stock-gem:edit', 'แก้ไขสต็อกพลอย', 'Stock Gem', 'system'),
    -- Stock Product
    ('stock-product:view', 'ดูสต็อกสินค้า', 'Stock Product', 'system'),
    ('stock-product-gr:view', 'ดูรับสินค้าจากผลิต', 'Stock Product', 'system'),
    ('stock-product-gr:create', 'สร้างรับสินค้าจากผลิต', 'Stock Product', 'system'),
    ('stock-product-gr-image:create', 'อัพโหลดรูปสินค้า', 'Stock Product', 'system'),
    -- Customer
    ('customer:view', 'ดูลูกค้า', 'Customer', 'system'),
    ('customer:create', 'สร้างลูกค้า', 'Customer', 'system'),
    ('customer:edit', 'แก้ไขลูกค้า', 'Customer', 'system'),
    -- Worker
    ('worker:view', 'ดูช่าง', 'Worker', 'system'),
    ('worker:create', 'สร้างช่าง', 'Worker', 'system'),
    ('worker:edit', 'แก้ไขช่าง', 'Worker', 'system'),
    -- Report
    ('report:view', 'ดูรายงาน', 'Report', 'system'),
    -- Master
    ('master:view', 'ดู Master Data', 'Master', 'system'),
    -- Sale
    ('sale:view', 'ดูการขาย', 'Sale', 'system'),
    ('sale:create', 'สร้างการขาย', 'Sale', 'system'),
    -- Mobile
    ('mobile:dashboard', 'Mobile แดชบอร์ด', 'Mobile', 'system'),
    ('mobile:scan', 'Mobile สแกน QR', 'Mobile', 'system'),
    ('mobile:tasks', 'Mobile งานของฉัน', 'Mobile', 'system'),
    ('mobile:profile', 'Mobile โปรไฟล์', 'Mobile', 'system'),
    ('mobile:notifications', 'Mobile แจ้งเตือน', 'Mobile', 'system'),
    ('mobile:sale', 'Mobile การขาย', 'Mobile', 'system')
ON CONFLICT (code) DO NOTHING;

-- =============================================
-- 4. Seed: Role-Permission mapping (ตรงตาม ROLE_PERMISSIONS ใน config.js)
-- =============================================

-- Dev: ทุก permission
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Dev' AND r.is_active = TRUE AND p.is_active = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Admin: ทุกอย่างยกเว้น user:delete, user:dev
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Admin' AND r.is_active = TRUE AND p.is_active = TRUE
    AND p.code NOT IN ('user:delete', 'user:dev')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- StockOperator
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'StockOperator' AND r.is_active = TRUE
    AND p.code IN (
        'stock-product:view', 'stock-product-gr:view', 'stock-product-gr:create', 'stock-product-gr-image:create',
        'mobile:dashboard', 'mobile:scan', 'mobile:profile'
    )
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- ProductionOperator
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'ProductionOperator' AND r.is_active = TRUE
    AND p.code IN (
        'production:view', 'production:create', 'production:edit',
        'mobile:dashboard', 'mobile:scan', 'mobile:tasks', 'mobile:profile'
    )
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Operator
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Operator' AND r.is_active = TRUE
    AND p.code IN (
        'dashboard',
        'production:view', 'production:create', 'production:edit',
        'mold:view', 'mold:create', 'mold:edit',
        'stock-gem:view', 'stock-gem:create', 'stock-gem:edit',
        'customer:view', 'customer:create', 'customer:edit',
        'worker:view', 'worker:create', 'worker:edit',
        'report:view',
        'mobile:dashboard', 'mobile:scan', 'mobile:tasks', 'mobile:profile', 'mobile:notifications'
    )
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Sale
INSERT INTO tbt_role_permission (role_id, permission_id, create_by)
SELECT r.id, p.id, 'system'
FROM tbm_user_role r
CROSS JOIN tbm_permission p
WHERE r.name = 'Sale' AND r.is_active = TRUE
    AND p.code IN (
        'sale:view', 'sale:create',
        'mobile:dashboard', 'mobile:profile', 'mobile:sale'
    )
ON CONFLICT (role_id, permission_id) DO NOTHING;
