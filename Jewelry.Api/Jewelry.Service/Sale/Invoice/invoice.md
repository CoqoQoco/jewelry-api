# Invoice Service Documentation

## Overview
The Invoice Service provides CRUD operations for managing sale invoices in the jewelry management system. It handles invoice creation from sale orders, retrieval, listing, and deletion operations.

## Features
- Create invoice from sale order items
- Get invoice details with items
- List invoices with filtering and pagination
- Delete invoice and update related sale order products
- Auto-generate invoice numbers

## Database Tables
- **TbtSaleInvoiceHeader**: Main invoice header information
- **TbtSaleOrderProduct**: Sale order products with invoice references

## API Endpoints

### 1. Create Invoice
**POST** `/Invoice/Create`

Creates a new invoice from sale order items and updates the sale order products with invoice references.

**Request Body:**
```json
{
  "soNumber": "SO-2025-001",
  "customerCode": "CUST001",
  "customerName": "ABC Company Ltd",
  "customerAddress": "123 Main Street, Bangkok",
  "customerTel": "02-123-4567",
  "customerEmail": "contact@abc.com",
  "customerRemark": "VIP Customer",
  "currencyUnit": "THB",
  "currencyRate": 1.0,
  "deliveryDate": "2025-01-30T00:00:00",
  "depositPercent": 50,
  "discount": 5.0,
  "goldRate": 2000.0,
  "markup": 10.0,
  "paymentName": "Credit Card",
  "payment": 1,
  "priority": "High",
  "refQuotation": "QUO-2025-001",
  "remark": "Rush order",
  "status": 1,
  "statusName": "Active",
  "data": "{\"additionalInfo\": \"custom data\"}",
  "items": [
    {
      "stockNumber": "STK-001",
      "stockNumberOrigin": "STK-001-ORIG",
      "id": 1,
      "priceOrigin": 50000.0,
      "currencyUnit": "THB",
      "currencyRate": 1.0,
      "markup": 10.0,
      "discount": 5.0,
      "goldRate": 2000.0,
      "remark": "Premium item",
      "netPrice": "47500.0",
      "priceDiscount": 47500.0,
      "priceAfterCurrencyRate": 47500.0,
      "qty": 1.0
    }
  ]
}
```

**Response:**
```json
{
  "invoiceNumber": "INV-2025-001",
  "message": "Invoice created successfully"
}
```

**Validation:**
- Sale Order Number is required
- Customer Code is required
- Customer Name is required
- Invoice items are required
- Sale order products must exist for the specified items

### 2. Get Invoice
**POST** `/Invoice/Get`

Retrieves invoice details including all items.

**Request Body:**
```json
{
  "invoiceNumber": "INV-2025-001"
}
```

**Response:**
```json
{
  "invoiceNumber": "INV-2025-001",
  "createBy": "admin",
  "createDate": "2025-01-15T10:30:00",
  "updateBy": null,
  "updateDate": null,
  "currencyRate": 1.0,
  "currencyUnit": "THB",
  "customerAddress": "123 Main Street, Bangkok",
  "customerCode": "CUST001",
  "customerEmail": "contact@abc.com",
  "customerName": "ABC Company Ltd",
  "customerRemark": "VIP Customer",
  "customerTel": "02-123-4567",
  "data": "{\"additionalInfo\": \"custom data\"}",
  "deliveryDate": "2025-01-30T00:00:00",
  "depositPercent": 50,
  "discount": 5.0,
  "goldRate": 2000.0,
  "markup": 10.0,
  "paymentName": "Credit Card",
  "payment": 1,
  "priority": "High",
  "refQuotation": "QUO-2025-001",
  "remark": "Rush order",
  "status": 1,
  "statusName": "Active",
  "items": [
    {
      "running": "INV-2025-001-1",
      "soNumber": "SO-2025-001",
      "createDate": "2025-01-15T10:30:00",
      "createBy": "admin",
      "updateDate": "2025-01-15T10:30:00",
      "updateBy": "admin",
      "stockNumber": "STK-001",
      "stockNumberOrigin": "STK-001-ORIG",
      "id": 1,
      "priceOrigin": 50000.0,
      "currencyUnit": "THB",
      "currencyRate": 1.0,
      "markup": 10.0,
      "discount": 5.0,
      "goldRate": 2000.0,
      "remark": "Premium item",
      "netPrice": "47500.0",
      "priceDiscount": 47500.0,
      "priceAfterCurrencyRate": 47500.0,
      "qty": 1.0,
      "invoice": "INV-2025-001",
      "invoiceItem": "INV-2025-001-1"
    }
  ]
}
```

