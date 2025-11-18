using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class Stock9k
{
    public int Id { get; set; }

    public string? NoProduct { get; set; }

    public string? StyleNo { get; set; }

    public int? Qty { get; set; }

    public bool IsTransfer { get; set; }
}
