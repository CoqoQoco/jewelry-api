using Jewelry.Service.Stock.ProductImage;
using Xunit;

namespace Jewelry.Service.Tests.Stock.ProductImage
{
    public class ProductImageStemTests
    {
        [Theory]
        [InlineData("NF706SD7YL-1", "NF706SD7YL")]
        [InlineData("NF706SD7YL2", "NF706SD7YL")]
        [InlineData("NF706SD7WE1", "NF706SD7WE")]
        [InlineData("nf706sd7yl-12", "NF706SD7YL")]
        [InlineData("P01150CC9L", "P01150CC9L")]
        [InlineData("12345", "12345")]
        [InlineData("  ", "")]
        [InlineData(null, "")]
        public void Compute_StripsTrailingNumberOnlyAfterLetter(string? input, string expected)
        {
            Assert.Equal(expected, ProductImageStem.Compute(input));
        }
    }
}
