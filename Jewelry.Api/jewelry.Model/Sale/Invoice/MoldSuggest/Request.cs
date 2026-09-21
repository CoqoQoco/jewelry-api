namespace jewelry.Model.Sale.Invoice.MoldSuggest
{
    public class Request
    {
        public int Take { get; set; }
        public int Skip { get; set; }
        public SearchData? Search { get; set; }
    }

    public class SearchData
    {
        public string? Text { get; set; }
    }
}