### 3. List Invoices
**POST** `/Invoice/List`

Retrieves a paginated list of invoices with filtering options.

**Request Body:**
```json
{
  "invoiceNumber": "INV-2025",
  "customerName": "ABC",
  "customerCode": "CUST001",
  "soNumber": "SO-2025",
  "status": 1,
  "createDateFrom": "2025-01-01T00:00:00",
  "createDateTo": "2025-01-31T23:59:59",
  "deliveryDateFrom": "2025-01-15T00:00:00",
  "deliveryDateTo": "2025-02-15T23:59:59",
  "createBy": "admin",
  "skip": 0,
  "take": 10,
  "orderBy": "createDate",
  "orderDirection": "DESC"
}
```

**Response:**
```json
{
  "data": [
    {
      "invoiceNumber": "INV-2025-001",
      "createBy": "admin",
      "createDate": "2025-01-15T10:30:00",
      "updateBy": null,
      "updateDate": null,
      "currencyRate": 1.0,
      "currencyUnit": "THB",
      "customerAddress": "123 Main Street, Bangkok",
      "customerCode": "CUST001",
      "customerEmail": "contact@abc.com",
      "customerName": "ABC Company Ltd",
      "customerRemark": "VIP Customer",
      "customerTel": "02-123-4567",
      "deliveryDate": "2025-01-30T00:00:00",
      "depositPercent": 50,
      "discount": 5.0,
      "goldRate": 2000.0,
      "markup": 10.0,
      "paymentName": "Credit Card",
      "payment": 1,
      "priority": "High",
      "refQuotation": "QUO-2025-001",
      "remark": "Rush order",
      "status": 1,
      "statusName": "Active",
      "itemCount": 3,
      "totalAmount": 142500.0
    }
  ],
  "total": 1,
  "skip": 0,
  "take": 10
}
```

**Filtering Options:**
- `invoiceNumber`: Partial match on invoice number
- `customerName`: Partial match on customer name
- `customerCode`: Partial match on customer code
- `soNumber`: Partial match on sale order number
- `status`: Exact match on status
- `createDateFrom/createDateTo`: Date range for creation date
- `deliveryDateFrom/deliveryDateTo`: Date range for delivery date
- `createBy`: Partial match on creator

**Sorting Options:**
- `orderBy`: invoicenumber, customername, createdate, deliverydate, totalamount
- `orderDirection`: ASC or DESC (default: DESC)

### 4. Delete Invoice
**POST** `/Invoice/Delete`

Deletes an invoice and removes invoice references from sale order products.

**Request Body:**
```json
{
  "invoiceNumber": "INV-2025-001"
}
```

**Response:**
```json
{
  "message": "Invoice INV-2025-001 deleted successfully"
}
```

**Actions Performed:**
- Removes invoice header from `TbtSaleInvoiceHeader`
- Updates `TbtSaleOrderProduct` records to remove invoice and invoiceItem references
- Sets updateBy and updateDate for modified sale order products

### 5. Generate Invoice Number
**GET** `/Invoice/GenerateInvoiceNumber`

Generates a new invoice number using the running number service.

**Response:**
```json
{
  "invoiceNumber": "INV-2025-002"
}
```

## Business Logic

