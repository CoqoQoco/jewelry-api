# Sale Order Service Documentation

## ภาพรวม (Overview)
Sale Order Service เป็นบริการสำหรับจัดการใบสั่งขายในระบบ Jewelry Management System โดยใช้แบบแผนเดียวกันกับ Quotation Service และรองรับการ Upsert (Insert/Update) ข้อมูล

## โครงสร้างไฟล์ (File Structure)

```
Jewelry.Service/Sale/SaleOrder/
├── ISaleOrderService.cs        # Interface สำหรับ service
├── SaleOrderService.cs         # Implementation หลัก
└── saleorder.md               # เอกสารนี้

jewelry.Model/Sale/SaleOrder/
├── Create/
│   └── Request.cs             # DTO สำหรับสร้าง/อัปเดต
├── Get/
│   ├── Request.cs             # DTO สำหรับขอข้อมูล
│   └── Response.cs            # DTO สำหรับตอบกลับข้อมูล
├── List/
│   ├── Request.cs             # DTO สำหรับค้นหา
│   └── Response.cs            # DTO สำหรับรายการ
└── ConfirmStock/
    ├── Request.cs             # DTO สำหรับยืนยันการขาย stock
    └── Response.cs            # DTO สำหรับตอบกลับการยืนยัน

Jewelry.Api/Controllers/Sale/
└── SaleOrderController.cs     # API Controller
```

## ฟีเจอร์หลัก (Main Features)

### 1. Upsert Operation
- **สร้างใบสั่งขายใหม่**: เมื่อไม่พบ SoNumber ในระบบ
- **อัปเดตใบสั่งขายที่มีอยู่**: เมื่อพบ SoNumber ในระบบแล้ว
- **Running Number**: สร้างเลขรันอัตโนมัติด้วย prefix "SO"

### 2. Get Operation
- ดึงข้อมูลใบสั่งขายตาม SoNumber
- ตรวจสอบการมีอยู่และส่งคืนข้อมูลทั้งหมด

### 3. List Operation
- ค้นหาและกรองใบสั่งขายตามเงื่อนไขต่างๆ
- รองรับ pagination และ sorting ผ่าน Kendo DataSource

### 4. ConfirmStock Operation ⭐ **ENHANCED**
- ยืนยันการขาย stock items ในใบสั่งขาย
- **Enhanced Validation**: ตรวจสอบความถูกต้องอย่างครอบคลุม
  - ตรวจสอบสถานะใบสั่งขาย (อนุญาตเฉพาะสถานะ 100, 200)
  - ตรวจสอบการมีอยู่ของ Stock ในระบบ
  - ตรวจสอบจำนวน Stock ที่เหลือ (Qty - QtySale)
  - ป้องกันการยืนยัน Stock ซ้ำในใบสั่งขายเดียวกัน
- **Automatic Stock Updates**: อัปเดต QtySale ใน TbtStockProduct อัตโนมัติ
- **Price Calculation**: คำนวณราคาตาม currency rate, markup, และ discount
- **Transaction Safety**: ใช้ database transaction เพื่อความถูกต้องของข้อมูล

## โครงสร้างข้อมูล (Data Structure)

### TbtSaleOrder Entity
ตาราง database ที่มีฟิลด์หลักดังนี้:

```csharp
- Running (string)           # เลขรันของระบบ
- SoNumber (string)          # เลขที่ใบสั่งขาย (Primary Key)
- CreateDate/CreateBy        # ข้อมูลการสร้าง
- UpdateDate/UpdateBy        # ข้อมูลการอัปเดต
- DeliveryDate (DateTime?)   # วันที่ส่งมอบ
- Status (int)               # สถานะใบสั่งขาย
- StatusName (string)        # ชื่อสถานะ
- RefQuotation (string?)     # อ้างอิงใบเสนอราคา
- Payment (int)              # เงื่อนไขการชำระ
- PaymentName (string?)      # ชื่อเงื่อนไขการชำระ
- DepositPercent (int?)      # เปอร์เซ็นต์เงินมัดจำ
- Priority (string)          # ลำดับความสำคัญ
- Data (string?)             # ข้อมูล JSON สำหรับรายการสินค้า
- CustomerName (string)      # ชื่อลูกค้า
- CustomerCode (string)      # รหัสลูกค้า
- CustomerAddress (string?)  # ที่อยู่ลูกค้า
- CustomerTel (string?)      # เบอร์โทรลูกค้า
- CustomerEmail (string?)    # อีเมลลูกค้า
- CustomerRemark (string?)   # หมายเหตุลูกค้า
- CurrencyUnit (string)      # หน่วยสกุลเงิน
- CurrencyRate (decimal)     # อัตราแลกเปลี่ยน
- Markup (decimal?)          # อัตรากำไร
- Discount (decimal?)        # ส่วนลด
- GoldRate (decimal?)        # ราคาทอง
- Remark (string?)           # หมายเหตุ
```

