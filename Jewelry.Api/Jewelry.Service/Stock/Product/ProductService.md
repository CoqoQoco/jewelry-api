# ProductService — Stock Product Service

## ภาพรวม

`ProductService` เป็น Service Layer หลักสำหรับจัดการ **สินค้าคงคลัง (Stock Product)** ของระบบ Jewelry Management System

รับผิดชอบ:
- ค้นหา / ดูรายละเอียด สินค้าในคลัง
- อัปเดตข้อมูลสินค้าและวัสดุ
- บริหาร **Cost Plan** และ **Cost Version** (การคำนวณต้นทุนสินค้า)
- Dashboard: ข้อมูลสรุปสต็อก รายวัน รายสัปดาห์ รายเดือน

---

## Files ในโฟลเดอร์นี้

| File | หน้าที่ |
|------|---------|
| `IProductService.cs` | Interface ประกาศ method ทั้งหมด |
| `ProductService.cs` | Implementation หลัก |
| `ProductServiceExtension.cs` | Extension class (ว่างเปล่า — สำหรับใช้งานในอนาคต) |

---

## Dependencies (Constructor Injection)

```csharp
public ProductService(
    JewelryContext JewelryContext,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment HostingEnvironment,
    IProductionPlanService ProductionPlanService,
    IRunningNumber runningNumberService
)
```

| Dependency | Field | ใช้งาน |
|-----------|-------|--------|
| `JewelryContext` | `_jewelryContext` | query / save ข้อมูลทุก entity |
| `IHttpContextAccessor` | via BaseService | ดึง `CurrentUsername` จาก JWT token |
| `IHostEnvironment` | `_hostingEnvironment` | inject ไว้ (ไม่ได้ใช้โดยตรงในไฟล์นี้) |
| `IProductionPlanService` | `_productionPlanService` | inject ไว้ (ไม่ได้ใช้โดยตรงในไฟล์นี้) |
| `IRunningNumber` | `_runningNumberService` | สร้าง running number (prefix "CP", "CV") |

---

## Database Tables ที่เกี่ยวข้อง

| Table | ใช้งานใน |
|-------|---------|
| `TbtStockProduct` | ทุก method — ข้อมูลสินค้าหลัก |
| `TbtStockProductMaterial` | List, Get, Update — วัสดุของสินค้า (ทอง, เพชร, พลอย) |
| `TbtStockCostPlan` | CreateProductCostDeatialPlan, AddProductCostDeatialVersion, ListStockCostPlan |
| `TbtStockCostVersion` | AddProductCostDeatialVersion, GetProductCostDetailVersion, ListCostVersion, GetCostVersion |
| `TbtMyJob` | CreateProductCostDeatialPlan, AddProductCostDeatialVersion |
| `TbtProductionPlan` | Get — ดึงข้อมูลต้นทุนจาก production plan |
| `TbtProductionPlanPrice` | Get — รายการราคาวัสดุจาก production plan |

---

## Public Methods

### 1. `List(Search request)` → `IQueryable<Response>`

ค้นหาสินค้าในคลัง กรอง Status = `"Available"` เสมอ พร้อม filter หลายเงื่อนไข

**Filters ที่รองรับ:**

| Parameter | ประเภท | เงื่อนไข |
|-----------|--------|---------|
| `ReceiptType` | `string[]` | `ProductionType` IN array |
| `StockNumber` | string | Contains |
| `StockNumberOrigin` | string | `ProductCode` Contains |
| `Mold` | string | `MoldDesign` Contains |
| `ProductType` | `string[]` | `ProductType` IN array |
| `Gold` | `string[]` | `ProductionType` IN array |
| `GoldSize` | `string[]` | `ProductionTypeSize` IN array |
| `ProductNumber` | string | Contains |
| `ProductNameTh` | string | Contains |
| `ProductNameEn` | string | Contains |
| `Size` | string | Contains |

**Response fields สำคัญ:**
- `StockNumber`, `StockNumberOrigin` (= `ProductCode`)
- `ReceiptNumber`, `ReceiptDate`, `ReceiptType`
- `Mold` (= `MoldDesign ?? Mold`)
- `WoText` (= `$"{Wo}{WoNumber}"`)
- `TagPriceMultiplier` (default `1` ถ้า null)
- `Materials` — list ของ `TbtStockProductMaterial`

> Include `TbtStockProductMaterial` ใน query

---

### 2. `GetStockCostDetail(string stockNumber)` → `IQueryable<PriceTransection>`

ดึง `ProductCostDetail` (JSON) จาก `TbtStockProduct` แล้ว Deserialize เป็น list ของ `PriceTransection`

