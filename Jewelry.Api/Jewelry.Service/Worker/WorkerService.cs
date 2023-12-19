using jewelry.Model.Worker;
using Jewelry.Data.Context;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Worker
{
    public interface IWorkerService 
    {
        IQueryable<MasterWorkerProductionTypeResponse> GetWorkerProductionType();
    }
    public class WorkerService : IWorkerService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        public WorkerService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
        }

        public IQueryable<MasterWorkerProductionTypeResponse> GetWorkerProductionType() 
        {
            var getId = new int[] { 50, 60, 80, 90 };
            var query = (from item in _jewelryContext.TbmProductionPlanStatus
                         where getId.Contains(item.Id)
                         select new MasterWorkerProductionTypeResponse()
                         { 
                             Id = item.Id,
                             NameEn = item.NameEn,
                             NameTh = item.NameTh,
                             Description = item.Description ?? null
                         });

            return query;
        }

    }
}
