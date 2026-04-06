---
name: migration
description: คู่มือสร้าง DB migration script สำหรับ PostgreSQL — ใช้เมื่อสร้าง table ใหม่, เพิ่ม column, สร้าง index, หรือ seed data ใดๆ
---

# DB Migration

## ที่เก็บไฟล์

```
Jewelry.Data/Migrations/
├── create_permission_tables.sql
├── create_tbt_stock_basket.sql
└── <migration_name>.sql     ← สร้างใหม่ที่นี่เสมอ
```

---

## Naming Convention

| ประเภท | รูปแบบ | ตัวอย่าง |
|---|---|---|
| สร้าง table ใหม่ | `create_<table_name>.sql` | `create_tbt_stock_basket.sql` |
| เพิ่ม column | `add_<column>_to_<table>.sql` | `add_gold_percent_to_tbm_gold.sql` |
| สร้าง table กลุ่ม | `create_<feature_name>_tables.sql` | `create_permission_tables.sql` |
| แก้ไข column | `alter_<column>_on_<table>.sql` | `alter_status_on_tbt_sale_order.sql` |

---

## SQL Style (PostgreSQL)

### Types ที่ใช้

| ✅ ใช้ | ❌ ห้ามใช้ | หมายเหตุ |
|---|---|---|
| `CHARACTER VARYING` | `VARCHAR` | PostgreSQL standard |
| `TIMESTAMPTZ` | `TIMESTAMP` | ต้องมี timezone เสมอ |
| `BIGSERIAL` | `BIGINT` + sequence แยก | สำหรับ auto-increment PK |
| `SERIAL` | `INT` + sequence แยก | สำหรับ auto-increment PK เล็ก |
| `BOOLEAN` | `BIT`, `INT` | boolean flag |
| `NUMERIC` / `DECIMAL` | `FLOAT` | ตัวเลขทางการเงิน |

### Constraint Naming

| ประเภท | รูปแบบ | ตัวอย่าง |
|---|---|---|
| Primary Key | `<table>_pk` | `tbt_stock_basket_pk` |
| Foreign Key | `<table>_<col>_fk` | `tbt_stock_basket_item_basket_fk` |
| Unique | `<table>_<col>_uq` | `tbm_permission_code_uq` |

### Safety Rules

- ใช้ `IF NOT EXISTS` สำหรับทุก `CREATE TABLE` และ `CREATE INDEX`
- ใช้ `ON CONFLICT DO NOTHING` สำหรับ seed INSERT
- ใส่ comment อธิบาย status/enum values เสมอ

---

## Template — สร้าง Table ใหม่

```sql
-- =============================================
-- Migration: <Feature Name>
-- Date: YYYY-MM-DD
-- Description: <อธิบายสั้นๆ>
-- =============================================

-- =============================================
-- 1. <Table Name>
-- =============================================
CREATE TABLE IF NOT EXISTS <table_name> (
    running         CHARACTER VARYING NOT NULL,
    -- fields...
    status          INT NOT NULL DEFAULT 0,
    -- Status: 0=Draft, 1=Active, 2=Closed
    status_name     CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT <table>_pk PRIMARY KEY (running)
);

CREATE INDEX IF NOT EXISTS idx_<table>_<col> ON <table_name>(<col>);
```

---

## Template — เพิ่ม Column

```sql
-- Migration: Add <column> to <table>
-- Date: YYYY-MM-DD
ALTER TABLE <table_name>
    ADD COLUMN IF NOT EXISTS <column_name> CHARACTER VARYING;
```

---

---

## หลัง Run Migration — สร้าง Entity + อัปเดต JewelryContext

ไม่ใช้ scaffold อัตโนมัติ — Claude สร้าง entity files เองหลังผู้ดูแลระบบ run SQL แล้ว

### ขั้นตอน

1. **สร้าง Entity Class** ใน `Jewelry.Data/Models/Jewelry/<TableName>.cs`
2. **เพิ่ม DbSet** ใน `Jewelry.Data/Context/JewelryContext.cs`
3. **เพิ่ม modelBuilder** ใน `OnModelCreating()` ใน JewelryContext.cs
4. **Build ตรวจ**: `cd Jewelry.Data && dotnet build`

### Template — Entity Class

```csharp
using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtMyTable
{
    public string Running { get; set; } = null!;        // NOT NULL
    public string? OptionalField { get; set; }           // nullable
    public int Status { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreateBy { get; set; } = null!;
    public DateTime? UpdateDate { get; set; }
    public string? UpdateBy { get; set; }

    // Navigation (ถ้ามี FK)
    public virtual ICollection<TbtMyTableItem> TbtMyTableItem { get; set; } = new List<TbtMyTableItem>();
}
```

### Template — DbSet (ใน JewelryContext.cs)

```csharp
public virtual DbSet<TbtMyTable> TbtMyTable { get; set; }
public virtual DbSet<TbtMyTableItem> TbtMyTableItem { get; set; }
```

### Template — modelBuilder (ใน OnModelCreating)

```csharp
modelBuilder.Entity<TbtMyTable>(entity =>
{
    entity.HasKey(e => e.Running).HasName("tbt_my_table_pk");
    entity.ToTable("tbt_my_table");

    entity.Property(e => e.Running)
        .HasColumnType("character varying")
        .HasColumnName("running");
    entity.Property(e => e.Status).HasColumnName("status");
    entity.Property(e => e.CreateDate).HasColumnName("create_date");
    entity.Property(e => e.CreateBy)
        .HasColumnType("character varying")
        .HasColumnName("create_by");
    entity.Property(e => e.UpdateDate).HasColumnName("update_date");
    entity.Property(e => e.UpdateBy)
        .HasColumnType("character varying")
        .HasColumnName("update_by");

    // FK relationship
    entity.HasMany(e => e.TbtMyTableItem)
        .WithOne(e => e.MyTableNavigation)
        .HasForeignKey(e => e.MyTableRunning)
        .HasConstraintName("tbt_my_table_item_table_fk");
});
```

### กฎ mapping

| SQL Type | C# Property | HasColumnType |
|---|---|---|
| `CHARACTER VARYING` | `string` / `string?` | `.HasColumnType("character varying")` |
| `TIMESTAMPTZ` | `DateTime` / `DateTime?` | ไม่ต้องระบุ (EF รู้จักเอง) |
| `INT` | `int` | ไม่ต้องระบุ |
| `BIGSERIAL` | `long` + `.ValueGeneratedOnAdd()` | ไม่ต้องระบุ |
| `BOOLEAN` | `bool` / `bool?` | ไม่ต้องระบุ |
| `NUMERIC` / `DECIMAL` | `decimal` / `decimal?` | ไม่ต้องระบุ |

---

## ตัวอย่างจริง

```sql
-- create_tbt_stock_basket.sql
CREATE TABLE IF NOT EXISTS tbt_stock_basket (
    running         CHARACTER VARYING NOT NULL,
    basket_number   CHARACTER VARYING NOT NULL,
    basket_name     CHARACTER VARYING NOT NULL,
    event_date      TIMESTAMPTZ,
    status          INT NOT NULL DEFAULT 0,
    -- Status: 0=Draft, 1=PendingApproval, 2=Approved, 3=CheckedOut, 4=Closed
    status_name     CHARACTER VARYING,
    create_date     TIMESTAMPTZ NOT NULL,
    create_by       CHARACTER VARYING NOT NULL,
    update_date     TIMESTAMPTZ,
    update_by       CHARACTER VARYING,
    CONSTRAINT tbt_stock_basket_pk PRIMARY KEY (running)
);
```