**Logic:**
- ไม่พบสินค้า → return empty list
- `ProductCostDetail` เป็น null/empty → return empty list
- มีข้อมูล → Deserialize JSON → return as `IQueryable`

---

### 3. `Get(Request request)` → `Task<Get.Response>` ⭐ ซับซ้อนที่สุด

ดูรายละเอียดสินค้าชิ้นเดียว พร้อมข้อมูลต้นทุนแบบ per-piece

**Validation:**
- ต้องมีอย่างน้อย 1 จาก: `StockNumber`, `ProductNumber`, `StockNumberOrigin`
- ถ้าไม่มีเลย → throw `HandleException("StockNumber or ProductNumber or StockNumberOrigin is Required")`
- กรอง `QtyRemaining > 0` เท่านั้น
- ถ้าหาไม่พบ → throw `HandleException(ErrorMessage.NotFound)`

**ลำดับการดึงข้อมูล PriceTransactions:**

```
ขั้น 1: TbtStockProduct.ProductCostDetail (JSON)
   → มี → Deserialize → ใช้เลย

ขั้น 2: มี Wo+WoNumber และ PriceTransactions ยังว่าง?
   → ค้นหา TbtProductionPlan ที่ตรงกัน
       ├── มี TbtProductionPlanPrice?
       │   ├── หาแถว "น้ำหนักทองรวมหลังหักเพชรพลอย" → เพิ่ม Gold entry (÷ plan.ProductQty)
       │   └── เพิ่ม Non-Gold entries ทั้งหมด (÷ plan.ProductQty)
       └── ไม่มี → ดึงจาก Materials (Gold, Gem, Diamond เท่านั้น)

ขั้น 3: ไม่มี Wo และ PriceTransactions ยังว่าง?
   → ดึงจาก Materials (Gold, Gem, Diamond เท่านั้น)
```

**การแปลง NameGroup:**

| Type | NameGroup |
|------|-----------|
| `"Gold"` | `"Gold"` |
| `"Gem"` | `"Gem"` |
| `"Diamond"` | `"Gem"` |
| อื่นๆ | `"Other"` |

**Response fields พิเศษ:**
- `PlanQty` — จำนวนสินค้าต่อ batch จาก production plan
- `PriceTransactions` — list รายการต้นทุนวัสดุ (per-piece)
- ค่า `Qty` และ `QtyWeight` จาก plan price หาร `plan.ProductQty` (per-piece calculation)

---

### 4. `Update(Request request)` → `Task<string>`

อัปเดตข้อมูลสินค้า + วัสดุทั้งหมด

**Permission:** `CheckPermissionLevel("update_stock")`

**Validation:** ค้นหาด้วย `StockNumber` + `ReceiptNumber` — ถ้าไม่พบ throw `NotFound`

**Fields ที่อัปเดต:**

| Field | มาจาก |
|-------|-------|
| `ProductNameEn`, `ProductNameTh` | request |
| `ImagePath`, `ImageName` | request |
| `Qty`, `ProductPrice` | request |
| `MoldDesign` | request.Mold |
| `Size`, `Location` | request |
| `UpdateBy`, `UpdateDate` | CurrentUsername, DateTime.UtcNow |

**วัสดุ (Materials):** ลบ `TbtStockProductMaterial` เดิมทั้งหมด → เพิ่มใหม่จาก request

> ใช้ `TransactionScope` เพื่อ atomic operation

**Return:** `"success"`

---

### 5. `CreateProductCostDeatialPlan(Request request)` → `Task<string>`

สร้าง **Cost Plan Job** สำหรับการตั้งราคาต้นทุนสินค้า

**Flow:**
1. Generate running number prefix `"CP"` → `_running`
2. ค้นหา stock (Status = "Available") — ถ้าไม่พบ throw `NotFound`
3. สร้าง `TbtStockCostPlan` (Status = Pending)
4. สร้าง `TbtMyJob` (JobType = `PlanStockCost`, Status = Pending)
5. Serialize plan data เป็น JSON → `myJob.DataJob`
6. Save ทั้งคู่

**Return:** running number ของ plan (เช่น `"CP240001"`)

---

### 6. `AddProductCostDeatialVersion(Request request)` → `Task<string>`

บันทึก **Cost Version** (ผลการตั้งราคาต้นทุน) เชื่อมกับ plan

**Flow:**
1. ค้นหา stock — ถ้าไม่พบ throw `NotFound`
2. Generate running number prefix `"CV"` → สร้าง `TbtStockCostVersion`
3. Serialize `request.Prictransection` → `ProductCostDetail` (JSON)
4. ถ้า `request.IsOriginCost == true`:
   - อัปเดต `TbtStockProduct.ProductCostDetail` ด้วย JSON นี้
   - อัปเดต `TbtStockProduct.ProductCost` = `SUM(Prictransection.TotalPrice)`
   - อัปเดต `TbtStockProduct.TagPriceMultiplier`