### Invoice Creation Process
1. Validate required fields (SoNumber, CustomerCode, CustomerName, Items)
2. Generate new invoice number using running number service with "INV" prefix
3. Create invoice header record in `TbtSaleInvoiceHeader`
4. Update each sale order product with invoice references:
   - Set `Invoice` field to invoice number
   - Set `InvoiceItem` field to "{invoiceNumber}-{itemId}"
   - Update `UpdateBy` and `UpdateDate` fields
5. Validate that all specified sale order products exist
6. Save all changes to database

### Invoice Deletion Process
1. Validate invoice exists
2. Find all sale order products linked to the invoice
3. Clear invoice references (set Invoice and InvoiceItem to null)
4. Update modification fields (UpdateBy, UpdateDate)
5. Delete invoice header record
6. Save all changes to database

## Error Handling

### Common Error Responses
- **400 Bad Request**: Validation errors, missing required fields
- **404 Not Found**: Invoice not found, sale order products not found
- **500 Internal Server Error**: Database errors, unexpected exceptions

### Validation Rules
- Sale Order Number cannot be empty
- Customer Code cannot be empty  
- Customer Name cannot be empty
- Items list cannot be empty
- All referenced sale order products must exist in the database

## Security
- All endpoints require authorization (`[Authorize]` attribute)
- User context is automatically injected for audit fields (CreateBy, UpdateBy)
- Input validation prevents SQL injection and invalid data

## Dependencies
- `JewelryContext`: Entity Framework database context
- `IRunningNumber`: Service for generating invoice numbers
- `IHostEnvironment`: Environment information
- `IHttpContextAccessor`: For accessing user context

## Usage Examples

### Creating an Invoice from Sale Order
```csharp
var request = new jewelry.Model.Sale.Invoice.Create.Request
{
    SoNumber = "SO-2025-001",
    CustomerCode = "CUST001",
    CustomerName = "ABC Company Ltd",
    // ... other properties
    Items = new List<InvoiceItem>
    {
        new InvoiceItem
        {
            StockNumber = "STK-001",
            Id = 1,
            PriceOrigin = 50000,
            Qty = 1
            // ... other properties
        }
    }
};

var invoiceNumber = await invoiceService.Create(request);
```

### Retrieving Invoice List with Filtering
```csharp
var request = new jewelry.Model.Sale.Invoice.List.Request
{
    CustomerName = "ABC",
    CreateDateFrom = DateTime.Today.AddDays(-30),
    CreateDateTo = DateTime.Today,
    Skip = 0,
    Take = 20,
    OrderBy = "createDate",
    OrderDirection = "DESC"
};

var invoices = invoiceService.List(request)
    .Skip(request.Skip)
    .Take(request.Take)
    .ToList();
```

## Database Schema Notes

### TbtSaleInvoiceHeader Fields
- `Running`: Primary key, invoice number
- `CreateBy/CreateDate`: Audit fields for creation
- `UpdateBy/UpdateDate`: Audit fields for modification
- Customer fields: Code, Name, Address, Tel, Email, Remark
- Financial fields: CurrencyUnit, CurrencyRate, Discount, Markup, GoldRate
- Business fields: Status, Priority, Payment, DeliveryDate, DepositPercent
- Reference fields: RefQuotation, Remark, Data (JSON storage)

### TbtSaleOrderProduct Fields
- `Invoice`: Reference to invoice number
- `InvoiceItem`: Unique identifier for invoice item
- All other fields retain sale order product information
- Price calculations are preserved from original sale order

## Performance Considerations
- Invoice list queries use efficient filtering with indexes on commonly searched fields
- Item count and total amount are calculated using subqueries for better performance
- Pagination is implemented to handle large result sets
- Consider adding database indexes on frequently filtered fields (CustomerCode, CreateDate, Status)

## Future Enhancements
- Add invoice status workflow (Draft, Confirmed, Paid, Cancelled)
- Implement invoice numbering per branch/location
- Add support for partial invoicing (invoicing subset of sale order items)
- Implement invoice PDF generation integration
- Add invoice history tracking for audit purposes
- Support for invoice modifications/amendments