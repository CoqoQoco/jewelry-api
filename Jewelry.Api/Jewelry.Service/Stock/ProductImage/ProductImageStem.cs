using System.Text.RegularExpressions;

namespace Jewelry.Service.Stock.ProductImage
{
    public static class ProductImageStem
    {
        private static readonly Regex TrailingNumber = new Regex(@"(?<=[A-Z])-?\d+$", RegexOptions.Compiled);

        public static string Compute(string? productNumber)
        {
            var value = (productNumber ?? string.Empty).Trim().ToUpperInvariant();
            if (value.Length == 0) return value;
            var stem = TrailingNumber.Replace(value, string.Empty);
            return stem.Length == 0 ? value : stem;
        }
    }
}