5. ถ้ามี `PlanRunning`:
   - อัปเดต `TbtStockCostPlan` Status → Completed, บันทึก `VersionRunning`
   - อัปเดต `TbtMyJob` Status → Completed

**Request fields สำคัญ:**

| Field | หน้าที่ |
|-------|---------|
| `StockNumber` | สินค้าที่ต้องการตั้งราคา |
| `PlanRunning` | Plan ที่เชื่อมกัน (optional) |
| `IsOriginCost` | ถ้า true → อัปเดตต้นทุนหลักของสินค้า |
| `TagPriceMultiplier` | ตัวคูณราคา tag |
| `CurrencyUnit`, `CurrencyRate` | สกุลเงินที่ใช้คำนวณ |
| `CustomerCode/Name/Address/Tel/Email` | ลูกค้าที่ขอ quotation |
| `Prictransection` | list รายการต้นทุนวัสดุ |

**Return:** `"success"`

---

### 7. `GetProductCostDetailVersion(string stockNumber)` → `IQueryable<ListProductCost.Response>`

ดึงประวัติ Cost Version ทั้งหมดของ stock number หนึ่ง

**Query:** `TbtStockCostVersion WHERE StockNumber == stockNumber`

**Response:** รวม `Prictransection` (Deserialize JSON) ทุก version

---

### 8. `GetCostVersion(Request request)` → `GetCostVersion.Response`

ดูรายละเอียด Cost Version เดียว โดยใช้ `PlanRunning`

**Validation:**
- `PlanRunning` ต้องไม่ว่าง — throw `HandleException("PlanRunning is Required")`
- ถ้าหาไม่พบ — throw `NotFound`

**Query:** `TbtStockCostVersion WHERE JobRunning == request.PlanRunning`

---

### 9. `ListStockCostPlan(Search request)` → `IQueryable<ListStockCostPlan.Response>`

ค้นหา Cost Plans พร้อม filter

**Filters:**

| Parameter | เงื่อนไข |
|-----------|---------|
| `StockNumber` | Contains |
| `Running` | Contains |
| `StatusId` | Equals |
| `StatusName` | Contains |
| `CreateBy` | Contains |
| `CreateDateFrom` | `>= CreateDateFrom` |
| `CreateDateTo` | `< CreateDateTo.AddDays(1)` |
| `IsActive` | Equals |

---

### 10. `ListCostVersion(Search request)` → `IQueryable<ListCostVersion.Response>`

ค้นหา Cost Versions พร้อม filter

**Filters:**

| Parameter | เงื่อนไข |
|-----------|---------|
| `StockNumber` | Contains |
| `Running` | Contains |
| `CreateBy` | Contains |
| `CreateDateFrom` | `>= CreateDateFrom` |
| `CreateDateTo` | `< CreateDateTo.AddDays(1)` |

---

### 11. `ListName(Request request)` → `IQueryable<ListName.Response>`

ค้นหาชื่อสินค้าสำหรับ autocomplete

**Mode:**
- `"TH"` → ค้นใน `ProductNameTh` (Contains) → return distinct
- `"EN"` → ค้นใน `ProductNameEn` (Contains) → return distinct
- อื่นๆ → throw `HandleException("Mode is Required")`

---

## Dashboard Methods

> ทุก method รองรับ filter ผ่าน `DashboardRequest`:
> - `ProductType[]`, `ProductionType[]`, `ProductionTypeSize[]`, `Status`

Base query ของ Dashboard: `TbtStockProduct WHERE QtyRemaining > 0` (AsNoTracking)

### 12. `GetProductDashboard(DashboardRequest)` → `Task<DashboardResponse>`

ข้อมูลภาพรวมสต็อก ณ ปัจจุบัน

| ส่วน | ข้อมูล |
|------|-------|
| `Summary` | TotalProducts, TotalQuantity, TotalValue, Available/OnProcess count & qty |
| `Categories` | groupBy ProductTypeName + ProductionType + ProductionTypeSize → sort TotalValue DESC |
| `LastActivities` | 10 รายการล่าสุด (orderBy CreateDate DESC) |
| `DataAtDate` | `DateTime.UtcNow` |

---

### 13. `GetTodayReport(DashboardRequest)` → `Task<TodayReportResponse>`

รายงานประจำวัน (วันนี้ UTC)

