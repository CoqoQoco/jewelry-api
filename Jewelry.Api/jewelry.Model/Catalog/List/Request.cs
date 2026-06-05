namespace jewelry.Model.Catalog.List
{
    public class Request
    {
        public int Take { get; set; } = 20;
        public int Skip { get; set; } = 0;
        public string? SortField { get; set; }
        public string? SortDir { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
    }
}
