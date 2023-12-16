using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmCustomer
{
    public string Code { get; set; } = null!;

    public string NameTh { get; set; } = null!;

    public string? NameEn { get; set; }

    public string? Address { get; set; }

    public string TypeCode { get; set; } = null!;

    public string? Telephone1 { get; set; }

    public string? Telephone2 { get; set; }

    public string? ContactName { get; set; }

    public string? Email { get; set; }

    public string? Remark { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbmCustomerType TypeCodeNavigation { get; set; } = null!;
}
