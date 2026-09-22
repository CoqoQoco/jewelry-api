using System.Collections.Generic;

namespace jewelry.Model.Stock.StockProductGallery.MissingList;

public class Item
{
    public string Mold { get; set; } = null!;
    public int MissingPieceCount { get; set; }
    public int InStockPieceCount { get; set; }
    public string SampleStockNumber { get; set; } = null!;
    public string? SampleStockNumberOrigin { get; set; }
    public string ProductNameTh { get; set; } = null!;
    public string ProductNameEn { get; set; } = null!;
    public string? ProductType { get; set; }
    public string? ProductTypeName { get; set; }
    public string? InternalImagePath { get; set; }
}

public class Summary
{
    public int MoldCount { get; set; }
    public int MissingPieceCount { get; set; }
}

public class Response
{
    public List<Item> Data { get; set; } = new();
    public int Total { get; set; }
    public Summary Summary { get; set; } = new();
}
