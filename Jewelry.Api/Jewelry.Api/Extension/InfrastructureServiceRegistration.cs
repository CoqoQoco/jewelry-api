using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using Jewelry.Service.ProductionPlan;
using Jewelry.Service.User;
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

            return services;
        }
    }
}
