using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Sale.BillingNote;

namespace Jewelry.Service.Tests.Sale;

public class BillingTotalsTests
{
    private static TbtSaleInvoiceHeader Included(decimal grand, decimal vat) => new()
    {
        VatIncluded = true,
        GrandTotalRounded = grand,
        VatAmount = vat,
        SubTotal = grand - vat + 5m
    };

    private static TbtSaleInvoiceHeader Old(decimal sub) => new() { VatIncluded = false, SubTotal = sub };

    [Fact]
    public void AmountBeforeVat_IncludedUsesGrandMinusVat_OldUsesSubTotal()
    {
        Assert.Equal(9.35m, BillingNoteService.InvoiceAmountBeforeVat(Included(10m, 0.65m)));
        Assert.Equal(200m, BillingNoteService.InvoiceAmountBeforeVat(Old(200m)));
    }

    [Fact]
    public void AllVatIncluded_SumsVatPerInvoice()
    {
        var r = BillingNoteService.BillingTotals(new[] { Included(10m, 0.65m), Included(10m, 0.65m) }, 7m);

        Assert.Equal(18.70m, r.subTotal);
        Assert.Equal(1.30m, r.vatAmount);
        Assert.Equal(20.00m, r.grandTotal);
    }

    [Fact]
    public void Mixed_IncludedAndOldInvoice()
    {
        var r = BillingNoteService.BillingTotals(new[] { Included(107m, 7m), Old(200m) }, 7m);

        Assert.Equal(300m, r.subTotal);
        Assert.Equal(21m, r.vatAmount);
        Assert.Equal(321m, r.grandTotal);
    }

    [Fact]
    public void VatPercentZero_IncludedInvoiceKeepsItsOwnVat()
    {
        var r = BillingNoteService.BillingTotals(new[] { Included(107m, 7m), Old(200m) }, 0m);

        Assert.Equal(300m, r.subTotal);
        Assert.Equal(7m, r.vatAmount);
        Assert.Equal(307m, r.grandTotal);
    }

    private static TbtSaleInvoiceHeader Named(TbtSaleInvoiceHeader h, string running) { h.Running = running; return h; }

    private static TbtSaleBillingNoteItem Item(string running, decimal amount) => new() { InvoiceRunning = running, AmountBeforeVat = amount };

    [Fact]
    public void BuiltUnderNewRule_OldNoteWithBackfilledInvoice_IsFalse()
    {
        var inv = Named(Included(10m, 0.65m), "I1");
        Assert.False(BillingNoteService.BuiltUnderNewRule(new[] { inv }, new[] { Item("I1", 14.02m) }));
    }

    [Fact]
    public void BuiltUnderNewRule_NewNote_IsTrue()
    {
        var inv = Named(Included(10m, 0.65m), "I1");
        Assert.True(BillingNoteService.BuiltUnderNewRule(new[] { inv }, new[] { Item("I1", 9.35m) }));
    }

    [Fact]
    public void BuiltUnderNewRule_NoVatIncludedInvoice_IsFalse()
    {
        var inv = Named(Old(200m), "I1");
        Assert.False(BillingNoteService.BuiltUnderNewRule(new[] { inv }, new[] { Item("I1", 200m) }));
    }

    [Fact]
    public void BuiltUnderNewRule_MixedNote()
    {
        var inc = Named(Included(107m, 7m), "I1");
        var old = Named(Old(200m), "I2");
        Assert.True(BillingNoteService.BuiltUnderNewRule(new[] { inc, old }, new[] { Item("I1", 100m), Item("I2", 200m) }));
        Assert.False(BillingNoteService.BuiltUnderNewRule(new[] { inc, old }, new[] { Item("I1", 105m), Item("I2", 200m) }));
    }
}
