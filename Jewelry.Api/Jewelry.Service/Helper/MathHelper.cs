using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Helper
{
    public static class MathHelper
    {
        public static decimal RoundDecimal(decimal value, int decimalPosition)
        {
            return Math.Round(value, decimalPosition);
        }
        public static decimal SafeDivide(decimal numerator, decimal denominator, int decimalPosition)
        {
            return denominator == 0 ? 0 : RoundDecimal(numerator / denominator, decimalPosition);
        }
        public static decimal CalculatePercentage(decimal part, decimal whole, int decimalPosition)
        {
            return SafeDivide(part * 100, whole, decimalPosition);
        }
        public static decimal GetPositiveValue(decimal value)
        {
            return Math.Max(0, value);
        }
        public static decimal CalculateRate(decimal amount, decimal percent, int decimalPosition)
        {
            return RoundDecimal(amount * (percent / 100), decimalPosition);
        }

        // เกณฑ์ปัดเศษเงินของเอกสารขายต้องตรงกับฝั่ง UI เสมอ (jeweley-ui: src/services/utils/money.js)
        // ไม่ปัดเศษระหว่างทาง ปัดครั้งเดียวที่ยอดสุดท้ายเป็นทศนิยม 2 ตำแหน่งแบบ away-from-zero — ใช้เกณฑ์เดียวกันทั้งสกุลต่างประเทศและ THB
        public static decimal RoundMoney(decimal value)
            => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        // ปัดครึ่งขึ้นเป็นทศนิยม 2 ตำแหน่งทุกสกุลเงิน — เก็บ signature ไว้เพื่อ compat กับจุดเรียกเดิม
        public static decimal RoundMoney(decimal value, string currencyUnit)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        public static bool IsForeignCurrency(string currencyUnit)
            => !string.Equals((currencyUnit ?? string.Empty).Trim(), "THB", StringComparison.OrdinalIgnoreCase);

        public static decimal Round2(decimal value)
            => Math.Round(Math.Round(value, 6, MidpointRounding.AwayFromZero), 2, MidpointRounding.AwayFromZero);

        // invoice THB ที่ราคารวม VAT แล้ว: ถอด VAT ทีละแถว ปัดครึ่งขึ้น 2 ตำแหน่ง (ต้องตรงกับ money.js ฝั่ง UI)
        public static (decimal sub, decimal discEx, decimal addEx, decimal freightEx, decimal vat, decimal grand)
            ComputeInvoiceTotalsVatIncluded(IEnumerable<(decimal netInclVat, decimal qty)> rows,
                decimal specialDiscount, decimal specialAddition, decimal freight, decimal vatPercent = 7m)
        {
            var divisor = 1m + vatPercent / 100m;
            var sub = rows.Sum(r => Round2(Round2(r.netInclVat / divisor) * r.qty));
            var discEx = Round2(specialDiscount / divisor);
            var addEx = Round2(specialAddition / divisor);
            var freightEx = Round2(freight / divisor);
            var after = sub - discEx + addEx + freightEx;
            var vat = Round2(after * (vatPercent / 100m));
            return (sub, discEx, addEx, freightEx, vat, after + vat);
        }

        public static (decimal subTotal, decimal vatAmount, decimal raw, decimal rounded, decimal adjustment)
            ComputeTotals(decimal subTotal, decimal specialDiscount, decimal specialAddition, decimal freight, decimal vatPercent)
        {
            var afterSpecial = subTotal - specialDiscount + specialAddition + freight;
            var vatAmount = afterSpecial * (vatPercent / 100m);
            var raw = afterSpecial + vatAmount;
            var rounded = RoundMoney(raw);
            return (subTotal, vatAmount, raw, rounded, 0m);
        }
    }
}
