using System.Collections.Generic;
using System.Threading.Tasks;
using ByChannel = jewelry.Model.Sale.SaleReport.ByChannel;
using SalesSummary = jewelry.Model.Sale.SaleReport.SalesSummary;
using ProductGroupSales = jewelry.Model.Sale.SaleReport.ProductGroupSales;
using TopDesignSales = jewelry.Model.Sale.SaleReport.TopDesignSales;
using InvoiceCustomerSuggest = jewelry.Model.Sale.SaleReport.InvoiceCustomerSuggest;

namespace Jewelry.Service.Sale.SaleReport
{
    public interface ISaleReportService
    {
        Task<List<jewelry.Model.Sale.SaleReport.CustomerProductionStatus.Response>> CustomerProductionStatus(jewelry.Model.Sale.SaleReport.CustomerProductionStatus.Request request);
        Task<ByChannel.Response> ByChannel(ByChannel.Request request);
        Task<SalesSummary.Response> SalesSummary(SalesSummary.Request request);
        Task<ProductGroupSales.Response> ProductGroupSales(ProductGroupSales.Request request);
        Task<TopDesignSales.Response> TopDesignSales(TopDesignSales.Request request);
        Task<List<InvoiceCustomerSuggest.Response>> InvoiceCustomerSuggest(InvoiceCustomerSuggest.Request request);
    }
}
