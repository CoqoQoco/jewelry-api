using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.StockReport.Summary
{
    public class Response
    {
        public decimal PieceQty { get; set; }
        public int PieceRowCount { get; set; }
        public decimal ReservedQty { get; set; }
        public int DesignCount { get; set; }
        public decimal AgedOver2YearsQty { get; set; }
        public decimal AgedOver2YearsPercent { get; set; }
        public int OverProducedDesignCount { get; set; }
        public int LowStockDesignCount { get; set; }
        public FilterOptionsData FilterOptions { get; set; } = new FilterOptionsData();
        public DateTime DataAsOf { get; set; }
    }

    public class FilterOptionsData
    {
        public List<LocationOption> Locations { get; set; } = new List<LocationOption>();
        public List<ProductTypeOption> ProductTypes { get; set; } = new List<ProductTypeOption>();
        public List<string> Golds { get; set; } = new List<string>();
        public List<string> GoldSizes { get; set; } = new List<string>();
    }

    public class LocationOption
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class ProductTypeOption
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