| ส่วน | ข้อมูล |
|------|-------|
| `ReportDate` | today UTC |
| `Summary` | TotalTransactions, NewStockItems, TotalValue |
| `Transactions` | รายการสินค้าที่เพิ่มวันนี้ทั้งหมด |

---

### 14. `GetWeeklyReport(DashboardRequest)` → `Task<WeeklyReportResponse>`

รายงานประจำสัปดาห์ (เริ่มวันอาทิตย์)

| ส่วน | ข้อมูล |
|------|-------|
| `WeekStartDate`, `WeekEndDate` | ช่วงสัปดาห์ |
| `WeekNumber` | `"Week N"` |
| `Summary` | TotalTransactions, TotalValue |
| `DailyMovements` | groupBy `CreateDate.Date` → TransactionCount + TotalValue แต่ละวัน |

---

### 15. `GetMonthlyReport(DashboardRequest)` → `Task<MonthlyReportResponse>`

รายงานประจำเดือน (เดือนปัจจุบัน)

| ส่วน | ข้อมูล |
|------|-------|
| `Year`, `Month`, `MonthName` | ข้อมูลเดือน |
| `MonthStartDate`, `MonthEndDate` | ช่วงวันที่ |
| `Summary` | TotalTransactions, TotalValue, TotalAvailableProducts |
| `WeeklyComparisons` | groupBy WeekOfYear → เปรียบเทียบแต่ละสัปดาห์ในเดือน |

> `GetWeeklyComparisons` ดึงข้อมูลด้วย `ToListAsync()` ก่อน แล้ว group ใน memory (C# LINQ)

---

## Private Helper Methods

| Method | Signature | หน้าที่ |
|--------|-----------|---------|
| `BuildStockQuery` | `(DashboardRequest)` | สร้าง base query + apply filters |
| `GetStockSummary` | `(DashboardRequest)` | groupBy 1 → summary aggregate |
| `GetCategoryBreakdown` | `(DashboardRequest)` | groupBy 3 fields → sort DESC |
| `GetLastActivities` | `(DashboardRequest)` | Take 10 latest |
| `GetTodaySummary` | `(today, tomorrow, request)` | filter วันนี้ → summary |
| `GetTodayTransactions` | `(today, tomorrow, request)` | filter วันนี้ → list |
| `GetWeeklySummary` | `(start, end, request)` | filter สัปดาห์ → summary |
| `GetDailyMovements` | `(start, end, request)` | filter สัปดาห์ → groupBy วัน |
| `GetMonthlySummary` | `(start, end, request)` | filter เดือน → summary |
| `GetWeeklyComparisons` | `(start, end, request)` | filter เดือน → ToListAsync → groupBy WeekOfYear |
| `GetWeekOfYear` | `(DateTime date)` | คำนวณเลขสัปดาห์ของปีตาม CultureInfo |
| `GetNameGroupGroup` | `(string type)` | map type → NameGroup (Gold/Gem/Other) |

---

## Running Number Prefixes

| Prefix | ใช้ใน | ตัวอย่าง |
|--------|-------|---------|
| `"CP"` | `CreateProductCostDeatialPlan` | `CP240001` |
| `"CV"` | `AddProductCostDeatialVersion` | `CV240001` |

---

## Business Logic สำคัญ

### Cost Plan Lifecycle

```
สร้าง Plan → CreateProductCostDeatialPlan()
   TbtStockCostPlan  (Status: Pending)
   TbtMyJob          (JobType: PlanStockCost, Status: Pending)
         ↓
บันทึก Version → AddProductCostDeatialVersion()
   TbtStockCostVersion  (บันทึก quotation/ราคาต้นทุน)
   ถ้า IsOriginCost=true → อัปเดต TbtStockProduct.ProductCostDetail
   TbtStockCostPlan  (Status: Completed + VersionRunning)
   TbtMyJob          (Status: Completed)
```

### PriceTransaction Priority ใน Get()

```
1. TbtStockProduct.ProductCostDetail (JSON) — บันทึกโดย AddProductCostDeatialVersion
      ↓ ถ้าไม่มี
2. TbtProductionPlan + TbtProductionPlanPrice (เฉพาะสินค้าที่มี Wo/WoNumber)
      ↓ ถ้าไม่มี plan price
3. TbtStockProductMaterial (เฉพาะ Type: Gold, Gem, Diamond)
```

### Per-Piece Calculation

ค่า `Qty` และ `QtyWeight` จาก `TbtProductionPlanPrice` จะหาร `plan.ProductQty` เพื่อให้ได้ต้นทุนต่อชิ้น (production plan เก็บข้อมูลทั้ง batch)

---

*Last updated: 2026-02-24*
