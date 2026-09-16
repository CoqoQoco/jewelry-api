using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.SaleOrderDeposit.List
{
    public class Response
    {
        public string SoNumber { get; set; } = null!;
        public string? CurrencyUnit { get; set; }

        public decimal TotalReceived { get; set; }
        public decimal TotalApplied { get; set; }
        public decimal Balance { get; set; }

        public List<DepositItem> Deposits { get; set; } = new List<DepositItem>();
    }

    public class DepositItem
    {
        public string Running { get; set; } = null!;
        public DateTime DepositDate { get; set; }

        public decimal Amount { get; set; }
        public string? CurrencyUnit { get; set; }
        public decimal? CurrencyRate { get; set; }

        public int? Payment { get; set; }
        public string? PaymentName { get; set; }
        public string? BankCode { get; set; }
        public string? BankBranch { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? ImagePath { get; set; }
        public string? Remark { get; set; }

        public bool IsDelete { get; set; }
        public string? DeleteReason { get; set; }

        public decimal AppliedAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        public string CreateBy { get; set; } = null!;
        public DateTime CreateDate { get; set; }

        public List<ApplyItem> Applies { get; set; } = new List<ApplyItem>();
    }

    public class ApplyItem
    {
        public string InvoiceRunning { get; set; } = null!;
        public decimal Amount { get; set; }
        public bool IsDelete { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
