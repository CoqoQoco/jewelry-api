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
    }
}
