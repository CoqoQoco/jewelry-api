namespace jewelry.Model.Catalog.RemoveProduct
{
    public class Request
    {
        public int CatalogId { get; set; }
        public long? ItemId { get; set; }
        public string? ProductNumber { get; set; }
    }
}
