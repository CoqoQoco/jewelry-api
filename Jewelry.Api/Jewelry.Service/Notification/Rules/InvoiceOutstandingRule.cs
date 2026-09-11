using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Notification.Rules;

public class InvoiceOutstandingRule : INotificationRule
{
    private readonly JewelryContext _jewelryContext;

    public string TypeCode => "INVOICE_OUTSTANDING";

    public InvoiceOutstandingRule(JewelryContext jewelryContext)
    {
        _jewelryContext = jewelryContext;
    }

    public async Task<IReadOnlyList<NotificationCandidate>> Evaluate()
    {
        var query = from invoice in _jewelryContext.TbtSaleInvoiceHeader.AsNoTracking()
                     where invoice.IsDelete == false
                        && invoice.GrandTotalRounded != null
                        && invoice.GrandTotalRounded > 0
                     select new
                     {
                         invoice.Running,
                         invoice.CustomerName,
                         invoice.GrandTotalRounded,
                         invoice.Deposit,
                         invoice.CurrencyUnit,
                         invoice.DueDate,
                         invoice.CreateDate,
                         invoice.SalePersonUsername,
                         invoice.CreateBy,
                         PaidAmount = _jewelryContext.TbtSaleInvoicePaymentItem
                             .Where(p => p.InvoiceRunning == invoice.Running && p.IsDelete == false)
                             .Sum(p => p.Amount)
                     };

        var invoices = await query.ToListAsync();

        var candidates = new List<NotificationCandidate>();

        foreach (var invoice in invoices)
        {
            var outstanding = invoice.GrandTotalRounded!.Value - invoice.Deposit - invoice.PaidAmount;
            if (outstanding <= 0.005m)
                continue;

            var recipient = !string.IsNullOrEmpty(invoice.SalePersonUsername)
                ? invoice.SalePersonUsername
                : invoice.CreateBy;

            if (string.IsNullOrEmpty(recipient))
                continue;

            candidates.Add(new NotificationCandidate
            {
                RecipientUsername = recipient,
                RefDocType = "INVOICE",
                RefDocNo = invoice.Running,
                Title = $"{invoice.Running} · {invoice.CustomerName}",
                Body = $"ยอดคงเหลือ {outstanding:N2} {invoice.CurrencyUnit}",
                Amount = outstanding,
                CurrencyUnit = invoice.CurrencyUnit,
                DueDate = invoice.DueDate ?? invoice.CreateDate,
                ActionUrl = $"/invoice-detail?invoiceNumber={invoice.Running}"
            });
        }

        return candidates;
    }
}