### TbtSaleOrderProduct Entity
ตาราง database สำหรับรายการสินค้าในใบสั่งขาย:

```csharp
- Running (string)            # เลขรันของใบสั่งขาย
- SoNumber (string)           # เลขที่ใบสั่งขาย (Foreign Key)
- CreateDate/CreateBy         # ข้อมูลการสร้าง
- UpdateDate/UpdateBy         # ข้อมูลการอัปเดต
- StockNumber (string)        # เลขที่ stock สินค้า
- Stocknumberorigin (string?) # เลขที่สินค้าต้นฉบับ
- Id (long)                   # ID ของสินค้า
- PriceOrigin (decimal)       # ราคาต้นฉบับ (THB)
- CurrencyUnit (string)       # หน่วยสกุลเงิน
- CurrencyRate (decimal)      # อัตราแลกเปลี่ยน
- Markup (decimal?)           # อัตรากำไร
- Discount (decimal?)         # ส่วนลด
- GoldRate (decimal?)         # ราคาทอง
- Remark (string?)            # หมายเหตุ
- NetPrice (string?)          # ราคาสุทธิ
- PriceDiscount (decimal)     # ราคาหลังหักส่วนลด
- PriceAfterCurrecyRate (decimal) # ราคาหลังคิดอัตราแลกเปลี่ยน
- Qty (decimal)               # จำนวน
- IsConfirmed (bool)          # สถานะการยืนยันการขาย (default: false)
- ConfirmedDate (DateTime?)   # วันที่ยืนยันการขาย
- ConfirmedBy (string?)       # ผู้ยืนยันการขาย
```

## API Endpoints

### 1. POST /SaleOrder/Upsert
สร้างหรืออัปเดตใบสั่งขาย

**Request Body:**
```json
{
  "soNumber": "SO-2025-001",
  "deliveryDate": "2025-02-15T00:00:00Z",
  "status": 1,
  "statusName": "ร่าง",
  "refQuotation": "QUO-2025-001",
  "payment": 1,
  "paymentName": "เงินสด",
  "depositPercent": 30,
  "priority": "ปกติ",
  "data": "{\"items\":[...]}",
  "customerName": "บริษัท ABC จำกัด",
  "customerCode": "CUST001",
  "customerAddress": "123 ถนนสุขุมวิท กรุงเทพ",
  "customerTel": "02-123-4567",
  "customerEmail": "contact@abc.com",
  "customerRemark": "ลูกค้า VIP",
  "currencyUnit": "THB",
  "currencyRate": 1.0,
  "markup": 15.5,
  "discount": 5.0,
  "goldRate": 32500.0,
  "remark": "หมายเหตุเพิ่มเติม"
}
```

**Response:**
```json
"success"
```

### 2. POST /SaleOrder/Get
ดึงข้อมูลใบสั่งขาย

**Request Body:**
```json
{
  "soNumber": "SO-2025-001"
}
```

**Response:**
```json
{
  "running": "SO2025001",
  "soNumber": "SO-2025-001",
  "createDate": "2025-01-28T10:30:00Z",
  "createBy": "admin",
  "updateDate": "2025-01-28T15:45:00Z",
  "updateBy": "admin",
  "deliveryDate": "2025-02-15T00:00:00Z",
  "status": 1,
  "statusName": "ร่าง",
  // ... ฟิลด์อื่นๆ
}
```

### 3. POST /SaleOrder/List
ค้นหารายการใบสั่งขาย

**Request Body:**
```json
{
  "search": {
    "soNumber": "SO-2025",
    "customerName": "ABC",
    "refQuotation": "QUO-2025",
    "currencyUnit": "THB",
    "status": 1,
    "createBy": "admin",
    "createDateStart": "2025-01-01T00:00:00Z",
    "createDateEnd": "2025-01-31T23:59:59Z",
    "deliveryDateStart": "2025-02-01T00:00:00Z",
    "deliveryDateEnd": "2025-02-28T23:59:59Z"
  },
  "take": 10,
  "skip": 0,
  "sort": [
    {
      "field": "createDate",
      "dir": "desc"
    }
  ]
}
```

