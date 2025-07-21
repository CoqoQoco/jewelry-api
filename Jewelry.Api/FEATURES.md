# Jewelry API Features Documentation

## Stock Gem Transaction Analysis Enhancement

### Overview
Enhanced the Stock Gem Service to provide transaction analysis categorized by transaction type with specialized handling for Type 7 (เบิกออกคลัง) transactions.

### Key Features Implemented

#### 1. Transaction Type Categorization
- **Method**: `GetTransactionSummariesByType(DashboardRequest request)`
- **Purpose**: Categorizes gem transactions by transaction type instead of gem characteristics
- **Transaction Types**:
  - Type 1: รับเข้าคลัง [พลอยใหม่] (New Gem Inbound)
  - Type 2: รับเข้าคลัง [พลอยนอกสต๊อก] (Non-Stock Gem Inbound)
  - Type 3: รับเข้าคลัง [พลอยคืน] (Returned Gem Inbound)
  - Type 4: จ่ายออกคลัง (Outbound)
  - Type 5: ยืมออกคลัง (Borrow Outbound)
  - Type 6: คืนเข้าคลัง (Return Inbound)
  - Type 7: เบิกออกคลัง (Production Usage Outbound)

#### 2. Type 7 Production Categorization
- **Enhanced Feature**: Type 7 transactions are specially categorized by production type
- **Categories Supported**:
  - **Gold (ทอง)**: Identified by keywords: "gold", "ทอง", "au"
  - **Silver (เงิน)**: Identified by keywords: "silver", "เงิน", "ag"
  - **Diamond (เพชร)**: Identified by keywords: "diamond", "เพชร"
  - **Ruby (ทับทิม)**: Identified by keywords: "ruby", "ทับทิม"
  - **Sapphire (ไพลิน)**: Identified by keywords: "sapphire", "ไพลิน"
  - **Emerald (มรกต)**: Identified by keywords: "emerald", "มรกต"
  - **Other (อื่นๆ)**: Default category for unmatched items

#### 3. Date Range Filtering
- **Requirement**: Both `StartDate` and `EndDate` must be provided
- **Validation**: Method throws `ArgumentException` if dates are missing
- **Usage**: Supports flexible date range analysis for dashboard reporting

### API Response Structure

#### TransactionTypeCategorySummary
```csharp
{
    "Type": 7,
    "TypeName": "เบิกออกคลัง",
    "TotalTransactions": 150,
    "TotalQuantity": 250.50,
    "TotalWeight": 125.75,
    "GemDetails": [
        {
            "Code": "GEM001",
            "GroupName": "Gold Jewelry",
            "TransactionCount": 25,
            "TotalQuantity": 50.25,
            "TotalWeight": 25.12,
            "CurrentQuantity": 100.00,
            "CurrentWeight": 50.00,
            "LastTransactionDate": "2024-01-15T10:30:00Z",
            "ProductionType": "Gold",
            "ProductionTypeName": "ทอง",
            "ProductionDetails": [
                {
                    "Wo": "WO-2024-001",
                    "WoNumber": 1,
                    "Mold": "MOLD-001",
                    "ProductNumber": "PROD-001",
                    "QuantityUsed": 10.5,
                    "WeightUsed": 5.25,
                    "JewelryType": "แหวน"
                }
            ]
        }
    ]
}
```

### Implementation Details

#### Helper Methods
1. **GetProductionTypeCategory()**: Analyzes gem group names to determine production category
2. **GetProductionTypeFromDetail()**: Extracts jewelry type from product details
3. **GetProductionTypeSizeFromDetail()**: Determines jewelry size classification

#### Type 7 Special Processing
- Groups transactions by production type first (Gold/Silver/etc.)
- Then groups by individual gem codes within each production type
- Provides hierarchical categorization for better analysis
- Maintains production details for traceability

#### Other Transaction Types
- Standard grouping by gem code and group name
- No production categorization (not applicable)
- Focused on transaction volume and gem movement analysis

### Usage Examples

#### API Call
```http
POST /api/stock/gem/transaction-summaries-by-type
Content-Type: application/json

{
    "StartDate": "2024-01-01T00:00:00Z",
    "EndDate": "2024-01-31T23:59:59Z",
    "GroupName": null,
    "Shape": null,
    "Grade": null
}
```

#### Response Focus Areas
1. **Dashboard Analytics**: Transaction volume by type
2. **Production Analysis**: Type 7 categorized by Gold/Silver usage
3. **Inventory Management**: Current stock levels per transaction type
4. **Traceability**: Production plan linkage for Type 7 transactions

### Benefits
1. **Clear Categorization**: Transactions grouped by actual business operation type
2. **Production Insights**: Type 7 specifically categorized by metal/gem type
3. **Flexible Reporting**: Date-range based analysis
4. **Enhanced Visibility**: Gold vs Silver vs Other production usage tracking
5. **Dashboard Integration**: Structured data for monthly dashboard reporting

### Technical Notes
- **Performance**: Uses efficient grouping and LINQ operations
- **Scalability**: Supports large transaction volumes with proper indexing
- **Extensibility**: Easy to add new production categories
- **Localization**: Supports both Thai and English categorization names