using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtStockGemTransectionPrice
{
    public int Id { get; set; }

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string? Remark { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public string Code { get; set; } = null!;

    public decimal PreviousPrice { get; set; }

    public decimal NewPrice { get; set; }

    public decimal PreviousPriceUnit { get; set; }

    public decimal NewPriceUnit { get; set; }

    public string Unit { get; set; } = null!;

    public string? UnitCode { get; set; }
}
