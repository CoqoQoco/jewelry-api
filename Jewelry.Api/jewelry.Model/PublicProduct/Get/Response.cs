using System;
using System.Collections.Generic;

namespace jewelry.Model.PublicProduct.Get
{
    public class Response
    {
        public string StockNumber { get; set; }
        public string? StockNumberOrigin { get; set; }
        public string? ProductNumber { get; set; }

        public string ProductNameTh { get; set; }
        public string ProductNameEn { get; set; }
        public string? ProductTypeName { get; set; }

        public string? ImagePath { get; set; }

        public string? MetalKarat { get; set; }
        public string? MetalColorCode { get; set; }
        public decimal? MetalWeight { get; set; }
        public string? MetalWeightUnit { get; set; }

        public string? Size { get; set; }
        public string? EarringStemSize { get; set; }

        public List<Gem> Gems { get; set; } = new List<Gem>();

        public decimal? DisplayPrice { get; set; }
        public string Currency { get; set; } = "THB";

        public bool? IsAvailable { get; set; }
    }

    public class Gem
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string? Code { get; set; }
        public decimal Qty { get; set; }
        public decimal Weight { get; set; }
        public string? WeightUnit { get; set; }
        public string? Size { get; set; }
        public string? Origin { get; set; }
    }
}
