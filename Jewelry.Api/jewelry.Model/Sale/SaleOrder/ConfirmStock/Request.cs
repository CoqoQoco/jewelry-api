using System.ComponentModel.DataAnnotations;

namespace jewelry.Model.Sale.SaleOrder.ConfirmStock
{
    public class Request
    {
        [Required]
        public string SoNumber { get; set; } = null!;

        [Required]
        public List<StockItemConfirmation> StockItems { get; set; } = new List<StockItemConfirmation>();
    }

    public class StockItemConfirmation
    {
        
        [Required]
        public string StockNumber { get; set; } = null!;
        public string? ProductNumber { get; set; }
        
        [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Qty { get; set; }
        [Range(0.001, double.MaxValue, ErrorMessage = "Appraisal price must be greater than 0")]
        public decimal AppraisalPrice { get; set; }
        
    }
}