---
name: paging
description: ทำ list endpoint แบบ paging/sort/filter — ใช้ DataSourceExtension / ToDataSourceResult เพื่อ return DataSourceResult รองรับ Take/Skip/Sort จาก Kendo DataSource
---

# Paging Convention (DataSourceResult)

## WHAT

`DataSourceExtension.cs` คือ convention มาตรฐานสำหรับ list endpoint ที่ต้องรองรับ paging, sorting, filtering ในโปรเจกต์นี้

- Extension: `Jewelry.Api/Extension/DataSourceExtension.cs`
- Underlying: `Kendo.DynamicLinqCore.QueryableExtensions.ToDataSourceResult()`
- Return type: `DataSourceResult { Data, Total }`

---

## HOW (Backend)

### ขั้นตอนที่ 1 — Request DTO `: DataSourceRequest`

```csharp
using Kendo.DynamicLinqCore;

public class SearchMyRequest : DataSourceRequest
{
    // domain filters เพิ่มได้ตรงๆ หรือจะ nest ก็ได้
    public string? Status { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
}
```

`DataSourceRequest` มี property ที่รับจาก frontend: `Take`, `Skip`, `Sort`, `Filter`, `Aggregate`, `Group`

ห้ามประกาศ `Take`, `Skip`, `Sort` ซ้ำใน DTO — มาจาก base class แล้ว

### ขั้นตอนที่ 2 — Service

**กรณีปกติ** (ไม่มี post-processing): return `IQueryable<Response>` แล้วให้ Controller เรียก `.ToDataSource(request)`

```csharp
public IQueryable<MyResponse> List(SearchMyRequest request)
{
    var query = _context.TbtMyTable.AsQueryable();
    // apply domain filters...
    return query.Select(x => new MyResponse { ... });
}
```

**กรณีมี post-processing** (ต้องเสริมข้อมูลหลัง materialize): เรียก `ToDataSourceResult` ก่อน แล้ว enrich เฉพาะ paged entities

```csharp
using Kendo.DynamicLinqCore;

public async Task<DataSourceResult> Search(SearchMyRequest request)
{
    var query = _context.TbtMyTable
        .Include(x => x.Items)
        .AsQueryable();

    // apply domain filters...
    query = query.OrderByDescending(x => x.CreateDate);

    // paging: materialize เฉพาะ page ปัจจุบัน
    var dataSource = query.ToDataSourceResult(request);
    var pageEntities = dataSource.Data.Cast<TbtMyTable>().ToList();

    // enrich เฉพาะ pageEntities (ไม่ใช่ทั้งตาราง)
    var ids = pageEntities.Select(x => x.Id).ToList();
    var extras = await _context.TbtRelated.Where(r => ids.Contains(r.OwnerId)).ToListAsync();

    var result = pageEntities.Select(x => new MyResponse
    {
        // ... mapping + extras
    }).ToList();

    dataSource.Data = result;
    return dataSource;
}
```

ตัวอย่างจริง: `Jewelry.Service/Production/PrePlan/ProductionPrePlanService.cs` — method `Search`

### ขั้นตอนที่ 3 — Controller

```csharp
using Kendo.DynamicLinqCore;
using Jewelry.Api.Extension;

[Route("Search")]
[HttpPost]
[ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
public async Task<IActionResult> Search([FromBody] SearchMyRequest request)
{
    var response = await _service.Search(request);
    return Ok(response);
}

// หรือถ้า service return IQueryable<Response>:
[Route("List")]
[HttpPost]
public DataSourceResult List([FromBody] SearchMyRequest request)
{
    var query = _service.List(request);
    return query.ToDataSource(request);  // extension ใน Jewelry.Api.Extension
}
```

---

## DataSourceExtension — Two Overloads

| Overload | Input | รองรับ |
|---|---|---|
| `IQueryable<T>.ToDataSource(request)` | lazy query | Take, Skip, Sort, Filter, Aggregate, Group |
| `IEnumerable<T>.ToDataSource(request)` | materialized list | Take, Skip, Sort, Group |

หมายเหตุ: `IQueryable` รองรับ Filter ได้ (server-side), `IEnumerable` ไม่รองรับ Filter

---

## Frontend Contract

Frontend ส่ง request body:

```json
{
  "take": 20,
  "skip": 0,
  "sort": [{ "field": "createDate", "dir": "desc" }],
  "status": "Draft"
}
```

อ่าน response:

```javascript
const res = await api.post('/MyEndpoint/Search', payload)
// res.data.data  → array ของ items
// res.data.total → จำนวนทั้งหมด (ใช้คำนวณ total pages)
```

Component ใช้ `DataTableWithPaging` พร้อม `handlePageChange` / `handleSortChange`

---

## ตัวอย่างจริง

✅ **Good — pattern ถูกต้อง:**

```
Jewelry.Api/Controllers/Sale/SaleOrderController.cs
Jewelry.Api/Controllers/Stock/StockProductController.cs
Jewelry.Api/Controllers/Production/ProductionPrePlanController.cs (Search, หลังแก้)
Jewelry.Service/Production/PrePlan/ProductionPrePlanService.cs (Search, หลังแก้)
```

```csharp
// StockProductController.cs — simple IQueryable pattern
[Route("List")]
[HttpPost]
public DataSourceResult List([FromBody] jewelry.Model.Stock.Product.List.Request request)
{
    var response = _service.List(request.Search);
    return response.ToDataSource(request);
}
```

❌ **Bad — pattern เก่าของ PrePlan Search (ก่อนแก้):**

```csharp
// ❌ materialize ทั้งตาราง ไม่ใช้ paging
var list = await query.OrderByDescending(x => x.CreateDate).ToListAsync();

// ❌ return IList ไม่มี Total
return result; // IList<SearchPrePlanResponse>

// ❌ Take/Skip ใน DTO ไม่ได้ถูกใช้จริง
public int Take { get; set; } = 50;
public int Skip { get; set; } = 0;
```

---

## หมายเหตุ — Service Layer

`Jewelry.Service` ไม่ reference `Jewelry.Api` โดยตรง (จะเกิด circular dependency)

ถ้า service ต้องเรียก paging ให้ใช้ `ToDataSourceResult` จาก `Kendo.DynamicLinqCore` โดยตรง:

```csharp
using Kendo.DynamicLinqCore;

var dataSource = query.ToDataSourceResult(request);
// ใช้ได้เพราะ Jewelry.Service → jewelry.Model → DynamicLinqCore (transitive)
```

`ToDataSource` (extension ใน `Jewelry.Api.Extension`) จะใช้ได้เฉพาะใน Controller layer
