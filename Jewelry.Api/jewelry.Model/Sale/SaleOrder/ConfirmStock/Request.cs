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
        public decimal Discount { get; set; }
        public string? LineKey { get; set; }

        public bool IsPlaceholder { get; set; }

        // ระบุชิ้นที่จอง (tbt_stock_unit.id) — ว่าง = ให้ระบบเลือก · จำนวนต้องเท่ากับ Qty
        public List<long>? UnitIds { get; set; }

        // จองจากคลังนี้เท่านั้น (ถ้าระบุ UnitIds ทุกชิ้นต้องอยู่คลังนี้) — ว่าง = คลังหลักก่อน แล้วคลังที่มีมากสุด
        public string? LocationCode { get; set; }

    }
}