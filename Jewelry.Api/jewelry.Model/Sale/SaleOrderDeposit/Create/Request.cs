using Microsoft.AspNetCore.Http;
using System;

namespace jewelry.Model.Sale.SaleOrderDeposit.Create
{
    public class Request
    {
        public string SoNumber { get; set; } = null!;
        public DateTimeOffset DepositDate { get; set; }
        public decimal Amount { get; set; }

        public int Payment { get; set; }
        public string PaymentName { get; set; } = null!;

        public string? BankCode { get; set; }
        public string? BankBranch { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Remark { get; set; }

        public IFormFile? ReceiptImage { get; set; }
    }
}
