using System.ComponentModel.DataAnnotations;

namespace jewelry.Model.Sale.SaleOrder.UnconfirmStock
{
    public class Request
    {
        [Required]
        public string SoNumber { get; set; } = null!;

        [Required]
        public List<StockItemUnconfirmation> StockItems { get; set; } = new List<StockItemUnconfirmation>();
    }

    public class StockItemUnconfirmation
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string StockNumber { get; set; } = null!;
    }
}
