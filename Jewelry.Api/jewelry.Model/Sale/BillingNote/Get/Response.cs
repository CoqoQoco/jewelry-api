using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.BillingNote.Get
{
    public class Response
    {
        public string Running { get; set; } = null!;
        public DateTime DocumentDate { get; set; }

        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string? CustomerAddress { get; set; }
        public string? CustomerTel { get; set; }

        public int BillCount { get; set; }

        public int GoldResizeQty { get; set; }
        public decimal GoldResizeAmount { get; set; }
        public int SilverResizeQty { get; set; }
        public decimal SilverResizeAmount { get; set; }

        public decimal GoldResizePerUnit { get; set; }
        public decimal SilverResizePerUnit { get; set; }
        public bool HasSupport { get; set; }
        public decimal SupportPercent { get; set; }
        public decimal SupportAmount { get; set; }

        public decimal SubTotal { get; set; }
        public decimal VatPercent { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrandTotal { get; set; }

        public string? Remark { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public string CreateBy { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }

        // payment summary (header level)
        public decimal TotalBilled { get; set; }
        public decimal TotalReceived { get; set; }
        public decimal TotalOutstanding { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";

        public List<Item> Items { get; set; } = new List<Item>();
        public List<Product> Products { get; set; } = new List<Product>();
    }

    public class Item
    {
        public int Seq { get; set; }
        public string InvoiceRunning { get; set; } = null!;
        public DateTime? InvoiceDate { get; set; }
        public decimal AmountBeforeVat { get; set; }
        public string? Remark { get; set; }

        public decimal InvoiceGrandTotal { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
    }

    public class Product
    {
        public string InvoiceRunning { get; set; } = null!;
        public string? ProductNumber { get; set; }
        public string? ProductType { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductionType { get; set; }
        public decimal Qty { get; set; }
        public decimal Amount { get; set; }
        public string? Remark { get; set; }
    }
}
