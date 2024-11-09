using jewelry.Model.User.PasswordHash;
using Jewelry.Data.Context;
using Jewelry.Service.Customer;
using Jewelry.Service.Helper;
using Jewelry.Service.Master;
using Jewelry.Service.Mold;
using Jewelry.Service.Production.Plan;
using Jewelry.Service.ProductionPlan;
using Jewelry.Service.Receipt.Gem;
using Jewelry.Service.Stock;
using Jewelry.Service.Stock.PlanReceipt;
using Jewelry.Service.Stock.Product;
using Jewelry.Service.User;
using Jewelry.Service.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Api.Extension
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var SQLServerConnectionString = configuration["ConnectionStrings:DefaultConnection"];

            if (string.IsNullOrEmpty(SQLServerConnectionString))
                throw new ArgumentNullException(nameof(SQLServerConnectionString));

            //Register DB
            services.AddDbContext<JewelryContext>(options =>
                options.UseNpgsql(SQLServerConnectionString));

            //Register Service
            services.AddScoped<IReadExcelProduct, ReadExcelProduct>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProductionPlanService, ProductionPlanService>();
            services.AddScoped<IFileExtension, FileExtension>();
            services.AddScoped<IMasterService, MasterService>();
            services.AddScoped<IMoldService, MoldService>();
            services.AddScoped<IRunningNumber, RunningNumber>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IWorkerService, WorkerService>();
            services.AddScoped<IProductionPlanCostService, ProductionPlanCostService>();
            services.AddScoped<IStockGemService, StockGemService>();
            services.AddScoped<IPasswordHash, PasswordHashService>();
            services.AddScoped<IMoldPlanService, MoldPlanService>();
            services.AddScoped<IStockMoldService, StockMoldService>();
            services.AddScoped<IReceiptAndIssueStockGemService, ReceiptAndIssueStockGemService>();

            services.AddScoped<IPlanService, PlanService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IPlanReceiptService, PlanReceiptService>();

            return services;
        }
    }
}
