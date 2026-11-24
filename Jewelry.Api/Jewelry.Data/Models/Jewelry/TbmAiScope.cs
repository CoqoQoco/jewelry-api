using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmAiScope
{
    public string Code { get; set; } = null!;
    public string NameTh { get; set; } = null!;
    public string? DescriptionTh { get; set; }
    public string Kind { get; set; } = null!;
    public bool AllowPat { get; set; }
    public bool IsActive { get; set; }
    public int Sort { get; set; }
}
