# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Workflow Rule

**ทุก task → เสนอ Plan ก่อนเสมอ → รอ confirm → ค่อย implement**

---

## Project Overview

**Jewelry Management System API** — .NET 8 + Entity Framework Core + PostgreSQL

ระบบจัดการการผลิตเครื่องประดับ ครอบคลุม Production Planning, Stock, Worker, Receipt, Cost Tracking

---

## Layer Structure

| Layer | Purpose |
|---|---|
| **Jewelry.Api** | Entry point: controllers, middleware, DI registration — ห้าม business logic โดยตรง |
| **Jewelry.Service** | Business logic ทั้งหมด; controllers ต้องผ่าน service เสมอ |
| **Jewelry.Data** | EF models (auto-generated via scaffold) + `JewelryContext` — ห้าม edit manual |
| **jewelry.Model** | DTOs, request/response objects, constants — ไม่มี logic |
| **DynamicLinqCore** | Custom LINQ extensions สำหรับ dynamic query filtering |

### Middleware Pipeline (Jewelry.Api)
1. `AuthenticationMiddleware` — JWT validation + user status check
2. `ExceptionMiddleware` — Global exception handling
3. Standard ASP.NET Core middleware

### Service Registration
Services ทั้งหมด register ใน `InfrastructureServiceRegistration.cs` ผ่าน DI pattern

### Controller Base
Controllers extend `ApiControllerBase` สำหรับ consistent model state handling

---

## Development Commands

```bash
# Development (from Jewelry.Api directory)
dotnet run --project Jewelry.Api

# Build entire solution
dotnet build Jewelry.Api.sln

# Docker
cd Jewelry.Api
docker-compose down && docker-compose build && docker-compose up -d

# DB Migration — ดูรายละเอียดที่ @.claude/skills/migration/SKILL.md
# สร้าง SQL ใน Jewelry.Data/Migrations/ แล้วผู้ดูแลระบบ run เอง + สร้าง entity เอง
```

---

## Configuration

| Setting | Development | Docker |
|---|---|---|
| DB Host | `localhost:5432` | `host.docker.internal:5432` |
| API Port | `http://localhost:2001` (https 7000/7001 optional) | Port `2001` → container `80` |
| Swagger | `/swagger` | ไม่เปิด |
| CORS | `localhost:2002` (UI dev), `2001`, `5173–5175`, `7000`, `7001` | — |

Connection string อยู่ใน `appsettings.json`

---

## Gotchas

### DateTime — PostgreSQL `timestamptz` Rule
**ใช้ `DateTime.UtcNow` เสมอ** — PostgreSQL `timestamp with time zone` รับเฉพาะ UTC

```csharp
// ❌ Bad — throws DbUpdateException (Kind=Local)
header.UpdateDate = DateTime.Now;

// ✅ Good
header.UpdateDate = DateTime.UtcNow;
```

### EF Core — Update Pattern
เมื่อ update entity ที่ load ด้วย `Include` ต้องเรียก `Update`/`UpdateRange` ก่อน `SaveChangesAsync`:

```csharp
// ✅ Header
_jewelryContext.TbtProductionPlanStatusHeader.Update(header);

// ✅ Detail collection
var updatedDetails = new List<TbtProductionPlanStatusDetail>();
foreach (var item in request.Items)
{
    var detail = header.TbtProductionPlanStatusDetail.FirstOrDefault(d => d.ItemNo == item.ItemNo);
    if (detail == null) continue;
    detail.SomeField = item.SomeField;
    updatedDetails.Add(detail);
}
_jewelryContext.TbtProductionPlanStatusDetail.UpdateRange(updatedDetails);
await _jewelryContext.SaveChangesAsync();
```

### File Storage
Images stored in `Images/` subdirectories organized by feature (Mold, Stock, etc.)

### Audit Fields — create_by / update_by
เก็บ `CurrentUsername` (ชื่อผู้ใช้) เสมอ — **ห้ามเก็บ user id**

```csharp
// ✅ Good — ตรงมาตรฐานทั้งระบบ
ticket.CreateBy = CurrentUsername;
ticket.UpdateBy = CurrentUsername;

// ❌ Bad — เก็บเป็นเลข id อ่านไม่รู้เรื่อง ต้อง join เพิ่มตอนแสดงผล
ticket.CreateBy = CurrentUserId ?? CurrentUsername;
```

- filter "ของฉัน" ก็เทียบด้วย username: `x.CreateBy == CurrentUsername`
- `CurrentUserId` ใช้เฉพาะ logic ที่ต้องการ id จริงๆ ไม่ใช่ audit field

---

## Skills & Agents Reference

| Resource | ใช้เมื่อ |
|---|---|
| @.claude/skills/md-instruction/SKILL.md | เขียน/แก้ไขไฟล์ .md |
| @.claude/skills/datetime-handling/SKILL.md | DateTimeOffset, UTC filter, บันทึกวันที่ |
| @.claude/skills/migration/SKILL.md | สร้าง DB migration SQL, naming convention, scaffold |
| @.claude/skills/paging/SKILL.md | ทำ list endpoint แบบ paging/sort/filter |
| @.claude/agents/api-implementer.md | implement API ตาม plan ที่ confirm แล้ว |
