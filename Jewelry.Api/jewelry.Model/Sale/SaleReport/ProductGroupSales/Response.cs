using System.Collections.Generic;

namespace jewelry.Model.Sale.SaleReport.ProductGroupSales
{
    public class Response
    {
        public List<GroupData> Groups { get; set; } = new List<GroupData>();
        public decimal TotalPieceCount { get; set; }
        public decimal TotalAmountThb { get; set; }
        public FilterOptionsData FilterOptions { get; set; } = new FilterOptionsData();
    }

    public class GroupData
    {
        public string Key { get; set; } = null!;
        public string? Label { get; set; }
        public decimal PieceCount { get; set; }
        public decimal AmountThb { get; set; }
    }

    public class FilterOptionsData
    {
        public List<ProductTypeOption> ProductTypes { get; set; } = new List<ProductTypeOption>();
        public List<string> Golds { get; set; } = new List<string>();
        public List<string> GoldSizes { get; set; } = new List<string>();
    }

    public class ProductTypeOption
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
