# StockGemService Business Logic Documentation

## Overview
The `StockGemService` manages gem inventory operations including stock search, price management, and comprehensive dashboard reporting for the jewelry management system.

## Core Business Operations

### 1. Stock Search & Filtering

#### SearchGem (Simple)
- **Purpose**: Basic gem search with name-based filtering
- **Logic**: 
  - Constructs display name as: `{Code}-{GroupName}-{Shape}-{Size}-{Grade}`
  - Supports text search across this combined name
  - Supports direct ID lookup
- **Use Case**: Quick gem lookup for basic operations

#### SearchGemData (Advanced)
- **Purpose**: Comprehensive gem search with multiple filter criteria
- **Filtering Options**:
  - **Code**: Partial match (case-insensitive)
  - **GroupName**: Array-based inclusion filter
  - **Size**: Array-based inclusion filter  
  - **Shape**: Array-based inclusion filter
  - **Grade**: Array-based inclusion filter
  - **TypeCheck**: Special inventory status filters
    - `qty-remain`: Items with available quantity > 0
    - `qty-process-remain`: Items with on-process quantity > 0
    - `qty-weight-remain`: Items with weight quantity > 0
    - `qty-weight-process-remain`: Items with weight on-process quantity > 0

#### GroupGemData (Options)
- **Purpose**: Generate filter options for UI dropdowns
- **Supported Types**:
  - `GROUPGEM`: Distinct group names
  - `GRADE`: Distinct grades
  - `SHAPE`: Distinct shapes
  - `SIZE`: Distinct sizes
- **Logic**: Groups by requested type and returns unique values

### 2. Price Management

#### Price Update Operation
- **Authentication**: Optional password validation via `TbmAccount` (GI-GEM user)
- **Business Rules**:
  - Must find existing gem by Code and ID
  - Creates audit trail in `TbtStockGemTransectionPrice`
  - Updates gem record with new pricing
  - Uses database transaction for data consistency
- **Audit Fields**:
  - Previous/New price comparison
  - Previous/New price unit comparison
  - Unit and unit code changes
  - Timestamp and user tracking

#### PriceHistory
- **Purpose**: Retrieve price change history for a gem
- **Logic**: Returns all price transactions for a gem code, ordered by most recent first

### 3. Dashboard & Reporting

#### Main Dashboard (`GetStockGemDashboard`)
**Enhanced with DateTimeOffset Support & Last Activities**

Provides comprehensive stock overview with:
- **Stock Summary**: Total quantities, values, and availability
- **Category Breakdown**: Group-wise analysis
- **Transaction Trends**: Daily movement patterns
- **Top Movements**: Most active gems
- **Price Alerts**: Significant price changes (>5%)
- **Last Activities**: 10 most recent transactions with full details

**Recent Updates (2024)**:
- **DateTimeOffset Implementation**: Uses `StartOfDayUtc()` and `EndOfDayUtc()` helpers for accurate timezone handling
- **Last Activities**: New feature showing recent transaction history with:
  - Transaction type mapping to master descriptions
  - Gem details (code, group, shape, grade, size)
  - Quantity and status information
  - Job/PO references
  - Running numbers and timestamps

#### Time-Based Reports

##### Today Report (`GetTodayReport`)
**Enhanced with DateTimeOffset Support**
- **Date Logic**: Uses `DateTimeOffset.UtcNow.Date` for accurate daily boundaries
- **Summary**: Daily transaction counts and quantities
- **Transactions**: Recent transactions with details
- **Price Changes**: Today's price updates
- **New Stocks**: Recently added gems
- **Low Stock Alerts**: Items below threshold (≤10 units)

##### Weekly Report (`GetWeeklyReport`)
**Enhanced with DateTimeOffset Support**
- **Period**: Sunday to Saturday based on current UTC date
- **Date Logic**: Uses `DateTimeOffset.UtcNow` for week boundary calculations
- **Metrics**: Week-over-week comparisons
- **Analysis**: Daily breakdowns within the week
- **Performance**: Top performing gems

##### Monthly Report (`GetMonthlyReport`)
**Enhanced with DateTimeOffset Support**
- **Period**: Calendar month based on UTC dates
- **Date Logic**: Uses `DateTimeOffset.UtcNow` for month boundary calculations
- **Analysis**: Monthly trends and patterns
- **Comparisons**: Week-by-week breakdown
- **Insights**: Inventory analysis, price trends, supplier performance

## Recent Enhancements (2024)

### DateTimeOffset Implementation
**Problem Solved**: Previous DateTime usage caused timezone inconsistencies in global deployments.