### 4. POST /SaleOrder/ConfirmStockItems
ยืนยันการขาย stock items

**Request Body:**
```json
{
  "soNumber": "SO-2025-001",
  "stockItems": [
    {
      "id": 12345,
      "stockNumber": "ST-2025-001",
      "productNumber": "PROD-001",
      "qty": 1.0,
      "appraisalPrice": 15000.0,
      "isConfirmed": true,
      "confirmedAt": "2025-01-28T16:00:00Z"
    },
    {
      "id": 12346,
      "stockNumber": "ST-2025-002", 
      "productNumber": "PROD-002",
      "qty": 2.0,
      "appraisalPrice": 8500.0,
      "isConfirmed": true,
      "confirmedAt": "2025-01-28T16:00:00Z"
    }
  ]
}
```

**Enhanced Validation Rules:**
- **Stock Existence**: ตรวจสอบ StockNumber ว่ามีอยู่ใน TbtStockProduct
- **Available Quantity**: ตรวจสอบว่ามี Stock เพียงพอ (Qty - QtySale >= Requested Qty)
- **Sale Order Status**: อนุญาตเฉพาะสถานะ 100 และ 200
- **Duplicate Prevention**: ป้องกันการยืนยัน Stock เดียวกันซ้ำใน SO เดียวกัน
- **Data Validation**: Qty และ AppraisalPrice ต้องมากกว่า 0

**Response:**
```json
{
  "success": true,
  "message": "Successfully confirmed 2 stock items.",
  "confirmedItemsCount": 2,
  "confirmedStockNumbers": [
    "ST-2025-001",
    "ST-2025-002"
  ],
  "confirmedDate": "2025-01-28T16:00:00Z"
}
```

### 5. POST /SaleOrder/GenerateRunningNumber
สร้างเลขรันอัตโนมัติสำหรับใบสั่งขายใหม่

**Request Body:** (ไม่ต้องส่ง body)

**Response:**
```json
"SO-2025-003"
```

## Logic การทำงาน (Business Logic)

### Upsert Operation
```csharp
1. ตรวจสอบ SoNumber ว่าไม่เป็น null หรือ empty
2. ค้นหา SaleOrder ที่มี SoNumber เดียวกัน
3. หากไม่พบ:
   - สร้าง TbtSaleOrder ใหม่
   - Generate Running Number ด้วย prefix "SO"
   - Set CreateDate และ CreateBy
   - บันทึกลงฐานข้อมูล
4. หากพบแล้ว:
   - อัปเดตฟิลด์ต่างๆ ตาม request
   - Set UpdateDate และ UpdateBy
   - บันทึกการเปลี่ยนแปลง
```

### ConfirmStock Operation ⭐ **ENHANCED**
```csharp
// Phase 1: Basic Validation
1. ตรวจสอบ SoNumber ว่าไม่เป็น null หรือ empty
2. ตรวจสอบ StockItems ว่ามีข้อมูลและไม่เป็น empty
3. ค้นหาและตรวจสอบ Sale Order ว่ามีอยู่และสถานะถูกต้อง (100, 200)

// Phase 2: Comprehensive Stock Validation
4. สำหรับแต่ละ stock item:
   - ตรวจสอบ StockNumber ไม่เป็น null/empty
   - ตรวจสอบ Qty > 0 และ AppraisalPrice > 0
   - ตรวจสอบ Stock ว่ามีอยู่ใน TbtStockProduct
   - ตรวจสอบ Stock ที่เหลือเพียงพอ (Qty - QtySale >= Requested Qty)
   - ตรวจสอบไม่มีการยืนยันซ้ำใน SO เดียวกัน

// Phase 3: Database Transaction
5. เริ่ม Database Transaction
6. สำหรับแต่ละ stock item ที่ผ่านการตรวจสอบ:
   - สร้าง TbtSaleOrderProduct ใหม่พร้อมข้อมูลครบถ้วน
   - คำนวณราคาตาม currency rate, markup, discount
   - อัปเดต QtySale ใน TbtStockProduct (+= Qty)
   - บันทึก audit trail (CreateBy, CreateDate)
7. บันทึกการเปลี่ยนแปลงทั้งหมด
8. Commit Transaction
9. ส่งคืน Response พร้อมรายการ stock ที่ยืนยันแล้ว

// Error Handling
- รวบรวม validation errors ทั้งหมดก่อนส่งคืน
- Rollback transaction หากเกิดข้อผิดพลาด
- ส่งคืนข้อผิดพลาดที่ละเอียดและเป็นประโยชน์
```

