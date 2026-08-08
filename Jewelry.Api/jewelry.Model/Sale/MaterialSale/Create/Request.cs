using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.MaterialSale.Create
{
    public class Request
    {
        public string? DocumentNo { get; set; }
        public DateTimeOffset DocumentDate { get; set; }

        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerTel { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerTaxId { get; set; }

        public decimal VatPercent { get; set; } = 7m;
        public string? Remark { get; set; }

        public List<Item> Items { get; set; } = new List<Item>();
    }

    public class Item
    {
        public int ItemNo { get; set; }
        public string GemCode { get; set; } = null!;
        public string? GemName { get; set; }
        public string? GemGroup { get; set; }
        public string? GemShape { get; set; }
        public string? GemSize { get; set; }
        public string? GemGrade { get; set; }
        public string? Description { get; set; }
        public decimal QtyPiece { get; set; }
        public decimal QtyWeight { get; set; }
        public decimal PriceInclVat { get; set; }
        public decimal? RefStockPrice { get; set; }
        public string? Remark { get; set; }
    }
}
