using System.Collections.Generic;

namespace jewelry.Model.Receipt.Outsource.Confirm
{
    public class Request
    {
        public string Vendor { get; set; }
        public string? PoNumber { get; set; }

        public List<OutsourceStock> Stocks { get; set; }
    }

    public class OutsourceStock
    {
        public string ProductNumber { get; set; }
        public string ProductNameTH { get; set; }
        public string ProductNameEN { get; set; }

        public string? MoldDesign { get; set; }

        public string ProductType { get; set; }
        public string ProductionType { get; set; }
        public string? ProductionTypeSize { get; set; }

        public decimal Qty { get; set; }
        public decimal Price { get; set; }

        public string? Size { get; set; }
        public string? StudEarring { get; set; }
        public string? Location { get; set; }

        public string? ImageName { get; set; }
        public string? ImagePath { get; set; }

        public string? Remark { get; set; }

        public List<OutsourceMaterial> Materials { get; set; } = new List<OutsourceMaterial>();
    }

    public class OutsourceMaterial
    {
        public string Type { get; set; }
        public string? TypeName { get; set; }
        public string? TypeCode { get; set; }
        public string? TypeBarcode { get; set; }

        public decimal? Qty { get; set; }
        public string? QtyUnit { get; set; }
        public decimal? Weight { get; set; }
        public string? WeightUnit { get; set; }

        public string? Size { get; set; }
        public string? Region { get; set; }
        public decimal? Price { get; set; }
    }
}
