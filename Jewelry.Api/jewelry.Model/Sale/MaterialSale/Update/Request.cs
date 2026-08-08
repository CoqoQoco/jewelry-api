using System;
using System.Collections.Generic;
using Item = jewelry.Model.Sale.MaterialSale.Create.Item;

namespace jewelry.Model.Sale.MaterialSale.Update
{
    public class Request
    {
        public string Running { get; set; } = null!;

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
}
