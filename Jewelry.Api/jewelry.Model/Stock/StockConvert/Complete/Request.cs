namespace jewelry.Model.Stock.StockConvert.Complete;

public class Request
{
    public string Running { get; set; }
    public string? LocationCode { get; set; }
    public decimal ConvertCost { get; set; }
    public ResultRequest Result { get; set; }
}

public class ResultRequest
{
    public string ProductNumber { get; set; }
    public string ProductNameEn { get; set; }
    public string ProductNameTh { get; set; }
    public string? ProductType { get; set; }
    public string? ProductionType { get; set; }
    public string? ProductionTypeSize { get; set; }
    public string? Size { get; set; }
    public string? Mold { get; set; }
    public string? MoldDesign { get; set; }
    public string? ImagePath { get; set; }
    public decimal? DefaultPrice { get; set; }
    public List<MaterialRequest> Materials { get; set; } = new();
}

public class MaterialRequest
{
    public string Type { get; set; }
    public string? TypeName { get; set; }
    public string? TypeCode { get; set; }
    public string? TypeOrigin { get; set; }
    public string? Size { get; set; }
    public decimal? Qty { get; set; }
    public string? QtyUnit { get; set; }
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public string? Region { get; set; }
    public decimal? Price { get; set; }
}
