---
name: datetime-handling
description: การจัดการ DateTime/DateTimeOffset กับ PostgreSQL — ใช้เมื่อสร้าง request DTO ที่มี date, filter ด้วยวันที่, หรือบันทึกวันที่ลง DB
---

# DateTime Handling (PostgreSQL UTC)

## กฎหลัก

PostgreSQL ใช้ `timestamp with time zone` (timestamptz) — ต้องส่ง UTC เสมอ

---

## Request DTO — ใช้ `DateTimeOffset` ไม่ใช่ `DateTime`

Frontend ส่ง ISO string (`2026-04-05T00:00:00.000Z`) → .NET parse เป็น `DateTimeOffset` อัตโนมัติ

```csharp
// ✅ Good
public DateTimeOffset? StartDate { get; set; }
public DateTimeOffset? EndDate { get; set; }

// ❌ Bad — DateTime ไม่มี offset info, อาจเกิด ArgumentOutOfRangeException
public DateTime? StartDate { get; set; }
```

---

## Filter ใน Service — ใช้ Helper Extensions

**File:** `Jewelry.Service/Helper/Datetime.cs`

| Method | ใช้เมื่อ | ผลลัพธ์ |
|---|---|---|
| `.StartOfDayUtc()` | filter วันเริ่มต้น | `00:00:00 UTC` |
| `.EndOfDayUtc()` | filter วันสิ้นสุด | `23:59:59 UTC` |
| `.StartOfDay()` | ต้องการ local offset | `00:00:00 +offset` |
| `.EndOfDay()` | ต้องการ local offset | `23:59:59 +offset` |

```csharp
using Jewelry.Service.Helper;

// ✅ Good — ใช้ extension method
if (request.StartDate.HasValue)
{
    query = query.Where(x => x.CreateDate >= request.StartDate.Value.StartOfDayUtc());
}
if (request.EndDate.HasValue)
{
    query = query.Where(x => x.CreateDate <= request.EndDate.Value.EndOfDayUtc());
}

// ❌ Bad — manual DateTime.SpecifyKind
var start = DateTime.SpecifyKind(request.StartDate.Value.Date, DateTimeKind.Utc);
```

---

## บันทึกลง DB — ใช้ `DateTime.UtcNow`

```csharp
// ✅ Good
header.CreateDate = DateTime.UtcNow;
header.UpdateDate = DateTime.UtcNow;

// ❌ Bad — Kind=Local → DbUpdateException
header.CreateDate = DateTime.Now;
```

---

## Frontend ส่งมาอย่างไร

UI (Vue 3) ใช้ `formatISOString(date)` จาก `src/services/utils/dayjs.js`:
- ส่งเป็น ISO 8601 string: `"2026-04-01T00:00:00.000+07:00"`
- .NET parse เป็น `DateTimeOffset` → มี offset info ครบ

---

## สรุป

| ที่ | ใช้ Type | ห้ามใช้ |
|---|---|---|
| Request DTO (date filter) | `DateTimeOffset?` | `DateTime?` |
| Service filter | `.StartOfDayUtc()` / `.EndOfDayUtc()` | `DateTime.SpecifyKind()` |
| บันทึก DB | `DateTime.UtcNow` | `DateTime.Now` |
