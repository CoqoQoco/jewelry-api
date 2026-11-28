using Jewelry.Service.Helper;

namespace Jewelry.Service.Tests.Sale;

public class InvoiceVatIncludedTotalsTests
{
    private static IEnumerable<(decimal, decimal)> Rows(params decimal[] prices) => prices.Select(p => (p, 1m));

    [Fact]
    public void Inv261118022_TwentyOneRows()
    {
        var r = MathHelper.ComputeInvoiceTotalsVatIncluded(
            Rows(63400m, 35040m, 47840m, 47920m, 7920m, 16480m, 9360m, 16000m, 15880m, 10480m, 8720m,
                10280m, 15840m, 9520m, 8200m, 7480m, 7360m, 7560m, 10720m, 8720m, 6800m),
            0m, 0m, 0m);

        Assert.Equal(347214.94m, r.sub);
        Assert.Equal(24305.05m, r.vat);
        Assert.Equal(371519.99m, r.grand);
    }

    [Fact]
    public void ThreeRows_1424_3008_1616()
    {
        var r = MathHelper.ComputeInvoiceTotalsVatIncluded(Rows(1424m, 3008m, 1616m), 0m, 0m, 0m);

        Assert.Equal(5652.33m, r.sub);
        Assert.Equal(395.66m, r.vat);
        Assert.Equal(6047.99m, r.grand);
    }

    [Fact]
    public void SpecialDiscountIsVatInclusive()
    {
        var r = MathHelper.ComputeInvoiceTotalsVatIncluded(Rows(50010.24m), 10.24m, 0m, 0m);

        Assert.Equal(46738.54m, r.sub);
        Assert.Equal(9.57m, r.discEx);
        Assert.Equal(3271.03m, r.vat);
        Assert.Equal(50000.00m, r.grand);
    }
}
