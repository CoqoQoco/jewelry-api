namespace jewelry.Model.Stock.Product.Image.BulkPreview
{
    public class Response
    {
        public string StockNumber { get; set; }
        public string? ProductCode { get; set; }
        public string? Mold { get; set; }
        public string? ReceiptNumber { get; set; }
        public bool Found { get; set; }
        public bool HasImage { get; set; }
        public string? ImagePath { get; set; }
    }
}
