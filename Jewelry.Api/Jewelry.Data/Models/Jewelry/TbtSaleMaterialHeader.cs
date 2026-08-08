using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleMaterialHeader
{
    public string Running { get; set; } = null!;

    public string DocumentNo { get; set; } = null!;

    public DateTime DocumentDate { get; set; }

    public string? CustomerCode { get; set; }

    public string CustomerName { get; set; } = null!;

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

    public bool IsDelete { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual ICollection<TbtSaleMaterialItem> TbtSaleMaterialItem { get; set; } = new List<TbtSaleMaterialItem>();
}
