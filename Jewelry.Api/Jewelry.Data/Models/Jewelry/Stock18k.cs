using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class Stock18k
{
    public string? NoProduct { get; set; }

    public string? StyleNo { get; set; }

    public int? Qty { get; set; }

    public bool IsTransfer { get; set; }

    public int Id { get; set; }
}
