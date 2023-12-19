using jewelry.Model.Exceptions;
using jewelry.Model.Worker;
using jewelry.Model.Worker.Create;
using jewelry.Model.Worker.List;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
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
        Task<string> Create(CreateProductionWorkerRequest request);
        IQueryable<ListWorkerProductionResponse> Search(ListWorkerProduction request);
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

        public async Task<string> Create(CreateProductionWorkerRequest request)
        {
            var dub = (from item in _jewelryContext.TbmWorker
                       where item.Code == request.Code.ToUpper()
                       select item).SingleOrDefault();

            if (dub != null)
            {
                throw new HandleException($"พบรหัสพนักงาน {request.Code} ซ้ำในระบบ กรุณาสร้างรหัสใหม่");
            }

            var add = new TbmWorker()
            {
                Code = request.Code.ToUpper(),
                NameEn = request.NameEn,
                NameTh = request.NameTh,
                TypeId = request.Type,
                IsActive = true,

                CreateBy = _admin,
                CreateDate = DateTime.UtcNow,
                UpdateBy = _admin,
                UpdateDate = DateTime.UtcNow,
            };

            _jewelryContext.TbmWorker.Add(add);
            await _jewelryContext.SaveChangesAsync();

            return $"{request.Code.ToUpper()}-{request.NameTh}";
        }
        public IQueryable<ListWorkerProductionResponse> Search(ListWorkerProduction request)
        {
            var query = (from item in _jewelryContext.TbmWorker
                         join type in _jewelryContext.TbmProductionPlanStatus on item.TypeId equals type.Id into typeJoind
                         from tj in typeJoind.DefaultIfEmpty()
                         select new ListWorkerProductionResponse()
                         {
                             Code = item.Code,
                             NameEn = item.NameEn,
                             NameTh = item.NameTh,

                             Type = item.TypeId,
                             TypeName = tj.Description,
                             IsActive = item.IsActive,

                             CreateDate = item.CreateDate,
                             CreateBy = item.CreateBy,
                             UpdateDate = item.UpdateDate,
                             UpdateBy = item.UpdateBy,
                         });

            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query
                         where item.Code.Contains(request.Text.ToUpper())
                         || item.NameTh.Contains(request.Text)
                         || item.NameEn.Contains(request.Text)
                         select item);
            }
            if (request.Type.HasValue)
            {
                query = (from item in query
                         where item.Type == request.Type
                         select item);
            }
            if (request.Active.HasValue)
            {
                if (request.Active == 1)
                {
                    query = (from item in query
                             where item.IsActive == true
                             select item);
                }
                if (request.Active == 2)
                {
                    query = (from item in query
                             where item.IsActive == false
                             select item);
                }
            }

            return query.OrderByDescending(x => x.UpdateDate);
        }

    }
}
