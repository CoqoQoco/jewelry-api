using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockGem
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int? Daterec { get; set; }

    public string GroupName { get; set; } = null!;

    public string? Size { get; set; }

    public string Shape { get; set; } = null!;

    public string? Original { get; set; }

    public string Grade { get; set; } = null!;

    public string? GradeDia { get; set; }

    public decimal Price { get; set; }

    public decimal PriceQty { get; set; }

    public string? Unit { get; set; }

    public string? UnitCode { get; set; }

    public string? Remark1 { get; set; }

    public int? Wg { get; set; }

    public decimal Quantity { get; set; }

    public string? Remark2 { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string? GradeCode { get; set; }

    public virtual TbmGoldSize? GradeCodeNavigation { get; set; }

    public virtual TbmGemShape ShapeNavigation { get; set; } = null!;
}
