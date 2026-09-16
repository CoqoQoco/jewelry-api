using System.ComponentModel.DataAnnotations;

namespace jewelry.Model.Sale.SaleOrder.ReplaceConfirmedStock
{
    public class Request
    {
        [Required]
        public string SoNumber { get; set; } = null!;

        [Required]
        public long SaleOrderProductId { get; set; }

        [Required]
        public string NewStockNumber { get; set; } = null!;

        public decimal? Qty { get; set; }
    }
}
