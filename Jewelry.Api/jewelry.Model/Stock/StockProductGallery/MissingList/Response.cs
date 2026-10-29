using System.Collections.Generic;

namespace jewelry.Model.Stock.StockProductGallery.MissingList;

public class Item
{
    public string Mold { get; set; } = null!;
    // นับเป็นชิ้น (Σ qty ของล็อตที่ไม่ SOLD รวมที่จอง) ไม่ใช่จำนวนล็อต
    public int MissingPieceCount { get; set; }
    public int InStockPieceCount { get; set; }
    // จำนวนล็อต (แถว tbt_stock_piece) ของค่าสองตัวบน
    public int MissingLotCount { get; set; }
    public int InStockLotCount { get; set; }
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
    public int MissingLotCount { get; set; }
}

public class Response
{
    public List<Item> Data { get; set; } = new();
    public int Total { get; set; }
    public Summary Summary { get; set; } = new();
}
