using System.ComponentModel.DataAnnotations;

namespace jewelry.Model.Sale.Invoice.CancelAndUnconfirm
{
    public class Request
    {
        [Required]
        public string InvoiceNumber { get; set; } = null!;
    }
}
