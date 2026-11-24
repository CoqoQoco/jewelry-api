using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmAiGlossary
{
    public string MetricCode { get; set; } = null!;
    public string Module { get; set; } = null!;
    public string NameTh { get; set; } = null!;
    public string DefinitionTh { get; set; } = null!;
    public string? CaveatTh { get; set; }
    public bool IsActive { get; set; }
    public DateTime? UpdateDate { get; set; }
}
