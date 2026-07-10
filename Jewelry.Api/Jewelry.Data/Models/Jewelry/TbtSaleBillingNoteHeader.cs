using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleBillingNoteHeader
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

    public bool IsDelete { get; set; }

    public string? DeleteReason { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public virtual ICollection<TbtSaleBillingNoteItem> TbtSaleBillingNoteItem { get; set; } = new List<TbtSaleBillingNoteItem>();

    public virtual ICollection<TbtSaleBillingNoteProduct> TbtSaleBillingNoteProduct { get; set; } = new List<TbtSaleBillingNoteProduct>();
}
