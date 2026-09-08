using System.ComponentModel.DataAnnotations;

namespace jewelry.Model.PublicProduct.Link
{
    public class Request
    {
        [Required]
        public string StockNumber { get; set; }
    }
}
