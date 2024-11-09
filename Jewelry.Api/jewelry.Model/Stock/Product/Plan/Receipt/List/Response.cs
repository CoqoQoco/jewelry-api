﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Product.Plan.Receipt.List
{
    public class Response
    {
        public int Id { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public string WoText { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public string Mold { get; set; }

        public string? ProductRunning { get; set; }
        public string? ProductName { get; set; }
        public string? ProductType { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductNumber { get; set; }
        public string? ProductDetail { get; set; }
        public int ProductQty { get; set; }
        public string? ProductQtyUnit { get; set; }

        public string? CustomerNumber { get; set; }
        public string? CustomerName { get; set; }

        public string? CustomerType { get; set; }
        public string? CustomerTypeName { get; set; }

        public string? Remark { get; set; }

        public string? Gold { get; set; }
        public string? GoldSize { get; set; }

        public string ReceiptNumber { get; set; }
        public DateTime ReceiptDate { get; set; }
    }
}