### Validation Rules
- **SoNumber**: ต้องไม่เป็น null หรือ empty
- **CustomerName**: ต้องไม่เป็น null หรือ empty
- **CustomerCode**: ต้องไม่เป็น null หรือ empty
- **CurrencyUnit**: ต้องไม่เป็น null หรือ empty
- **Priority**: ต้องไม่เป็น null หรือ empty

#### ConfirmStock Validation Rules ⭐ **ENHANCED**
##### Basic Validation
- **SoNumber**: ต้องไม่เป็น null หรือ empty
- **StockItems**: ต้องมีข้อมูลอย่างน้อย 1 รายการ
- **Sale Order**: ต้องมีอยู่ในระบบและสถานะถูกต้อง (100, 200)

##### Stock Item Validation  
- **StockNumber**: ต้องไม่เป็น null หรือ empty ในแต่ละ stock item
- **Quantity**: ต้องมากกว่า 0 (`Qty > 0`)
- **Appraisal Price**: ต้องมากกว่า 0 (`AppraisalPrice > 0`)
- **Stock Existence**: Stock ต้องมีอยู่ใน `TbtStockProduct`
- **Available Quantity**: Stock ที่เหลือต้องเพียงพอ (`Qty - QtySale >= Requested Qty`)
- **Duplicate Prevention**: ไม่อนุญาตให้ยืนยัน Stock เดียวกันซ้ำใน SO เดียวกัน

##### Business Logic Validation
- **Sale Order Status**: เฉพาะสถานะ 100 (Draft) และ 200 (Confirmed) เท่านั้น
- **Currency Consistency**: ใช้ currency settings จาก Sale Order
- **Price Calculation**: คำนวณราคาตาม formula ที่กำหนด

### Default Values
- **CurrencyUnit**: "THB" หากไม่ระบุ
- **Priority**: "ปกติ" หากไม่ระบุ
- **CreateDate/UpdateDate**: DateTime.UtcNow
- **CreateBy/UpdateBy**: จาก BaseService.CurrentUsername

## การใช้งาน (Usage)

### 1. Registration in Dependency Injection
Service ได้ถูกลงทะเบียนใน `InfrastructureServiceRegistration.cs`:

```csharp
services.AddScoped<ISaleOrderService, SaleOrderService>();
```

### 2. Controller Integration
SaleOrderController ใช้งาน ISaleOrderService:

```csharp
[Route("/[controller]")]
[ApiController]
[Authorize]
public class SaleOrderController : ApiControllerBase
{
    private readonly ISaleOrderService _service;
    // ...
}
```

### 3. Error Handling
ใช้ `HandleException` สำหรับ business logic errors:

```csharp
if (string.IsNullOrEmpty(request.SoNumber))
{
    throw new HandleException("Sale Order Number is Required.");
}
```

## การทดสอบ (Testing)

### Test Cases ที่แนะนำ

1. **Upsert - Create New Sale Order**
   - ส่ง SoNumber ที่ไม่มีในระบบ
   - ตรวจสอบว่าสร้างใหม่และ generate running number

2. **Upsert - Update Existing Sale Order**
   - ส่ง SoNumber ที่มีในระบบแล้ว
   - ตรวจสอบว่าอัปเดตข้อมูลถูกต้อง

3. **Get - Valid SoNumber**
   - ส่ง SoNumber ที่มีในระบบ
   - ตรวจสอบว่าได้ข้อมูลครบถ้วน

4. **Get - Invalid SoNumber**
   - ส่ง SoNumber ที่ไม่มีในระบบ
   - ตรวจสอบว่า throw HandleException

5. **List - With Filters**
   - ทดสอบ filter ต่างๆ
   - ตรวจสอบ pagination และ sorting

6. **ConfirmStock - Valid Request**
   - ส่ง SoNumber ที่มีในระบบ
   - ส่ง StockItems ที่ต้องการยืนยัน
   - ตรวจสอบว่าบันทึกสถานะยืนยันถูกต้อง

7. **ConfirmStock - Invalid SoNumber**
   - ส่ง SoNumber ที่ไม่มีในระบบ
   - ตรวจสอบว่า throw HandleException

8. **ConfirmStock - Empty StockItems**
   - ส่ง StockItems เป็น array ว่าง
   - ตรวจสอบว่า throw HandleException

