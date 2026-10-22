using Jewelry.Data.Context;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.Report.Executive;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Overview = jewelry.Model.Sale.Insight.Overview;

namespace Jewelry.Service.Sale.Insight
{
    public class SalesInsightService : BaseService, ISalesInsightService
    {
        private readonly JewelryContext _jewelryContext;

        public SalesInsightService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public async Task<Overview.Response> Overview(Overview.Request request)
        {
            var metal = MetalClassifier.Normalize(request.Metal);
            var months = SalesInsightThresholds.ClampMonths(request.Months);
            var thresholds = SalesInsightThresholds.Resolve(request.Thresholds);
            var now = DateTime.UtcNow;

            // loader เดียวกับ ExecutiveReport.Summary — ตัวเลขเงินตรงกันโดยโครงสร้าง
            var invoiceRows = await ExecutiveSalesLoader.GetInvoiceMoneyRowsAsync(_jewelryContext, metal);
            var soRows = await ExecutiveSalesLoader.GetSalesOrderMoneyRowsAsync(_jewelryContext, metal);

            return SalesInsightCalculator.Build(metal, months, thresholds, now, invoiceRows, soRows);
        }
    }
}
