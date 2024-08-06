using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtMoldPickingList
{
    public int Id { get; set; }

    public string Running { get; set; } = null!;

    public string MoldCode { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime CheckOutDate { get; set; }

    public string CheckOutName { get; set; } = null!;

    public DateTime ReturnDate { get; set; }

    public string? ReturnName { get; set; }

    public string? CheckOutDescription { get; set; }

    public string? ReturnDescription { get; set; }

    public virtual TbtProductMold MoldCodeNavigation { get; set; } = null!;
}
