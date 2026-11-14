using System.Collections.Generic;

namespace jewelry.Model.Receipt.Lot.SuggestByProduct
{
    public class Response
    {
        public string ProductNumber { get; set; } = null!;
        public List<jewelry.Model.Receipt.Lot.Search.Response> Lots { get; set; } = new();
    }
}
