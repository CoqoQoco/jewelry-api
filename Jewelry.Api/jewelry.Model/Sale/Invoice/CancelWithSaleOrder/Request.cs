using System.ComponentModel.DataAnnotations;

namespace jewelry.Model.Sale.Invoice.CancelWithSaleOrder
{
    public class Request
    {
        [Required]
        public string InvoiceNumber { get; set; } = null!;
    }
}
