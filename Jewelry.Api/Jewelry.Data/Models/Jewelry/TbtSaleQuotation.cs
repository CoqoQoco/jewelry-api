using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleQuotation
{
    public string Running { get; set; } = null!;

    public string Number { get; set; } = null!;

    public string? CustomerName { get; set; }

    public string? CustomerEmail { get; set; }

    public string? CustomerPhone { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string? Data { get; set; }

    public string? Currency { get; set; }

    public decimal? CurrencyRate { get; set; }

    public decimal? MarkUp { get; set; }

    public decimal? Discount { get; set; }

    public DateTime? Date { get; set; }

    public string? Remark { get; set; }

    public string? CustomerAddress { get; set; }
}