### การรัน Test
```bash
# Build project
dotnet build Jewelry.Api.sln

# Run unit tests (ถ้ามี)
dotnet test

# Run API และทดสอบผ่าน Swagger
dotnet run --project Jewelry.Api
# เปิด https://localhost:7001/swagger
```

## Error Codes

| Error Message | สาเหตุ | วิธีแก้ไข |
|---------------|--------|----------|
| "Sale Order Number is Required." | SoNumber เป็น null หรือ empty | ส่ง SoNumber ที่ถูกต้อง |
| "Sale Order Not Found." | ไม่พบ SaleOrder ที่ต้องการ | ตรวจสอบ SoNumber ว่าถูกต้อง |
| "Sale Order Number is required." | SoNumber เป็น null หรือ empty ใน ConfirmStock | ส่ง SoNumber ที่ถูกต้อง |
| "No stock items provided for confirmation." | StockItems เป็น null หรือ empty | ส่ง StockItems ที่มีข้อมูล |
| "Sale Order {SoNumber} not found." | ไม่พบ SaleOrder ใน ConfirmStock | ตรวจสอบ SoNumber ว่าถูกต้อง |
| "Error confirming stock items: {message}" | เกิดข้อผิดพลาดระหว่าง transaction | ตรวจสอบข้อมูลและลองใหม่ |

## การพัฒนาต่อ (Future Enhancements)

### 1. Status Management
เพิ่ม service สำหรับจัดการสถานะใบสั่งขาย:
- รองรับ workflow สถานะ
- ตรวจสอบการเปลี่ยนสถานะที่ถูกต้อง

### 2. Integration with Production Planning
เชื่อมต่อกับระบบ Production Planning:
- สร้างแผนการผลิตจากใบสั่งขาย
- อัปเดตสถานะผลิต

### 3. Notification System
เพิ่มการแจ้งเตือน:
- แจ้งเตือนเมื่อถึงวันส่งมอบ
- แจ้งเตือนการเปลี่ยนสถานะ

### 4. PDF Generation
เพิ่มการสร้าง PDF ใบสั่งขาย:
- Template สำหรับใบสั่งขาย
- รองรับหลายภาษา

## การบำรุงรักษา (Maintenance)

### การเพิ่มฟิลด์ใหม่
1. อัปเดต `TbtSaleOrder` entity
2. อัปเดต DTOs (Request/Response)
3. อัปเดต service logic
4. อัปเดต controller หากจำเป็น

### การเปลี่ยน Business Logic
1. อัปเดต validation rules ใน service
2. อัปเดต error messages
3. อัปเดต unit tests
4. อัปเดตเอกสาร

---
*สร้างเมื่อ: 2025-01-28*
*อัปเดตล่าสุด: 2025-10-03*
*เวอร์ชัน: 1.2.0*
*สถานะ: เสร็จสมบูรณ์ - พร้อมใช้งาน*

### การเปลี่ยนแปลงใน Version 1.2.0 ⭐ **ENHANCED**
- **Enhanced ConfirmStockItems API** พร้อม comprehensive validation
- **Automatic Stock Management**: อัปเดต QtySale ใน TbtStockProduct อัตโนมัติ
- **Advanced Validation**: ตรวจสอบ stock availability, sale order status, และ duplicate prevention
- **Improved Error Handling**: รวบรวมและส่งคืน validation errors ที่ละเอียด
- **Price Calculation Integration**: คำนวณราคาตาม currency rate, markup, และ discount
- **Enhanced Request Model**: เพิ่ม Id, IsConfirmed, ConfirmedAt fields พร้อม validation attributes
- **Transaction Safety**: ใช้ database transaction เพื่อความถูกต้องของข้อมูล
- **Frontend Integration**: ตรงกับการคำนวณราคาใน confirm-stock-modal.vue

### การเปลี่ยนแปลงใน Version 1.1.0
- เพิ่ม ConfirmStockItems API สำหรับยืนยันการขาย stock items
- เพิ่ม TbtSaleOrderProduct entity สำหรับเก็บรายการสินค้าในใบสั่งขาย
- เพิ่มฟิลด์ IsConfirmed, ConfirmedDate, ConfirmedBy สำหรับการติดตามสถานะการยืนยัน
- รองรับ workflow ใหม่: สร้าง Sale Order → ยืนยันการขาย Stock → ออก Invoice
- เพิ่ม transaction handling เพื่อความถูกต้องของข้อมูล