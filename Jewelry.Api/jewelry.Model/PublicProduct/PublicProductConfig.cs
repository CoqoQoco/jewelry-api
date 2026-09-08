namespace jewelry.Model.PublicProduct
{
    public class PublicProductConfig
    {
        public bool Enabled { get; set; } = false;
        public string? TokenSecret { get; set; }
        public bool ShowPrice { get; set; } = true;
        public bool ShowGemOrigin { get; set; } = true;
        public bool ShowAvailability { get; set; } = false;
        public int CacheSeconds { get; set; } = 60;
    }
}
