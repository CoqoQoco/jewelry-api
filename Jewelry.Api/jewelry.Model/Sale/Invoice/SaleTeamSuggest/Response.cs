using System.Collections.Generic;

namespace jewelry.Model.Sale.Invoice.SaleTeamSuggest
{
    public class Response
    {
        public List<Item> SalePersons { get; set; } = new();
        public List<Item> SaleSupports { get; set; } = new();
        public List<Item> Owners { get; set; } = new();
    }

    public class Item
    {
        public string Name { get; set; } = null!;
        public int InvoiceCount { get; set; }
    }
}
