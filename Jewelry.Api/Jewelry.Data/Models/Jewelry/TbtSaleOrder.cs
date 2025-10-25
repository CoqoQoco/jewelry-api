using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleOrder
{
    public string Running { get; set; } = null!;

    public string SoNumber { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public int Status { get; set; }

    public string StatusName { get; set; } = null!;

    public string? RefQuotation { get; set; }

    public string Priority { get; set; } = null!;

    public string? Data { get; set; }

    public string CustomerName { get; set; } = null!;

    public string CustomerCode { get; set; } = null!;

    public string? CustomerAddress { get; set; }

    public string? CustomerTel { get; set; }

    public string? CustomerEmail { get; set; }

    public string? CustomerRemark { get; set; }

    public string CurrencyUnit { get; set; } = null!;

    public decimal CurrencyRate { get; set; }

    public string? Remark { get; set; }

    public DateTime? SoDate { get; set; }

    public decimal? GoldRate { get; set; }

    public decimal? MarkUp { get; set; }
}
