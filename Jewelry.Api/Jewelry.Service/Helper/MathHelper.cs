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
        // ปัดครึ่งขึ้นแบบ away-from-zero เป็นจำนวนเต็ม — ใช้กับยอดสุดท้ายของเอกสารขายทุกสกุลเงิน
        public static decimal RoundMoney(decimal value)
            => Math.Round(value, 0, MidpointRounding.AwayFromZero);

        // ปัดครึ่งขึ้นตามความละเอียดของสกุลเงิน: สกุลต่างประเทศ (ไม่ใช่ THB) ปัดเป็นจำนวนเต็ม, THB ปัด 2 ตำแหน่ง
        public static decimal RoundMoney(decimal value, string currencyUnit)
        {
            var decimalPosition = IsForeignCurrency(currencyUnit) ? 0 : 2;
            return Math.Round(value, decimalPosition, MidpointRounding.AwayFromZero);
        }

        public static bool IsForeignCurrency(string currencyUnit)
            => !string.Equals((currencyUnit ?? string.Empty).Trim(), "THB", StringComparison.OrdinalIgnoreCase);

        public static (decimal subTotal, decimal vatAmount, decimal raw, decimal rounded, decimal adjustment)
            ComputeTotals(decimal subTotal, decimal specialDiscount, decimal specialAddition, decimal freight, decimal vatPercent)
        {
            var afterSpecial = subTotal - specialDiscount + specialAddition + freight;
            var vatAmount = afterSpecial * (vatPercent / 100m);
            var raw = afterSpecial + vatAmount;
            var rounded = RoundMoney(raw);
            return (subTotal, vatAmount, raw, rounded, rounded - raw);
        }
    }
}