**Solution**:
- **Request Model**: Changed `DashboardRequest` from `DateTime` to `DateTimeOffset`
- **Service Layer**: All dashboard methods now use `DateTimeOffset.UtcNow`
- **Date Filtering**: Implements `StartOfDayUtc()` and `EndOfDayUtc()` helpers
- **Database Queries**: Converts DateTimeOffset to DateTime for Entity Framework compatibility

**Business Impact**:
- Consistent date filtering across timezones
- Accurate daily/weekly/monthly boundary calculations
- Improved data integrity for international operations

### Last Activities Feature
**Business Requirement**: Users needed visibility into recent stock movements.

**Implementation**:
- **GetLastActivities()**: New method returning 10 most recent transactions
- **Transaction Type Mapping**: Uses master transaction types with Thai descriptions
- **Comprehensive Details**: Includes gem attributes, quantities, status, and references
- **Performance**: Limited to 10 records for UI responsiveness

**Data Included**:
- Gem identification (code, group, shape, grade, size)
- Transaction type with descriptive names
- Quantities and status information
- Job/PO references for traceability
- Running numbers and timestamps

## Key Business Rules

### Stock Management
1. **Dual Quantity Tracking**: Both piece count (`Quantity`) and weight (`QuantityWeight`)
2. **Process Allocation**: Separate tracking for items in production (`QuantityOnProcess`, `QuantityWeightOnProcess`)
3. **Availability Calculation**: Available = Total - OnProcess

### Price Management
1. **Audit Trail**: All price changes are logged with before/after values
2. **Unit Flexibility**: Support for both quantity-based and weight-based pricing
3. **Authentication**: Optional password protection for price updates

### Dashboard Thresholds
- **Low Stock Alert**: ≤10 units
- **Zero Stock Alert**: Exactly 0 units
- **Critical Stock Alert**: ≤5 units
- **Price Change Alert**: >5% change

### Transaction Types
Master transaction types with business logic:

1. **Type 1**: รับเข้าคลัง [พลอยใหม่] (Receive - New Gems)
   - **Effect**: Increases available stock
   - **Use Case**: New gem acquisitions

2. **Type 2**: รับเข้าคลัง [พลอยนอกสต๊อก] (Receive - Out-of-Stock Gems)
   - **Effect**: Increases available stock
   - **Use Case**: Restocking previously depleted items

3. **Type 3**: รับเข้าคลัง [พลอยคืน] (Receive - Returned Gems)
   - **Effect**: Increases available stock
   - **Use Case**: Returns from production or customers

4. **Type 4**: จ่ายออกคลัง (Issue from Warehouse)
   - **Effect**: Decreases available stock
   - **Use Case**: Formal inventory distribution

5. **Type 5**: ยืมออกคลัง (Borrow from Warehouse)
   - **Effect**: Decreases available stock (temporary)
   - **Use Case**: Temporary allocation to departments

6. **Type 6**: คืนเข้าคลัง (Return to Warehouse)
   - **Effect**: Increases available stock
   - **Use Case**: Return of borrowed items

7. **Type 7**: เบิกออกคลัง (Withdraw from Warehouse)
   - **Effect**: Decreases available stock
   - **Use Case**: Production consumption

**Net Calculation Logic**:
- **IN Types**: 1, 2, 3, 6 (increase stock)
- **OUT Types**: 4, 5, 7 (decrease stock)
- **Net Movement**: IN - OUT for trend analysis

## Data Relationships

### Primary Entities
- **TbtStockGem**: Main gem inventory table
- **TbtStockGemTransection**: Stock movement history
- **TbtStockGemTransectionPrice**: Price change audit log
- **TbmAccount**: User authentication for price updates

### Key Fields
- **Code**: Gem identifier (unique per gem type)
- **GroupName**: Gem category (Ruby, Sapphire, etc.)
- **Shape**: Physical shape (Round, Oval, etc.)
- **Size**: Dimensions or size grade
- **Grade**: Quality grade
- **Unit/UnitCode**: Measurement unit (Q=Quantity, W=Weight)

## Performance Considerations

### Query Optimization
- Uses `AsNoTracking()` for read-only operations
- Efficient grouping and aggregation for dashboard data
- Indexed filtering on common search fields

### Transaction Management
- Uses `TransactionScope` for price updates
- Ensures data consistency across related tables
- Proper exception handling for rollback scenarios

## Error Handling

### Common Exceptions
- **NotFound**: Gem not found for price updates
- **PermissionFail**: Invalid password for price updates
- **HandleException**: Custom business logic exceptions

### Validation Rules
- Price updates require valid gem Code and ID
- Optional password validation for sensitive operations
- Transaction data integrity checks