using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.MaterialSale.Get
{
    public class Response
    {
        public string Running { get; set; } = null!;
        public string DocumentNo { get; set; } = null!;
        public DateTime DocumentDate { get; set; }

        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerTel { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerTaxId { get; set; }

        public decimal SubTotal { get; set; }
        public decimal VatPercent { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrandTotal { get; set; }

        public string? Remark { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public DateTime? ConfirmDate { get; set; }
        public string? ConfirmBy { get; set; }
        public DateTime? CancelDate { get; set; }
        public string? CancelBy { get; set; }
        public string? CancelReason { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = null!;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public List<Item> Items { get; set; } = new List<Item>();
    }

    public class Item
    {
        public long Id { get; set; }
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
        public decimal PriceExclVat { get; set; }
        public decimal Amount { get; set; }
        public decimal? RefStockPrice { get; set; }
        public string? Remark { get; set; }
    }
}
