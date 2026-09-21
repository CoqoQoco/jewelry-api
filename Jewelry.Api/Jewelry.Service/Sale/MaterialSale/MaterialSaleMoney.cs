using Jewelry.Service.Helper;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Sale.MaterialSale
{
    // สูตรเงินของใบสั่งขายวัตถุดิบ — ไม่ปัดเศษราคาต่อหน่วย/จำนวนเงินต่อบรรทัดระหว่างทาง
    // ปัดครั้งเดียวที่ยอดสุดท้ายผ่าน MathHelper.ComputeTotals (ต้องได้ยอด = Σ ราคารวม Vat × กะรัต)
    public static class MaterialSaleMoney
    {
        public static decimal PriceExclVat(decimal priceInclVat, decimal vatPercent)
            => priceInclVat / (1m + vatPercent / 100m);

        public static decimal LineAmount(decimal priceInclVat, decimal qtyWeight, decimal vatPercent)
            => PriceExclVat(priceInclVat, vatPercent) * qtyWeight;

        public static (decimal subTotal, decimal vatAmount, decimal raw, decimal rounded, decimal adjustment)
            Totals(IEnumerable<(decimal PriceInclVat, decimal QtyWeight)> items, decimal vatPercent)
        {
            var subTotal = items.Sum(x => LineAmount(x.PriceInclVat, x.QtyWeight, vatPercent));
            return MathHelper.ComputeTotals(subTotal, 0, 0, 0, vatPercent);
        }
    }
}
