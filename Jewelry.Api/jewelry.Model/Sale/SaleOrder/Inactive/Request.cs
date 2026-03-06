using System.ComponentModel.DataAnnotations;

namespace jewelry.Model.Sale.SaleOrder.Inactive
{
    public class Request
    {
        [Required]
        public string SoNumber { get; set; } = null!;
    }
}
