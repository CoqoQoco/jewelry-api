namespace jewelry.Model.Stock.Location
{
    public class UpdateStockLocationRequest
    {
        public string Code { get; set; } = null!;
        public string? NameTh { get; set; }
        public string? NameEn { get; set; }
        public string? Type { get; set; }
        public string? ParentCode { get; set; }
        public bool? IsSalesPoint { get; set; }
        public bool? IsTemporary { get; set; }
        public bool? IsActive { get; set; }
        public int? SortOrder { get; set; }
    }
}
