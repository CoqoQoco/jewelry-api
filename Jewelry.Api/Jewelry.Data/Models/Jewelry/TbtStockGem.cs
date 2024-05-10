using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockGem
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public int? Daterec { get; set; }

    public string? Description { get; set; }

    public string? GroupName { get; set; }

    public string? SizeGem { get; set; }

    public string? Shape { get; set; }

    public string? Original { get; set; }

    public string? Grade { get; set; }

    public string? GradeDia { get; set; }

    public int? Price { get; set; }

    public int? PriceQty { get; set; }

    public string? Unit { get; set; }

    public string? UnitCode { get; set; }

    public string? Remark1 { get; set; }

    public int? Wg { get; set; }

    public int? Quantity { get; set; }

    public string? Remark2 { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }
}
