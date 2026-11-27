using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Product.List
{
    public class Request : DataSourceRequest
    {
      public Search Search { get; set;  }
    }

    public class Search
    {
        public string[]? ReceiptType { get; set; }

        public string? StockNumber { get; set; }
        public string? StockNumberOrigin { get; set; }
        public string? Mold { get; set; }

        public string? ProductNumber { get; set; }
        public List<string>? ProductNumbers { get; set; }
        public string? ProductNameEn { get; set; }
        public string? ProductNameTh { get; set; }

        public string? WoText { get; set; }
        public string[]? ProductType { get; set; }
        public string[]? Gold { get; set; }
        public string[]? GoldSize { get; set; }
        public string[]? LocationCodes { get; set; }
        public string? Size { get; set; }

        public bool? HasCostDetail { get; set; }

        public string? ReceiptNumber { get; set; }
        public string? Keyword { get; set; }

        public string? PieceStatus { get; set; }

        public bool? IncludeLastMovement { get; set; }

        public string[]? LastMoveFromLocations { get; set; }
        public string? LastMoveBy { get; set; }
        public DateTimeOffset? LastMoveDateFrom { get; set; }
        public DateTimeOffset? LastMoveDateTo { get; set; }

        public string[]? Materials { get; set; }
        public bool? MaterialMatchAll { get; set; }
        public decimal? PriceMin { get; set; }
        public decimal? PriceMax { get; set; }

        // null/"" = ทั้งหมด, NO_NORMAL = ยังไม่เคยตีราคาปกติ, HAS_QUICK = มีราคาตีด่วน
        public string? PriceStatus { get; set; }

        // กรองเป็นคู่ (stockNumber, productCode) exact ไม่เกิน 500 — ใช้กับการอ่านแท็ก
        public List<PieceKey>? Pieces { get; set; }

        // null/"" = ทั้งหมด, bound = ผูกครบทุกชิ้น, unbound = ยังไม่ผูกเลย, partial = ผูกบางชิ้น
        public string? TagStatus { get; set; }
    }

    public class PieceKey
    {
        public string? StockNumber { get; set; }
        public string? ProductCode { get; set; }
    }
}
